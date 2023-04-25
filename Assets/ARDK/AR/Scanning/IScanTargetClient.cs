// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;
using Niantic.ARDK.LocationService;

namespace Niantic.ARDK.AR.Scanning
{
  /// Client for requesting scan targets close to the user's location. A scan target is a location that can be scanned
  /// and activated for VPS.
  ///
  /// Use <see cref="ScanTargetClientFactory"/> to create a scan target client.
  public interface IScanTargetClient
  {
    /// Request scan targets within a given radius of a location using the callback pattern.
    /// @param queryLocation Center of query.
    /// @param queryRadius Radius for query between 0m and 2000m. Negative radius will default to the maximum radius of 2000m.
    /// @param onScanTargetReceived Callback function to process the received ScanTargetResponse.
    public void RequestScanTargets(LatLng queryLocation, int queryRadius,
      Action<ScanTargetResponse> onScanTargetReceived);

    /// Requests scan targets within a given radius of a location using the async/await pattern.
    /// @param queryLocation Center of query.
    /// @param queryRadius Radius for query between 0m and 2000m. Negative radius will default to the maximum radius of 2000m.
    /// @returns Task with the received ScanTargetResponse as result. 
    public Task<ScanTargetResponse> RequestScanTargetsAsync(LatLng queryLocation, int queryRadius);
  }
}