using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Configuration.Authentication;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VPSCoverage;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _AuthoringUtilities
  {
    // Will return null if the API key is not set.
    private static HttpClient CreateVpsHttpClient()
    {
      var apiKey = ArdkGlobalConfig._Internal.GetApiKey();
      if (string.IsNullOrEmpty(apiKey))
      {
        ARLog._Error($"An API key must be set in order to use Lightship VPS.");
        return null;
      }

      HttpClient client = new HttpClient();
      client.BaseAddress = new Uri("https://vps-frontend.nianticlabs.com/web/vps_frontend.protogen.Localizer/");
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiKey);

      return client;
    }

    private static _VpsDefinitions.Localization[] BuildIdentityLocalization(string nodeId)
    {
      // Mock an identity localization to the node id passed in to the function.
      // We do this because we already know the offset between node and virtual object.
      // So the local tracking system does not matter and can be eliminated from the equation by making it Identity
      _VpsDefinitions.Transform serLocalizationTransform = new _VpsDefinitions.Transform(Matrix4x4.identity);
      _VpsDefinitions.Localization[] serLocalizations = new _VpsDefinitions.Localization[1];
      serLocalizations[0] = new _VpsDefinitions.Localization(nodeId, 0.7F, serLocalizationTransform);

      return serLocalizations;
    }

    private static _VpsDefinitions.CreationInput[] BuildCreationInputs(Matrix4x4 pose)
    {
      // Convert from unity coordinates to narwhal coordinates
      var narPose = NARConversions.FromUnityToNAR(pose);

      // Serialize transform and set it in the API as the requested pose for the wayspot anchor
      _VpsDefinitions.Transform serManagedPoseTransform = new _VpsDefinitions.Transform(narPose);
      _VpsDefinitions.CreationInput[] serCreationInputs = new _VpsDefinitions.CreationInput[1];
      serCreationInputs[0] = new _VpsDefinitions.CreationInput(Guid.NewGuid().ToString(), serManagedPoseTransform);

      return serCreationInputs;
    }

    private static string _GetMetadataAsString()
    {
      return null;
      // TODO(AR-14998): Enable this code once it is safe to call from any thread.
      // var metadata = ArdkGlobalConfig._Internal.GetCommonDataEnvelopeWithRequestIdAsStruct();
      // return JsonUtility.ToJson(metadata, true);
    }

    private static HttpRequestMessage BuildVpsRequest(string requestUri, string contentJson)
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);
      request.Content = new StringContent
      (
        contentJson,
        Encoding.UTF8,
        "application/json"
      );

      return request;
    }

    private static Task<HttpResponseMessage> SendVpsRequest(HttpRequestMessage request)
    {
      HttpClient client = CreateVpsHttpClient();
      if (client != null)
      {
        return client.SendAsync(request);
      }

      return null;
    }

    // Calls a VPS API (described in the request) and returns an anchor payload, or null if an error occurrs.
    private static async Task<string> SendCreateAnchorRequest(HttpRequestMessage request)
    {
      var response = await SendVpsRequest(request);

      if (response == null)
      {
        return null;
      }
      
      // Check success
      if (!response.IsSuccessStatusCode)
      {
        ARLog._Error($"Request to create WayspotAnchor failed with HTTP error code {response.StatusCode}.");
        return null;
      }

      // Get JSON response
      string content = await response.Content.ReadAsStringAsync();
      var createResponse = JsonUtility.FromJson<_VpsDefinitions.CreateManagedPosesResponse>(content);

      // Code below assumes only a single anchor was created, which is true above
      // So we access the first element in the response array to get the anchor blob

      // Check status of anchors
      // TODO (kcho): what is the overall status vs each anchor's status?
      var status = _ResponseStatusTranslator.FromString(createResponse.statusCode);
      if (status != _VpsDefinitions.StatusCode.STATUS_CODE_SUCCESS)
      {
        ARLog._Error($"Request to create WayspotAnchor failed due to {status}.");
        return null;
      }

      // Save B64 encoded anchor
      string managedPoseB64 = createResponse.creations[0].managedPose.data;
      return managedPoseB64;
    }
    
    // TODO (kcho): Send multiple poses in single create request to reduce latency when creating
    // multiple anchors
    // @param pose Transform from the node origin to the pose
    // @param nodeId
    // @returns (anchorIdentifier, anchorPayload)
    public static async Task<(string, string)> CreateRelativeToNode(Matrix4x4 pose, string nodeId)
    {
      // Create the request
      var anchorIdentifier = Guid.NewGuid().ToString();
      var localizations = BuildIdentityLocalization(nodeId);
      var creationInputs = BuildCreationInputs(pose);
      var metadata = _GetMetadataAsString();
      var createRequest =
        new _VpsDefinitions.CreateManagedPosesRequest(anchorIdentifier, localizations, creationInputs, metadata);

      var requestString = JsonUtility.ToJson(createRequest, true);
      HttpRequestMessage request = BuildVpsRequest("CreateManagedPoses", requestString);

      string managedPoseB64 = await SendCreateAnchorRequest(request);
      if (string.IsNullOrEmpty(managedPoseB64))
      {
        return (null, null);
      }
      return (anchorIdentifier, managedPoseB64);
    }
    
    // @param pose Transform relative to the given anchor
    // @param anchorPayload A b64-encoded anchor payload
    // @returns (anchorIdentifier, anchorPayload)
    public static async Task<(string, string)> CreateRelativeToAnchor(Matrix4x4 pose, string anchorPayload)
    {
      // Create the request
      var anchorIdentifier = Guid.NewGuid().ToString();
      var blob = new _VpsDefinitions.WayspotAnchorBlob(anchorPayload);
      var creationInputs = BuildCreationInputs(pose);
      var metadata = _GetMetadataAsString();
      var createRequest =
        new _VpsDefinitions.CreateManagedPosesWithOffsetsRequest(anchorIdentifier, blob, creationInputs, metadata);

      var requestString = JsonUtility.ToJson(createRequest, true);
      HttpRequestMessage request = BuildVpsRequest("CreateManagedPosesWithOffsets", requestString);

      string managedPoseB64 = await SendCreateAnchorRequest(request);
      if (string.IsNullOrEmpty(managedPoseB64))
      {
        return (null, null);
      }
      return (anchorIdentifier, managedPoseB64);
    }
    
    // A helper function that will call either call CreateRelativeToAnchor() or CreateRelativeToNode().
    //
    // This function is identical to CreateRelativeToAnchor() as long as anchorPayload is not empty.
    //
    // If anchorPayload is empty, will print a warning and fallback to calling CreateRelativeToNode().
    //
    // This behavior only makes sense if the anchorPayload is located at the node origin.
    //
    // @param pose Transform relative to the given node and anchor
    // @param nodeId
    // @param anchorPayload A b64-encoded anchor payload
    // @returns (anchorIdentifier, anchorPayload)
    public static Task<(string, string)> CreateRelativeToAnchorAtNodeOriginWithFallback(
      Matrix4x4 pose, string nodeId, string anchorPayload)
    {
      if (string.IsNullOrEmpty(anchorPayload))
      {
        ARLog._WarnRelease("VPSLocationManifest is from an older release. " +
                           "Use the 'Upgrade and Refresh All Anchors' button on the manifest to remove this warning.");

        return CreateRelativeToNode(pose, nodeId);
      }

      return CreateRelativeToAnchor(pose, anchorPayload);
    }
  }
}
