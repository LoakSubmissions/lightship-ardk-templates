// Copyright 2013-2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Niantic.Titan.Uploader.Internal;

namespace Niantic.Titan.Uploader {

  /// <summary>
  /// Represents a session in which a single chunked file upload will be
  /// performed.  This session is responsible for the actual upload work
  /// (through the GCS Uploader's <see cref="ChunkedFileUploadClient"/>)
  /// as well as raising events back to the caller to indicate upload
  /// progress and completion.
  /// </summary>
  [PublicAPI]
  public interface IChunkedFileUploadSession : IDisposable {

    /// <summary>
    /// Whether the upload is complete
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    bool IsSuccessful { get; }

    /// <summary>
    /// Whether the upload has been canceled
    /// </summary>
    bool IsCanceled { get; }

    /// <summary>
    /// Whether the upload has been paused
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Whether the upload is actively running (ie, the upload
    /// has started but not completed, canceled, or paused).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Invoked while a file is uploaded to indicate progress (from
    /// 0.0 to 1.0).  This event is raised from Unity's main thread.
    /// </summary>
    event Action<float> ProgressUpdated;

    /// <summary>
    /// Begins the session's upload, which will run asynchronously.
    /// </summary>
    /// <returns>Task that tracks the upload and can be awaited for the result</returns>
    Task<UploadResult> UploadAsync();

    /// <summary>
    /// Pauses an active upload session.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused upload session.
    /// </summary>
    void Resume();

    /// <summary>
    /// Cancels an upload session.
    /// </summary>
    void Cancel();
  }

  /// <inheritdoc/>
  internal class ChunkedFileUploadSession : IChunkedFileUploadSession {

    private IChunkedFileUploadClient _client;
    private readonly uint _uploadWorkerThreadCount;

    public bool IsComplete { get; private set; }
    public bool IsSuccessful { get; private set; }
    public bool IsCanceled { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsActive { get; private set; }

    public event Action<float> ProgressUpdated;
    public event Action Disposing;

    internal ChunkedFileUploadSession(
      IChunkedFileUploadRequest uploadRequest,
      uint uploadWorkerThreadCount) {
      _client = new ChunkedFileUploadClient(uploadRequest);
      _uploadWorkerThreadCount = uploadWorkerThreadCount;
      IsActive = true;
    }

    internal ChunkedFileUploadSession(IChunkedFileUploadClient client) {
      _client = client;
      IsActive = true;
    }

    public void Dispose() {
      Disposing?.Invoke();
      Disposing = null;

      _client?.Dispose();
      _client = null;
    }

    /// <inheritdoc/>
    public async Task<UploadResult> UploadAsync() {

      // Begin an asynchronous upload and await its result
      await _client.UploadAsync(OnProgressUpdated, _uploadWorkerThreadCount)
        .ContinueWith(
          task => {
            IsActive = false;
            IsComplete = !IsCanceled;
            IsSuccessful =
              task.IsCompleted &&
              !task.IsFaulted &&
              !task.IsCanceled &&
              task.Result;
          })
        .ConfigureAwait(false);

      return IsCanceled
        ? UploadResult.Cancelled
        : IsSuccessful
          ? UploadResult.Succeeded
          : UploadResult.Failed;
    }

    private void OnProgressUpdated(float progress) {
      if (ProgressUpdated != null) {
        // Always raise progress events on Unity's main thread, since event handlers in
        // game client code will most likely update the UI when these events are fired.
        UnitySynchronizationContext.RunOnMainThread(() => ProgressUpdated.Invoke(progress));
      }
    }

    /// <inheritdoc/>
    public void Pause() {
      IsPaused = true;
      IsActive = false;
      _client?.PauseUpload();
    }

    /// <inheritdoc/>
    public void Resume() {
      IsPaused = false;
      IsActive = true;
      _client?.ResumeUpload();
    }

    /// <inheritdoc/>
    public void Cancel() {
      IsCanceled = true;
      _client?.CancelUpload();
    }
  }
}