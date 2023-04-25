using System;
using System.Collections.Generic;
using Niantic.ARDK.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;
using AuthoredWayspotAnchor = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.AuthoredWayspotAnchor;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  // Code for AuthoredWayspotAnchor Tags commented out until feature is more thought out
  [CustomEditor(typeof(AuthoredWayspotAnchor))]
  internal class _AuthoredWayspotAnchorInspector: UnityEditor.Editor
  {
    private AuthoredWayspotAnchor Target { get { return (AuthoredWayspotAnchor)target; } }

    private float _fullWidth;
    private float _thirdWidth;
    private float _colOneWidth;
    private float _colTwoWidth;

    private void RecalculateWidths()
    {
      _fullWidth = GUILayoutUtility.GetLastRect().width;
      _thirdWidth = _fullWidth * 0.33f;
      _colOneWidth = _fullWidth * 0.25f;
      _colTwoWidth = _fullWidth - _colOneWidth;
    }

    private RemoteAuthoringAssistant _raAssistant;

    private RemoteAuthoringAssistant SafeRemoteAuthoringAssistant
    {
      get
      {
        if (_raAssistant == null)
          _raAssistant = RemoteAuthoringAssistant.FindSceneInstance();

        return _raAssistant;
      }
    }

    private bool _showPrefabs;

    private Dictionary<AuthoredWayspotAnchorData.PrefabData, GameObject> _prefabAssets;

    public Dictionary<AuthoredWayspotAnchorData.PrefabData, GameObject> SafePrefabAssets
    {
      get
      {
        if (!ArePrefabDictionariesValid())
          RebuildPrefabDictionaries();

        return _prefabAssets;
      }
    }

    private _AnchorPrefabVisualizer _prefabVisualizer;

    private _AnchorPrefabVisualizer SafePrefabVisualizer
    {
      get
      {
        if (_prefabVisualizer == null)
          _prefabVisualizer = _RemoteAuthoringPresenceManager.GetVisualizer<_AnchorPrefabVisualizer>();

        return _prefabVisualizer;
      }
    }

    [SerializeField]
    private string _cachedWayspotIdentifier;

    private bool ArePrefabDictionariesValid()
    {
      // If one of these dictionaries is null, they'll all be null, because they're null due to the
      // scripts being reloaded or this being a new instance of the Inspector.
      return _prefabAssets != null && string.Equals(_cachedWayspotIdentifier, Target._AnchorManifestIdentifier);
    }

    private void RebuildPrefabDictionaries()
    {
      _cachedWayspotIdentifier = Target._AnchorManifestIdentifier;

      _prefabAssets = new Dictionary<AuthoredWayspotAnchorData.PrefabData, GameObject>();

      if (Target._Prefab != null)
        _prefabAssets.Add(Target._Prefab, Target._Prefab.Asset);
    }

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      // Invisible label so width can be fetched
      GUILayout.Label("");

      if (Event.current.type == EventType.Repaint)
        RecalculateWidths();

      var isSerialized =
        SafeRemoteAuthoringAssistant.ActiveManifest._GetAnchorData
        (
          Target._AnchorManifestIdentifier,
          out AuthoredWayspotAnchorData serializedAnchor
        );

      Target.GetDifferences(serializedAnchor, out bool isBackingAnchorInvalid, out bool isManifestInvalid);

      var transform = Target.transform;
      var currPos = transform.position;
      var currRot = TransformUtils.GetInspectorRotation(Target.transform);
      var currScale = transform.localScale;
      var widthTransformFields = _fullWidth * .42f;

      bool isAnchorNameDirty = !isSerialized;
      bool isAnchorPositionDirty = !isSerialized;
      bool isAnchorRotationDirty = !isSerialized;
      bool isAnchorScaleDirty = !isSerialized;
      bool arePrefabsDirty = !isSerialized;
      
      if (isSerialized)
      {
        isAnchorNameDirty = !string.Equals(serializedAnchor.Name, Target._AnchorName);

        isAnchorPositionDirty = currPos != serializedAnchor.Position;
        isAnchorRotationDirty = currRot != serializedAnchor.Rotation;
        isAnchorScaleDirty = currScale != serializedAnchor.Scale;
        
        arePrefabsDirty = (serializedAnchor.AssociatedPrefab == null) != (Target._Prefab == null);
        if (!arePrefabsDirty)
        {
          arePrefabsDirty = serializedAnchor.AssociatedPrefab.ValuesDifferFrom(Target._Prefab);
        }
      }

      GUILayout.Space(10); 

      using (var scope = new GUILayout.HorizontalScope())
      {
        var style = isAnchorPositionDirty ? CommonStyles.BoldLabelStyle : EditorStyles.label;
        GUILayout.Label("Position: ", style, GUILayout.Width(widthTransformFields));
        currPos = EditorGUILayout.Vector3Field("", currPos);
        transform.position = currPos;

      }

      using (var scope = new GUILayout.HorizontalScope())
      {
        var style = isAnchorRotationDirty ? CommonStyles.BoldLabelStyle : EditorStyles.label;
        GUILayout.Label("Rotation: ", style, GUILayout.Width(widthTransformFields));
        currRot = EditorGUILayout.Vector3Field("", currRot);
        transform.rotation = Quaternion.Euler(currRot);
      }

      using (var scope = new GUILayout.HorizontalScope())
      {
        var style = isAnchorScaleDirty ? CommonStyles.BoldLabelStyle : EditorStyles.label;
        GUILayout.Label("Scale: ", style, GUILayout.Width(widthTransformFields));
        currScale = EditorGUILayout.Vector3Field("", currScale);
        transform.localScale = currScale;
      }

      GUILayout.Space(10);
      using (var scope = new GUILayout.HorizontalScope())
      {
        GUILayout.Label
        (
          "Payload",
          GUILayout.Width(_colOneWidth)
        );

        if (!isBackingAnchorInvalid)
          DrawAnchorPayloadGUI(serializedAnchor.Payload);
        else
          GUILayout.Label("Invalid");

      }

      GUILayout.Space(10);

      using (var scope = new GUILayout.HorizontalScope())
      {
        var style = arePrefabsDirty ? CommonStyles.BoldLabelStyle : EditorStyles.label;

        GUILayout.Label("Associated Prefab", style, GUILayout.Width(_colOneWidth));
        DrawPrefabAssetGUI(Target._Prefab);
      }

      GUILayout.Space(30);

      if (isAnchorScaleDirty)
      {
        EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);
      }
    }
    
    private float _timeout;

    private void DrawAnchorPayloadGUI(string payload)
    {
      GUILayout.BeginVertical();
      var payloadHint = payload.Substring(0, 20) + "...";
      if (GUILayout.Button(payloadHint, _VPSLocationManifestInspector.PayloadStyle))
      {
        GUIUtility.systemCopyBuffer = payload;
        _timeout = Time.realtimeSinceStartup + 1;
      }

      if (Time.realtimeSinceStartup < _timeout)
        GUILayout.Label("Copied!", CommonStyles.CenteredLabelStyle);
      else
        GUILayout.Label("Click to copy", CommonStyles.CenteredLabelStyle);

      GUILayout.EndVertical();
    }

    public void UpdateSafeAssets(Dictionary<AuthoredWayspotAnchorData.PrefabData, GameObject> safeAssets)
    {
      SafePrefabAssets.Clear();
      foreach (var asset in safeAssets)
      {
        SafePrefabAssets.Add(asset.Key, asset.Value);
      }
    }
    private void DrawPrefabAssetGUI(AuthoredWayspotAnchorData.PrefabData prefabData)
    {
      SafePrefabAssets.TryGetValue(prefabData, out GameObject oldAsset);

      var currAsset = EditorGUILayout.ObjectField(oldAsset, typeof(GameObject), false) as GameObject;

      if (currAsset == oldAsset)
        return;

      if (oldAsset != null)
        SafePrefabVisualizer.RemovePrefab(Target, prefabData.Identifier);

      prefabData.Reset();
      if (currAsset != null)
      {
        prefabData.Asset = currAsset;
        SafePrefabAssets[prefabData] = currAsset;
        SafePrefabVisualizer.AddPrefab(Target, prefabData);
      }
      else
      {
        SafePrefabAssets.Remove(prefabData);
      }
    }
  }
}
