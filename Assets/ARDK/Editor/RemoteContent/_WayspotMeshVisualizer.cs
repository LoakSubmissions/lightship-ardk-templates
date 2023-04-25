using System;
using System.Collections.Generic;
using Niantic.ARDK.Utilities.Editor;

using UnityEditor;

using UnityEngine;

using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _WayspotMeshVisualizer: _IContentVisualizer
  {
    private HashSet<GameObject> _wayspotMeshes;

    private HashSet<GameObject> SafeWayspotMeshes
    {
      get
      {
        if (_wayspotMeshes == null)
          RefreshMeshReferences();

        return _wayspotMeshes;
      }
    }

    private void RefreshMeshReferences()
    {
      var ra = RemoteAuthoringAssistant.FindSceneInstance();
      if (ra != null)
      {
        var meshes = _SceneHierarchyUtilities.FindGameObjects<EditModeOnlyBehaviour._VisualizedWayspotTag>(null, ra.transform);
        _wayspotMeshes = new HashSet<GameObject>(meshes);
      }
      else
      {
        _wayspotMeshes = new HashSet<GameObject>();
      }
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
      foreach (var mesh in SafeWayspotMeshes)
        GameObject.DestroyImmediate(mesh);

      SafeWayspotMeshes.Clear();
    }

    private const string MESH_NAME_SUFFIX = " (Editor Only Wayspot Mesh)";
    public void UpdateDisplay(VPSLocationManifest prev, VPSLocationManifest curr)
    {
      if (prev != null)
        DestroyDisplays();

      // Happens when RemoteAuthoringAssistant component is reset
      if (curr == null)
      {
        var childrenMeshes = _SceneHierarchyUtilities.FindGameObjects<EditModeOnlyBehaviour._VisualizedWayspotTag>();
        foreach (var child in childrenMeshes)
          GameObject.DestroyImmediate(child);

        return;
      }

      var ra = RemoteAuthoringAssistant.FindSceneInstance();
      var mesh = CreateMeshObject(curr.LocationName, curr.Mesh, curr.Material, ra.gameObject);

      var currSelection = Selection.activeGameObject;
      try
      {
        SceneView.lastActiveSceneView.Frame(mesh.GetComponent<MeshFilter>().sharedMesh.bounds);
      }
      catch (NullReferenceException)
      {
        // Sometimes method runs into this exception on startup, but the null value is not check-able
        // outside of compiled Unity code, so just ignore it
      }
      
      Selection.activeGameObject = currSelection;
    }

    private GameObject CreateMeshObject(string name, UnityEngine.Mesh mesh, Material material, GameObject root, bool isSceneOnly = true)
    {
      var go = new GameObject(name + MESH_NAME_SUFFIX);
      SafeWayspotMeshes.Add(go);

      var meshFilter = go.AddComponent<MeshFilter>();
      meshFilter.sharedMesh = mesh;

      var renderer = go.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = material;

      go.transform.SetParent(root.transform, false);

      if (isSceneOnly)
      {
        go.AddComponent<EditModeOnlyBehaviour._VisualizedWayspotTag>();
        var collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
        go.transform.SetSiblingIndex(0);
      }

      return go;
    }
  }
}