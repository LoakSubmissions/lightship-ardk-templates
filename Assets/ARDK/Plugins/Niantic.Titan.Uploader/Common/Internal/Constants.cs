using System;

namespace Niantic.Titan.Uploader.Internal {

  internal static class Constants {

    /// <summary>
    /// The name of the native library called into using PInvoke
    /// </summary>
#if UNITY_EDITOR || UNITY_STANDALONE
    public const string LIBRARY_NAME = "geouploader";
#elif UNITY_IOS
    // On iOS, plugins are statically linked into the executable,
    // so we have to use __Internal as the library name.
    public const string LIBRARY_NAME = "__Internal";
#elif UNITY_ANDROID
    public const string LIBRARY_NAME = "geouploader";
#else
#error "Unsupported platform"
    public const string LIBRARY_NAME = null;
#endif

    /// <summary>
    /// Name of the channel used for logging through the Platform Debugging library
    /// </summary>
    public const string LOG_CHANNEL = "Uploader";

    /// <summary>
    /// Name of the channel used for logging events from the native layer
    /// </summary>
    public const string NATIVE_LOG_CHANNEL = "UploaderNative";

    /// <summary>
    /// Name of the channel used for logging application lifecycle events
    /// </summary>
    public const string LIFECYCLE_LOG_CHANNEL = "UploaderLifecycle";

#if UPLOADER_TESTS_ENABLED
    public static readonly bool NATIVE_TESTS_ENABLED = true;
#else
    public static readonly bool NATIVE_TESTS_ENABLED = false;
#endif

    public const uint DEFAULT_CHUNK_UPLOAD_THREAD_COUNT = 8;
  }
}