using System;
using System.Threading;
using UnityEngine;

namespace Niantic.Titan.Uploader.Internal {

  /// <summary>
  /// This class is used to invoke delegates on Unity's main thread.
  /// </summary>
  internal static class UnitySynchronizationContext {

    private static SynchronizationContext _synchronizationContext;

    /// <summary>
    /// This method is always run on Unity's main thread, so
    /// it captures the current synchronization context in
    /// order to execute on Unity's main thread later on.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CaptureSynchronizationContext() {
      _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Schedules or executes the action on the main thread.  If
    /// the current thread is the main thread, then the action will
    /// be executed inline (unlike <see cref="PostToMainThread"/>).
    /// </summary>
    public static void RunOnMainThread(Action action) {
      if (SynchronizationContext.Current == _synchronizationContext) {
        action.Invoke();
      } else {
        PostToMainThread(action);
      }
    }

    /// <summary>
    /// Schedules the action to be run on the main thread.
    /// Even if the current thread is the main thread, the action
    /// will be queued (unlike <see cref="RunOnMainThread"/>).
    /// </summary>
    private static void PostToMainThread(Action action) {
      _synchronizationContext.Post(_ => action.Invoke(), null);
    }
  }
}