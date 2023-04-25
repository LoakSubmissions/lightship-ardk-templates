using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using UnityEngine.Assertions;

namespace Niantic.Titan.Uploader.Internal {

  internal delegate void UploadProgressDelegate(float progress, int callbackId);
  internal delegate void UploadCompleteDelegate(bool success, int callbackId);

  /// <summary>
  /// This class represents a scoped context in which callbacks invoked from the
  /// unmanaged (C++) side of an upload can be associated with managed (C#) delegates,
  /// which will typically be the duration of a single asynchronous upload session.
  /// This implementation will work with either static or non-static delegates and
  /// is compatible with AOT compilation and non-JIT runtimes (eg, iOS and IL2CPP).
  /// </summary>
  internal class UploadCallbackScope : IDisposable {

    private static int _currentScopeId;
    private static readonly ConcurrentDictionary<int, UploadCallbackScope>
      _scopes = new ConcurrentDictionary<int, UploadCallbackScope>();

    private readonly TaskCompletionSource<bool> _tcs;
    private readonly Action<float> _onProgress;

    /// <summary>
    /// An id that uniquely identifies this scope
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// A <see cref="Task"/> that will be completed with the result from a
    /// <see cref="UploadCompleteCallback"/> called from the native side.
    /// </summary>
    public Task<bool> Task { get => _tcs.Task; }

    private UploadCallbackScope(Action<float> onProgress) {
      Id = Interlocked.Increment(ref _currentScopeId);
      _onProgress = onProgress;
      _tcs = new TaskCompletionSource<bool>(
        TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Creates and tracks a new <see cref="UploadCallbackScope"/>
    /// </summary>
    /// <param name="onProgress">A callback delegate that's
    /// invoked as the upload progresses.  The value passed to this
    /// delegate is the upload's percent complete (from 0.0 to 1.0).
    /// </param>
    public static UploadCallbackScope Create(Action<float> onProgress) {
      var scope = new UploadCallbackScope(onProgress);
      var addSuccess = _scopes.TryAdd(scope.Id, scope);
      Assert.IsTrue(addSuccess, "Could not add callback");
      return scope;
    }

    /// <summary>
    /// Disposes a scope after an upload is finished or callbacks are no
    /// longer needed.  This should always be called from a finally block
    /// or a using statement to guarantee that any tasks awaiting the
    /// results of this scope's <see cref="TaskCompletionSource{T}"/>
    /// are eventually completed.
    /// </summary>
    public void Dispose() {
      var removeSuccess = _scopes.TryRemove(Id, out _);
      Assert.IsTrue(removeSuccess, "Could not remove callback");
      _tcs?.TrySetResult(false);
    }

    /// <summary>
    /// Upload progress callback, invoked from unmanaged to managed code
    /// </summary>
    /// <param name="progress">The upload's progress (from 0.0 to 1.0)</param>
    /// <param name="callbackId">Used to identify the scope
    /// instance that our callback should be invoked on</param>
    [MonoPInvokeCallback(typeof(UploadProgressDelegate))]
    public static void UploadProgressCallback(float progress, int callbackId) {
      if (_scopes.TryGetValue(callbackId, out var scope)) {
        scope._onProgress?.Invoke(progress);
      } else {
        throw new InvalidOperationException($"Couldn't find callback id '{callbackId}' in {nameof(UploadProgressCallback)}");
      }
    }

    /// <summary>
    /// Upload completed callback, invoked from unmanaged to managed code
    /// </summary>
    /// <param name="success">True if the upload succeeded</param>
    /// <param name="callbackId">Used to identify the scope
    /// instance that our callback should be invoked on</param>
    [MonoPInvokeCallback(typeof(UploadCompleteDelegate))]
    public static void UploadCompleteCallback(bool success, int callbackId) {
      if (_scopes.TryGetValue(callbackId, out var scope)) {
        scope._tcs?.TrySetResult(success);
      } else {
        throw new InvalidOperationException($"Couldn't find callback id '{callbackId}' in {nameof(UploadCompleteCallback)}");
      }
    }
  }
}