// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.ComponentModel;
using Niantic.ARDK.VirtualStudio;

namespace Niantic.ARDK.AR.Scanning
{
  /// Factory for creating new <see cref="IScanTargetClient"/> instances. 
  public class ScanTargetClientFactory
  {
    /// Creates a new IScanTargetClient.
    /// @param env the runtime environment in which to create the client
    /// @param mockResponse a ScanTargetResponse to return in the Mock and Playback environments. In other
    ///        environments, this is ignored.
    public static IScanTargetClient Create(RuntimeEnvironment env, ScanTargetResponse mockResponse = null)
    {
      if (env == RuntimeEnvironment.Default)
        return Create(_VirtualStudioLauncher.SelectedMode, mockResponse);

      switch (env)
      {
        case RuntimeEnvironment.LiveDevice:
          return new _NativeScanTargetClient();

        case RuntimeEnvironment.Remote:
          throw new NotSupportedException();

        case RuntimeEnvironment.Mock:
        case RuntimeEnvironment.Playback:
          return new _MockScanTargetClient(mockResponse);

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }
    }
  }
}