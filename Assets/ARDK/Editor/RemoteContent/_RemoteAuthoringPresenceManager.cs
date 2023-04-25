using System;
using System.Linq;
using System.Threading.Tasks;
using Niantic.ARDK.Editor;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Editor;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;
using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  [InitializeOnLoad]
  internal class _RemoteAuthoringPresenceManager: IPreprocessBuildWithReport, IPostprocessBuildWithReport
  {
    private static _IContentVisualizer[] _visualizers;

    private const string SCENE_NAME = "Remote Authoring (Editor Only)";
    private const string INIT_KEY = "ARDK_RA_Initialized";
    
    private static Color _prettyBackgroundColor = new Color32(194, 100, 0, 255);

    private static bool _isBuilding;
    private static bool _canUseRemoteAuthoringOverride = true;
    
    // Have to use this to keep track of whether Play Mode is fully exited or not, because
    // the value of `EditorApplication.isPlayingOrWillChangePlaymode` is false between exiting play mode
    // and entering edit mode, and scene management isn't allowed outside pure edit mode.
    private static PlayModeStateChange _lastPlayModeStageChange = PlayModeStateChange.EnteredEditMode;
    public static bool CanUseRemoteAuthoring
    {
      get
      {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               _lastPlayModeStageChange == PlayModeStateChange.EnteredEditMode &&
               !_isBuilding &&
               _canUseRemoteAuthoringOverride;
      }
    }

    private static Scene GetAuthoringScene()
    {
      return EditorSceneManager.GetSceneByName(SCENE_NAME);
    }

    static _RemoteAuthoringPresenceManager()
    {
      _visualizers =
        new _IContentVisualizer[]
        {
          new _WayspotMeshVisualizer(),
          new _AnchorPrefabVisualizer()
        };

      EditorSceneManager.sceneOpened += OnSceneOpened;
      EditorSceneManager.sceneClosing += OnSceneClosing;

      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

      RemoteAuthoringAssistant.ActiveManifestChanged += UpdateDisplay;

      BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuild);

      // Have to delay call because no scenes are loaded on Editor launch frame
      EditorApplication.delayCall += () =>
      {
        if (!SessionState.GetBool(INIT_KEY, false))
        {
          SessionState.SetBool(INIT_KEY, true);

          if (EditorWindow.HasOpenInstances<_RemoteAuthoringEditorWindow>())
          {
            _RemoteAuthoringEditorWindow.CloseWindow();
            EditorApplication.delayCall += _RemoteAuthoringEditorWindow.ShowWindow;
          }
          else
            RemovePresence();
        }
      };
    }

    public static T GetVisualizer<T>() where T: _IContentVisualizer
    {
      return (T)_visualizers.FirstOrDefault(v => v.GetType() == typeof(T));
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
      _lastPlayModeStageChange = change;
      switch (change)
      {
        case PlayModeStateChange.EnteredEditMode:
          ReinstatePresence();
          break;
        case PlayModeStateChange.ExitingEditMode:
          // RA scene is only scene open
          if (SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == SCENE_NAME)
          {
            EditorUtility.DisplayDialog
            (
              RemoteAuthoringAssistant.DIALOG_TITLE,
              "Remote Authoring scene can not be opened in Play Mode. Returning to Edit Mode.",
              "OK"
            );
          }
          else
          {
            PausePresence();
          }
         
          break;

        case PlayModeStateChange.EnteredPlayMode:
          if (SceneManager.GetActiveScene().name == SCENE_NAME)
            EditorApplication.ExitPlaymode();
          
          break;

        case PlayModeStateChange.ExitingPlayMode:
          break;
      }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
      if (_isBuilding)
        return;

      if (scene.name != SCENE_NAME)
      {
        if (!GetAuthoringScene().isLoaded)
          ReinstatePresence();
      }
      else
      {
        _HierarchyMonitor.Enable(SceneManager.GetActiveScene());
      }
    }

    private static void OnSceneClosing(Scene scene, bool removingScene)
    {
      if (scene.name == SCENE_NAME)
      {
        _RemoteAuthoringEditorWindow.CloseWindow();
        _HierarchyMonitor.Disable();
      }
    }

    // Must check for and unload Authoring scene before build, because leaving it be results in it
    // being reloaded with missing scripts after the build completes. Must be done in this method
    // instead of OnPreprocessBuild, because scene will not fully unload(?) in latter
    private static void OnBuild(BuildPlayerOptions options)
    {
      var scene = GetAuthoringScene();
      if (scene.isLoaded)
      {
        _HierarchyMonitor.Disable();

        var asyncOperation = SceneManager.UnloadSceneAsync(scene);
        asyncOperation.completed += (o) => BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
      }
      else
      {
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
      }
    }

    
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildReport report)
    {
      _isBuilding = true;
    }

    public void OnPostprocessBuild(BuildReport report)
    {
      _isBuilding = false;

      // Cannot call ReinstatePresence here, because this callback is invoked
      // when scenes still haven't been reloaded
    }
    
    public static RemoteAuthoringAssistant AddPresence()
    {
      var authoringScene = GetAuthoringScene();
      var activeScene = SceneManager.GetActiveScene();
      var activeSceneIsNewScene = string.IsNullOrEmpty(activeScene.path);

      var raa = RemoteAuthoringAssistant.FindSceneInstance();
      if (authoringScene.isLoaded)
      {
        if (raa == null)
        {
          // RAA may be in weird state when it is auto-opened on Editor reboot, so just reload it 
          foreach (var go in authoringScene.GetRootGameObjects())
            GameObject.DestroyImmediate(go);
          
          SceneManager.SetActiveScene(authoringScene);
        }
        else
        {
          return raa;
        }
      }
      else
      {
        if (activeSceneIsNewScene)
        {
          var willSave =
            EditorUtility.DisplayDialog
            (
              RemoteAuthoringAssistant.DIALOG_TITLE,
              "Save the currently opened scene in order to open Remote Authoring.",
              "OK",
              "Cancel"
            );

          if (willSave)
          {
            EditorSceneManager.SaveScene(activeScene);
            authoringScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
          }
          else
          {
            // Set override to false to prevent more AddPresence calls in this frame.
            _canUseRemoteAuthoringOverride = false;
            
            // Need to close the window or else there are continual calls to AddPresence
            EditorApplication.delayCall += () =>
            {
              _canUseRemoteAuthoringOverride = true;
              _RemoteAuthoringEditorWindow.CloseWindow();
            };
            return null;
          }
            
        }
        else
        {
          // Case where scene has been unloaded but not removed. This happens if dev manually unloads
          // scene, and sometimes when exiting Play Mode.
          if (authoringScene.IsValid())
          {
            // Need to close scene, because scene has not been saved as an asset and thus cannot be
            // opened.
            EditorSceneManager.CloseScene(authoringScene, true);
            EditorApplication.delayCall += _RemoteAuthoringEditorWindow.ShowWindow;
          }
        
          authoringScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }
      }
      
      // Add hierarchy warning
      var warning = new GameObject("Do NOT directly add/delete objects to this scene");
      var pretty = warning.AddComponent<_PrettyHeirarchyItem>();
      pretty.BackgroundColor = _prettyBackgroundColor;
      
      // Add RAA
      var ra = RemoteAuthoringAssistant._Create(authoringScene);
      if (!EditorSceneManager.SaveScene(authoringScene,
            $"{Application.temporaryCachePath}/{SCENE_NAME}.unity"))
      {
        ARLog._Release("Unable to create VPS Authoring scene at this time.");
        EditorSceneManager.UnloadSceneAsync(authoringScene);
        return null;
      }
      if (!activeSceneIsNewScene)
        SceneManager.SetActiveScene(activeScene);

      EditorApplication.delayCall += () => _HierarchyMonitor.Enable(activeScene);

      return ra;
    }
    
    public static void RemovePresence()
    {
      PausePresence();
    }

    private static void ReinstatePresence()
    {
      if (EditorWindow.HasOpenInstances<_RemoteAuthoringEditorWindow>())
        AddPresence();
    }

    private static void PausePresence(bool reload = false)
    {
      var scene = GetAuthoringScene();
      if (scene.isLoaded)
      {
        _HierarchyMonitor.Disable();

        if (SceneManager.sceneCount == 1)
        {
          EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
          if (reload)
            AddPresence();
        }
        else
        {
          var asyncOperation = SceneManager.UnloadSceneAsync(scene);
          if (reload)
            asyncOperation.completed += (o) => AddPresence();
        }
      }

      // Need to set the active scene to one other than the AuthoringScene,
      // else new scene will be opened by AddPresence
      var newActiveScene = SceneManager.GetSceneAt(0);
      if (newActiveScene.isLoaded) // Might not be loaded when File > New Scene is used
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
    }

    private static void UpdateDisplay(VPSLocationManifest prev, VPSLocationManifest curr)
    {
      foreach (var viewer in _visualizers)
        viewer.UpdateDisplay(prev, curr);
    }
  }
}
