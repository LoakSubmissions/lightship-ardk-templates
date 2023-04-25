// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.LocationService;
using Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  internal class _FakeAnchoredSceneServiceImpl : 
    _IAnchoredSceneServiceImpl
  {
    internal static readonly _FakeAnchoredSceneServiceImpl _Instance = new _FakeAnchoredSceneServiceImpl();
    private string _appId;
    private string _endpoint;
    private readonly Dictionary<string, _ExperienceCommon> _experiences = new Dictionary<string, _ExperienceCommon>();

    public IntPtr InitializeService(string endpoint, string appId)
    {
      _endpoint = endpoint;
      _appId = appId;
      return new IntPtr(1);
    }

    public _CreateExperienceResponse CreateExperience(_CreateExperienceRequest request, out AnchoredSceneServiceStatus status)
    {
      var exp = new _ExperienceCommon()
      {
        experienceId = Guid.NewGuid().ToString(),
        name = request.name,
        description = request.description,
        emptyRoomTimeoutSeconds = 0,
        initData = request.initData,
        appId = null,
        lat = request.lat,
        lng = request.lng
      };

      _experiences[exp.experienceId] = exp;

      var res = new _CreateExperienceResponse()
      {
        experience = exp
      };

      status = AnchoredSceneServiceStatus.Ok;
      return res;
    }

    public _GetExperienceResponse GetExperience(_GetExperienceRequest request, out AnchoredSceneServiceStatus status)
    {
      if (!_experiences.ContainsKey(request.experienceId))
      {
        status = AnchoredSceneServiceStatus.NotFound;
        return new _GetExperienceResponse();
      }

      status = AnchoredSceneServiceStatus.Ok;
      return new _GetExperienceResponse() { experience = _experiences[request.experienceId] };
    }

    public _ListExperiencesResponse ListExperiencesInRadius
    (
      _ListExperiencesRequest request,
      out AnchoredSceneServiceStatus status
    )
    {
      var listExperiences = new List<_ExperienceCommon>();
      var requestLatLng = new LatLng(request.filter.circle.lat, request.filter.circle.lng);
      foreach (var experience in _experiences.Values)
      {
        var expLatLng = new LatLng(experience.lat, experience.lng);
        if (requestLatLng.Distance(expLatLng) <= request.filter.circle.radiusMeters)
        {
          listExperiences.Add(experience);
        }
      }

      status = AnchoredSceneServiceStatus.Ok;
      return new _ListExperiencesResponse() {experiences = listExperiences};
    }

    public void DeleteExperience(_DeleteExperienceRequest request, out AnchoredSceneServiceStatus status)
    {
      _experiences.Remove(request.experienceId);
      status = AnchoredSceneServiceStatus.Ok;
    }
    
    public void ReleaseService(IntPtr handle)
    {
      // Do nothing
      _endpoint = null;
      _appId = null;
      _experiences.Clear();
    }
  }
}