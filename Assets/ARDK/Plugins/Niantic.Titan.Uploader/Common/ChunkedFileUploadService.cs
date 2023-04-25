// Copyright 2013-2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Niantic.Titan.Uploader.Internal;
using UnityEngine.Assertions;


namespace Niantic.Titan.Uploader {

  using SessionDictionary = Dictionary<IChunkedFileUploadRequest, IChunkedFileUploadSession>;

  /// <summary>
  /// Enum describing all potential results of an upload
  /// </summary>
  public enum UploadResult {
    Succeeded,
    Failed,
    Cancelled,
  }

  /// <summary>
  /// This service is responsible for managing chunked upload
  /// sessions (<see cref="IChunkedFileUploadSession"/>) for
  /// files uploaded through the GCS Uploader library.
  /// </summary>
  [PublicAPI]
  public interface IChunkedFileUploadService {

    /// <summary>
    /// Sets whether uploads are currently paused.
    /// Setting to true will pause all current uploads,
    /// and false will resume all paused uploads.
    /// </summary>
    bool IsFileUploadPaused { set; }

    /// <summary>
    /// Gets or sets the number of worker threads to use for chunk uploads
    /// </summary>
    uint UploadWorkerThreadCount { get; set; }

    /// <summary>
    /// Start an asynchronous upload.
    /// </summary>
    /// <param name="uploadRequest">Details of the upload request</param>
    /// <param name="onChunkUploadPercentageUpdate">Callback when each chunk is
    /// successfully uploaded. Params are submission ID, context, the number of
    /// successfully uploaded chunks and the total number of chunks</param>
    /// <returns>Task that tracks the upload and can be awaited for the result</returns>
    Task<UploadResult> UploadFileToCloudStorage(
      IChunkedFileUploadRequest uploadRequest,
      Action<string, string, float> onChunkUploadPercentageUpdate);

    /// <summary>
    /// Cancel upload by submission ID
    /// </summary>
    /// <param name="submissionId">Unique submission ID,
    /// each submission can contain multiple files.</param>
    void CancelUploadBySubmissionId(string submissionId);

    /// <summary>
    /// Cancel all in-progress uploads
    /// </summary>
    void CancelAllUploads();
  }

  /// <summary>
  /// This interface exposes some of <see cref="ChunkedFileUploadService"/>'s
  /// internal state for test purposes.
  /// </summary>
  public interface IDebugChunkedFileUploadService : IChunkedFileUploadService {

    /// <summary>
    /// Gets the number of sessions that are currently active
    /// </summary>
    int SessionCount { get; }

    /// <summary>
    /// Checks whether a given session is active
    /// </summary>
    bool IsSessionActive(IChunkedFileUploadRequest uploadRequest);

    /// <summary>
    /// Creates a <see cref="IChunkedFileUploadSession"/> for a new chunked file upload session
    /// </summary>
    IChunkedFileUploadSession CreateSession(IChunkedFileUploadRequest uploadRequest);
  }

  /// <inheritdoc />
  [UsedImplicitly]
  public class ChunkedFileUploadService : IDebugChunkedFileUploadService {

    private static ChannelLogger Log { get; } =
      new ChannelLogger(Constants.LOG_CHANNEL);

    private readonly SessionDictionary _sessions = new SessionDictionary();
    private SessionDictionary _suspendedSessions = new SessionDictionary();

    /// <inheritdoc />
    public int SessionCount {
      get => _sessions.Count;
    }

    /// <inheritdoc />
    public uint UploadWorkerThreadCount { get; set; } =
      Constants.DEFAULT_CHUNK_UPLOAD_THREAD_COUNT;

    /// <inheritdoc />
    public bool IsFileUploadPaused {
      set {
        foreach (var session in _sessions.Values) {
          if (value) {
            session.Pause();
          }
          else if (session.IsPaused) {
            session.Resume();
          }
        }
      }
    }

