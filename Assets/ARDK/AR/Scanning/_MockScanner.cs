// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Niantic.ARDK.Utilities;
using Unity.Collections;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _MockScanner : IScanner
  {
    private sealed class _MockRaycastBuffer : IRaycastBuffer
    {
      private const int Width = 256;
      private const int Height = 192;


      public _MockRaycastBuffer()
      {
      }

      public void Dispose()
      {
      }

      public bool CreateOrUpdateColorTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Point)
      {
        if (texture == null)
        {
          texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
          byte[] bytes = LoadResourceOrThrow<TextAsset>("ARDK/MockScanningColorTexture").bytes;
          texture.LoadRawTextureData(bytes);
          texture.Apply();
        }

        return false;
      }

      public bool CreateOrUpdateNormalTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Point)
      {
        if (texture == null)
        {
          texture = LoadResourceOrThrow<Texture2D>("ARDK/MockScanningNormalTexture");
        }

        return false;
      }

      public bool CreateOrUpdatePositionTexture(ref Texture2D texture, FilterMode filterMode = FilterMode.Point)
      {
        if (texture == null)
        {
          texture = new Texture2D(Width, Height, TextureFormat.RGBAHalf, false, false);
          byte[] bytes = LoadResourceOrThrow<TextAsset>("ARDK/MockScanningPosition").bytes;
          texture.LoadRawTextureData(bytes);
          texture.Apply();
        }

        return false;
      }
    }

    private IScanner.State _state;
    private readonly IRaycastBuffer _mockRaycastBuffer;
    private readonly IVoxelBuffer _mockVoxelBuffer;
    private TexturedMesh _texturedMesh;
    private float _progress = 0.0f;
    private ScanStore _scanStore;

    public _MockScanner(string dataPath)
    {
      _state = IScanner.State.Ready;
      _mockRaycastBuffer = new _MockRaycastBuffer();

      TextAsset mockPointCloud = LoadResourceOrThrow<TextAsset>("ARDK/MockScanningPointCloud");
      byte[] bytes = mockPointCloud.bytes;
      NativeArray<byte> pointBytes = new NativeArray<byte>
        (bytes.Length / 2, Allocator.Temp);
      NativeArray<byte> colorBytes = new NativeArray<byte>
        (bytes.Length / 2, Allocator.Temp);
      NativeArray<byte>.Copy(bytes, 0, pointBytes, 0, bytes.Length / 2);
      NativeArray<byte>.Copy(bytes, bytes.Length / 2, colorBytes, 0, bytes.Length / 2);
      NativeArray<Vector4> points = pointBytes.Reinterpret<Vector4>(1);
      NativeArray<Color> colors = colorBytes.Reinterpret<Color>(1);
      _mockVoxelBuffer = new _MockVoxelBuffer(points.ToList(), colors.ToList());
      pointBytes.Dispose();
      colorBytes.Dispose();

      UnityEngine.Mesh outputMesh = Resources.Load<UnityEngine.Mesh>("ARDK/MockScanningMesh");
      Texture2D outputTexture = Resources.Load<Texture2D>("ARDK/MockScanningTexture");
      _texturedMesh = new TexturedMesh(outputMesh, outputTexture);
      _scanStore = new ScanStore(dataPath, RuntimeEnvironment.Mock);
    }

    public IScanner.State GetState()
    {
      return _state;
    }

    private void SetState(IScanner.State state)
    {
      _state = state;
      _CallbackQueue.QueueCallback(() => StateChanged?.Invoke(new IScanner.StateChangedArgs(state)));
    }

    public float GetProcessingProgress()
    {
      return _progress;
    }

    public string GetScanId()
    {
      return "mockScan";
    }

    public IVoxelBuffer GetVoxelBuffer()
    {
      return this._mockVoxelBuffer;
    }

    public event ArdkEventHandler<IScanner.StateChangedArgs> StateChanged;
    public event ArdkEventHandler<IScanner.VisualizationUpdatedArgs> VisualizationUpdated;
    public event ArdkEventHandler<IScanner.ScanProcessedArgs> ScanProcessed;

    public void StartScanning(ScanningOptions options)
    {
      SetState(IScanner.State.Scanning);
      Task.Run
      (
        () =>
        {
          while (_state == IScanner.State.Scanning || _state == IScanner.State.Paused)
          {
            _CallbackQueue.QueueCallback(
              () =>
              {
                if (options.EnableRaycastVisualization || options.EnableVoxelVisualization)
                {
                  IVoxelBuffer vb = options.EnableVoxelVisualization ? _mockVoxelBuffer : null;
                  IRaycastBuffer rb = options.EnableRaycastVisualization ? _mockRaycastBuffer : null;
                  VisualizationUpdated?.Invoke(new IScanner.VisualizationUpdatedArgs(vb, rb));
                }
              });
            Thread.Sleep(100);
          }
        }
      );
    }

    public void StopScanning()
    {
      SetState(IScanner.State.ScanCompleted);
    }

    public void PauseScanning()
    {
      SetState(IScanner.State.Paused);
    }

    public void ResumeScanning()
    {
      SetState(IScanner.State.Scanning);
    }

    public void StartProcessing(ReconstructionOptions options)
    {
      SetState(IScanner.State.Processing);
      Task.Run(() =>
      {
        _progress = 0.0f;
        for (int i = 0; i < 10; i++)
        {
          Thread.Sleep(100);
          _progress = i * 0.1f;
        }

        _progress = 1.0f;
        if (_state == IScanner.State.Processing)
        {
          SetState(IScanner.State.Done);
          _CallbackQueue.QueueCallback(
            () =>
            {
              _scanStore.SaveCurrentMesh(_texturedMesh.mesh, GetScanId());
              ScanProcessed?.Invoke(new IScanner.ScanProcessedArgs(cloneTexturedMesh(_texturedMesh), Vector3.zero));
            });
        }
      });
    }

    public IScanStore GetScanStore()
    {
      return _scanStore;
    }

    public IScanQualityClassifier GetScanQualityClassifier()
    {
      return new MockScanQualityClassifier();
    }

    public void CancelProcessing()
    {
      if (_state == IScanner.State.Processing)
        SetState(IScanner.State.Cancelled);
    }

    public void Restart()
    {
      SetState(IScanner.State.Ready);
    }

    public IRaycastBuffer GetRaycastBuffer()
    {
      return _mockRaycastBuffer;
    }

    public void SetScanTargetId(string scanTargetId)
    {
    }

    // Make a copy of the textured mesh to allow modification and deletion
    private TexturedMesh cloneTexturedMesh(TexturedMesh texturedMesh)
    {
      Texture2D texture = new Texture2D(2, 2);
      texture.LoadImage(texturedMesh.texture.EncodeToJPG());
      UnityEngine.Mesh mesh = new UnityEngine.Mesh();
      mesh.SetVertices(texturedMesh.mesh.vertices);
      mesh.SetTriangles(texturedMesh.mesh.triangles, 0);
      mesh.SetUVs(0, texturedMesh.mesh.uv);

      return new TexturedMesh(mesh, texture);
    }

    private static T LoadResourceOrThrow<T>(string path) where T : Object
    {
      var resource = Resources.Load<T>(path);
      if (resource == null)
      {
        throw new FileNotFoundException(
          $"Scanning resource \"{path}\" was not found. Scanning in the Mock environment is only " +
          "supported in the Unity Editor.");
      }
      return resource;
    }
  }
}