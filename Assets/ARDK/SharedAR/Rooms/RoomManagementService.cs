// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Utilities.Logging;
using Niantic.Experimental.ARDK.SharedAR.Rooms.MarshMessages;

using UnityEngine;

namespace Niantic.Experimental.ARDK.SharedAR.Rooms
{
  /// Results from service requests
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public enum RoomManagementServiceStatus : 
    Int32
  {
    Ok = 200,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
  }

  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public static class RoomManagementService
  {
    private static _IRoomManagementServiceImpl _serviceImpl;

    public static void InitializeService(string endpoint)
    {
      var impl = _HttpRoomManagementServiceImpl._Instance;
      
      _InitializeServiceCommon(impl, endpoint);
    }

    internal static void _InitializeServiceForTesting(string endpoint)
    {
      var impl = _FakeRoomManagementServiceImpl._Instance;
      
      _InitializeServiceCommon(impl, endpoint);
    }

    private static void _InitializeServiceCommon(_IRoomManagementServiceImpl impl, string endpoint)
    {
      _serviceImpl = impl;

      var appId = Application.identifier;
      _serviceImpl.InitializeService(endpoint, appId);
    }

    public static RoomManagementServiceStatus CreateRoom
    (
      RoomParams roomParams,
      out IRoom outRoom
    )
    {
      outRoom = null;
      if (_serviceImpl == null)
      {
        ARLog._Error("Must initialize RoomManagementService before using");
        return RoomManagementServiceStatus.BadRequest;
      }

      var request = new _CreateRoomRequest()
      {
        experienceId = roomParams.ExperienceId,
        name = roomParams.Name,
        description = roomParams.Description,
        capacity = roomParams.Capacity,
        passcode = roomParams.Visibility == RoomVisibility.Private ? roomParams.Passcode : ""
      };

      var response = _serviceImpl.CreateRoom(request, out var status);
      if (status != RoomManagementServiceStatus.Ok)
      {
        ARLog._Error($"Room Management Create request failed with status {status}");
        return status;
      }

      outRoom = new Room(response.room);
      
      return RoomManagementServiceStatus.Ok;
    }

    public static RoomManagementServiceStatus GetRoomsForExperience(string experienceId, out List<IRoom> rooms)
    {
      rooms = new List<IRoom>();
      if (_serviceImpl == null)
      {
        ARLog._Error("Must initialize RoomManagementService before using");
        return RoomManagementServiceStatus.BadRequest;
      }

      var request = new _GetRoomForExperienceRequest()
      {
        experienceIds = new List<string>() { experienceId }
      };

      var response = _serviceImpl.GetRoomsForExperience(request, out var status);
      if (status != RoomManagementServiceStatus.Ok)
      {
        ARLog._Error($"Room Management Get request failed with status {status}");
        return status;
      }

      foreach (var room in response.rooms)
      {
        rooms.Add(new Room(room));
      }
      return RoomManagementServiceStatus.Ok;
    }

    public static RoomManagementServiceStatus DeleteRoom(string roomId)
    {
      if (_serviceImpl == null)
      {
        ARLog._Error("Must initialize RoomManagementService before using");
        return RoomManagementServiceStatus.BadRequest;
      }

      var request = new _DestroyRoomRequest()
      {
        roomId = roomId
      };
      
      _serviceImpl.DestroyRoom(request, out var status);
      if (status != RoomManagementServiceStatus.Ok)
      {
        ARLog._Error($"Room Management Destroy request failed with status {status}");
        return status;
      }

      return RoomManagementServiceStatus.Ok;
    }

    public static RoomManagementServiceStatus GetRoom(string roomId, out IRoom outRoom)
    {
      outRoom = null;
      if (_serviceImpl == null)
      {
        ARLog._Error("Must initialize RoomManagementService before using");
        return RoomManagementServiceStatus.BadRequest;
      }

      var request = new _GetRoomRequest()
      {
        roomId = roomId
      };

      var response = _serviceImpl.GetRoom(request, out var status);
      if (status != RoomManagementServiceStatus.Ok)
      {
        ARLog._Error($"Room Management Get request failed with status {status}");
        return status;
      }

      outRoom = new Room(response.room);

      return RoomManagementServiceStatus.Ok;
    }

    public static void ReleaseService()
    {
      _serviceImpl?.ReleaseService();
      _serviceImpl = null;
    }
  }
}