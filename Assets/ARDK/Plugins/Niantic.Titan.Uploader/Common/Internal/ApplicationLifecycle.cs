using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Niantic.Titan.Uploader.Internal {

  /// <summary>
  /// This class exposes events that are raised at specific points in the host
  /// application's lifecycle (eg shutdown, pause/resume, changes in focus, etc.)
  /// </summary>
  internal static class ApplicationLifecycle {

    #region AppLifecycleHook private class

    /// <summary>
    /// Nested private class used to hook lifecycle events that
    /// occur as methods Unity calls on active MonoBehaviours
    /// </summary>
    private class AppLifecycleHook : MonoBehaviour {

      /// <summary>
      /// Called when the application gains or loses focus
      /// </summary>
      /// <param name="hasFocus">Whether the app is focused</param>
      private void OnApplicationFocus(bool hasFocus) {
        OnApplicationFocusChanged(hasFocus);
      }

      /// <summary>
      /// Called when the application is paused or resumed
      /// </summary>
      /// <param name="pauseStatus">Whether the app is paused</param>
      private void OnApplicationPause(bool pauseStatus) {
        OnApplicationPauseChanged(pauseStatus);
      }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the application gains focus
    /// </summary>
    public static event Action ApplicationGainedFocus;

    /// <summary>
    /// Event raised when the application loses focus
    /// </summary>
    public static event Action ApplicationLostFocus;

    /// <summary>
    /// Event raised when the application is paused
    /// </summary>
    public static event Action ApplicationPaused;

    /// <summary>
    /// Event raised when the application is resumed
    /// </summary>
    public static event Action ApplicationUnpaused;

    /// <summary>
    /// Event raised when the application is shutting down
    /// </summary>
    public static event Action ApplicationIsShuttingDown;

    /// <summary>
    /// Event raised when the application is unloading
    /// </summary>
    public static event Action ApplicationIsUnloading;

    #endregion

    private static ChannelLogger Log { get; } =
      new ChannelLogger(Constants.LIFECYCLE_LOG_CHANNEL);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {

      // Create a hidden GameObject for our AppLifecycleHook class
      var gameObject = new GameObject {
        hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector
      };

      _ = gameObject.AddComponent<AppLifecycleHook>();
      UnityObject.DontDestroyOnLoad(gameObject);

      Application.quitting += OnApplicationIsShuttingDown;
      Application.unloading += OnApplicationUnloading;
    }

    private static void OnApplicationIsShuttingDown() {
      Log.Info("Application is shutting down");
      ApplicationIsShuttingDown?.Invoke();
    }

    private static void OnApplicationUnloading() {
      Log.Info("Application is unloading");
      ApplicationIsUnloading?.Invoke();
    }

    private static void OnApplicationFocusChanged(bool hasFocus) {
      if (hasFocus) {
        Log.LogTrace("Application gained focus");
        ApplicationGainedFocus?.Invoke();
      } else {
        Log.LogTrace("Application lost focus");
        ApplicationLostFocus?.Invoke();
      }
    }

    private static void OnApplicationPauseChanged(bool isPaused) {
      if (isPaused) {
        Log.Info("Application paused");
        ApplicationPaused?.Invoke();
      } else {
        Log.Info("Application unpaused");
        ApplicationUnpaused?.Invoke();
      }
    }
  }
}