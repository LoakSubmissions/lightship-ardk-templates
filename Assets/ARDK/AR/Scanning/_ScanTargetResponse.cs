// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.LocationService;

namespace Niantic.ARDK.AR.Scanning.Messaging
{
  [Serializable]
  internal class _ScanTargetResponse
  {
    public _ScanTarget[] scan_targets;
    public string status;
  }

  [Serializable]
  internal class _ScanTarget
  {
    public string id;
    public Shape shape;
    public string name;
    public string image_url;
    public string vps_status;
  }

  [Serializable]
  internal struct Shape
  {
    public LatLng point;

    public Shape(LatLng point)
    {
      this.point = point;
    }
  }
}