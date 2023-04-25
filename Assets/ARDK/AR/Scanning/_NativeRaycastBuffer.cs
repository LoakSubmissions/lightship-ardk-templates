// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Internals;

using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _NativeRaycastBuffer: IRaycastBuffer
  {
    static _NativeRaycastBuffer()
    {
      _Platform.Init();
    }

    private IntPtr _nativeHandle;
    private readonly int _width;
    private readonly int _height;
    private readonly int _depth;
    private readonly int _bufferBytes;

    internal _NativeRaycastBuffer(IntPtr nativeHandle)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = nativeHandle;
      _width = (int) _RaycastBuffer_GetWidth(nativeHandle);
      _height = (int) _RaycastBuffer_GetHeight(nativeHandle);
      _depth = (int) _RaycastBuffer_GetDepth(nativeHandle);
      _bufferBytes = _width * _height * _depth * sizeof(UInt32);
      GC.AddMemoryPressure(_bufferBytes);
    }

    ~_NativeRaycastBuffer()
    {
      Dispose();
    }
    
    public void Dispose()
    {
      if (_nativeHandle != IntPtr.Zero)
      {
        _RaycastBuffer_Release(_nativeHandle);
        GC.SuppressFinalize(this);
        GC.RemoveMemoryPressure(_bufferBytes);
        _nativeHandle = IntPtr.Zero;
      }
    }
    
    private void InitTexture(ref Texture2D texture, TextureFormat format, FilterMode filterMode)
    {
      if (texture == null || texture.format != format)
      {
        texture = new Texture2D(_width, _height, format, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      else
      {
        if (texture.width != _width || texture.height != _height)
#if UNITY_2021_2_OR_NEWER
          texture.Reinitialize(_width, _height);
#else
          texture.Resize(_width, _height);
#endif
        if (texture.filterMode != filterMode)
          texture.filterMode = filterMode;
      }
    }
    
    public bool CreateOrUpdateColorTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Bilinear)
    {
      InitTexture(ref texture, TextureFormat.RGBA32, filterMode);
      unsafe
      {
        _RaycastBuffer_GetColorDataPointer(_nativeHandle, new IntPtr(NativeArrayUnsafeUtility.GetUnsafePtr(texture.GetRawTextureData<Color32>())));
      }

      texture.Apply(false);
      return true;
    }

    public bool CreateOrUpdateNormalTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Bilinear)
    {
      InitTexture(ref texture, TextureFormat.RGBA32, filterMode);
      unsafe
      {
        _RaycastBuffer_GetNormalDataPointer(_nativeHandle, new IntPtr(NativeArrayUnsafeUtility.GetUnsafePtr(texture.GetRawTextureData<Color32>())));
      }
      texture.Apply(false);
      return true;
    }

    public bool CreateOrUpdatePositionTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Bilinear)
    {
      InitTexture(ref texture, TextureFormat.RGBAHalf, filterMode);
      unsafe
      {
        _RaycastBuffer_GetPositionDataPointer(_nativeHandle, new IntPtr(NativeArrayUnsafeUtility.GetUnsafePtr(texture.GetRawTextureData<Color32>())));
      }
      texture.Apply(false);
      return true;
      
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _RaycastBuffer_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _RaycastBuffer_GetWidth(IntPtr nativeHandle);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _RaycastBuffer_GetHeight(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _RaycastBuffer_GetDepth(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _RaycastBuffer_GetColorDataPointer(IntPtr nativeHandle, IntPtr dataPtr);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _RaycastBuffer_GetNormalDataPointer(IntPtr nativeHandle, IntPtr dataPtr);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _RaycastBuffer_GetPositionDataPointer(IntPtr nativeHandle, IntPtr dataPtr);
  }
}
