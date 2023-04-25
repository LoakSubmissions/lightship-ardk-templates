using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.Internals;

namespace Niantic.Experimental.ARDK.SharedAR {

public interface IVpsColocalizerNativeInterface {
  delegate void ColocalizationStateChangedCallbackType(
    IntPtr application_handle, byte state, byte failure_code, byte error_code);

  IntPtr VpsColocalizer_Initialize(byte[] stageIdentifier,
                                   IntPtr networkingHandle,
                                   byte[] data,
                                   ulong dataSize);

  void VpsColocalizer_Release(IntPtr nativeHandle);

  void VpsColocalizer_StartColocalization(IntPtr nativeHandle);

  void VpsColocalizer_GetAlignedSpaceOrigin(IntPtr nativeHandle, float[] outPose);

  byte VpsColocalizer_AlignedPoseToLocal(
    IntPtr nativeHandle, float[] alignedPose, float[] outPose);

  byte VpsColocalizer_LocalPoseToAligned(
    IntPtr nativeHandle, float[] localPose, float[] outPose);

  void VpsColocalizer_SetColocalizationStateCallback(
      IntPtr applicationHandle,
      IntPtr nativeHandle,
      ColocalizationStateChangedCallbackType callback);
}

public class NarVpsColocalizerNativeAPI : IVpsColocalizerNativeInterface {
  public IntPtr VpsColocalizer_Initialize(byte[] stageIdentifier,
                                          IntPtr networkingHandle,
                                          byte[] data,
                                          ulong dataSize) {
    return _NARVpsColocalizer_Initialize(stageIdentifier, networkingHandle, data, dataSize);
  }

  public void VpsColocalizer_Release(IntPtr nativeHandle) {
    _NARVpsColocalizer_Release(nativeHandle);
  }

  public void VpsColocalizer_StartColocalization(IntPtr nativeHandle) {
    _NARVpsColocalizer_StartColocalization(nativeHandle);
  }

  public void VpsColocalizer_GetAlignedSpaceOrigin(IntPtr nativeHandle, float[] outPose) {
    _NARVpsColocalizer_GetAlignedSpaceOrigin(nativeHandle, outPose);
  }

  public byte VpsColocalizer_AlignedPoseToLocal(
    IntPtr nativeHandle, float[] alignedPose, float[] outPose) {
      return _NARVpsColocalizer_AlignedPoseToLocal(nativeHandle, alignedPose, outPose);
  }

  public byte VpsColocalizer_LocalPoseToAligned(
    IntPtr nativeHandle, float[] localPose, float[] outPose) {
    return _NARVpsColocalizer_LocalPoseToAligned(nativeHandle, localPose, outPose);
  }

  public void VpsColocalizer_SetColocalizationStateCallback(
      IntPtr applicationHandle,
      IntPtr nativeHandle,
      IVpsColocalizerNativeInterface.ColocalizationStateChangedCallbackType callback) {
    _NARVpsColocalizer_SetColocalizationStateCallback(applicationHandle, nativeHandle, callback);
  }

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern IntPtr _NARVpsColocalizer_Initialize(byte[] stageIdentifier,
                                                             IntPtr networkingHandle,
                                                             byte[] data,
                                                             ulong dataSize);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern void _NARVpsColocalizer_Release(IntPtr nativeHandle);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern void _NARVpsColocalizer_StartColocalization(IntPtr nativeHandle);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern void _NARVpsColocalizer_GetAlignedSpaceOrigin(IntPtr nativeHandle, float[] outPose);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern byte _NARVpsColocalizer_AlignedPoseToLocal(
    IntPtr nativeHandle, float[] alignedPose, float[] outPose);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern byte _NARVpsColocalizer_LocalPoseToAligned(
    IntPtr nativeHandle, float[] localPose, float[] outPose);

  [DllImport(_ARDKLibrary.libraryName)]
  private static extern void _NARVpsColocalizer_SetColocalizationStateCallback(
      IntPtr applicationHandle,
      IntPtr nativeHandle,
      IVpsColocalizerNativeInterface.ColocalizationStateChangedCallbackType callback);
};

}
