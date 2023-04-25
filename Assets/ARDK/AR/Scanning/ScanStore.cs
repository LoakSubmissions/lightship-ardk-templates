// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Protobuf;
using Google.Protobuf;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.Telemetry;
using Niantic.Titan.Uploader;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Uploader.Protos;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;
using UnityEngine.Networking;

using TelemetryOperation = Niantic.ARDK.AR.Protobuf.ScanningFrameworkEvent.Types.Operation;
using TelemetryState = Niantic.ARDK.AR.Protobuf.ScanningFrameworkEvent.Types.State;
using TelemetryInternet = Niantic.ARDK.AR.Protobuf.ScanUploadEvent.Types.Internet;

namespace Niantic.ARDK.AR.Scanning
{
  internal class ScanStore: IScanStore
  {
    private const string ScanUploadEndpoint = "https://wayfarer-ugc-api.nianticlabs.com/api/proto/v1/";
    private const string RequestUploadMethodId = "620404";
    private const string RequestFileUploadMethodId = "620405";
    private const string UploadCompleteMethodId = "620406";
    internal string _dataPathRoot;
    internal RuntimeEnvironment _runtimeEnvironment;

    internal ScanStore(string dataPathRoot, RuntimeEnvironment runtimeEnvironment)
    {
      this._dataPathRoot = dataPathRoot;
      this._runtimeEnvironment = runtimeEnvironment;
    }

    /// <inheritdoc />
    public List<string> GetScanIDs()
    {
      if (!Directory.Exists(ScanPath.GetBasePath(_dataPathRoot, _runtimeEnvironment)))
      {
        return new List<string>();
      }
      return Directory.GetDirectories
          (ScanPath.GetBasePath(_dataPathRoot, _runtimeEnvironment))
        .Select(path => new DirectoryInfo(path).Name)
        .ToList();
    }

    /// <inheritdoc />
    public SavedScan GetSavedScan(string scanId)
    {
      return new SavedScan(scanId, this._dataPathRoot, this._runtimeEnvironment);
    }
    
    private void CopyDirectory(string sourceDir, string destinationDir)
    {
      var dir = new DirectoryInfo(sourceDir);

      if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

      Directory.CreateDirectory(destinationDir);

      // Get the files in the source directory and copy to the destination directory
      foreach (FileInfo file in dir.GetFiles())
      {
        string targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath);
      }
      
      foreach (DirectoryInfo subDir in dir.GetDirectories())
      {
        string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
        CopyDirectory(subDir.FullName, newDestinationDir);
      }
    }

