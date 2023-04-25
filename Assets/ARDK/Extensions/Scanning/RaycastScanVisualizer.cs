// Copyright 2023 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Scanning;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// Visualization that renders a raycast of the scene (Built-in Render Pipeline version).
  ///
  /// The visualization can be customized by setting the "Material" field to be a Unity Material with a
  /// shader that implements the desired visualization.
  ///
  /// @note This component should be added to the GameObject containing the main camera in the Unity scene.
  /// @note If you are using the Universal Render Pipeline, you should use
  ///       <see cref="UrpRaycastScanVisualizer"/> instead.
  [RequireComponent(typeof(Camera))]
  public class RaycastScanVisualizer : MonoBehaviour, IScanVisualizer
  {
    [SerializeField]
    [Tooltip("Contains the shader used to render the raycast visualization")]
    private Material _material;
    
    private Texture2D _inColorTexture;
    private Texture2D _inNormalTexture;
    private Texture2D _inPositionAndConfidenceTexture;
    private IRaycastBuffer _raycastBuffer;
    
    private bool _active;

    void Start()
    {
      if (_material == null)
      {
        _material = (Material)Resources.Load("ARDK/ScanningStripes");
      }

      _active = false;
    }

    private void Render(RenderTexture src, IRaycastBuffer raycastBuffer, RenderTexture dst)
    {
      raycastBuffer.CreateOrUpdateColorTexture(ref _inColorTexture);
      raycastBuffer.CreateOrUpdateNormalTexture(ref _inNormalTexture);
      raycastBuffer.CreateOrUpdatePositionTexture(ref _inPositionAndConfidenceTexture);
      
      _material.SetTexture("_ColorTex", _inColorTexture);
      _material.SetTexture("_NormalTex", _inNormalTexture);
      _material.SetTexture("_PositionAndConfidenceTex", _inPositionAndConfidenceTexture);
      _material.SetInt("_ScreenOrientation", (int) Screen.orientation);
      Graphics.Blit(src, dst, _material);
    }
    
    public void SetVisualizationActive(bool active)
    {
      _active = active;
  
      if (active && gameObject.GetComponent<Camera>() == null)
      {
        ARLog._Warn("This RaycastScanVisualizer is attached to a GameObject without a Camera. As a result, " +
                    "its visualization will not be rendered. To fix this, add the RaycastScanVisualizer " +
                    "as a component on the main camera.");
      }
    }

    public void OnScanProgress(IVoxelBuffer voxels, IRaycastBuffer raycast)
    {
      _raycastBuffer = raycast;
    }

    public void ClearCurrentVisualizationState()
    {
      _raycastBuffer = null;
    }

    /// Returns false since this visualizer does not use voxel data
    public bool RequiresVoxelData()
    {
      return false;
    }

    /// Returns true since this visualizer uses the raycast data.
    public bool RequiresRaycastData()
    {
      return true;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
      if (_active && _raycastBuffer != null)
      {
        Render(src, _raycastBuffer, dst);
      }
      else
      {
        Graphics.Blit(src, dst);
      }
    }
  }
}