// Copyright 2023 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Scanning;
using UnityEngine;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// Visualization of world-space position and normal data derived from an <see cref="IRaycastBuffer"/>.
  ///
  /// To customize the visualization, set the "Material" field to be a Unity Material that
  /// implements the desired visualization.
  public class WorldSpaceScanVisualizer : MonoBehaviour, IScanVisualizer
  {
    /// Material implementing the visualization. See ScreenSpaceNormalPointCloud.shader for an example.
    public Material material;
    
    /// Animation curve that can be used to implement time-based effects in the shader.
    public AnimationCurve animationCurve;

    private Texture2D _colors;
    private Texture2D _normals;
    private Texture2D _depths;

    private bool _active;
    private float _curveTimer;
    private float _curveInterval;

    void Update()
    {
      if (_active)
      {
        _curveTimer += Time.deltaTime;
        if (_curveTimer > _curveInterval)
        {
          _curveTimer -= _curveInterval;
        }

        material.SetFloat("_Progress", animationCurve.Evaluate(_curveTimer));
      }
    }


    public void SetVisualizationActive(bool active)
    {
      _active = active;
    }

    public void ClearCurrentVisualizationState()
    {
      Destroy(this._colors);
      Destroy(this._normals);
      Destroy(this._depths);

      this._colors = null;
      this._normals = null;
      this._depths = null;
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

    void OnRenderObject()
    {
      if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0 || this._depths == null || !this._active)
      {
        return;
      }

      material.SetPass(0);
      material.SetMatrix("_Transform", transform.localToWorldMatrix);
      Graphics.DrawProceduralNow(MeshTopology.Triangles, _depths.width * _depths.height * 3, 1);
    }

    public void OnScanProgress(IVoxelBuffer voxels, IRaycastBuffer raycast)
    {
      raycast.CreateOrUpdateColorTexture(ref _colors);
      raycast.CreateOrUpdateNormalTexture(ref _normals);
      raycast.CreateOrUpdatePositionTexture(ref _depths);

      material.SetInt("_Width", _colors.width);
      material.SetInt("_Height", _colors.height);

      material.SetTexture("_PositionTex", _depths);
      material.SetTexture("_NormalTex", _normals);
      material.SetTexture("_ColorTex", _colors);
    }
  }
}
