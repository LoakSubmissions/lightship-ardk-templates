// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  /// Represents a location that can be scanned and activated for VPS.
  [Serializable]
  public struct ScanTarget
  {
    /// A unique identifier for this scan target.
    /// @note This identifier is not guaranteed to be stable across sessions.
    public string scanTargetIdentifier;

    /// The shape of this ScanTarget, as a point or polygon. It is recommended to use the <see cref="Centroid"/>
    /// property to get a point representing the location of the scan target. 
    public LatLng[] shape;

    /// The name of this scan target.
    public string name;

    /// The URL of an image depicting the scan target, or empty string if none exists. 
    public string imageUrl;

    /// A point representing the center of this scan target.
    public LatLng Centroid => shape[0];

    /// The localizability status of this scan target. This indicates whether the scan target is currently
    /// activated for VPS.
    public ScanTargetLocalizabilityStatus localizabilityStatus;

    public enum ScanTargetLocalizabilityStatus
    {
      /// The localizability of the scan target is unknown.
      UNSET,
      /// The scan target is activated as a VPS production wayspot and has a high chance of successful localization. 
      PRODUCTION,
      /// The scan target is activated as a VPS experimental wayspot and may have a lower chance of successful
      /// localization than a PRODUCTION scan target. 
      EXPERIMENTAL,
      /// The scan target is not currently activated for VPS.
      NOT_ACTIVATED
    }

    /// Downloads the image for this scan target, returning it as a Texture.
    public async void DownloadImage(Action<Texture> onImageDownloaded)
    {
      Texture image = await _HttpClient.DownloadImageAsync(imageUrl);
      onImageDownloaded?.Invoke(image);
    }
  }
}