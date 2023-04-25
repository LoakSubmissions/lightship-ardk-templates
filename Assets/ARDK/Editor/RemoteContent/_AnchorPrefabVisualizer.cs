using Niantic.ARDK.Utilities.Editor;

using UnityEditor;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Niantic.ARDK.Utilities.Collections;

using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;
using AuthoredWayspotAnchor = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.AuthoredWayspotAnchor;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _AnchorPrefabVisualizer: _IContentVisualizer
  {
    // Mapping of PrefabData.Identifier to the spawned GameObject
    private Dictionary<string, Dictionary<string, GameObject>> _visualizedPrefabs;
    private Dictionary<string, Dictionary<string, GameObject>> SafeVisualizedPrefabs
    {
      get
      {
        if (_visualizedPrefabs == null)
          RebuildVisualizedPrefabsDictionary();

        return _visualizedPrefabs;
      }
    }

    private void RebuildVisualizedPrefabsDictionary()
    {
      _visualizedPrefabs = new Dictionary<string, Dictionary<string, GameObject>>();

      var anchors = GameObject.FindObjectsOfType<AuthoredWayspotAnchor>();
      foreach (var anchor in anchors)
      {
        var prefabGuidsToGameObjects = new Dictionary<string, GameObject>();
        var prefabTags = _SceneHierarchyUtilities.FindComponents<EditModeOnlyBehaviour._VisualizedPrefabTag>(null, anchor.transform);
        foreach (var prefabTag in prefabTags)
          prefabGuidsToGameObjects.Add(prefabTag.PrefabIdentifier, prefabTag.gameObject);

        _visualizedPrefabs.Add(anchor._AnchorManifestIdentifier, prefabGuidsToGameObjects);
      }
    }

    public GameObject GetSpawnedPrefab(AuthoredWayspotAnchor anchor, string prefabIdentifier)
    {
      if (!SafeVisualizedPrefabs.ContainsKey(anchor._AnchorManifestIdentifier))
        RebuildVisualizedPrefabsDictionary();

      if (SafeVisualizedPrefabs.ContainsKey(anchor._AnchorManifestIdentifier))
      {
        if (SafeVisualizedPrefabs[anchor._AnchorManifestIdentifier].TryGetValue(prefabIdentifier, out GameObject go))
          return go;
      }

      return null;
    }

    public void AddPrefab(AuthoredWayspotAnchor anchor, AuthoredWayspotAnchorData.PrefabData prefabData)
    {
      InstantiatePrefab(prefabData, anchor);
    }

    public void RemovePrefab(AuthoredWayspotAnchor anchor, string prefabIdentifier)
    {
      var go = GetSpawnedPrefab(anchor, prefabIdentifier);
      if (go == null)
        return;

      GameObject.DestroyImmediate(go);
      SafeVisualizedPrefabs[anchor._AnchorManifestIdentifier].Remove(prefabIdentifier);
    }

    public void CreateDisplays()
    {
      var ra = RemoteAuthoringAssistant.FindSceneInstance();
      if (ra != null)
        UpdateDisplay(null, ra.ActiveManifest);
    }

    public void DestroyDisplays()
    {
      // Cannot use UpdateDisplay because RemoteAuthoringAssistant component may already have
      // been destroyed, meaning no ActiveManifest value can be got
      foreach (var prefabTag in GameObject.FindObjectsOfType<EditModeOnlyBehaviour._VisualizedPrefabTag>(true))
      {
        GameObject.DestroyImmediate(prefabTag.gameObject);
      }

      _visualizedPrefabs = null;
    }

    private const string PREFAB_NAME_SUFFIX = "(Prefab View)";
    public void UpdateDisplay(VPSLocationManifest prev, VPSLocationManifest curr)
    {
      if (prev != null)
        DestroyDisplays();

      // Happens when RemoteAuthoringAssistant component is reset
      if (curr == null)
      {
        var spawnedPrefabs = _SceneHierarchyUtilities.FindGameObjects<EditModeOnlyBehaviour._VisualizedPrefabTag>();
        foreach (var go in spawnedPrefabs)
          GameObject.DestroyImmediate(go);

        return;
      }

      var ra = RemoteAuthoringAssistant.FindSceneInstance();
      var anchors = ra.ActiveAnchors;
      foreach (var anchor in anchors)
      {
        var prefabData = anchor._Prefab;
        if (prefabData != null && prefabData.Asset != null)
          InstantiatePrefab(prefabData, anchor);
      }
    }

    private GameObject InstantiatePrefab(AuthoredWayspotAnchorData.PrefabData prefabData, AuthoredWayspotAnchor owner)
    {
      var go = (GameObject) PrefabUtility.InstantiatePrefab(prefabData.Asset, owner.transform);
      go.name = prefabData.Asset.name + PREFAB_NAME_SUFFIX;

      if (!SafeVisualizedPrefabs.ContainsKey(owner._AnchorManifestIdentifier))
        SafeVisualizedPrefabs.Add(owner._AnchorManifestIdentifier, new Dictionary<string, GameObject>());

      // Need to add to dictionary BEFORE adding _VisualizedPrefabTag component in case the dictionary
      // gets rebuilt
      var anchorsPrefabs = SafeVisualizedPrefabs[owner._AnchorManifestIdentifier];
      if (!anchorsPrefabs.ContainsKey(prefabData.Identifier))
        anchorsPrefabs.Add(prefabData.Identifier, go);
      else
        anchorsPrefabs[prefabData.Identifier] = go;

      var viewTag = go.AddComponent<EditModeOnlyBehaviour._VisualizedPrefabTag>();
      viewTag.Initialize(prefabData);

      go.AddComponent<_TransformFixer>();

      go.hideFlags = HideFlags.HideInHierarchy;

      SceneView.lastActiveSceneView.Repaint();

      return go;
    }
  }
}
