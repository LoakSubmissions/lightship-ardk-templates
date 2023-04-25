// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.ARDK.AR.Scanning;
using Niantic.ARDK.Utilities.Logging;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// Visualization of the scene as a point cloud.
  ///
  /// This visualizer uses the <see cref="IVoxelBuffer"/> to render the reconstructed voxels in the scene
  /// as a point cloud overlaid on the AR view.
  [ExecuteInEditMode]
  public class PointCloudVisualizer : MonoBehaviour, IScanVisualizer
  {
    /// Material used to render the point cloud. 
    public Material material;
    
    /// Animation curve used to set the "_Progress" field on the material. This can be used to add
    /// time-based effects to the point cloud.
    public AnimationCurve animationCurve;

    /// Interval, in seconds, at which the voxel data is refreshed.
    public float updateInterval = 3f;

    private ComputeBuffer _positionBuffer;
    private ComputeBuffer _colorBuffer;

    private float _updateTimer;

    // Interval for material animation.
    private float _curveTimer;
    private float _curveInterval;

    private IVoxelBuffer _voxelBuffer;
    private bool _active;
    private int _pointCount;
    
    // Buffers used for fallback when structured buffer isn't supported.
    private float[] _positionBufferFloatArray;
    private float[] _colorBufferFloatArray;
    private float[] _positionTextureArray;
    private float[] _colorTextureArray;
    private Texture2D _positionTexture;
    private Texture2D _colorTexture;

    void Start()
    {
      _curveInterval = animationCurve.keys.Max(k => k.time);
      if (!IsComputeBufferSupported())
      {
        _positionTextureArray = new float[1024 * 1024 * 4];
        _colorTextureArray = new float[1024 * 1024 * 4];
        _positionTexture =  new Texture2D(1024, 1024, TextureFormat.RGBAFloat, false);
        _colorTexture =  new Texture2D(1024, 1024, TextureFormat.RGBAFloat, false);
      }
    }

    void OnDestroy()
    {
      Dispose();
      if (!IsComputeBufferSupported())
      {
        Destroy(_positionTexture);
        Destroy(_colorTexture);
      }
    }

    private void ReleaseBuffer(ref ComputeBuffer buffer)
    {
      if (buffer != null)
      {
        buffer.Dispose();
        buffer = null;
      }
    }

    private void Dispose()
    {
      ReleaseBuffer(ref _positionBuffer);
      ReleaseBuffer(ref _colorBuffer);
    }

    void OnDisable()
    {
#if UNITY_EDITOR
      // OnDestroy is called too late when Unity enters play mode from edit mode
      // Without this, a warning will be logged when GC cleans up the compute buffers.
      if (EditorApplication.isPlayingOrWillChangePlaymode)
      {
        Dispose();
      }
#endif
    }

    public void ClearCurrentVisualizationState()
    {
      Dispose();
      _curveTimer = 0;
      _updateTimer = updateInterval;
    }

    /// Returns true since this visualizer uses voxel data
    public bool RequiresVoxelData()
    {
      return true;
    }

    /// Returns false since this visualizer does not use raycast data
    public bool RequiresRaycastData()
    {
      return false;
    }

    private void SetData(List<Vector4> positions, List<Color> colors)
    {
      if (positions.Count == 0 || colors.Count == 0)
      {
        ARLog._Debug("No points");
        return;
      }

      if (_positionBuffer == null || positions.Count > _positionBuffer.count)
      {
        ReleaseBuffer(ref _positionBuffer);
        ReleaseBuffer(ref _colorBuffer);

        _positionBuffer = new ComputeBuffer(positions.Count, sizeof(float) * 4, ComputeBufferType.Structured,
          ComputeBufferMode.Dynamic);
        _colorBuffer = new ComputeBuffer(colors.Count, sizeof(float) * 4, ComputeBufferType.Structured,
          ComputeBufferMode.Dynamic);
      }

      Vector3 cameraPosition = Camera.main.transform.position;
      float maxDistanceFromCamera = 3;
      for (int i = 0; i < positions.Count; i += 100)
      {
        float cameraDistance = Vector3.Distance(positions[i], cameraPosition);
        if (cameraDistance > maxDistanceFromCamera)
        {
          maxDistanceFromCamera = cameraDistance;
        }
      }
      
      
      
      material.SetFloat("_MaxDistance", maxDistanceFromCamera);
      _pointCount = positions.Count;
      _positionBuffer.SetData(positions);
      _colorBuffer.SetData(colors);
      if (!IsComputeBufferSupported())
      {
        Array.Clear(_positionTextureArray, 0, _positionTextureArray.Length);
        Array.Clear(_colorTextureArray, 0, _colorTextureArray.Length);
        
        // TODO: Probably only need to copy once.
        using NativeArray<Vector4> positionArr = new NativeArray<Vector4>(positions.ToArray(), Allocator.Temp);
        NativeArray<float> positionArrFloat = positionArr.Reinterpret<float>(16);
        // Length is number of bytes, not number of elements.
        Buffer.BlockCopy(positionArrFloat.ToArray(), 0, _positionTextureArray, 0, Mathf.Min(_pointCount * 4, _positionTextureArray.Length) * 4);
        using NativeArray<Color> colorArr = new NativeArray<Color>(colors.ToArray(), Allocator.Temp);
        NativeArray<float> colorArrFloat = colorArr.Reinterpret<float>(16);
        Buffer.BlockCopy(colorArrFloat.ToArray(), 0, _colorTextureArray, 0, Mathf.Min(_pointCount * 4, _colorTextureArray.Length) * 4);
        
        _positionTexture.SetPixelData(_positionTextureArray, 0);
        _colorTexture.SetPixelData(_colorTextureArray, 0);
        _positionTexture.Apply();
        _colorTexture.Apply();
      }


    }

    void Update()
    {
      if (_active && _voxelBuffer != null)
      {
        _curveTimer += Time.deltaTime;
        _updateTimer += Time.deltaTime;
        if (_curveTimer > _curveInterval)
        {
          _curveTimer -= _curveInterval;
        }

        material.SetFloat("_Progress", animationCurve.Evaluate(_curveTimer));
        if (_updateTimer > updateInterval)
        {
          SetData(_voxelBuffer.GetPositions(), _voxelBuffer.GetColors());
          _updateTimer -= updateInterval;
        }
      }
    }

    private bool IsComputeBufferSupported()
    {
      return SystemInfo.maxComputeBufferInputsVertex >= 2;
    }

    void OnRenderObject()
    {
      if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0 || this._positionBuffer == null || !this._active)
      {
        return;
      }

      material.SetPass(0);
      if (IsComputeBufferSupported())
      {
        material.SetBuffer("_PositionBuffer", _positionBuffer);
        material.SetBuffer("_ColorBuffer", _colorBuffer);
        material.EnableKeyword("ENABLE_COMPUTE_BUFFERS");
      }
      else
      {
        material.SetTexture("_PositionBuffer", _positionTexture);
        material.SetTexture("_ColorBuffer", _colorTexture);
        material.DisableKeyword("ENABLE_COMPUTE_BUFFERS");
      }


      material.SetMatrix("_Transform", transform.localToWorldMatrix);
      Graphics.DrawProceduralNow(MeshTopology.Points, _pointCount, 1);
    }
 
    public void SetVisualizationActive(bool active)
    {
      _active = active;
      if (!active)
      {
        Dispose();
      }
    }

    public void OnScanProgress(IVoxelBuffer voxels, IRaycastBuffer raycast)
    {
      this._voxelBuffer = voxels;
    }
  }
}
