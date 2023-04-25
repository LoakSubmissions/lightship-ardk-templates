using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Niantic.Titan.Uploader.Internal;

namespace Niantic.Titan.Uploader {

  /// <summary>
  /// A client that's responsible for uploading a file to GCS
  /// in chunks.  These chunks are composed into a single GCS
  /// object once all of the chunk uploads have finished.
  /// </summary>
  [PublicAPI]
  internal interface IChunkedFileUploadClient : IDisposable {

    /// <summary>
    /// Performs an asynchronous chunked file upload
    /// </summary>
    /// <param name="onUploadProgressUpdated">A callback delegate that's
    /// invoked as the upload progresses.  The value passed to this
    /// delegate is the upload's percent complete (from 0.0 to 1.0).
    /// </param>
    /// <param name="workerThreadCount">An optional parameter specifying
    /// the number of worker threads to use for chunk uploading.</param>
    /// <returns>True if the upload succeeded</returns>
    Task<bool> UploadAsync(Action<float> onUploadProgressUpdated, uint workerThreadCount = Constants.DEFAULT_CHUNK_UPLOAD_THREAD_COUNT);

    /// <summary>
    /// Cancels an already started asynchronous chunked file upload
    /// </summary>
    void CancelUpload();

    /// <summary>
    /// Pauses an already started asynchronous chunked file upload
    /// </summary>
    void PauseUpload();

    /// <summary>
    /// Resumes an already started asynchronous chunked file upload
    /// </summary>
    void ResumeUpload();
  }

  /// <summary>
  /// C# bridge class that's responsible for calling into unmanaged
  /// code and marshalling data between managed and unmanaged memory.
  /// </summary>
  internal class ChunkedFileUploadClient : IChunkedFileUploadClient {

    #region PInvoke functions

    /// <summary>
    /// Native functions called using P/Invoke
    /// </summary>
    private static class Native {

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern IntPtr ChunkedUploadClient_New(
        string localFilePath,
        string bucketName,
        uint chunkCount,
        string composedObjectPath,
        string composeAuthorization,
        string composeDate,
        string composeSignedPayload,
        string[] chunkObjectPaths,
        string[] chunkAuthorizations,
        string[] chunkDates);

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern void ChunkedUploadClient_Delete(HandleRef nativeHandle);

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern bool ChunkedUploadClient_UploadAsync(
        HandleRef nativeHandle,
        uint numUploadThreadsToCreate,
        UploadCompleteDelegate uploadCompletedCallback,
        UploadProgressDelegate uploadProgressCallback,
        int callbackId);

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern void ChunkedUploadClient_CancelUpload(HandleRef nativeHandle);

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern void ChunkedUploadClient_PauseUpload(HandleRef nativeHandle);

      [DllImport(Constants.LIBRARY_NAME, CharSet = CharSet.Ansi)]
      public static extern void ChunkedUploadClient_ResumeUpload(HandleRef nativeHandle);
    }

    #endregion

    private HandleRef _nativeHandle;
    private readonly IChunkedFileUploadRequest _uploadRequest;
    public string SubmissionId => _uploadRequest.SubmissionId;

    /// <summary>
    /// Constructs a client that will perform a chunked upload of single file
    /// </summary>
    /// <param name="uploadRequest">Info for the file being uploaded</param>
    public ChunkedFileUploadClient(IChunkedFileUploadRequest uploadRequest) {
      _uploadRequest = uploadRequest;

      var chunkCount = uploadRequest.Chunks.Count;
      var chunkObjectPaths = new string[chunkCount];
      var chunkAuthStrings = new string[chunkCount];
      var chunkAuthDates = new string[chunkCount];

      for (var i = 0; i < chunkCount; i++) {
        var chunk = uploadRequest.Chunks[i];
        chunkObjectPaths[i] = chunk.ObjectPath;
        chunkAuthStrings[i] = chunk.UploadAuth.AuthString;
        chunkAuthDates[i] = chunk.UploadAuth.AuthDate;
      }

      var nativePtr = Native.ChunkedUploadClient_New(
        uploadRequest.LocalFilePath,
        uploadRequest.BucketName,
        (uint)uploadRequest.Chunks.Count,
        uploadRequest.ComposeInfo.ObjectPath,
        uploadRequest.ComposeInfo.Auth.AuthString,
        uploadRequest.ComposeInfo.Auth.AuthDate,
        uploadRequest.ComposeInfo.AuthPayload,
        chunkObjectPaths,
        chunkAuthStrings,
        chunkAuthDates);

      _nativeHandle = new HandleRef(this, nativePtr);
    }

    public void Dispose() {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }

    ~ChunkedFileUploadClient() {
      ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Frees the native class in unmanaged memory.
    /// </summary>
    private void ReleaseUnmanagedResources() {
      if (_nativeHandle.Handle != IntPtr.Zero) {
        Native.ChunkedUploadClient_Delete(_nativeHandle);
        _nativeHandle = new HandleRef();
      }
    }

    private void ThrowOnInvalidPointer() {
      if (_nativeHandle.Handle == IntPtr.Zero) {
        throw new ArgumentException("Invalid pointer to unmanaged memory");
      }
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(Action<float> onUploadProgress,
      uint workerThreadCount = Constants.DEFAULT_CHUNK_UPLOAD_THREAD_COUNT) {
      ThrowOnInvalidPointer();

      using (var callbackScope = UploadCallbackScope.Create(onUploadProgress)) {
        var success = Native.ChunkedUploadClient_UploadAsync(
          _nativeHandle,
          workerThreadCount,
          UploadCallbackScope.UploadCompleteCallback,
          UploadCallbackScope.UploadProgressCallback,
          callbackScope.Id);

        return success && await callbackScope.Task.ConfigureAwait(false);
      }
    }

    /// <inheritdoc />
    public void CancelUpload() {
      ThrowOnInvalidPointer();

      // Stop async uploading from continuing on the C++ side
      Native.ChunkedUploadClient_CancelUpload(_nativeHandle);
    }

    /// <inheritdoc />
    public void PauseUpload() {
      ThrowOnInvalidPointer();

      // Pauses an in-progress upload on the C++ side
      Native.ChunkedUploadClient_PauseUpload(_nativeHandle);
    }

    /// <inheritdoc />
    public void ResumeUpload() {
      ThrowOnInvalidPointer();

      // Resumes a paused upload on the C++ side
      Native.ChunkedUploadClient_ResumeUpload(_nativeHandle);
    }
  }
}