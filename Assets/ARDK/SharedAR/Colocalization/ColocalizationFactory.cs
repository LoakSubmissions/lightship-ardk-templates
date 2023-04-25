// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.Utilities;

namespace Niantic.Experimental.ARDK.SharedAR
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  public static class ColocalizationFactory
  {
    /// @note This is an experimental feature. Experimental features should not be used in
    /// production products as they are subject to breaking changes, not officially supported, and
    /// may be deprecated without notice
    public class ColocalizationCreatedArgs :
      IArdkEventArgs
    {
      public IColocalization Colocalization { get; private set; }

      public ColocalizationCreatedArgs(IColocalization colocalization)
      {
        Colocalization = colocalization;
      }
    }

    // TODO : eventually remove this when a sharedArManager is implemented : AR-12779
    public static event ArdkEventHandler<ColocalizationCreatedArgs> ColocalizationCreated;

    // TODO: Cleanup ColocalizationFactory
    // https://niantic.atlassian.net/browse/AR-14212
    public static IColocalization Create(INetworking networking, IARSession arSession)
    {
      var args = new ColocalizationCreatedArgs(null);
      ColocalizationCreated(args);
      return null;
    }

    public static IColocalization Create(INetworking networking, IARSession arSession, WayspotAnchorPayload content)
    {
      return null;
    }
  }
}
