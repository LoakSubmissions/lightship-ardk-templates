// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _NativeScanUploadPayloadBuilder : _ThreadCheckedObject, _IScanUploadPayloadBuilder
  {
    static _NativeScanUploadPayloadBuilder()
    {
      _Platform.Init();
    }

    private IntPtr _nativeHandle;

    internal _NativeScanUploadPayloadBuilder(IntPtr nativeHandle)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = nativeHandle;
    }

    internal _NativeScanUploadPayloadBuilder(string basePath, string scanId, string userDataStr) :
      this(_Scanner_UploaderCreate(basePath, scanId, userDataStr))
    {
    }

    ~_NativeScanUploadPayloadBuilder()
    {
      _Scanner_UploaderRelease(_nativeHandle);
    }

    public bool HasMoreChunks()
    {
      return _Scanner_UploaderHasMoreChunks(_nativeHandle);
    }

    public bool IsValid()
    {
      return _Scanner_UploaderIsValid(_nativeHandle);
    }

    public string GetNextChunk()
    {
      StringBuilder result = new StringBuilder(256);
      _Scanner_UploaderGetNextChunk(_nativeHandle, result, 256);
      if (result.Length == 0)
      {
        throw new IOException("Error building scan upload chunk");
      }
      return result.ToString();
    }

    public string GetNextChunkUuid()
    {
      StringBuilder result = new StringBuilder(40);
      _Scanner_UploaderGetNextChunkUuid(_nativeHandle, result, 40);
      return result.ToString();
    }

    public string GetScanTargetId()
    {
      StringBuilder result = new StringBuilder(256);
      _Scanner_UploaderGetPoiId(_nativeHandle, result, 256);
      return result.ToString();
    }

    public List<LocationData> getLocationData()
    {
      int count = _Scanner_UploaderGetLocationDataCount(_nativeHandle);
      if (count > 0)
      {
        unsafe
        {
          NativeArray<LocationData> locations = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<LocationData>(
            _Scanner_UploaderGetLocationDataPtr(_nativeHandle).ToPointer(), count, Allocator.Persistent);
          return locations.ToList();
        }
      }
      else
      {
        return new List<LocationData>();
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern int _Scanner_UploaderGetLocationDataCount(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_UploaderGetLocationDataPtr(IntPtr nativeHandle);


    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_UploaderCreate(string rootPath, string scanId, string userJson);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_UploaderRelease(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_UploaderGetNextChunk(IntPtr nativeHandle, StringBuilder result, int stringMaxLength);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_UploaderGetNextChunkUuid(IntPtr nativeHandle, StringBuilder result, int stringMaxLength);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _Scanner_UploaderIsValid(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _Scanner_UploaderHasMoreChunks(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_UploaderGetPoiId(IntPtr nativeHandle, StringBuilder result, int stringMaxLength);
  }
}