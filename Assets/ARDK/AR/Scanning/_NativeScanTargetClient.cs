// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Protobuf;
using Niantic.ARDK.AR.Scanning.Messaging;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Telemetry;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VPSCoverage;
using Niantic.ARDK.VPSCoverage.GeoserviceMessages;
using UnityEngine;
using LocationServiceStatus = UnityEngine.LocationServiceStatus;

namespace Niantic.ARDK.AR.Scanning
{
  internal class _NativeScanTargetClient : IScanTargetClient
  {
    private const string ScanTargetEndpoint =
      "https://vps-coverage-api.nianticlabs.com/api/json/v1/SEARCH_SCAN_TARGETS";

    private const string ScanTargetMethodName = "SEARCH_SCAN_TARGETS";

    private Dictionary<string, string> _encodedScanIds = new Dictionary<string, string>();

    public async void RequestScanTargets(LatLng queryLocation, int queryRadius,
      Action<ScanTargetResponse> onScanTargetReceived)
    {
      ScanTargetResponse result = await RequestScanTargetsAsync(queryLocation, queryRadius);
      onScanTargetReceived?.Invoke(result);
    }

    public async Task<ScanTargetResponse> RequestScanTargetsAsync(LatLng queryLocation, int queryRadius)
    {
      _ScanTargetRequest request;

      // Server side we use radius == 0 then use max radius, radius < 0 then set radius to 0.
      // Client side we want a to use radius == 0 then radius = 0, radius < 0 then use max radius.
      if (queryRadius == 0)
        queryRadius = -1;
      else if (queryRadius < 0)
        queryRadius = 0;

      ARCommonMetadataStruct metadata = ArdkGlobalConfig._Internal.GetCommonDataEnvelopeWithRequestIdAsStruct();
      var requestHeaders = ArdkGlobalConfig._Internal.GetApiGatewayHeaders();
      ARLog._Debug(JsonUtility.ToJson(metadata, true));

      if (Input.location.status == LocationServiceStatus.Running)
      {
        int distanceToQuery = (int)queryLocation.Distance(new LatLng(Input.location.lastData));
        request = new _ScanTargetRequest(queryLocation, queryRadius, distanceToQuery, metadata);
      }
      else
      {
        request = new _ScanTargetRequest(queryLocation, queryRadius, metadata);
      }

      _HttpResponse<_ScanTargetResponse> response =
        await _HttpClient.SendPostAsync<_ScanTargetRequest, _ScanTargetResponse>
        (
          ScanTargetEndpoint,
          request,
          requestHeaders
        );
      ResponseStatus trueResponseStatus = response.Status == ResponseStatus.Success
        ? _ResponseStatusTranslator.FromString(response.Data.status)
        : response.Status;

      ReportResponseToTelemetry(ScanTargetMethodName, request.ar_common_metadata.request_id, response.HttpStatusCode,
        trueResponseStatus);

      if (trueResponseStatus != ResponseStatus.Success)
      {
        return new ScanTargetResponse(trueResponseStatus);
      }

      if (response.Data.scan_targets == null)
      {
        // Request is successful, but there are no results.
        return new ScanTargetResponse(ResponseStatus.Success);
      }

      List<ScanTarget> result = response.Data.scan_targets.Select(scanTarget =>
      {
        ScanTarget target = new ScanTarget();
        target.name = scanTarget.name;
        target.shape = new LatLng[] { scanTarget.shape.point };
        target.imageUrl = scanTarget.image_url;
        Enum.TryParse(scanTarget.vps_status, out ScanTarget.ScanTargetLocalizabilityStatus status);
        target.localizabilityStatus = status;
        string encodedScanTargetId; 
        if (_encodedScanIds.ContainsKey(scanTarget.id))
        {
          encodedScanTargetId = _encodedScanIds[scanTarget.id];
        }
        else
        {
          // Introduce randomness to the ID here so they are not expected to be stable.

          byte[] key = new byte[8];
          byte[] iv = new byte[8];
          RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider();
          rngCryptoServiceProvider.GetBytes(key);
          rngCryptoServiceProvider.GetBytes(iv);
          SymmetricAlgorithm algorithm = DES.Create();
          ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
          byte[] inputBuffer = Encoding.Unicode.GetBytes(scanTarget.id);
          byte[] encodedId = transform.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
          byte[] outputWithKey = new byte[16 + 1 + encodedId.Length];
          outputWithKey[0] = 0; // First byte is version.
          Buffer.BlockCopy(key, 0, outputWithKey, 1, 8);
          Buffer.BlockCopy(iv, 0, outputWithKey, 9, 8);
          Buffer.BlockCopy(encodedId, 0, outputWithKey, 17, encodedId.Length);
          rngCryptoServiceProvider.Dispose();
          encodedScanTargetId = Convert.ToBase64String(outputWithKey);
          _encodedScanIds.Add(scanTarget.id, encodedScanTargetId);
        }
        target.scanTargetIdentifier = encodedScanTargetId;
        
        return target;
      }).ToList();

      result.Sort((a, b) => a.Centroid.Distance(queryLocation)
        .CompareTo(b.Centroid.Distance(queryLocation))
      );

      return new ScanTargetResponse(result);
    }

    private void ReportResponseToTelemetry(string methodName, string requestId, long httpStatus,
      ResponseStatus responseStatus)
    {
      try
      {
        bool isSuccess = httpStatus >= 200 && httpStatus < 300;

        _TelemetryService.RecordEvent(
          new LightshipServiceEvent()
          {
            IsRequest = false,
            ApiMethodName = methodName,
            Success = isSuccess,
            Response = responseStatus.ToString(),
            HttpStatus = httpStatus.ToString(),
          },
          requestId);
      }
      catch (Exception e)
      {
        ARLog._Debug($"Error logging scan target response: {e}");
      }
    }
  }
}