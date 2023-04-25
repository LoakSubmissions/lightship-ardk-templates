// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  internal class _MockScanUploadPayloadBuilder : _IScanUploadPayloadBuilder
  {
    public bool HasMoreChunks()
    {
      return false;
    }

    public bool IsValid()
    {
      return false;
    }

    public string GetNextChunk()
    {
      return null;
    }

    public string GetNextChunkUuid()
    {
      return null;
    }

    public string GetScanTargetId()
    {
      return
        "ALDKY47zD6tSzcmpzHXLqrB4yGiX12ApWY/tlAxonYaBKDo4B0UaaPBdtAThVa5BwWU7ilylTUXWd/eDXAkfg2UKnclLQAO+qoRJdkvuQDgTxLACqSN/zys=";
    }
    
    public List<LocationData> getLocationData()
    {
      return new List<LocationData>();
    }
  }
}