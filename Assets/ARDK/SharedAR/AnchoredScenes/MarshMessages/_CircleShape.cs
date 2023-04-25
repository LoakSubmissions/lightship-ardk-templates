// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  [Serializable]
  internal struct _CircleShape
  {
    // Lower camelCase names to match Json format that Marsh expects
#region APIs to be serialized to Marsh
    // Note - these fields cannot be modified to maintain compatibility with Marsh.
    //  No additional public fields should be added without corresponding server changes
    
    // Latitude of the shape.
    public double lat;
    // Longitude of the shape.
    public double lng;
    // Radius of the shape. (0-10km]
    public double radiusMeters;
#endregion
  }
}
