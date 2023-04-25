// Copyright 2022 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Internals;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Niantic.ARDK.Utilities;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _NativeVoxelBuffer: IVoxelBuffer
  {
    private IntPtr _nativeHandle;
    private long memorySize = 0;

    internal _NativeVoxelBuffer(IntPtr nativeHandle)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = nativeHandle;

      memorySize = _VoxelBuffer_GetVertexCount(nativeHandle) * sizeof(float) * 8;
      if (memorySize > 0)
      {
        GC.AddMemoryPressure(memorySize);
      }
    }

    ~_NativeVoxelBuffer()
    {
      _VoxelBuffer_Release(_nativeHandle);
      _nativeHandle = IntPtr.Zero;
      if (memorySize > 0)
      {
        GC.RemoveMemoryPressure(memorySize);
      }
      
    }
    
    public List<Vector4> GetPositions()
    {
      unsafe
      {
        int vertexCount = _VoxelBuffer_GetVertexCount(_nativeHandle);
        if (vertexCount == 0)
        {
          return new List<Vector4>();
        }
        IntPtr posPtr = _VoxelBuffer_GetPositionPointer(_nativeHandle);
        var posArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector4>(
          posPtr.ToPointer(), vertexCount, Allocator.Persistent);

        return posArray.ToList();
      }
    }

    public List<Color> GetColors()
    {
      unsafe
      {
        int vertexCount = _VoxelBuffer_GetVertexCount(_nativeHandle);
        if (vertexCount == 0)
        {
          return new List<Color>();
        }
        IntPtr posPtr = _VoxelBuffer_GetColorPointer(_nativeHandle);
        var colorArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Color>(
          posPtr.ToPointer(), vertexCount, Allocator.Persistent);

        return colorArray.ToList();
      }    
    }
    
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _VoxelBuffer_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _VoxelBuffer_GetVertexCount(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _VoxelBuffer_GetPositionPointer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _VoxelBuffer_GetColorPointer(IntPtr nativeHandle);

  }
}
