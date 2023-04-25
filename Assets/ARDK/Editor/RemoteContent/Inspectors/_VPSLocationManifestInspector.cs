using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.Editor;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Mock;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  [CustomEditor(typeof(VPSLocationManifest))]
  internal class _VPSLocationManifestInspector: UnityEditor.Editor
  {
    private VPSLocationManifest Target { get { return (VPSLocationManifest)target; } }

    private int _colOneWidth = 200;
    private bool _anchorFoldoutState = false;

    public override void OnInspectorGUI()
    {
      GUILayout.BeginHorizontal();
      {
        GUILayout.Label("Location Name", GUILayout.Width(_colOneWidth));
        var newLocationName = EditorGUILayout.DelayedTextField(Target.LocationName);
        if (!string.Equals(Target.LocationName, newLocationName))
          Target.LocationName = newLocationName;
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        GUILayout.Label("Wayspot Mesh", GUILayout.Width(_colOneWidth));
        DrawMeshGUI();
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        GUILayout.Label("Mock Asset", GUILayout.Width(_colOneWidth));
        DrawMockAssetGUI();
      }
      GUILayout.EndHorizontal();

      GUILayout.Space(20);

      _anchorFoldoutState =
        EditorGUILayout.Foldout
        (
          _anchorFoldoutState,
          $"Authored Wayspot Anchors ({Target.AuthoredAnchorsData.Count})"
        );

      if (_anchorFoldoutState)
      {
        EditorGUI.indentLevel++;

        foreach (var anchorData in Target.AuthoredAnchorsData)
        {
          DrawAnchorGUI(anchorData);
          GUILayout.Space(5);
        }

        EditorGUI.indentLevel--;
      }

      GUILayout.Space(50);

      if (GUILayout.Button("Export as JSON"))
      {
        const string saveTitle = "Save VPS Location Manfiest as JSON";
        var path =
          EditorUtility.SaveFilePanel
          (
            saveTitle,
            Target._JsonExportPath,
            Target.LocationName,
            "json"
          );

        if (path.Length > 0)
        {
          Debug.Log(path);
          File.WriteAllText(path, Target.ExportToJson());

          var targetDirs = path.Split('/');
          var projectDirs = Application.dataPath.Split('/');

          var jsonSavedToProject = true;
          for (int i = 0; i < projectDirs.Length; i++)
          {
            if (!string.Equals(projectDirs[i], targetDirs[i]))
            {
              jsonSavedToProject = false;
              break;
            }
          }

          if (jsonSavedToProject)
          {
            var relPath = "Assets" + "/" + path.Substring(Application.dataPath.Length);
            AssetDatabase.ImportAsset(relPath);
          }
        }
      }
      if (GUILayout.Button(new GUIContent("Upgrade and Refresh All Anchors",
            "Upgrades locations imported using ARDK 2.3 beta to use the latest APIs. " 
            + "The anchors will not move, but all payloads will be updated.")))
      {
        Target.UpgradeAndRefreshAllAnchors();
      }

    }

    private void DrawMeshGUI()
    {
      EditorGUILayout.ObjectField(Target.Mesh, typeof(GameObject), false);
    }

    private void DrawMockAssetGUI()
    {
      GUILayout.BeginHorizontal();

      var asset = Target._MockAsset;
      var newAsset = EditorGUILayout.ObjectField(asset, typeof(GameObject), false) as GameObject;

      if (asset != newAsset)
      {
        if (newAsset == null)
          Target._MockAsset = null;

        else if (PrefabUtility.GetPrefabAssetType(newAsset) == PrefabAssetType.NotAPrefab)
          ARLog._Release("Invalid mock scene asset selected. Must be a prefab.");

        else if (newAsset.GetComponent<MockSceneConfiguration>() == null)
          ARLog._Release("Invalid mock scene asset selected. Must have a MockSceneConfiguration component.");

        else
        {
          Target._MockAsset = newAsset;
        }
      }

      if (asset == null)
      {
        if (GUILayout.Button(new GUIContent("Create", "Create a prefab for this VPS Location that can be used in Virtual Studio Mock Mode.")))
          Target._CreateMockAsset();
      }

      GUILayout.EndHorizontal();
    }

    // TODO (kcho): expandable
    private Dictionary<string, bool> _anchorFoldoutStates;

    private Dictionary<string, bool> SafeAnchorFoldoutStates
    {
      get
      {
        if (_anchorFoldoutStates == null)
        {
          _anchorFoldoutStates = new Dictionary<string, bool>();
          foreach (var data in Target.AuthoredAnchorsData)
          {
            _anchorFoldoutStates.Add(data._ManifestIdentifier, false);
          }
        }

        return _anchorFoldoutStates;
      }
    }
    private void DrawAnchorGUI(AuthoredWayspotAnchorData data)
    {
      SafeAnchorFoldoutStates[data._ManifestIdentifier] = EditorGUILayout.Foldout(SafeAnchorFoldoutStates[data._ManifestIdentifier], data.Name, true);
      if (!SafeAnchorFoldoutStates[data._ManifestIdentifier])
        return;

      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Anchor Identifier", GUILayout.Width(_colOneWidth));
        if (!string.IsNullOrEmpty(data.Identifier))
          EditorGUILayout.LabelField(data.Identifier.Substring(0, 20) + "...");
      }
      GUILayout.EndHorizontal();

      // GUILayout.BeginHorizontal();
      // {
      //   EditorGUILayout.LabelField("Anchor Tags", GUILayout.Width(_colOneWidth));
      //   EditorGUILayout.LabelField(data.Tags);
      // }
      // GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Associated Prefab", GUILayout.Width(_colOneWidth));
        if (data.AssociatedPrefab != null && data.AssociatedPrefab.Asset != null)
          EditorGUILayout.LabelField(data.AssociatedPrefab.Asset.name);
      }
      GUILayout.EndHorizontal();

      EditorGUI.indentLevel++;
      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Scale", GUILayout.Width(_colOneWidth));
        EditorGUILayout.LabelField(data.Scale.ToString());
      }
      GUILayout.EndHorizontal();
      EditorGUI.indentLevel--;


      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Relative Position", GUILayout.Width(_colOneWidth));
        EditorGUILayout.LabelField(data.Position.ToString());
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Relative Rotation", GUILayout.Width(_colOneWidth));
        EditorGUILayout.LabelField(data.Rotation.ToString());
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
        EditorGUILayout.LabelField("Anchor Payload", GUILayout.Width(_colOneWidth));
        DrawAnchorPayloadGUI(data.Payload, data._ManifestIdentifier);
      }
      GUILayout.EndHorizontal();
    }

    private string _copiedAnchorIdentifier = null;
    private float _timeout;

    private void DrawAnchorPayloadGUI(string payload, string anchorIdentifier)
    {
      GUILayout.BeginVertical();
      var payloadHint = payload.Substring(0, 20) + "...";
      if (GUILayout.Button(payloadHint, PayloadStyle))
      {
        GUIUtility.systemCopyBuffer = payload;
        _timeout = Time.realtimeSinceStartup + 1;
        _copiedAnchorIdentifier = anchorIdentifier;
      }

      if (anchorIdentifier.Equals(_copiedAnchorIdentifier) && Time.realtimeSinceStartup < _timeout)
        GUILayout.Label("Copied!", CommonStyles.CenteredLabelStyle);
      else
        GUILayout.Label("Click to copy", CommonStyles.CenteredLabelStyle);

      GUILayout.EndVertical();
    }

    private static GUIStyle _payloadStyle;
    public static GUIStyle PayloadStyle
    {
      get
      {
        if (_payloadStyle == null)
        {
          _payloadStyle = new GUIStyle(EditorStyles.textArea);
          _payloadStyle.wordWrap = false;
        }

        return _payloadStyle;
      }
    }
  }
}