    public ChunkedFileUploadService() {
      // Attach handlers to lifecycle events in order to suspend, resume, and
      // cancel active uploads when state transitions occur in the host app.
      ApplicationLifecycle.ApplicationPaused += OnApplicationPaused;
      ApplicationLifecycle.ApplicationUnpaused += OnApplicationUnpaused;
      ApplicationLifecycle.ApplicationIsShuttingDown += CancelAllUploads;
      ApplicationLifecycle.ApplicationIsUnloading += CancelAllUploads;
      NativeLogger.Initialize();
    }

    /// <summary>
    /// This method suspends any active uploads when the application is
    /// paused.  The native threads will continue to run, but callbacks
    /// into managed code may hang or fail, which can lead to instability
    /// and upload failures.  To play it safe, we suspend these uploads
    /// and resume them when Unity is up and running again.
    /// </summary>
    private void OnApplicationPaused() {
      var sessions = new SessionDictionary(_sessions);

      foreach (var session in sessions) {
        if (session.Value.IsActive) {
          Log.Info($"Suspending upload for submission {session.Key.SubmissionId}");
          _suspendedSessions.Add(session.Key, session.Value);
          session.Value.Pause();
        }
      }
    }

    /// <summary>
    /// This method resumes uploads suspended by <see cref="OnApplicationPaused"/>
    /// when the application resumes after being paused.
    /// </summary>
    private void OnApplicationUnpaused() {
      var sessions = Interlocked.Exchange(ref _suspendedSessions, new SessionDictionary());
      foreach (var session in sessions) {
        Log.Info($"Resuming upload for submission {session.Key.SubmissionId}");
        session.Value.Resume();
      }
    }

    /// <inheritdoc />
    public bool IsSessionActive(IChunkedFileUploadRequest uploadRequest) {
      return _sessions.ContainsKey(uploadRequest);
    }

    /// <inheritdoc />
    public IChunkedFileUploadSession CreateSession(
      IChunkedFileUploadRequest uploadRequest) {
      Assert.IsTrue(!_sessions.ContainsKey(uploadRequest));
      var session = new ChunkedFileUploadSession(uploadRequest, UploadWorkerThreadCount);
      session.Disposing += () => _sessions.Remove(uploadRequest);
      _sessions[uploadRequest] = session;
      return session;
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadFileToCloudStorage(
      IChunkedFileUploadRequest uploadRequest,
      Action<string, string, float> onChunkUploadPercentageUpdate) {

      // Create the chunked file upload session
      using var session = CreateSession(uploadRequest);
      session.ProgressUpdated += progress => {
        onChunkUploadPercentageUpdate?.Invoke(
          uploadRequest.SubmissionId,
          uploadRequest.Context,
          progress);
      };

      // Begin the async upload and return its awaited result
      Log.Info($"Started GCS uploader upload for submissionId {uploadRequest.SubmissionId}");
      var sessionUploadAsyncResult = await session.UploadAsync().ConfigureAwait(false);
      Log.Info($"Finished GCS uploader upload for submissionId {uploadRequest.SubmissionId}");

      return sessionUploadAsyncResult;
    }

    /// <inheritdoc />
    public void CancelUploadBySubmissionId(string submissionId) {
      foreach (var iter in _sessions) {
        if (iter.Key.SubmissionId == submissionId) {
          iter.Value.Cancel();
          // For a given submission id, there will currently only ever be one upload in progress
          break;
        }
      }
    }

    /// <inheritdoc />
    public void CancelAllUploads() {
      CancelAllUploadsInternal(true);
    }

    /// <summary>
    /// Cancel all uploads that are active and not paused
    /// </summary>
    /// <param name="suppressWarning">If false, warnings are logged when each
    /// session is cancelled.  If true, these warnings are suppressed.</param>
    private void CancelAllUploadsInternal(bool suppressWarning = false) {
      var sessionsToCancel = new SessionDictionary(_sessions);

      foreach (var sessionToCancel in sessionsToCancel) {
        var session = sessionToCancel.Value;
        if (session.IsActive || session.IsPaused) {
          if (!suppressWarning) {
            Log.Warning($"Cancelling upload for submission '{sessionToCancel.Key.SubmissionId}'");
          }
          session.Cancel();
        }
      }
    }
  }
}