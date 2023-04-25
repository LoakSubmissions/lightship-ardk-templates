// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages;

using UnityEngine;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes
{
  /// Results from service requests
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public enum AnchoredSceneServiceStatus: Int32
  {
    Ok = 200,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
  }

  // Manages the service to talk to backend servers for creation and retrieval of Anchored Scenes. 
  // @note InitializeService() must be called before any service requests to initialize the connection.
  //    ReleaseService() will close the connection, requiring InitializeService() to be called again
  //    before usage. 
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public static class AnchoredSceneService
  {
    private static _IAnchoredSceneServiceImpl _serviceImpl;
    private static IntPtr _nativeGrpcHandle = IntPtr.Zero;
    
    // Initialize the connection to the specified endpoint.
    public static void InitializeService(string endpoint)
    {
      var impl = _HttpAnchoredSceneServiceImpl._Instance;
      _InitializeServiceCommon(impl, endpoint);
    }

    internal static void _InitializeServiceForTesting(string endpoint)
    {
      var impl = _FakeAnchoredSceneServiceImpl._Instance;
      _InitializeServiceCommon(impl, endpoint);
    }

    private static void _InitializeServiceCommon(_IAnchoredSceneServiceImpl impl, string endpoint)
    {
      _serviceImpl = impl;
      // Use AppId for now, move to GlobalConfig later
      var appId = Application.identifier;
      _nativeGrpcHandle = _serviceImpl.InitializeService(endpoint, appId);
    }

    // Store an Anchored scene for future retrieval
    public static AnchoredSceneServiceStatus StoreAnchoredScene(AnchoredScene scene, out string outSceneId)
    {
      outSceneId = "";

      if (_nativeGrpcHandle == IntPtr.Zero || _serviceImpl == null)
      {
        ARLog._Error("No grpc handle, must initialize AnchoredSceneService before using");
        return AnchoredSceneServiceStatus.BadRequest;
      }

      var initDataString = _ExperienceCommon._ConvertGameDataAndWayspotAnchorsToInitData
      (
        scene.PersistentGameData,
        scene.WayspotAnchors
      );
      
      var createRequest = new _CreateExperienceRequest
      {
        name = scene.Name,
        description = scene.Kind,
        lat = scene.Location.Latitude,
        lng = scene.Location.Longitude,
        initData = initDataString,
      };

      // Make the blocking request that outputs the response proto
      var response = _serviceImpl.CreateExperience(createRequest, out var status);
      if (status != AnchoredSceneServiceStatus.Ok)
      {
        ARLog._Error($"Anchored Scene Get request failed with status {status}");
        return status;
      }

      // Output the Anchored Scene Id for storage
      outSceneId = response.experience.experienceId;
      return AnchoredSceneServiceStatus.Ok;
    }

    // Retrieve the Anchored Scene specified by the sceneId
    public static AnchoredSceneServiceStatus RetrieveAnchoredScene(string sceneId, out AnchoredScene scene)
    {
      scene = new AnchoredScene();
      
      if (_nativeGrpcHandle == IntPtr.Zero || _serviceImpl == null)
      {
        ARLog._Error("No grpc handle, must initialize AnchoredSceneService before using");
        return AnchoredSceneServiceStatus.BadRequest;
      }

      if (string.IsNullOrEmpty(sceneId))
      {
        ARLog._Error("Scene Id is null, returning");
        return AnchoredSceneServiceStatus.BadRequest;
      }

      var request = new _GetExperienceRequest()
      {
        experienceId = sceneId
      };

      // Make the blocking request that outputs a serialized response proto
      var response = _serviceImpl.GetExperience(request, out var status);
      if (status != AnchoredSceneServiceStatus.Ok)
      {
        ARLog._Error($"Anchored Scene Get request failed with status {status}");
        return status;
      }

      // Get the response proto out from bytes
      scene = response.experience;
      return status;
    }

    /// <summary>
    /// Get all AnchoredScenes within a radius of a location
    /// </summary>
    /// <param name="location">LatLng location</param>
    /// <param name="radiusInMeters">Radius to query</param>
    /// <param name="scenes">Out list of scenes that were found. Will not be null</param>
    public static AnchoredSceneServiceStatus ListAnchoredScenesInRadius
    (
      LatLng location,
      double radiusInMeters,
      out List<AnchoredScene> scenes
    )
    {
      scenes = new List<AnchoredScene>();
      
      if (_nativeGrpcHandle == IntPtr.Zero || _serviceImpl == null)
      {
        ARLog._Error("No grpc handle, must initialize AnchoredSceneService before using");
        return AnchoredSceneServiceStatus.BadRequest;
      }

      var request = new _ListExperiencesRequest()
      {
        filter = new _ListExperiencesFilter()
        {
          circle = new _CircleShape()
          {
            lat = location.Latitude,
            lng = location.Longitude,
            radiusMeters = radiusInMeters
          }
        }
      };

      var response = _serviceImpl.ListExperiencesInRadius(request, out var status);
      if (status != AnchoredSceneServiceStatus.Ok)
      {
        ARLog._Error($"Anchored Scene Get request failed with status {status}");
        return status;
      }

      foreach (var experience in response.experiences)
      {
        scenes.Add(experience);
      }

      return AnchoredSceneServiceStatus.Ok;
    }

    // Close the existing connection 
    public static void ReleaseService()
    {
      _serviceImpl?.ReleaseService(_nativeGrpcHandle);
      _serviceImpl = null;
      _nativeGrpcHandle = IntPtr.Zero;
    }

    // Internal API for deleting created AnchoredScenes. This is used to clean up internal testing,
    //  and may be moved to other services without warning.
    internal static void DeleteAnchoredScene(string sceneId)
    {
      if (_nativeGrpcHandle == IntPtr.Zero || _serviceImpl == null)
      {
        ARLog._Error("No grpc handle, must initialize AnchoredSceneService before using");
        return;
      }

      var request = new _DeleteExperienceRequest()
      {
        experienceId = sceneId,
      };
      
      _serviceImpl.DeleteExperience(request, out var status);
    }
    
    [Serializable]
    private class WayspotAnchorsData
    {
      /// The payloads to save via JsonUtility
      public string[] Payloads = Array.Empty<string>();
    }
  }
}
