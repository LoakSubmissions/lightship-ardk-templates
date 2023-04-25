// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using Niantic.ARDK.VPSCoverage;


namespace Niantic.ARDK.AR.Scanning
{
  /// Response from the server to requests from <see cref="IScanTargetClient"/>.
  [Serializable]
  public class ScanTargetResponse
  {
    /// List of targets returned by the server. This may be empty if there were no scan targets within the
    /// radius of the query.
    public List<ScanTarget> scanTargets;
    
    /// Status code returned by the server.
    public ResponseStatus status;

    public ScanTargetResponse(List<ScanTarget> scanTargets)
    {
      this.status = ResponseStatus.Success;
      this.scanTargets = scanTargets;
    }

    public ScanTargetResponse(ResponseStatus status)
    {
      this.status = status;
      this.scanTargets = new List<ScanTarget>();
    }
  }
}