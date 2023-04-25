// Copyright 2022 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.Experimental.ARDK.SharedAR
{

  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public interface IRoom : 
    IDisposable
  {
    // Identifiers
    public RoomParams RoomParams { get; }

    public void Join();
    public byte[] ExperienceInitData { get; }

    // Shared AR Client components
    public INetworking Networking { get; }
    public IColocalization Colocalization { get; }

    public void Leave();
  }
} // namespace Niantic.ARDK.SharedAR