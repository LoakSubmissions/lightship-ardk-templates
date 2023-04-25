using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace Niantic.Titan.Uploader {

  /// <summary>
  /// Data class with all the information needed to perform a chunked file upload
  /// </summary>
  [PublicAPI]
  public interface IChunkedFileUploadRequest {

    /// <summary>
    /// A string that uniquely identifies this upload
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The id of the submission that this upload is associated with
    /// </summary>
    string SubmissionId { get; }

    /// <summary>
    /// The local path to the file being uploaded
    /// </summary>
    string LocalFilePath { get; }

    /// <summary>
    /// A context string (typically the file name) associated with this upload
    /// </summary>
    string Context { get; }

    /// <summary>
    /// The name of the upload's GCS bucket
    /// </summary>
    string BucketName { get; }

    /// <summary>
    /// A collection of <see cref="IChunkInfo"/> for each chunk being uploaded
    /// </summary>
    ReadOnlyCollection<IChunkInfo> Chunks { get; }

    /// <summary>
    /// Info for the compose step
    /// </summary>
    IComposeInfo ComposeInfo { get; }
  }

  /// <summary>
  /// Data class for a single chunk
  /// </summary>
  [PublicAPI]
  public interface IChunkInfo {

    /// <summary>
    /// The object path on GCS where this chunk will be uploaded
    /// </summary>
    string ObjectPath { get; }

    /// <summary>
    /// Auth information for uploading a chunk to GCS
    /// </summary>
    IAuthenticationInfo UploadAuth { get; }

    /// <summary>
    /// Auth information for deleting a chunk from GCS
    /// </summary>
    IAuthenticationInfo DeleteAuth { get; }
  }

  /// <summary>
  /// Data class for a given upload's compose step
  /// </summary>
  [PublicAPI]
  public interface IComposeInfo {

    /// <summary>
    /// The GCS object path for the final file composed from each of the chunks
    /// </summary>
    string ObjectPath { get; }

    /// <summary>
    /// Auth info for the compose step
    /// </summary>
    IAuthenticationInfo Auth { get; }

    /// <summary>
    /// A signed string used for authentication
    /// </summary>
    string AuthPayload { get; }
  }

  /// <summary>
  /// Information used to authenticate calls made to GCS
  /// </summary>
  [PublicAPI]
  public interface IAuthenticationInfo {

    /// <summary>
    /// A string containing authentication credentials
    /// </summary>
    string AuthString { get; }

    /// <summary>
    /// A date string sent with authenticated calls
    /// </summary>
    string AuthDate { get; }
  }

  /// <inheritdoc />
  [PublicAPI]
  public class ChunkedFileUploadRequest : IChunkedFileUploadRequest {
    public string Id { get; set; }
    public string SubmissionId { get; set; }
    public string LocalFilePath { get; set; }
    public string Context { get; set; }
    public string BucketName { get; set; }
    public ReadOnlyCollection<IChunkInfo> Chunks { get; private set; }
    public IComposeInfo ComposeInfo { get; set; }

    public IEnumerable<IChunkInfo> ChunkEnumerable {
      set => Chunks = new ReadOnlyCollection<IChunkInfo>(new List<IChunkInfo>(value));
    }
  }

  /// <inheritdoc />
  [PublicAPI]
  public class ChunkInfo : IChunkInfo {
    public string ObjectPath { get; set; }
    public IAuthenticationInfo UploadAuth { get; set; }
    public IAuthenticationInfo DeleteAuth { get; set; }
  }

  /// <inheritdoc />
  [PublicAPI]
  public class ComposeInfo : IComposeInfo {
    public string ObjectPath { get; set; }
    public IAuthenticationInfo Auth { get; set; }
    public string AuthPayload { get; set; }
  }

  /// <inheritdoc />
  [PublicAPI]
  public class AuthenticationInfo : IAuthenticationInfo {
    public string AuthString { get; set; }
    public string AuthDate { get; set; }
  }
}