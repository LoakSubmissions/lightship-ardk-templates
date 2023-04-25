// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{

  /// Options controlling the scan capture process.
  public class ScanningOptions
  {
    [StructLayout(LayoutKind.Sequential)]
    // Direct memory mapping from C# to cpp is a simpler alternative to
    // serializing to protobuf, passing fields 1 by 1, or calling a native option object with C-style setters.

    // struct wrapped with class to allow explicit initialization parameters.
    internal struct NativeScanningOptions
    {
      [MarshalAs(UnmanagedType.I1)]
      internal bool EnableVoxelVisualization;
      [MarshalAs(UnmanagedType.I1)]
      internal bool EnableRaycastVisualization;

      internal float MaxScanningDistance;
      internal long ScanCaptureInterval;
    }

    internal NativeScanningOptions _nativeScanningOptions;

    /// If true, raycast-based visualizations are enabled. The <see cref="IScanner.VisualizationUpdated"/>
    /// event will contain an <see cref="IRaycastBuffer"/>.
    public bool EnableRaycastVisualization
    {
      get => _nativeScanningOptions.EnableRaycastVisualization;
      set => _nativeScanningOptions.EnableRaycastVisualization = value;
    }

    /// If true, voxel-based visualizations are enabled. The <see cref="IScanner.VisualizationUpdated"/> event
    /// will contain an <see cref="IVoxelBuffer"/> that can be used to render voxel-based visualizations.
    public bool EnableVoxelVisualization 
    {
      get => _nativeScanningOptions.EnableVoxelVisualization;
      set => _nativeScanningOptions.EnableVoxelVisualization = value;
    }

    /// Maximum range, in meters, at which the scanner will collect data.
    public float MaxScanningDistance     
    {
      get => _nativeScanningOptions.MaxScanningDistance;
      set => _nativeScanningOptions.MaxScanningDistance = Mathf.Clamp(value, 0.1f, 5.0f);
    }
    
    /// Constructs scanning options.
    /// @param enableRaycastVisualization If true, raycast-based visualizations will be enabled.
    /// @param enableVoxelVisualization If true, voxel-based visualizations will be enabled.
    /// @param maxScanningDistance Maximum range, in meters, at which the scanner will collect data. This should
    ///        be set to values between 0.1 and 5.0 meters and will be clamped to this range if not. When
    ///        capturing scans for VPS activation, this parameter should be set to 5.
    /// @param scanRecordFps The rate at which images in the scan will be recorded, expressed in frames per second.
    ///        This should be set to a value between 1 and 15 and will be clamped to this range if not.
    ///        When capturing scans for VPS activation, this parameter should be set to 15 fps. In other cases,
    ///        a lower frame rate such as 3 can lead to faster reconstruction without loss of quality.
    public ScanningOptions(bool enableRaycastVisualization = true, bool enableVoxelVisualization = true,
      float maxScanningDistance = 5, int scanRecordFps = 3)
    {
      _nativeScanningOptions = default;
      _nativeScanningOptions.EnableRaycastVisualization = enableRaycastVisualization;
      _nativeScanningOptions.EnableVoxelVisualization = enableVoxelVisualization;
      if (maxScanningDistance < 0.1 || maxScanningDistance > 5)
      {
        ARLog._WarnRelease($"Max scanning distance must be between 0.1 and 5, but got: {maxScanningDistance}");
        maxScanningDistance = Mathf.Clamp(maxScanningDistance, 0.1f, 5);
      }
      _nativeScanningOptions.MaxScanningDistance = maxScanningDistance;
      if (scanRecordFps < 1 || scanRecordFps > 15)
      {
        ARLog._WarnRelease($"Scan record FPS must be between 1 and 15, but got: {scanRecordFps}");
        scanRecordFps = Mathf.Clamp(scanRecordFps, 1, 15);
      }
      _nativeScanningOptions.ScanCaptureInterval = (long)10000 / scanRecordFps;
    }
  }

  /// Options for 3D reconstruction of a scene from a scan. 
  public class ReconstructionOptions
  {
    /// The mode to use for reconstruction. We currently support two modes suitable for different use cases. 
    public enum ReconstructionMode
    {
      /// This mode is recommended for larger scenes and cases where surfaces without texture (such as empty walls) 
      /// need to be handled.
      Area = 0,
      /// This mode is recommended for scanning objects and smaller scenes.
      Detail = 1,
    }

    /// Quality levels. These allow the caller to choose ta tradeoff between speed and quality in reconstruction.
    /// See <see cref="MeshQuality"/> and <see cref="TextureQuality"/> for more information.
    public enum Quality
    {
      Low = 0,
      Medium = 1,
      High = 2,
      VeryHigh = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeReconstructionOptions
    {
      internal ReconstructionMode Mode;
      internal Quality MeshQuality;
      internal Quality TextureQuality;
    }

    internal NativeReconstructionOptions _nativeReconstructionOptions;

    /// The mode to use when generating a textured mesh from this scan.
    public ReconstructionMode Mode
    {
      get => _nativeReconstructionOptions.Mode;
      set => _nativeReconstructionOptions.Mode = value;
    }

    /// Quality level to use when building the mesh. Higher quality levels will produce a mesh with greater geometric
    /// detail, but will take more time and memory to process and require more resources to render and store. In
    /// general, this setting will have a larger impact on processing speed than <see cref="TextureQuality"/>. 
    public Quality MeshQuality
    {
      get => _nativeReconstructionOptions.MeshQuality;
      set => _nativeReconstructionOptions.MeshQuality = value;
    }

    /// Quality level to use when generating the texture for the mesh. Higher quality levels will produce a sharper,
    /// more accurate texture, but will take more time and memory to process and require more resources to render and
    /// store. In general, this setting will have a smaller impact on processing speed than <see cref="MeshQuality"/>.
    public Quality TextureQuality
    {
      get => _nativeReconstructionOptions.TextureQuality;
      set => _nativeReconstructionOptions.TextureQuality = value;
    }

    /// Constructs reconstruction options
    /// @param mode mode for reconstruction (see <see cref="ReconstructionMode"/>).
    /// @param meshQuality mesh quality for reconstruction (see <see cref="MeshQuality"/>).
    /// @param textureQuality texture quality for reconstruction (see <see cref="TextureQuality"/>).
    public ReconstructionOptions(ReconstructionMode mode = ReconstructionMode.Area,
      Quality meshQuality = Quality.High, Quality textureQuality = Quality.High)
    {
      _nativeReconstructionOptions = default;
      _nativeReconstructionOptions.Mode = mode;
      _nativeReconstructionOptions.MeshQuality = meshQuality;
      _nativeReconstructionOptions.TextureQuality = textureQuality;
    }
  }

  /// Performs 3D scanning and reconstruction.
  public interface IScanner
  {
    /// The scanning process can be represented as a state machine. Some methods on the
    /// scanner will only have an effect if called in specific states (described below).
    /// 
    /// In a typical scan without errors, the scanner will go through the following states:
    ///    - Initializing
    ///    - Ready
    ///    - Scanning
    ///    - ScanCompleted
    ///    - Processing
    ///    - Done
    public enum State
    {
      /// Initial state of the scanner upon construction. It will automatically transition to the
      /// *Ready* state once the AR session and depth are initialized. 
      Initializing = 0,

      /// The scanner is ready to scan. From this state:
      ///    - <see cref="StartScanning"/> can be called to begin scanning.
      Ready = 1,
      
      /// The scanner is currently scanning. In this state, the <see cref="VisualizationUpdated"/> event
      /// will repeatedly fire with updated visualization data. From this state:
      ///    - <see cref="PauseScanning"/> can be called to temporarily pause the scanner, transitioning
      ///      to the *Paused* state.
      ///    - <see cref="StopScanning"/> can be called to end the scan, transitioning to the *ScanCompleted* state. 
      Scanning = 2,
      
      /// Scanning is paused. From this state:
      ///    - <see cref="ResumeScanning"/> can be called to continue scanning, transitioning to the *Scanning* state.
      ///    - <see cref="StopScanning"/> can be called to end the scan, transitioning to the *ScanCompleted* state.
      Paused = 9,

      /// The scan has completed and is ready to be processed. From this state:
      ///    - <see cref="StartProcessing"/> can be called to begin processing the scan, transitioning to the
      ///      *Processing* state.
      ///    - <see cref="Restart"/> can be called to discard the scan and reset to the *Ready* state.
      ScanCompleted = 3,

      /// The scan is currently being processed. From this state:
      ///    - Processing may complete successfully and the scanner will transition to the *Done* state.
      ///    - Processing may fail, and the scanner will transition to the *Error* state.
      ///    - <see cref="CancelProcessing"/> can be called to cancel processing and transition to the *Cancelling* state.
      Processing = 4,

      /// Scan processing has completed successfully. Immediately after transitioning to this state, the
      /// <see cref="ScanProcessed"/> event will fire with the resulting mesh. From this state:
      ///    - <see cref="Restart"/> can be called to reset the scanner to the *Ready* state.
      Done = 6,

      /// Scan processing is being cancelled. During cancellation, the scanner will spend a short period of
      /// time in this state before transitioning to the *Cancelled* state.
      Cancelling = 5,

      /// Scan processing has been cancelled. From this state:
      ///    - <see cref="Restart"/> can be called to reset the scanner to the *Ready* state.
      Cancelled = 7,

      /// Scan processing has failed. From this state:
      ///    - <see cref="Restart"/> can be called to reset the scanner to the *Ready* state.
      Error = 8,
    }
    
    /// Returns the current state of the scanner. See <see cref="State"/> for details on the scanner
    /// states and transitions between them.
    State GetState();

    /// Returns the current progress of scan processing, as a value between 0 and 1.
    float GetProcessingProgress();

    /// Returns a unique ID for the current scan. The scan ID is valid after the scan has started (i.e. once
    /// the scanner has entered the <see cref="State.Scanning">Scanning</see> state).
    string GetScanId();

    /// Start scanning with the given options.
    ///    - This can be called in the <see cref="State.Ready">Ready</see> state.
    ///    - The scanner will transition to the <see cref="State.Scanning">Scanning</see> state. 
    void StartScanning(ScanningOptions scanningOptions);

    /// Ends the current scan.
    ///    - This can be called in the <see cref="State.Scanning">Scanning</see> or <see cref="State.Paused">Paused</see>
    ///      states.
    ///    - The scanner will transition to the <see cref="State.ScanCompleted">ScanCompleted</see> state.
    void StopScanning();

    /// Temporarily pauses a scan that is in progress.
    /// 
    /// The scan can be resumed later by calling <see cref="ResumeScanning"/>.
    ///    - This can be called in the <see cref="State.Scanning">Scanning</see> state.
    ///    - The scanner will transition to the <see cref="State.Paused">Paused</see> state.
    void PauseScanning();

    /// Resumes a scan that has been paused.
    ///    - This can be called in the <see cref="State.Paused">Paused</see> state.
    ///    - The scanner will transition to the <see cref="State.Scanning">Scanning</see> state.
    void ResumeScanning();

    /// Starts processing a scan.
    ///
    /// Processing generates a <see cref="TexturedMesh"/> from the raw scan data. Depending on the options and the,
    /// processing capabilities of the user's device, this may take anywhere from a few seconds to several minutes. 
    ///    - This can be called in the <see cref="State.ScanCompleted">ScanCompleted</see> state.
    ///    - The scanner will transition to the <see cref="State.Processing">Processing</see> state.
    /// 
    /// The caller should listen for the <see cref="ScanProcessed"/> event to receive the <see cref="TexturedMesh"/>
    /// when processing is complete.
    ///
    /// @param reconstructionOptions options controlling the reconstruction of the scan
    void StartProcessing(ReconstructionOptions reconstructionOptions);

    /// Cancel processing that is currently in progress.
    ///    - This can be called in the <see cref="State.Processing">Processing</see> state.
    ///    - The scanner will transition to the <see cref="State.Cancelling">Cancelling</see> state.
    ///    - When cancellation is complete (typically within a few seconds), the scanner will transition to
    ///      the <see cref="State.Cancelled">Cancelled</see> state. 
    void CancelProcessing();

    /// Reset the scanner for another scan.
    ///    - This can be called in the <see cref="State.ScanCompleted">ScanCompleted</see>,
    ///      <see cref="State.Done">Done</see>, <see cref="State.Error">Error</see>, or
    ///      <see cref="State.Cancelled">Cancelled</see> states.
    ///    - The scanner will transition to the <see cref="State.Ready">Ready</see> state.
    void Restart();

    /// Set the scan target ID for the current scan. You must call this method prior to saving the scan if you
    /// intend to upload it for VPS activation.
    /// 
    /// A scan target represents a location that can be scanned and activated for VPS. You can find scan targets
    /// near the user and obtain their IDs using <see cref="IScanTargetClient"/>.
    ///
    /// Calling <see cref="Restart"/> will clear the scan target ID.
    ///  
    /// @param scanTargetId the ID of the scan target for the current scan 
    void SetScanTargetId(string scanTargetId);

    /// Returns the scan store. This can be used to save and load scans.
    IScanStore GetScanStore();

    /// Returns a scan quality classifier.
    IScanQualityClassifier GetScanQualityClassifier();

    /// Arguments to the <see cref="StateChanged"/> event.
    public readonly struct StateChangedArgs : IArdkEventArgs
    {
      /// The new state of the scanner.
      public readonly State NewState;

      internal StateChangedArgs(State newState)
      {
        this.NewState = newState;
      }
    }

    /// Invoked when the state of the scanner changes. See <see cref="State"/> for more information on
    /// the scanner states and transitions between them.
    public event ArdkEventHandler<StateChangedArgs> StateChanged;

    /// Arguments to the <see cref="VisualizationUpdated"/> event.
    public readonly struct VisualizationUpdatedArgs : IArdkEventArgs
    {
      /// Buffer containing voxel data. This is only present if EnableVoxelVisualization was true in the
      /// options passed to StartScanning.
      public readonly IVoxelBuffer VoxelBuffer;

      /// Buffer containing voxel data. This is only present if EnableRaycastVisualization was true in the
      /// options passed to StartScanning.
      public readonly IRaycastBuffer RaycastBuffer;

      internal VisualizationUpdatedArgs(IVoxelBuffer voxelBuffer, IRaycastBuffer raycastBuffer)
      {
        this.VoxelBuffer = voxelBuffer;
        this.RaycastBuffer = raycastBuffer;
      }
    }

    /// Invoked when the scanning visualization is updated.
    public event ArdkEventHandler<VisualizationUpdatedArgs> VisualizationUpdated;

    /// Arguments to the <see cref="ScanProcessed"/> event.
    public readonly struct ScanProcessedArgs : IArdkEventArgs
    {
      /// The reconstructed mesh, along with the texture to apply to the mesh.
      public readonly TexturedMesh TexturedMesh;

      /// The position of the center of the mesh in world coordinates.
      public readonly Vector3 Center;

      internal ScanProcessedArgs(TexturedMesh texturedMesh, Vector3 center)
      {
        this.TexturedMesh = texturedMesh;
        this.Center = center;
      }
    }

    /// Invoked when the scan has successfully finished processing.
    public event ArdkEventHandler<ScanProcessedArgs> ScanProcessed;
  }
}
