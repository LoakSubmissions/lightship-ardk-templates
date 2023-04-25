// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Scanning;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

namespace Niantic.ARDK.Extensions.Scanning
{
  /// The ARScanManager can be placed in a scene to easily manage scanning, visualization, and reconstruction.
  public class ARScanManager: ARSessionListener
  {
    /// Returns the current state of the scanner. See <see cref="IScanner.State"/> for details on scanner states
    /// and the transitions between them.
    public IScanner.State ScannerState { get => _scanner?.GetState() ?? IScanner.State.Initializing; }

    private IScanVisualizer visualizer;
    private IScanner _scanner;
    private IScanQualityClassifier _scanQualityClassifier;

    /// The mode used to generate the textured mesh from the scanning data.
    public ReconstructionOptions.ReconstructionMode reconstructionMode;

    /// Quality level to use when building the mesh. Higher quality levels will produce a mesh with greater geometric
    /// detail, but will take more time and memory to process and require more resources to render and store. In
    /// general, this setting will have a larger impact on processing speed than <see cref="textureQuality"/>. 
    public ReconstructionOptions.Quality meshQuality;

    /// Quality level to use when generating the texture for the mesh. Higher quality levels will produce a sharper,
    /// more accurate texture, but will take more time and memory to process and require more resources to render and
    /// store. In general, this setting will have a smaller impact on processing speed than <see cref="meshQuality"/>.
    public ReconstructionOptions.Quality textureQuality;

    /// Maximum range, in meters, at which the scanner will collect data. This should
    /// be set to values between 0.1 and 5.0 meters and will be clamped to this range if not. When
    /// capturing scans for VPS activation, this parameter should be set to 5.
    public float maxScanDistance = 5f;

    /// The rate at which images in the scan will be recorded, expressed in frames per second. This should be set to a
    /// value between 1 and 15 and will be clamped to this range if not. When capturing scans for VPS activation,
    /// this parameter should be set to 15 fps. For other cases, a lower FPS such as 3 can lead to a faster
    /// reconstruction without loss of quality.
    public int scanRecordFps = 15;

    /// Whether to record location data in the scan. When capturing scans for VPS activation, this must be true.
    public bool recordLocation = true;
    private ILocationService _locationService;


    protected override void ListenToSession()
    {
      _scanner = ScannerFactory.Create(ARSession, Application.persistentDataPath);
      _scanner.VisualizationUpdated += (IScanner.VisualizationUpdatedArgs args) =>
      {
        visualizer?.OnScanProgress(args.VoxelBuffer, args.RaycastBuffer);
      };
      _scanQualityClassifier = _scanner.GetScanQualityClassifier();

      _scanner.ScanProcessed += (IScanner.ScanProcessedArgs args) =>
      {
        ScanProcessed?.Invoke(args);
      };
      if (recordLocation)
      {
        _locationService = LocationServiceFactory.Create(ARSession.RuntimeEnvironment);
        _locationService.Start();
      }
      ARSession.SetupLocationService(_locationService);
    }

    protected override void StopListeningToSession()
    {
      _scanner = null;
    }

    /// Set a visualizer to show scans in progress.
    /// Visualizers will be fed with voxel or raycast data as they need.
    /// This should be called before <see cref="StartScanning"/>.
    /// @param newVisualizer the visualizer to use
    public void SetVisualizer(IScanVisualizer newVisualizer)
    {
      if (visualizer != null)
      {
        visualizer.SetVisualizationActive(false);
      }
      visualizer = newVisualizer;
      if (_scanner != null)
      {
        newVisualizer.SetVisualizationActive(_scanner.GetState() == IScanner.State.Scanning);
      }
    }

    /// Start scanning the scene.
    ///    - This can be called in the <see cref="IScanner.State.Ready">Ready</see> state.
    ///    - The scanner will transition to the <see cref="IScanner.State.Scanning">Scanning</see> state.
    ///
    /// If <see cref="SetVisualizer"/> has been called, this will also activate the visualization.  
    public void StartScanning()
    {
      Screen.sleepTimeout = SleepTimeout.NeverSleep;
      bool enableRaycastVisualization = false;
      bool enableVoxelVisualization = false;
      if (visualizer != null)
      {
        visualizer.SetVisualizationActive(true);
        enableRaycastVisualization = visualizer.RequiresRaycastData();
        enableVoxelVisualization = visualizer.RequiresVoxelData();
      }
      _scanner?.StartScanning(new ScanningOptions(enableRaycastVisualization, enableVoxelVisualization, maxScanDistance, scanRecordFps));
    }

    /// Temporarily pauses a scan that is in progress.
    /// 
    /// The scan can be resumed later by calling <see cref="ResumeScanning"/>.
    ///    - This can be called in the <see cref="IScanner.State.Scanning">Scanning</see> state.
    ///    - The scanner will transition to the <see cref="IScanner.State.Paused">Paused</see> state.
    public void PauseScanning()
    {
      ARLog._Debug("ARScanManager:PauseScanning");
      _scanner.PauseScanning();
      if (visualizer != null)
      {
        visualizer.SetVisualizationActive(false);
      }
    }

    /// Resumes a scan that has been paused.
    ///    - This can be called in the <see cref="IScanner.State.Paused">Paused</see> state.
    ///    - The scanner will transition to the <see cref="IScanner.State.Scanning">Scanning</see> state.
    public void ResumeScanning()
    {
      ARLog._Debug("ARScanManager:ResumeScanning");
      _scanner.ResumeScanning();
      if (visualizer != null)
      {
        visualizer.ClearCurrentVisualizationState();
        visualizer.SetVisualizationActive(true);
      }
    }

