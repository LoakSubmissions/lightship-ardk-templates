// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

namespace Niantic.Experimental.ARDK.SharedAR
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public class Room:
    IRoom
  {
    public Room(RoomParams roomParams)
    {
      RoomParams = roomParams;
    }

    public RoomParams RoomParams { get; internal set; }
    public byte[] ExperienceInitData { get; internal set; }
    public INetworking Networking { get; internal set; }
    public IColocalization Colocalization { get; internal set; }

    public void Join()
    {
      throw new System.NotImplementedException();
    }

    public void Leave()
    {
      throw new System.NotImplementedException();
    }

    public void Dispose()
    {
    }
  }
}
