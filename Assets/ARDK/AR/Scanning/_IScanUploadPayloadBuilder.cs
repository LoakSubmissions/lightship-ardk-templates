// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Niantic.ARDK.AR.Scanning
{
  [StructLayout(LayoutKind.Sequential)]
  public struct LocationData
  {
    /// Timestamp, in seconds since 1970, when this location was recorded. 
    public double timestamp;

    /// Latitude of the location, in degrees.
    public double latitude;

    /// Longitude of the location, in degrees.
    public double longitude;

    /// Horizontal uncertainty of this location, in meters.
    public float accuracy;

    /// Elevation of this location, in meters.
    public float elevationMeters;

    /// Uncertainty in elevation, in meters.
    public float elevationAccuracy;

    /// Direction the camera was facing, in degrees clockwise from true north. 
    public float headingDegrees;

    /// Uncertainty of the heading, in degrees.
    public float headingAccuracy;
  }
  
  // This interface is internal as it is only used by ScanStore.
  internal interface _IScanUploadPayloadBuilder
  {
    bool HasMoreChunks();
    bool IsValid();
    string GetNextChunk();
    string GetNextChunkUuid();
    string GetScanTargetId();
    List<LocationData> getLocationData();
  }
}
