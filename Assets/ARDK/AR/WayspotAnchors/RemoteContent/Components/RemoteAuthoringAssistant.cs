using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.VirtualStudio;
using UnityEditor;

#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Threading.Tasks;

using Niantic.ARDK.Utilities.Logging;

using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public partial class EditModeOnlyBehaviour
  {
    [ExecuteInEditMode]
    public class RemoteAuthoringAssistant: MonoBehaviour
    {
      internal const string DIALOG_TITLE = "Remote Authoring Assistant";

      private const string ACTIVE_MANIFEST_KEY = "ARDK_ActiveManifest_Name";

      [SerializeField]
      private VPSLocationManifest _activeManifest;

      public VPSLocationManifest ActiveManifest
      {
        get
        {
          return _activeManifest;
        }
        private set
        {
          _activeManifest = value;
          EditorUtility.SetDirty(this);
        }
      }

      // Surfaces <old, new> manifests
      public static Action<VPSLocationManifest, VPSLocationManifest> ActiveManifestChanged;

      public static Action<VPSLocationManifest> ActiveManifestUpdated;

      [SerializeField]
      private VPSLocationManifest[] _allManifests;

      public IReadOnlyList<VPSLocationManifest> AllManifests
      {
        get
        {
          return _allManifests.AsNonNullReadOnly();
        }
      }

      private List<AuthoredWayspotAnchor> _activeAnchors;

      // List instead of hashset so the RemoteAuthoringEditorWindow has a consistently ordered collection of anchors
      private List<AuthoredWayspotAnchor> SafeActiveAnchors
      {
        get
        {
          // Collections are not serialized, so when scripts are reloaded, _activeAnchors needs
          // to be repopulated.
          if (_activeAnchors == null)
          {
            var found = GameObject.FindObjectsOfType<AuthoredWayspotAnchor>();
            _activeAnchors = new List<AuthoredWayspotAnchor>(found);
          }

          return _activeAnchors;
        }
      }

      public IReadOnlyList<AuthoredWayspotAnchor> ActiveAnchors
      {
        get
        {
          return new ReadOnlyCollection<AuthoredWayspotAnchor>(SafeActiveAnchors);
        }
      }

      public static RemoteAuthoringAssistant FindSceneInstance()
      {
        return GameObject.FindObjectOfType<RemoteAuthoringAssistant>();
      }

      internal void RefreshActiveAnchors()
      {
        _activeAnchors = null;
      }

      internal void OpenLocation(VPSLocationManifest manifest, bool saveActiveLocation = true)
      {
        if (ActiveManifest != null)
        {
          if (saveActiveLocation)
            SaveUnsavedData();
        }
        
        // Need to unload anchors even if ActiveManifest is null, because the ActiveManifest may have just been deleted
        UnloadAnchors();

        var oldManifest = ActiveManifest;

        if (!_ValidateAPIKey())
          ActiveManifest = null;
        else
          ActiveManifest = manifest;

        if (ActiveManifest != null)
        {
          PlayerPrefs.SetString(ACTIVE_MANIFEST_KEY, ActiveManifest.LocationName);
          LoadAnchors(ActiveManifest);
          UpdateMockMesh();
        }
        
        // Newly opened scene, so mark it as not dirty
        if (!string.IsNullOrEmpty(gameObject.scene.path))
          EditorSceneManager.SaveScene(gameObject.scene);
        
        ActiveManifestChanged?.Invoke(oldManifest, ActiveManifest);
      }

      internal static bool _ValidateAPIKey(bool printError = true)
      {
        var apiKey = ArdkGlobalConfig._Internal.GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
          StartupSystems._SetAuthenticationParameters();
          apiKey = ArdkGlobalConfig._Internal.GetApiKey();
          if (string.IsNullOrEmpty(apiKey))
          {
            if (printError)
              ARLog._Error($"An API key must be set in order to use Remote Authoring.");
            return false;
          }
        }

        return true;
      }

      internal void LoadAllManifestsInProject()
      {
        var manifests = _AssetDatabaseUtilities.FindAssets<VPSLocationManifest>();
        var qry = from m in manifests orderby m.LocationName select m;
        _allManifests = qry.ToArray();
      }

      internal void SaveUnsavedData(bool validate = true)
      {
        if (ActiveManifest == null)
          return;

        var saveableAnchors = GetSaveableAnchors();
        if (saveableAnchors.Count == 0)
          return;

        if (validate)
        {
          var msg =
            "Do you want to save the change(s) you made in the VPS Location Manifest: \n" +
            $"{AssetDatabase.GetAssetPath(ActiveManifest)}\n" +
            "Your changes will be lost if you don't.";

          var save = EditorUtility.DisplayDialog
          (
            "Remote Authoring Location Has Been Modified",
            msg,
            "Save",
            "Don't Save"
          );

          if (!save)
            return;
        }

        // Iterate through all anchors to save
        var savedAnchors = 0;
        var totalAnchors = saveableAnchors.Count;
        foreach (var kvp in saveableAnchors)
        {
          savedAnchors++;

          var success = UpdateAnchor(kvp.Key, kvp.Value);

          if (!success && savedAnchors < totalAnchors)
          {
            var keepGoing = EditorUtility.DisplayDialog
            (
              DIALOG_TITLE,
              $"Failed to save anchor \'{kvp.Key._AnchorName}\'. Continue saving other anchors?",
              "Yes",
              "No"
            );

            if (!keepGoing)
            {
              ARLog._Error($"Failed to save VPS Location Manifest {ActiveManifest.LocationName}.");
              return;
            }
          }
        }
        
        ARLog._Release($"Completed saving VPS Location Manifest {ActiveManifest.LocationName}.");
      }

      // Returns dictionary containing all saveable anchors, mapped to if their backing anchor is
      // invalid.
      private Dictionary<AuthoredWayspotAnchor, bool> GetSaveableAnchors()
      {
        var saveableAnchors = new Dictionary<AuthoredWayspotAnchor, bool>();
        foreach (var anchor in SafeActiveAnchors)
        {
          if (anchor == null)
            continue;

          ActiveManifest._GetAnchorData(anchor._AnchorManifestIdentifier, out AuthoredWayspotAnchorData data);
          anchor.GetDifferences(data, out bool isBackingAnchorInvalid, out bool isManifestInvalid);

          if (isBackingAnchorInvalid || isManifestInvalid)
            saveableAnchors.Add(anchor, isBackingAnchorInvalid);
        }

        return saveableAnchors;
      }

      private void UnloadAnchors()
      {
        // Search for all anchors in case anchors have been added manually
        foreach (var anchor in FindObjectsOfType<AuthoredWayspotAnchor>())
          anchor.Destroy();
      }

      private void LoadAnchors(VPSLocationManifest manifest)
      {
        _activeAnchors = new List<AuthoredWayspotAnchor>();

        if (manifest == null)
          return;

        foreach (var anchorData in manifest.AuthoredAnchorsData)
        {
          ARLog._Debug($"Loading anchor {anchorData.Name}");

          var anchorGo = AuthoredWayspotAnchor._Create(anchorData);
          var anchor = anchorGo.GetComponent<AuthoredWayspotAnchor>();
          _activeAnchors.Add(anchor);
        }
      }

      internal void AddEmptyAnchorToScene(Vector3 position, Vector3 rotation)
      {
        var anchorData =
          new AuthoredWayspotAnchorData
          (
            name: _SceneHierarchyUtilities.BuildObjectName("Anchor"),
            identifier: Guid.NewGuid().ToString(),
            payload: null,
            position: position,
            rotation: rotation,
            scale: Vector3.one,
            tags: null,
            prefab: new AuthoredWayspotAnchorData.PrefabData(Guid.NewGuid().ToString()),
            manifestIdentifier: Guid.NewGuid().ToString()
          );

        var anchorGo = AuthoredWayspotAnchor._Create(anchorData);
        var anchor = anchorGo.GetComponent<AuthoredWayspotAnchor>();
        Selection.activeGameObject = anchorGo;

        SafeActiveAnchors.Add(anchor);
      }

      internal void RemoveAnchor(AuthoredWayspotAnchor anchor)
      {
        anchor.Destroy();
        ActiveManifest._Remove(anchor._AnchorManifestIdentifier);
        SafeActiveAnchors.Remove(anchor);
        ActiveManifestUpdated?.Invoke(ActiveManifest);
      }

      internal bool UpdateAnchor(AuthoredWayspotAnchor anchor, bool createBacking)
      {
        if (string.IsNullOrEmpty(anchor._AnchorName))
        {
          ARLog._WarnRelease($"Cannot save \'{anchor.gameObject.name}\' with an empty name.");
          return false;
        }

        if (ActiveManifest.AuthoredAnchorsData.Any(a => a.Name == anchor._AnchorName && a._ManifestIdentifier != anchor._AnchorManifestIdentifier))
        {
          ARLog._WarnRelease
          (
            $"Cannot save \'{anchor.gameObject.name}\' with the name \'{anchor._AnchorName}\', " +
            "because an anchor with that name already exists in this manifest."
          );

          return false;
        }
        
        if (createBacking)
        {
          var anchorTransform = anchor.gameObject.transform;
          var pos = anchorTransform.position;
          var rot = anchorTransform.rotation;

          var pose = Matrix4x4.TRS(pos, rot, Vector3.one);
          var task = Task.Run(() => _AuthoringUtilities.CreateRelativeToAnchorAtNodeOriginWithFallback(
              pose, ActiveManifest._NodeIdentifier, ActiveManifest._MeshOriginAnchorPayload));
          task.Wait();

          var (identifier, payload) = task.Result;
          if (string.IsNullOrEmpty(identifier))
            return false;

          ActiveManifest._AddOrUpdateAnchorData
          (
            anchor._AnchorManifestIdentifier,
            anchorName: anchor._AnchorName,
            anchorIdentifier: identifier,
            payload: payload,
            position: pos,
            rotation: rot.eulerAngles,
            scale: anchorTransform.localScale,
            tags: anchor._Tags,
            prefab: anchor._Prefab
          );
        }
        else
        {
          ActiveManifest._AddOrUpdateAnchorData
          (
            anchor._AnchorManifestIdentifier,
            anchorName: anchor._AnchorName,
            tags: anchor._Tags,
            scale: anchor.gameObject.transform.localScale,
            prefab: anchor._Prefab
          );
        }

        ActiveManifest._GetAnchorData(anchor._AnchorManifestIdentifier, out AuthoredWayspotAnchorData data);

        var scene = anchor.gameObject.scene;
        if (scene.isLoaded)
        {
          // If method was invoked manually, instead of triggered by auto-saving upon scene unload
          anchor._ResetToData(data);

          // If no other anchors need saving, then the Remote Authoring scene is "clean"
          // and shouldn't trigger a dialog requesting to save it on close.
          if (scene.isDirty && GetSaveableAnchors().Count == 0)
            EditorSceneManager.SaveScene(scene);
        }
        
        ActiveManifestUpdated?.Invoke(ActiveManifest);
        return true;
      }

      internal static RemoteAuthoringAssistant _Create(Scene scene)
      {
        if (FindObjectsOfType<RemoteAuthoringAssistant>().Length > 0)
        {
          ARLog._Release("Only one RemoteAuthoringAssistant can exist per scene.");
          return null;
        }
        
        var name = "RemoteAuthoringAssistant";
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);

        go.hideFlags = HideFlags.HideInHierarchy;
        go.AddComponent<_TransformFixer>();
        
        var ra = go.AddComponent<RemoteAuthoringAssistant>();
        ra.LoadAllManifestsInProject();

        var prefManifestName = PlayerPrefs.GetString(ACTIVE_MANIFEST_KEY);
        if (!string.IsNullOrEmpty(prefManifestName))
        {
          var matches = ra.AllManifests.Where(m => string.Equals(m.LocationName, prefManifestName));
          if (matches.Any())
            ra.OpenLocation(matches.First());
        }
        else if (ra.AllManifests.Count > 0)
        {
          ra.OpenLocation(ra.AllManifests[0]);
        }

        return ra;
      }

      private void Reset()
      {
        if (!ValidateSingleton())
          DestroyImmediate(gameObject);
      }

      private void Awake()
      {
        // Need here in addition to on Reset because Reset is not invoked when component is duplicated
        // from Hierarchy
        if (!ValidateSingleton())
          DestroyImmediate(gameObject);
      }

      private void OnDestroy()
      {
        var isSceneDirty = gameObject.scene.isDirty;
        if (!isSceneDirty)
        {
          // Dev has already saved scene, indicating they want to save their changes.
          SaveUnsavedData(false);
        }
        else
        {
          SaveUnsavedData(true);
        }
        ActiveManifestUpdated?.Invoke(ActiveManifest);
      }

      private static bool ValidateSingleton()
      {
        if (FindObjectsOfType<RemoteAuthoringAssistant>().Length > 1)
        {
          ARLog._WarnRelease("Only one RemoteAuthoringAssistant can exist per scene.");
          return false;
        }

        return true;
      }
      
      private void UpdateMockMesh()
      {
        var launcher =
          (_MockModeLauncher)_VirtualStudioLauncher.GetOrCreateModeLauncher(RuntimeEnvironment.Mock);

        if (ActiveManifest._MockAsset == null)
        {
          // If you dont have a mock mesh, make one.
          ActiveManifest._CreateMockAsset();
        }

        // Assign the correct guid to the virtual studio launcher
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ActiveManifest._MockAsset));
        launcher.SceneGuid = guid;
      }

    }
  }
}
#endif
