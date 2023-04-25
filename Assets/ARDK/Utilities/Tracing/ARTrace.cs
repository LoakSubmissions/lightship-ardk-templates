using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;

namespace Niantic.ARDK.Utilities.Tracing
{
  internal sealed class _NativeARDKTrace
  {
    public void StartTracing()
    {
      _NAR_START_CTRACE();
    }

    public void StopTracing()
    {
      _NAR_STOP_CTRACE();
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_START_CTRACE();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_STOP_CTRACE();
  }

  public static class ARDKTrace
  {
    private static _NativeARDKTrace _impl;

    private static _NativeARDKTrace _Impl
    {
      get
      {
        return _impl ??= new _NativeARDKTrace();
      }
    }

    /// <summary>
    /// Start receiving ARDK tracing messages.
    /// traces will be dumped into a "ardk_trace.log" file under application directory.
    /// </summary>
    public static void StartTracing()
    {
      if (_NativeAccess.IsNativeAccessValid())
        _Impl.StartTracing();
    }

    /// <summary>
    /// Stop receiving ARDK tracing messages.
    /// </summary>
    public static void StopTracing()
    {
      if (_NativeAccess.IsNativeAccessValid())
        _Impl.StopTracing();
    }
  }
}


