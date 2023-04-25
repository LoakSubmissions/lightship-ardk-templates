// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;
using System.Threading;

using Niantic.ARDK.Utilities.Logging;
using Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages;

using UnityEngine;
using UnityEngine.Networking;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  internal class _HttpAnchoredSceneServiceImpl:
    _IAnchoredSceneServiceImpl
  {
    internal static _HttpAnchoredSceneServiceImpl _Instance = new _HttpAnchoredSceneServiceImpl();
    private string _endpoint;
    private string _appId;
    private const string _appIdHeader = "Grpc-Metadata-io.ctx.nia.appid";

    private string _createFormat = "https://{0}/experience/create";
    private string _getFormat = "https://{0}/experience/get";
    private string _deleteFormat = "https://{0}/experience/delete";
    private string _listExperiencesFormat = "https://{0}/room/list_exp";

    public IntPtr InitializeService(string endpoint, string appId)
    {
      _endpoint = endpoint;
      _appId = appId;

      return new IntPtr(1);
    }

    public _CreateExperienceResponse CreateExperience
    (
      _CreateExperienceRequest request,
      out AnchoredSceneServiceStatus status
    )
    {
      var json = JsonUtility.ToJson(request);
      var formattedJson = _DictionaryToJsonHelper._FormatJsonRequestForMarsh(json);

      var uri = String.Format(_createFormat, _endpoint);
      var response = SendBlockingWebRequest(uri, formattedJson, out var s);

      if (String.IsNullOrEmpty(response))
      {
        ARLog._Error("Got an empty response");
        status = (AnchoredSceneServiceStatus)s;
        return new _CreateExperienceResponse();
      }

      var res = JsonUtility.FromJson<_CreateExperienceResponse>(response);
      status = AnchoredSceneServiceStatus.Ok;
      return res;
    }

    public _GetExperienceResponse GetExperience
    (
      _GetExperienceRequest request,
      out AnchoredSceneServiceStatus status
    )
    {
      var json = JsonUtility.ToJson(request);
      var formattedJson = _DictionaryToJsonHelper._FormatJsonRequestForMarsh(json);
      var uri = String.Format(_getFormat, _endpoint);

      var response = SendBlockingWebRequest(uri, formattedJson, out var s);

      if (String.IsNullOrEmpty(response))
      {
        ARLog._Error("Got an empty response");
        status = (AnchoredSceneServiceStatus)s;
        return new _GetExperienceResponse();
      }

      var formattedResponse = _DictionaryToJsonHelper._FormatJsonResponseFromMarsh(response);
      var res = JsonUtility.FromJson<_GetExperienceResponse>(formattedResponse);
      status = AnchoredSceneServiceStatus.Ok;
      return res;
    }

    public _ListExperiencesResponse ListExperiencesInRadius
    (
      _ListExperiencesRequest request, 
      out AnchoredSceneServiceStatus status
    )
    {
      var json = JsonUtility.ToJson(request);
      var uri = String.Format(_listExperiencesFormat, _endpoint);
      var response = SendBlockingWebRequest(uri, json, out var s);
      
      if (String.IsNullOrEmpty(response))
      {
        ARLog._Error("Got an empty response");
        status = (AnchoredSceneServiceStatus)s;
        return new _ListExperiencesResponse();
      }
      
      var formattedResponseJson = 
        _DictionaryToJsonHelper._FormatMultipleJsonResponsesFromMarsh(response);

      var res = JsonUtility.FromJson<_ListExperiencesResponse>(formattedResponseJson);

      status = AnchoredSceneServiceStatus.Ok;
      return res;
    }

    public void DeleteExperience(_DeleteExperienceRequest request, out AnchoredSceneServiceStatus status)
    {
      var json = JsonUtility.ToJson(request);
      var formattedJson = _DictionaryToJsonHelper._FormatJsonRequestForMarsh(json);
      var uri = String.Format(_deleteFormat, _endpoint);
      SendBlockingWebRequest(uri, formattedJson, out var s);

      status = (AnchoredSceneServiceStatus)s;
    }

    public void ReleaseService(IntPtr handle)
    {
      _endpoint = null;
      _appId = null;
    }

    private string SendBlockingWebRequest(string uri, string body, out int status)
    {
      using (UnityWebRequest webRequest = new UnityWebRequest(uri, "POST"))
      {
        byte[] data = Encoding.UTF8.GetBytes(body);
        webRequest.uploadHandler = new UploadHandlerRaw(data);
        webRequest.uploadHandler.contentType = "application/json";
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader(_appIdHeader, _appId);

        var awaiter = webRequest.SendWebRequest();

        while (!awaiter.isDone)
        {
          Thread.Sleep(1);
        }

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
          status = (int)webRequest.responseCode;
          return webRequest.downloadHandler.text;
        }
        else
          status = (int)webRequest.responseCode;

        return null;
      }
    }
  }
}