    /// <inheritdoc />
    public void SaveCurrentScan(string scanId)
    {
      string path = ScanPath.GetScanPath(Application.temporaryCachePath, scanId, _runtimeEnvironment);
      if (Directory.Exists(path))
      {
        _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Save, TelemetryState.Started);
        string targetPath = ScanPath.GetScanPath(_dataPathRoot, scanId, _runtimeEnvironment);
        Directory.CreateDirectory(ScanPath.GetBasePath(_dataPathRoot, _runtimeEnvironment));
        if (Directory.Exists(targetPath))
        {
          Directory.Delete(targetPath, true);
        }
        CopyDirectory(path, targetPath);
        _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Save, TelemetryState.Finished);
        SendSaveTelemetry(scanId);
      }
      else
      {
        throw new InvalidOperationException("The specified scan ID does not exist");
      }
    }

    /// <inheritdoc />
    public void DeleteSavedScan(string scanId)
    {
      string path = ScanPath.GetScanPath(_dataPathRoot, scanId, _runtimeEnvironment);
      Directory.Delete(path, true);
    }

    /// <summary>
    /// It's a tricky to save the mesh after the fact, since the ownership of TexturedMesh is handed
    /// to the application, and it may be already destroyed when the application wants to save.
    ///
    /// Therefore, we save the mesh immediately on scan completion along with the texture which is
    /// already stored by the native code.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="scanId"></param>
    /// <param name="runtimeEnvironment"></param>
    internal void SaveCurrentMesh(UnityEngine.Mesh mesh, string scanId)
    {
      if (_runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        // Setup the target directory in the editor, as the mock scanner doesn't write anything.
        Directory.CreateDirectory(Application.temporaryCachePath +"/scanning/" + scanId);
      }
      string path = ScanPath.GetMeshPath(Application.temporaryCachePath, scanId, _runtimeEnvironment);
      SaveMesh(mesh, path);
    }

    private static void SaveMesh(UnityEngine.Mesh mesh, string path)
    {
      MemoryStream memoryStream = new MemoryStream();
      GlobalSerializer.Serialize(memoryStream, mesh);
      File.WriteAllBytes(path, memoryStream.ToArray());
    }

    private static UnityEngine.Mesh LoadMesh(string path)
    {
      FileStream stream = new FileStream(path, FileMode.Open);
      UnityEngine.Mesh result = (UnityEngine.Mesh) GlobalSerializer.Deserialize(stream);
      stream.Close();
      return result;
    }

    [Serializable]
    // TODO: use the protobuf
    // https://gitlab.nianticlabs.com/geo/titan-shared/-/blob/master/client-api/src/main/proto/rpc/client/titan_poi_management.proto
    private class GetUploadUrlRequest
    {
      public string submissionId;
      public string[] fileUploadContext;

      internal GetUploadUrlRequest(string submissionId, params string[] fileUploadContext)
      {
        this.fileUploadContext = fileUploadContext;
        this.submissionId = submissionId;
      }
    }

    private UnityWebRequest createPostRequest(string endpoint, string method, byte[] data)
    {
      UnityWebRequest uwr1 = new UnityWebRequest();
      uwr1.method = "POST";
      uwr1.uploadHandler = new UploadHandlerRaw(data);
      uwr1.url = endpoint + method;
      uwr1.uploadHandler.contentType = "application/protobuf";
      uwr1.downloadHandler = new DownloadHandlerBuffer();
      var requestHeaders = ArdkGlobalConfig._Internal.GetApiGatewayHeaders();
      foreach (var header in requestHeaders)
      {
        uwr1.SetRequestHeader(header.Key, header.Value);
      }
      uwr1.SetRequestHeader("Content-Type", "application/protobuf");
      return uwr1;
    }
    private _IScanUploadPayloadBuilder createScanUploader(string basePath, string scanId, string uploadUserJsonStr)
    {
      if (_runtimeEnvironment == RuntimeEnvironment.Mock)
      {
        return new _MockScanUploadPayloadBuilder();
      }
      else
      {
        return new _NativeScanUploadPayloadBuilder(basePath, scanId, uploadUserJsonStr);
      }
    }

    private IEnumerator WaitForTask(Task task)
    {
      while (!(task.IsCanceled || task.IsCompleted || task.IsFaulted))
      {
        yield return null;
      }
    }

    internal string ExtractScanTargetId(string encodedScanTargetId)
    {
      byte[] encodedId = Convert.FromBase64String(encodedScanTargetId);
      byte[] key = new byte[8];
      byte[] iv = new byte[8];
      byte[] remainder = new byte[encodedId.Length - 17];

      Buffer.BlockCopy(encodedId, 1, key, 0, 8);
      Buffer.BlockCopy(encodedId, 9, iv, 0, 8);
      Buffer.BlockCopy(encodedId, 17, remainder, 0, remainder.Length);

      SymmetricAlgorithm algorithm = DES.Create();
      ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
      byte[] decodedId = transform.TransformFinalBlock(remainder, 0, remainder.Length);
      return Encoding.Unicode.GetString(decodedId);
    }

    private async Task<Tuple<bool, string>> UploadScanAsync(string scanId, string basePath, IScanStore.UploadUserInfo uploadUserInfo, Action<float> onProgress)
    {
      onProgress(0);
      _IScanUploadPayloadBuilder scanUploadPayloadBuilder = await Task.Run(() => createScanUploader(basePath, scanId, JsonUtility.ToJson(uploadUserInfo)));
      if (!scanUploadPayloadBuilder.IsValid())
      {
        return new Tuple<bool, string>(false, "Scan does not have required information.");
      }
      if (scanUploadPayloadBuilder.GetScanTargetId() == "")
      {
        return new Tuple<bool, string>(false, "Scan must be associated with Scan Target ID");
      }
      
      ARCommonMetadata commonMetadata = ArdkGlobalConfig._Internal.GetCommonDataEnvelopeWithRequestIdAsProto();

      string finalID = scanUploadPayloadBuilder.GetScanTargetId();
      byte[] SubmissionMetadataBytes = await Task.Run( () =>
      {
        PoiVideoSubmissionMetadataProto metadataRequest = new PoiVideoSubmissionMetadataProto();
        metadataRequest.PoiId = finalID;
        metadataRequest.ArCommonMetadata = commonMetadata;
        byte[] metaPayload = metadataRequest.ToByteArray();
        return metaPayload;
      });

      using UnityWebRequest uwr1 = createPostRequest(ScanUploadEndpoint, RequestUploadMethodId, SubmissionMetadataBytes);
      await uwr1.SendWebRequest();

      if (uwr1.result != UnityWebRequest.Result.Success)
      {
        return new Tuple<bool, string>(false, "Request upload failed: " + uwr1.error);
      }

      PlayerSubmissionResponseProto metaResponse = PlayerSubmissionResponseProto.Parser.ParseFrom(uwr1.downloadHandler.data);

      if (metaResponse.Status != PlayerSubmissionResponseProto.Types.Status.Success)
      {
        return new Tuple<bool, string>(false, "Failed to request upload: " + metaResponse.Status);
      }

      GetGrapeshotUploadUrlProto grapeshotUploadUrlProto = new GetGrapeshotUploadUrlProto();
      grapeshotUploadUrlProto.SubmissionId = metaResponse.SubmissionId;
      Dictionary<string, string> partialToFullPath = new Dictionary<string, string>();
      Dictionary<string, string> partialPathToUuid = new Dictionary<string, string>();
      int chunkIndex = 0;
      await Task.Run(() =>
      {
        while (scanUploadPayloadBuilder.HasMoreChunks())
        {
          string uuid = scanUploadPayloadBuilder.GetNextChunkUuid();
          string path = scanUploadPayloadBuilder.GetNextChunk();
          string uploadName = "chunk_" + chunkIndex + ".tgz";
          grapeshotUploadUrlProto.FileUploadContext.Add(uploadName);
          partialToFullPath.Add(uploadName, path);
          partialPathToUuid.Add(uploadName, uuid);
          chunkIndex++;
        }
      });
      int fileCount = grapeshotUploadUrlProto.FileUploadContext.Count;
      using UnityWebRequest uwr2 = createPostRequest(ScanUploadEndpoint, RequestFileUploadMethodId,
        grapeshotUploadUrlProto.ToByteArray());
      await uwr2.SendWebRequest();

      if (uwr2.result != UnityWebRequest.Result.Success)
      {
        return new Tuple<bool, string>(false, "Get upload URL failed: " + uwr2.error);
      }

      ChunkedFileUploadService uploadService = new ChunkedFileUploadService();
      GetGrapeshotUploadUrlOutProto getGrapeshotUploadUrlOutProto =
        GetGrapeshotUploadUrlOutProto.Parser.ParseFrom(uwr2.downloadHandler.data);
      int index = 0;
      bool uploadSuccess = true;
      foreach (var source in getGrapeshotUploadUrlOutProto.FileContextToGrapeshotData.Keys)
      {
        var value = getGrapeshotUploadUrlOutProto.FileContextToGrapeshotData[source];
        List<IChunkInfo> chunks = new List<IChunkInfo>();
        foreach (var chunk in value.ChunkData)
        {
          chunks.Add(new ChunkInfo()
          {
            ObjectPath = chunk.ChunkFilePath,
            UploadAuth = new AuthenticationInfo()
            {
              AuthDate = chunk.UploadAuthentication.Date,
              AuthString = chunk.UploadAuthentication.Authorization
            }
          });
        }

        ChunkedFileUploadRequest uploadRequest = new ChunkedFileUploadRequest()
        {
          Id = metaResponse.SubmissionId + "-" + index,
          SubmissionId = metaResponse.SubmissionId,
          LocalFilePath = partialToFullPath[source],
          Context = source,
          BucketName = value.GcsBucket,
          ChunkEnumerable = chunks,
          ComposeInfo = new ComposeInfo()
          {
            Auth = new AuthenticationInfo()
            {
              AuthDate = value.ComposeData.Authentication.Date,
              AuthString = value.ComposeData.Authentication.Authorization
            },
            AuthPayload = value.ComposeData.Hash,
            ObjectPath = value.ComposeData.TargetFilePath
          }
        };

        _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Upload, TelemetryState.Started);
        UploadResult uploadResult = await uploadService.UploadFileToCloudStorage(uploadRequest, (submissionId, context, progress) =>
        {
          float realProgress = progress / (float)fileCount + (index) / (float)fileCount;
          onProgress(realProgress);
        });
        if (uploadResult != UploadResult.Succeeded)
        {
          uploadSuccess = false;
          ARLog._Error("Upload failed! " + uploadResult);
          _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Upload, TelemetryState.Error);
          break;
        }
        _TelemetryService.RecordScanningFrameworkEvent(scanId, TelemetryOperation.Upload, TelemetryState.Finished);
        SendUploadTelemetry(scanId, partialPathToUuid[source], index, new FileInfo(partialToFullPath[source]).Length);
        index++;
      }

      foreach (var path in partialToFullPath.Values)
      {
        File.Delete(path);
      }

      AsyncFileUploadCompleteProto uploadCompleteProto = new AsyncFileUploadCompleteProto();
      uploadCompleteProto.SubmissionId = metaResponse.SubmissionId;
      uploadCompleteProto.UploadStatus = uploadSuccess
        ? AsyncFileUploadCompleteProto.Types.Status.UploadDone
        : AsyncFileUploadCompleteProto.Types.Status.UploadFailed;
      uploadCompleteProto.ArCommonMetadata = commonMetadata;
      using UnityWebRequest uwr3 = createPostRequest(ScanUploadEndpoint, UploadCompleteMethodId,
        uploadCompleteProto.ToByteArray());
      await uwr3.SendWebRequest();

      if (uwr3.result != UnityWebRequest.Result.Success)
      {
        return new Tuple<bool, string>(false, "Submit upload result failed: " + uwr3.error);
      }

      return new Tuple<bool, string>(uploadSuccess, uploadSuccess ? "" : "Some segments failed to upload.");
    }

    /// <summary>
    /// Upload the saved scan to Niantic.
    /// </summary>
    /// <param name="scanId"> The ID of the scan to upload. </param>
    /// <param name="uploadUserInfo"></param>
    /// <param name="onProgress"> Callback with the progress of the current upload. </param>
    /// <param name="onResult"> Called when upload completes, with if the upload is successful or not. </param>
    public async void UploadScan(string scanId, IScanStore.UploadUserInfo uploadUserInfo,
      Action<float> onProgress, Action<bool, string> onResult)
    {
      var result = await UploadScanAsync(scanId, _dataPathRoot, uploadUserInfo, onProgress);
      onResult(result.Item1, result.Item2);
    }


    private void SendSaveTelemetry(string scanId)
    {
      var dir = new DirectoryInfo(ScanPath.GetScanPath(_dataPathRoot, scanId, _runtimeEnvironment));
      var size = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
      _TelemetryService.RecordEvent(new ScanSaveEvent()
      {
        ScanId = scanId,
        ScanFileSize = size
      });
    }

    private void SendUploadTelemetry(string scanId, string chunkUuid, int chunkIndex, long fileSize)
    {
      var internet = Application.internetReachability switch
      {
        NetworkReachability.ReachableViaLocalAreaNetwork => TelemetryInternet.Wifi,
        NetworkReachability.ReachableViaCarrierDataNetwork => TelemetryInternet.Mobile,
        _ => TelemetryInternet.Unknown
      };
      _TelemetryService.RecordEvent(new ScanUploadEvent()
      {
        ScanId = scanId,
        ScanChunkUuid = chunkUuid,
        ChunkOrder = chunkIndex,
        InternetType = internet,
        FileSize = fileSize
      });
    }
  }
}
