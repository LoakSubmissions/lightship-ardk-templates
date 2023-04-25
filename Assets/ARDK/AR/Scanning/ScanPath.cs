// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Niantic.ARDK.Internals;

namespace Niantic.ARDK.AR.Scanning
{
  internal class ScanPath
  {
    private const int STRING_SIZE = 512;
    internal static string GetBasePath(string basePath, RuntimeEnvironment runtimeEnvironment)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        string path = basePath + "/scanning";
        if (!Directory.Exists(path))
        {
          Directory.CreateDirectory(path);
        }
        return path;
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetBasePath(basePath, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }
    
    internal static string GetMeshPath(string basePath, string scanId, RuntimeEnvironment runtimeEnvironment)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        return basePath + "/scanning/" + scanId + "/mesh.mesh";
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetMeshPath(basePath, scanId, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }
    
    internal static string GetScanPath(string basePath, string scanId, RuntimeEnvironment runtimeEnvironment)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        return basePath + "/scanning/" + scanId;
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetScanPath(basePath, scanId, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }

    internal static string GetTexturePath(string basePath, string scanId, RuntimeEnvironment runtimeEnvironment)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        // This is not actually used.
        return basePath + "/scanning/" + scanId + "/MockScanningTexture.jpg";
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetTexturePath(basePath, scanId, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }

    internal static string GetImagePath(string basePath, string scanId, RuntimeEnvironment runtimeEnvironment,
      int index)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        return basePath + "/scanning/" + scanId + "/MockScanningTexture.jpg";
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetLargeImagePath(basePath, scanId, index, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }

    internal static string GetFrameDataPath(string basePath, string scanId, RuntimeEnvironment runtimeEnvironment)
    {
      if (runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        // This is not actually used.
        return basePath + "/scanning/" + scanId + "/frames.pb";
      }
      else
      {
        StringBuilder stringBuilder = new StringBuilder(STRING_SIZE);
        _Scanner_GetFrameDataPath(basePath, scanId, stringBuilder, STRING_SIZE);
        return stringBuilder.ToString();
      }
    }
    

    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetBasePath(string rootPath, StringBuilder result, int length);

    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetScanPath(string rootPath, string scanId, StringBuilder result, int length);

    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetTexturePath
      (string rootPath, string scanId, StringBuilder result, int length);

    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetMeshPath(string rootPath, string scanId, StringBuilder result, int length);
    
    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetFrameDataPath(string rootPath, string scanId, StringBuilder result, int length);
    
    [DllImport(_ARDKLibrary.libraryName)]
    internal static extern void _Scanner_GetLargeImagePath(string rootPath, string scanId, int index, StringBuilder result, int length);
  }
}