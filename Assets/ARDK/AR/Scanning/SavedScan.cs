// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using Niantic.ARDK.AR.Protobuf;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.BinarySerialization;
using UnityEngine;


namespace Niantic.ARDK.AR.Scanning
{

  /// Represents a single frame in the scan. 
  public class ScanFrame
  {
    internal ScanFrame(string imagePath, Vector3 cameraPosition, Quaternion cameraRotation, double time)
    {
      this.CameraPosition = cameraPosition;
      this.CameraRotation = cameraRotation;
      this.ImagePath = imagePath;
      this.Timestamp = time;
    }

    /// The absolute path of the camera image captured for this frame, encoded as a JPEG.
    public string ImagePath { get; private set; }

    /// The world space position of the camera during this frame.
    public Vector3 CameraPosition { get; private set; }

    /// The global rotation of the camera during this frame.
    public Quaternion CameraRotation { get; private set; }

    /// The time at which this frame was captured, in seconds since the beginning of the scan.
    public double Timestamp { get; private set; }

    public override string ToString()
    {
      return "Frame: " + this.CameraPosition + "," + this.CameraRotation + "," + this.Timestamp + "," + this.ImagePath;
    }
  }

  /// A scan that has been saved in an <see cref="IScanStore"/>. 
  public class SavedScan
  {
    /// The unique ID of this scan.
    public string ScanId { get; private set; }

    private TexturedMesh _texturedMesh;
    private string _dataPathRoot;
    private RuntimeEnvironment _runtimeEnvironment;
    private List<ScanFrame> scanFrames;
    private List<LocationData> scanLocationData;

    public SavedScan(string scanId, string dataPathRoot, RuntimeEnvironment runtimeEnvironment)
    {
      this.ScanId = scanId;
      this._dataPathRoot = dataPathRoot;
      this._runtimeEnvironment = runtimeEnvironment;
    }

    private void LoadFramesAndLocation()
    {
      using FileStream fileStream =
        File.OpenRead(ScanPath.GetFrameDataPath(_dataPathRoot, ScanId, _runtimeEnvironment));
      FramesProto frames = FramesProto.Parser.ParseFrom(fileStream);
      this.scanFrames = frames.Frames.Where(frame => frame.IsLargeImage).Select(frameProto =>
      {
        AffineTransformProto transformProto = frameProto.Transform;
        Vector3 position = new Vector3(transformProto.Translation[0], transformProto.Translation[1],
          transformProto.Translation[2]);
        Quaternion rotation = new Quaternion(transformProto.Rotation[0], transformProto.Rotation[1],
          transformProto.Rotation[2], transformProto.Rotation[3]);
        return new ScanFrame(ScanPath.GetImagePath(_dataPathRoot, ScanId, _runtimeEnvironment, frameProto.Id), position,
          rotation,
          frameProto.Timestamp);
      }).ToList();

      this.scanLocationData = frames.Locations.Select(locationProto =>
      {
        LocationData locationData = new LocationData();
        locationData.accuracy = locationProto.Accuracy;
        locationData.latitude = locationProto.Latitude;
        locationData.longitude = locationProto.Longitude;
        locationData.timestamp = locationProto.Timestamp;
        locationData.elevationAccuracy = locationProto.ElevationAccuracy;
        locationData.elevationMeters = locationProto.ElevationMeters;
        locationData.headingAccuracy = locationProto.HeadingAccuracy;
        locationData.headingDegrees = locationProto.HeadingDegrees;
        return locationData;
      }).ToList();
    }

    /// Returns a list of individual <see cref="ScanFrame">ScanFrames</see> saved for this scan.
    public List<ScanFrame> GetScanFrames()
    {
      if (this.scanFrames == null)
      {
        LoadFramesAndLocation();
      }

      return scanFrames;
    }

    private static long GetDirectorySize(DirectoryInfo directoryInfo)
    {
      long size = 0;
      FileInfo[] fileInfos = directoryInfo.GetFiles();
      foreach (FileInfo fileInfo in fileInfos)
      {
        size += fileInfo.Length;
      }

      DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();
      foreach (DirectoryInfo subDirectory in subDirectories)
      {
        size += GetDirectorySize(subDirectory);
      }

      return size;
    }

    /// Returns the size, in bytes, that is consumed by this scan on disk.
    public long GetScanSize()
    {
      string scanPath = ScanPath.GetScanPath(_dataPathRoot, this.ScanId, _runtimeEnvironment);
      return GetDirectorySize(new DirectoryInfo(scanPath));
    }

    /// Returns the <see cref="TexturedMesh"/> created from this scan.
    public TexturedMesh GetTexturedMesh()
    {
      if (this._texturedMesh == null)
      {
        UnityEngine.Mesh mesh = LoadMesh(ScanPath.GetMeshPath(_dataPathRoot, ScanId, _runtimeEnvironment));
        Texture2D texture = new Texture2D(2, 2);

        if (_runtimeEnvironment == RuntimeEnvironment.Mock)
        {
          // We need to make a copy, the application have ownership and can destroy the texture if needed.
          Texture2D textureAsset = Resources.Load<Texture2D>("ARDK/MockScanningTexture");
          texture.LoadImage(textureAsset.EncodeToPNG());
        }
        else
        {
          byte[] textureBytes = File.ReadAllBytes(ScanPath.GetTexturePath(_dataPathRoot, ScanId, _runtimeEnvironment));
          texture.LoadImage(textureBytes);
        }

        return new TexturedMesh(mesh, texture);
      }

      return this._texturedMesh;
    }

    /// Returns a list of <see cref="LocationData"/> captured as part of this scan.
    /// May be empty if no location was captured.
    public List<LocationData> GetScanLocationData()
    {
      if (this.scanLocationData == null)
      {
        LoadFramesAndLocation();
      }

      return this.scanLocationData;
    }


    private static UnityEngine.Mesh LoadMesh(string path)
    {
      FileStream stream = new FileStream(path, FileMode.Open);
      UnityEngine.Mesh result = (UnityEngine.Mesh) GlobalSerializer.Deserialize(stream);
      stream.Close();
      return result;
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_GetTexturePath
      (string rootPath, string scanId, StringBuilder result, int length);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_GetMeshPath(string rootPath, string scanId, StringBuilder result, int length);
  }
}
