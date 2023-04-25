// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  /// Contains per-voxel position and color data based on a live reconstruction of the scanned scene.
  ///
  /// As the user scans, <see cref="IScanner"/> builds up a voxel-based representation of the scene. This
  /// representation is packed into an IVoxelBuffer and periodically delivered in the
  /// <see cref="IScanner.VisualizationUpdated"/> event.
  ///
  /// The voxel information is stored as two parallel lists containing position and color information. Each
  /// list has one element per voxel. 
  ///
  /// Please see PointCloudVisualizer for an example of IVoxelBuffer usage.
  public interface IVoxelBuffer
  {
    /// Returns a list of XYZ positions for the center of each occupied voxel. The W component is always set to 0.
    /// The list is linked by index with the one returned by <see cref="GetColors"/>.
    List<Vector4> GetPositions();
    
    /// Returns a list of RGB colors, one for each occupied voxel.
    /// The list is linked by index with the one returned by <see cref="GetPositions"/>.
    List<Color> GetColors();
  }
}
