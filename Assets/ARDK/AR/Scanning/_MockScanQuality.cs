// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;


namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class MockScanQualityClassifier : IScanQualityClassifier
  {
    public void ComputeScanQuality(string scanId, Action<ScanQualityResult> onResult)
    {
      ScanQualityResult mockResult = new ScanQualityResult(0.9f, new List<ScanQualityRejectionReason>());
      onResult(mockResult);
    }
  }
}
