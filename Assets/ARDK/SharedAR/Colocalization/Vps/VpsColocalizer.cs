using System;
using UnityEngine;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.WayspotAnchors;

using AOT; // MonoPInvokeCallbackAttribute

namespace Niantic.Experimental.ARDK.SharedAR {

public class VpsColocalizer : IDisposable {
  public enum FailureCode {
    None = 0,
    Unknown = 1,
    VpsDependencyMissing = 2,
    DatastoreDependencyMissing = 3,

    // VpsDependencyFailure has associated ErrorCode from vps of type LocalizationFailureReason
    VpsDependencyFailure = 4,
  }

  private IVpsColocalizerNativeInterface _native;
  private IntPtr _nativeHandle;
  private readonly float[] _reusableMatrix;
  private readonly object _reusableMatrixLock;
  private SafeGCHandle<VpsColocalizer> _pinnedThis;

  private const long GCPressure = 64L * 1024L;

  public VpsColocalizer(IVpsColocalizerNativeInterface colocalizerInterface = null) {
    if (colocalizerInterface == null) {
      colocalizerInterface = new NarVpsColocalizerNativeAPI();
    }

    _native = colocalizerInterface;
    _reusableMatrix = new float[16];
    _reusableMatrixLock = new object();
  }

  public void Dispose() {
    if (_native != null && _nativeHandle != IntPtr.Zero) {
      _native.VpsColocalizer_Release(_nativeHandle);
      GC.RemoveMemoryPressure(GCPressure);
      _pinnedThis.Free();
    }
    _native = null;
  }

  public void Initialize(
    INetworking networking, IARSession arSessionForWayspotAnchors, WayspotAnchorPayload content = null) {
      if (networking is INativeNetworking nativeNeworking)
      {
        _nativeHandle = (content != null)
          ? _native.VpsColocalizer_Initialize(
            arSessionForWayspotAnchors.StageIdentifier.ToByteArray(),
            nativeNeworking.GetNativeHandle(),
            content._Blob,
            (ulong)content._Blob.Length
          )
          : _native.VpsColocalizer_Initialize(
            arSessionForWayspotAnchors.StageIdentifier.ToByteArray(),
            nativeNeworking.GetNativeHandle(),
            null,
            0
          );

        if (_nativeHandle != IntPtr.Zero)
        {
          GC.AddMemoryPressure(GCPressure);
          _pinnedThis = SafeGCHandle.Alloc(this);
        }
        else
        {
          ARLog._Error("Failed to create NativeVpsColocalizer");
        }
      }
      else
      {
        ARLog._Error("Can only use NativeVPSColocalization with NativeNetworking for now");
      }
  }

  public void StartColocalization() {
    _native.VpsColocalizer_StartColocalization(_nativeHandle);
  }

  public Matrix4x4 GetAlignedSpaceOrigin() {
    lock (_reusableMatrixLock) {
      _native.VpsColocalizer_GetAlignedSpaceOrigin(_nativeHandle, _reusableMatrix);
    }

    return NARConversions.FromNARToUnity(_Convert.InternalToMatrix4x4(_reusableMatrix));
  }

  public bool AlignedPoseToLocal(
    Matrix4x4 alignedPose, out Matrix4x4 localPose) {
    var poseArray = _Convert.Matrix4x4ToInternalArray
      (NARConversions.FromUnityToNAR(alignedPose));

    byte alignmentCode = 0;
    lock (_reusableMatrixLock) {
      alignmentCode = _native.VpsColocalizer_AlignedPoseToLocal(
        _nativeHandle, poseArray, _reusableMatrix);
      localPose = NARConversions.FromNARToUnity(
        _Convert.InternalToMatrix4x4(_reusableMatrix));
    }

    return alignmentCode == 1;
  }

  public bool LocalPoseToAligned(
    Matrix4x4 localPose, out Matrix4x4 alignedPose) {
    var poseArray = _Convert.Matrix4x4ToInternalArray
      (NARConversions.FromUnityToNAR(localPose));

    byte alignmentCode = 0;
    lock (_reusableMatrixLock) {
      alignmentCode = _native.VpsColocalizer_LocalPoseToAligned(
        _nativeHandle, poseArray, _reusableMatrix);
      alignedPose = NARConversions.FromNARToUnity(
        _Convert.InternalToMatrix4x4(_reusableMatrix));
    }

    return alignmentCode == 1;
  }

  public class OnColocalizationStateChangedArgs : IArdkEventArgs
  {
    public ColocalizationState state;
    public FailureCode failureCode;
    public byte errorCode;
  }

  private event ArdkEventHandler<OnColocalizationStateChangedArgs> state_changed_event_handler_;
  public event ArdkEventHandler<OnColocalizationStateChangedArgs> OnColocalizationStateChangedEvent
  {
    add
    {
      state_changed_event_handler_ += value;
      _native.VpsColocalizer_SetColocalizationStateCallback(
        _pinnedThis.ToIntPtr(), _nativeHandle, ColocalizationStateChangedCallback);
    }
    remove
    {
      state_changed_event_handler_ -= value;
    }
  }

  [MonoPInvokeCallback(typeof(IVpsColocalizerNativeInterface.ColocalizationStateChangedCallbackType))]
  private static void ColocalizationStateChangedCallback(
    IntPtr application_handle, byte state, byte failure_code, byte error_code) {

    var instance = SafeGCHandle.TryGetInstance<VpsColocalizer>(application_handle);

    if (instance == null || instance._native == null)
      return;

    _CallbackQueue.QueueCallback(
      () => {
        if (instance == null || instance._native == null) {
          ARLog._Warn(
            "Queued _failureReasonCallbackNative invoked after C# instance was destroyed."
          );

          return;
        }

        var handler = instance.state_changed_event_handler_;
        if (handler != null) {
          var args = new OnColocalizationStateChangedArgs();
          args.state = (ColocalizationState)state;
          args.failureCode = (FailureCode)failure_code;
          args.errorCode = error_code;
          handler(args);
        }
      }
    );
  }
}

}
