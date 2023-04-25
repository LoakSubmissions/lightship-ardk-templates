// Copyright 2023 Niantic, Inc. All Rights Reserved.
using Niantic.ARDK.AR.Scanning;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// Interface implemented by scanning visualizations attached to the ARScanManager.
  ///
  /// Visualizers can be used to provide visual feedback to the user while they are scanning. They
  /// are attached to the current ARScanManager by calling <see cref="ARScanManager.SetVisualizer"/>.
  ///
  /// Several implementations of this interface are provided as part of the ARDK:
  ///   - <see cref="RaycastScanVisualizer"/>
  ///   - <see cref="UrpRaycastScanVisualizer"/>
  ///   - <see cref="WorldSpaceScanVisualizer"/>
  ///   - <see cref="PointCloudVisualizer"/>
  public interface IScanVisualizer
  {
    /// Called when the visualizer should be enabled or disabled.
    /// @param active true if the visualization should be enabled
    void SetVisualizationActive(bool active);
    
    /// Called when new scan visualization data is available. The visualizer should update its visualization to reflect
    /// the latest data.
    /// @param voxels Voxel data for the current scene. This will be null if
    ///               <see cref="RequiresVoxelData"/> return false.
    /// @param raycast Buffer generated from a raycast of the scene from the current camera viewpoint.
    ///                This will be null if <see cref="RequiresRaycastData"/> return false.
    void OnScanProgress(IVoxelBuffer voxels, IRaycastBuffer raycast);

    /// Called to reset the visualizer's state. The previous voxel / raycast buffers are no longer valid.
    void ClearCurrentVisualizationState();

    /// Return true if the visualization requires an <see cref="IVoxelBuffer"/> to be provided to
    /// <see cref="OnScanProgress"/> and false otherwise.
    bool RequiresVoxelData();

    /// Return true if the visualization requires an <see cref="IRaycastBuffer"/> to be provided to
    /// <see cref="OnScanProgress"/> and false otherwise.
    bool RequiresRaycastData();
  }
}
