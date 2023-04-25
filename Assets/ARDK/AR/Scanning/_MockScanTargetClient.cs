// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;
using Niantic.ARDK.LocationService;


namespace Niantic.ARDK.AR.Scanning
{
  internal class _MockScanTargetClient : IScanTargetClient
  {
    internal _MockScanTargetClient(ScanTargetResponse mockResponse)
    {
      this._mockResponse = mockResponse;
    }

    private readonly ScanTargetResponse _mockResponse;

    public async void RequestScanTargets(LatLng queryLocation, int queryRadius,
      Action<ScanTargetResponse> onScanTargetReceived)
    {
      ScanTargetResponse result = await RequestScanTargetsAsync(queryLocation, queryRadius);
      onScanTargetReceived?.Invoke(result);
    }

    public Task<ScanTargetResponse> RequestScanTargetsAsync(LatLng queryLocation, int queryRadius)
    {
      return Task.FromResult(_mockResponse);
    }
  }
}