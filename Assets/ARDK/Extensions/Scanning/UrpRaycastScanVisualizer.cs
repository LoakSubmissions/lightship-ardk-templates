// Copyright 2023 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Scanning;
using UnityEngine;

#if ARDK_HAS_URP

using System;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Scanning;
using Niantic.ARDK.Rendering;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// Visualization that renders a raycast of the scene (URP version).
  ///
  /// The visualization can be customized by setting the "Material" field to be a Unity Material with a
  /// shader that implements the desired visualization.
  ///
  /// @note This visualizer is intended to be used with the Universal Render Pipeline. If you are using
  ///       the Built-in Render Pipeline, you should use the <see cref="RaycastScanVisualizer"/> instead.
  public class UrpRaycastScanVisualizer : ScriptableRendererFeature, IScanVisualizer
  {
    [SerializeField]
    [Tooltip("Contains the shader used to render the raycast visualization")]
    private Material _material;

    private Texture2D _inColorTexture;
    private Texture2D _inNormalTexture;
    private Texture2D _inPositionAndConfidenceTexture;
    private IRaycastBuffer _raycastBuffer;
    private float _time;
    
    private UrpScanningRaycastRenderingPass pass;

    private class UrpScanningRaycastRenderingPass : ScriptableRenderPass
    {
      private readonly Material _renderingMaterial;
      internal UrpScanningRaycastRenderingPass(Material renderingMaterial)
      {
        this._renderingMaterial = renderingMaterial;
        this.renderPassEvent = RenderPassEvent.AfterRendering;
      }
            
      public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
      {
        CommandBuffer commandBuffer = CommandBufferPool.Get(name: "UrpScanningPass");
        commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CurrentActive, _renderingMaterial);
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
      }
    }
    
    public void SetVisualizationActive(bool active)
    {
      base.SetActive(active);
    }

    public void OnScanProgress(IVoxelBuffer voxels, IRaycastBuffer raycast)
    {
      this._raycastBuffer = raycast;
      _raycastBuffer.CreateOrUpdateColorTexture(ref _inColorTexture);
      _raycastBuffer.CreateOrUpdateNormalTexture(ref _inNormalTexture);
      _raycastBuffer.CreateOrUpdatePositionTexture(ref _inPositionAndConfidenceTexture);
      
      _material.SetTexture("_ColorTex", _inColorTexture);
      _material.SetTexture("_NormalTex", _inNormalTexture);
      _material.SetTexture("_PositionAndConfidenceTex", _inPositionAndConfidenceTexture);
      _material.SetInt("_ScreenOrientation", (int) Screen.orientation);
    }

    public void ClearCurrentVisualizationState()
    {
      this._raycastBuffer = null;
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

    public override void Create()
    {
      pass = new UrpScanningRaycastRenderingPass(_material);
      base.SetActive(false);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
      renderer.EnqueuePass(pass);
    }
  }
}
#else 

namespace Niantic.ARDK.Extensions.Scanning
{
  public class UrpRaycastScanVisualizer: ScriptableObject {}
}

#endif