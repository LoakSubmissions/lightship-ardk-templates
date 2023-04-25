// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Internals;

using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.Utilities;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _ProcessedScan
  {
    private IntPtr _nativeHandle;
    private readonly uint _nativeSizeBytes;

    internal _ProcessedScan(IntPtr nativeHandle)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = nativeHandle;
      _nativeSizeBytes = _ProcessedScan_GetNativeSizeBytes(nativeHandle);
      GC.AddMemoryPressure(_nativeSizeBytes);
    }

    ~_ProcessedScan()
    {
        _ProcessedScan_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
        GC.RemoveMemoryPressure(_nativeSizeBytes);
    }

    internal UnityEngine.Mesh GetMesh()
    {
      unsafe
      {
        int vertexCount = _ProcessedScan_GetVertexCount(_nativeHandle);
        int faceCount = _ProcessedScan_GetFaceCount(_nativeHandle);
        IntPtr posPtr = _ProcessedScan_GetPositionPointer(_nativeHandle);
        IntPtr uvPtr = _ProcessedScan_GetUVPointer(_nativeHandle);
        IntPtr indexPtr = _ProcessedScan_GetIndexPointer(_nativeHandle);
        if (posPtr == IntPtr.Zero || uvPtr == IntPtr.Zero || indexPtr == IntPtr.Zero)
          return null;

        var posArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
          posPtr.ToPointer(), vertexCount, Allocator.Persistent);
        var uvArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector2>(
          uvPtr.ToPointer(), vertexCount, Allocator.Persistent);
        var indexArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Int32>(
          indexPtr.ToPointer(), faceCount * 3, Allocator.Persistent);

        var mesh = new UnityEngine.Mesh();
        if (posArray.Length >= 65536)
        {
          mesh.indexFormat = IndexFormat.UInt32;
        }
        mesh.SetVertices(posArray);
        mesh.SetUVs(0, uvArray);
        mesh.SetIndices(indexArray, MeshTopology.Triangles, 0);
        return mesh;
      }
    }

    internal Texture2D GetTexture()
    {
      int size = _ProcessedScan_GetTextureDataSize(_nativeHandle);
      IntPtr texturePtr = _ProcessedScan_GetTextureDataPointer(_nativeHandle);
      if (texturePtr == IntPtr.Zero)
        return null;

      byte[] textureBytes = new byte[size];
      Marshal.Copy(texturePtr, textureBytes, 0, size);
      Texture2D texture = new Texture2D(2, 2);
      texture.LoadImage(textureBytes);
      return texture;
    }

    internal Vector3 GetCenterPosition()
    {
      unsafe
      {
        IntPtr positionsPtr = _ProcessedScan_GetRootOffset(_nativeHandle);
        if (positionsPtr.ToInt32() != 0)
        {
          float* positions = (float*)positionsPtr;
          return new Vector3(positions[0], positions[1], positions[2]);
        }
        else
        {
          return Vector3.zero;
        }
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _ProcessedScan_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _ProcessedScan_GetNativeSizeBytes(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _ProcessedScan_GetVertexCount(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _ProcessedScan_GetFaceCount(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ProcessedScan_GetPositionPointer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ProcessedScan_GetUVPointer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ProcessedScan_GetIndexPointer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _ProcessedScan_GetTextureDataSize(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ProcessedScan_GetTextureDataPointer(IntPtr nativeHandle);
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _ProcessedScan_GetRootOffset(IntPtr nativeHandle);
  }
}
