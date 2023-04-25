using System;
using System.Runtime.InteropServices;
using AOT;
using Niantic.Titan.Uploader.Internal;
using UnityEngine;

namespace Niantic.Titan.Uploader {

  /// <summary>
  /// Hooks into logging events on the native side and sends them
  /// to the logging system in the Platform Debugging library.
  /// </summary>
  internal static class NativeLogger {

    #region PInvoke functions

    private static class Native {

      public delegate void LogCallback(ChannelLogger.LogLevel logLevel, string message);

      [DllImport(Constants.LIBRARY_NAME)]
      public static extern void Logger_Initialize(LogCallback logCallback);
    }

    #endregion

    private static ChannelLogger Log { get; } =
      new ChannelLogger(Constants.NATIVE_LOG_CHANNEL);

    private static bool hasInit = false;

    /// <summary>
    /// Initializes the logger and registers a callback that handles native log events.
    /// </summary>
    public static void Initialize() {
      if (!hasInit) {
        hasInit = true;
        Native.Logger_Initialize(HandleNativeLog);
      }
    }

    /// <summary>
    /// Disables the log handler for events raised from the native components.
    /// Disabling these logs suppresses some spam and should improve performance.
    /// </summary>
    public static void Disable() {
      Native.Logger_Initialize(null);
    }

    [MonoPInvokeCallback(typeof(Native.LogCallback))]
    private static void HandleNativeLog(ChannelLogger.LogLevel logPriority, string message) {
      Log.LogMessage(logPriority, message);
    }
  }
}