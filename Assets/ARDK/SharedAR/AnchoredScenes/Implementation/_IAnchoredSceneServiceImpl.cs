// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes
{
  // Provide an interface to native C-API calls 
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  internal interface _IAnchoredSceneServiceImpl
  {
    // Initialize the underlying service
    IntPtr InitializeService(string endpoint, string appId);

    _CreateExperienceResponse CreateExperience(_CreateExperienceRequest request, out AnchoredSceneServiceStatus status);

    _GetExperienceResponse GetExperience(_GetExperienceRequest request, out AnchoredSceneServiceStatus status);
    
    _ListExperiencesResponse ListExperiencesInRadius(_ListExperiencesRequest request, out AnchoredSceneServiceStatus status);
    
    // No return type now, rely on status code
    void DeleteExperience(_DeleteExperienceRequest request, out AnchoredSceneServiceStatus status);

    // Release a held service
    void ReleaseService(IntPtr handle);
  }
}