    /// Ends the scan.
    ///    - This can be called in the <see cref="IScanner.State.Scanning">Scanning</see>
    ///      or <see cref="IScanner.State.Paused">Paused</see> states.
    ///    - The scanner will transition to the <see cref="IScanner.State.ScanCompleted">ScanCompleted</see> state.
    public void StopScanning()
    {
      ARLog._Debug("ARScanManager:StopScanning");
      Screen.sleepTimeout = SleepTimeout.SystemSetting;
      _scanner?.StopScanning();
      if (visualizer != null)
      {
        visualizer.SetVisualizationActive(false);
      }
    }

    /// Starts processing a scan.
    ///
    /// Processing generates a <see cref="TexturedMesh"/> from the raw scan data. Depending on the options and the,
    /// processing capabilities of the user's device, this may take anywhere from a few seconds to several minutes. 
    ///    - This can be called in the <see cref="IScanner.State.ScanCompleted">ScanCompleted</see> state.
    ///    - The scanner will transition to the <see cref="IScanner.State.Processing">Processing</see> state.
    public void StartProcessing()
    {
      ARLog._Debug("ARScanManager:StartProcessing scan: " + _scanner.GetScanId());
      _scanner?.StartProcessing(new ReconstructionOptions(reconstructionMode, meshQuality, textureQuality));
    }

    /// Cancel processing a scan.
    ///    - This can be called in the <see cref="IScanner.State.Processing">Processing</see> state.
    ///    - The scanner will transition to the <see cref="IScanner.State.Cancelling">Cancelling</see> state.
    ///    - Within a few seconds, it will transition to the <see cref="IScanner.State.Cancelled">Cancelled</see> state. 
    public void CancelProcessing()
    {
      ARLog._Debug("ARScanManager:CancelProcessing");
      _scanner?.CancelProcessing();
    }

    /// Saves a processed scan.
    ///    - This can be called in the <see cref="IScanner.State.Done">Done</see> state.
    public void SaveCurrentScan()
    {
      _scanner.GetScanStore().SaveCurrentScan(_scanner.GetScanId());
    }

    /// Returns a unique ID for the current scan. The scan ID is valid once the scan has started (once
    /// the scanner has entered the <see cref="IScanner.State.Scanning">Scanning</see> state).
    public string GetScanId()
    {
      return _scanner.GetScanId();
    }

    /// Reset the scanner for another scan.
    ///    - This can be called in the <see cref="IScanner.State.ScanCompleted">ScanCompleted</see>,
    ///      <see cref="IScanner.State.Done">Done</see>, <see cref="IScanner.State.Error">Error</see>, or
    ///      <see cref="IScanner.State.Cancelled">Cancelled</see> states.
    ///    - The scanner will transition to the <see cref="IScanner.State.Ready">Ready</see> state.
    public void Restart()
    {
      _scanner?.Restart();
      if (visualizer != null)
      {
        // We might get here if visualizer is active and an error occurs.
        visualizer.SetVisualizationActive(false);
      }
    }

    /// Returns the current progress of scan processing, as a value between 0 and 1. This is only available in the
    /// <see cref="State.Processing">Processing</see> state.
    public float GetScanProgress()
    {
      return _scanner.GetProcessingProgress();
    }

    /// Uploads the given scan to Niantic for VPS activation. The scan target ID must be previously set.
    /// @param scanId the ID of the scan to upload
    /// @param onProgress Callback invoked periodically during upload to indicate progress (between 0 and 1).
    /// @param onResult Callback invoked when upload completes or fails. The first argument is true on success
    ///        and false on failure; the second argument describes the error in the failure case.
    public void UploadScan(string scanId, Action<float> onProgress, Action<bool, string> onResult)
    {
      _scanner.GetScanStore().UploadScan(scanId, new IScanStore.UploadUserInfo(), onProgress, onResult);
    }

    /// Given a scan ID, returns the quality estimation of the scan for VPS activation.
    /// @param scanId the ID of the scan
    /// @param onResult Invoked with the <see cref="ScanQualityResult"/> when the quality has been computed
    public void GetScanQuality(string scanId, Action<ScanQualityResult> onResult)
    {
      _scanQualityClassifier.ComputeScanQuality(scanId, onResult);
    }

    /// Set the current scan target for VPS activation. You must call this method prior to saving the scan if you
    /// intend to upload it for VPS activation.
    /// 
    /// A scan target represents a location that can be scanned and activated for VPS. You can find scan targets
    /// near the user and obtain their IDs using <see cref="IScanTargetClient"/>.
    ///
    /// Calling <see cref="Restart"/> will clear the scan target ID.
    ///  
    /// @param scanTargetId the ID of the scan target for the current scan 
    public void SetScanTargetId(string scanTargetId)
    {
      _scanner?.SetScanTargetId(scanTargetId);
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration config)
      {
        config.IsDepthEnabled = true;
        config.IsScanQualityEnabled = true;
      }
    }

    /// Delete a saved scan.
    /// @param scanId the ID of the scan to delete
    public void DeleteScan(string scanId)
    {
      _scanner.GetScanStore().DeleteSavedScan(scanId);
    }

    /// Returns all the IDs of previously saved scans.
    public List<string> GetSavedScans()
    {
      return _scanner.GetScanStore().GetScanIDs();
    }

    /// Returns the SavedScan for a given ID.
    public SavedScan GetSavedScan(string scanId)
    {
      return _scanner.GetScanStore().GetSavedScan(scanId);
    }
    
    /// Invoked when the scan has finished processing.
    public event ArdkEventHandler<IScanner.ScanProcessedArgs> ScanProcessed;
  }
}
