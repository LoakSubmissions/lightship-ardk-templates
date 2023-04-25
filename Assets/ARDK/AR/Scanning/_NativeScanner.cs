// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;

using AOT;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Protobuf;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Telemetry;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

using TelemetryOperation = Niantic.ARDK.AR.Protobuf.ScanningFrameworkEvent.Types.Operation;
using TelemetryState = Niantic.ARDK.AR.Protobuf.ScanningFrameworkEvent.Types.State;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _NativeScanner: _ThreadCheckedObject, IScanner
  {
    static _NativeScanner()
    {
      _Platform.Init();
    }

    private IntPtr _nativeHandle;

    private SafeGCHandle<_NativeScanner> _handle;

    private ScanStore _scanStore;

    private ReconstructionOptions _lastReconstructionOptions;
    private NativeScanQualityClassifier _scanQualityClassifier;

    private IARSession _session;

    private _NativeScanner(IntPtr nativeHandle)
    {
      _NativeAccess.AssertNativeAccessValid();
      _nativeHandle = nativeHandle;
      _handle = SafeGCHandle.Alloc(this);
      _Scanner_Set_StateChangeCallback
      (
        _handle.ToIntPtr(),
        _nativeHandle,
        _onStateChange
      );
      _Scanner_Set_VisualizationCallback
      (
        _handle.ToIntPtr(),
        _nativeHandle,
        _onScanVisualizationUpdate
      );
      _Scanner_Set_ProcessedScanCompleteCallback
      (
        _handle.ToIntPtr(),
        _nativeHandle,
        _onScanProcessComplete
      );
    }

    internal _NativeScanner(IARSession session, string dataPathRoot)
    : this(_Scanner_Create(session.StageIdentifier.ToString(), Application.temporaryCachePath,
      Application.identifier + " " + Application.version))
    {
      this._session = session;
      _scanStore = new ScanStore(dataPathRoot, session.RuntimeEnvironment);
    }
    ~_NativeScanner()
    {
      if (_nativeHandle != IntPtr.Zero)
      {
        _Scanner_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
        _handle.Free();
      }
    }

    public IScanner.State GetState()
    {
      _NativeAccess.AssertNativeAccessValid();
      return (IScanner.State) _Scanner_GetState(_nativeHandle);
    }

    public float GetProcessingProgress()
    {
      _NativeAccess.AssertNativeAccessValid();
      return _Scanner_GetProcessingProgress(_nativeHandle);
    }

    public string GetScanId()
    {
      _NativeAccess.AssertNativeAccessValid();
      return GetScanID(_nativeHandle);
    }

    public void StartScanning(ScanningOptions options)
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_StartScanning(_nativeHandle, options._nativeScanningOptions);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Capture, TelemetryState.Started);
    }

    public void PauseScanning()
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_PauseScanning(_nativeHandle);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Capture, TelemetryState.Paused);
    }

    public void ResumeScanning()
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_ResumeScanning(_nativeHandle);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Capture, TelemetryState.Started);
    }

    public void StopScanning()
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_StopScanning(_nativeHandle);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Capture, TelemetryState.Finished);
      SendCaptureTelemetry();
    }

    public void StartProcessing(ReconstructionOptions options)
    {
      _NativeAccess.AssertNativeAccessValid();
      _lastReconstructionOptions = options;
      _Scanner_StartProcessing(_nativeHandle, options._nativeReconstructionOptions);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Process, TelemetryState.Started);
    }

    public void CancelProcessing()
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_CancelProcessing(_nativeHandle);
      _TelemetryService.RecordScanningFrameworkEvent(GetScanId(), TelemetryOperation.Process, TelemetryState.Canceled);
    }

    public void Restart()
    {
      _NativeAccess.AssertNativeAccessValid();
      _Scanner_Restart(_nativeHandle);
    }

    private IRaycastBuffer GetRaycastBuffer()
    {
      _NativeAccess.AssertNativeAccessValid();
      IntPtr handle = _Scanner_GetRaycastBuffer(_nativeHandle);
      return handle == IntPtr.Zero ? null : new _NativeRaycastBuffer(handle);
    }

    private IVoxelBuffer GetVoxelBuffer()
    {
      _NativeAccess.AssertNativeAccessValid();
      IntPtr handle = _Scanner_GetVoxelBuffer(_nativeHandle);
      return handle == IntPtr.Zero ? null : new _NativeVoxelBuffer(handle);
    }

    private _ProcessedScan GetProcessedScan()
    {
      IntPtr handle = _Scanner_GetProcessedScan(_nativeHandle);
      if (handle == IntPtr.Zero)
        return null;
      _ProcessedScan scan = new _ProcessedScan(handle);
      return scan;
    }

    public IScanStore GetScanStore()
    {
      return _scanStore;
    }

    public void SetScanTargetId(string scanTargetId)
    {
      string decodedId = _scanStore.ExtractScanTargetId(scanTargetId);
      _Scanner_SetPoiId(_nativeHandle, decodedId);
    }

    public IScanQualityClassifier GetScanQualityClassifier()
    {
      if (this._scanQualityClassifier == null)
      {
        this._scanQualityClassifier = new NativeScanQualityClassifier(_session, this._scanStore._dataPathRoot);
      }
      return this._scanQualityClassifier;
    }

    /// <inheritdoc />
    public event ArdkEventHandler<IScanner.StateChangedArgs> StateChanged;

    /// <inheritdoc />
    public event ArdkEventHandler<IScanner.VisualizationUpdatedArgs> VisualizationUpdated;

    /// <inheritdoc />
    public event ArdkEventHandler<IScanner.ScanProcessedArgs> ScanProcessed;

    private static string GetScanID(IntPtr _nativeHandle)
    {
      StringBuilder stringBuilder = new StringBuilder(20);
      _Scanner_GetCurrentScanId(_nativeHandle, stringBuilder, 20);
      return stringBuilder.ToString();
    }

    [MonoPInvokeCallback(typeof(_NativeScannerVoidCallback))]
    private static void _onStateChange(IntPtr context, UInt32 state)
    {
      _NativeScanner scanner = SafeGCHandle.TryGetInstance<_NativeScanner>(context);
      if (scanner == null)
      {
        // scanner was deallocated
        return;
      }
      _CallbackQueue.QueueCallback(
        () =>
        {
          // We are not calling native methods here. This is safer and faster.
          scanner.StateChanged?.Invoke(new IScanner.StateChangedArgs((IScanner.State) state));
        });
    }

    [MonoPInvokeCallback(typeof(_NativeScannerVoidCallback))]
    private static void _onScanVisualizationUpdate(IntPtr context)
    {
      _NativeScanner scanner = SafeGCHandle.TryGetInstance<_NativeScanner>(context);
      if (scanner == null)
      {
        // scanner was deallocated
        return;
      }

      IVoxelBuffer voxelBuffer = scanner.GetVoxelBuffer();
      IRaycastBuffer raycastBuffer = scanner.GetRaycastBuffer();
      _CallbackQueue.QueueCallback(
        () =>
        {
          // We are not calling native methods here. This is safer and faster.
          scanner.VisualizationUpdated?.Invoke(new IScanner.VisualizationUpdatedArgs(voxelBuffer, raycastBuffer));
        });
    }

    [MonoPInvokeCallback(typeof(_NativeScannerVoidCallback))]
    private static void _onScanProcessComplete(IntPtr context)
    {
      try
      {
        _NativeScanner scanner = SafeGCHandle.TryGetInstance<_NativeScanner>(context);
        if (scanner == null)
        {
          // scanner was deallocated
          return;
        }
        _CallbackQueue.QueueCallback
        (
          () =>
          {
            _ProcessedScan processedScan = scanner.GetProcessedScan();
            IScanner.State scannerState = scanner.GetState();
            var scanId = scanner.GetScanId();

            if (processedScan == null)
            {
              // Did not process successfully
              _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Process, TelemetryState.Error);
            }
            else
            {
              TexturedMesh texturedMesh = new TexturedMesh(processedScan.GetMesh(), processedScan.GetTexture());
              var center = processedScan.GetCenterPosition();
              scanner._scanStore.SaveCurrentMesh(texturedMesh.mesh, GetScanID(scanner._nativeHandle));
              scanner.ScanProcessed?.Invoke(new IScanner.ScanProcessedArgs(texturedMesh, center));
              _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Process, TelemetryState.Finished);
              scanner.SendProcessTelemetry();
            }
          }
        );
      }
      catch (Exception e)
      {
        ARLog._Exception(e);
      }
    }

    private void SendCaptureTelemetry()
    {
      var isLidar = ARWorldTrackingConfigurationFactory.CheckLidarDepthSupport();
      _TelemetryService.RecordEvent(new ScanCaptureEvent()
      {
        ScanId = GetScanId(),
        DepthType = isLidar ? ScanCaptureEvent.Types.Depth.Lidar : ScanCaptureEvent.Types.Depth.Multidepth,
      });
    }

    private void SendProcessTelemetry()
    {
      var algo = _lastReconstructionOptions == null ? "" : _lastReconstructionOptions.Mode.ToString();
      _TelemetryService.RecordEvent(new ScanProcessEvent()
      {
        ScanId = GetScanId(),
        ReconstructionAlgo = algo
      });
    }

    private delegate void _NativeScannerVoidCallback(IntPtr context);
    private delegate void _NativeScannerUInt32Callback(IntPtr context, UInt32 value);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_Create(string stageUuid, string cachePath, string applicationName);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _Scanner_GetState(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _Scanner_GetProcessingProgress(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_GetProcessedScan(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_GetRaycastBuffer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _Scanner_GetVoxelBuffer(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_StartScanning(IntPtr nativeHandle, ScanningOptions.NativeScanningOptions options);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_PauseScanning(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_ResumeScanning(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_StopScanning(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_StartProcessing(IntPtr nativeHandle, ReconstructionOptions.NativeReconstructionOptions options);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_CancelProcessing(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_Restart(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_SetPoiId(IntPtr nativeHandle, string poiId);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_GetCurrentScanId(IntPtr nativeHandle, StringBuilder result, int length);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_Set_StateChangeCallback
    (
      IntPtr applicationScanner,
      IntPtr platformScanner,
      _NativeScannerUInt32Callback callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_Set_VisualizationCallback
    (
      IntPtr applicationScanner,
      IntPtr platformScanner,
      _NativeScannerVoidCallback callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _Scanner_Set_ProcessedScanCompleteCallback
    (
      IntPtr applicationScanner,
      IntPtr platformScanner,
      _NativeScannerVoidCallback callback
    );
  }
}
