using System;
using System.Linq;

using Niantic.ARDK.Editor;

using UnityEditor;

using UnityEngine;

using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  [CustomEditor(typeof(RemoteAuthoringAssistant))]
  internal class _RemoteAuthoringAssistantInspector: UnityEditor.Editor
  {
    private RemoteAuthoringAssistant Target { get { return (RemoteAuthoringAssistant)target; } }

    private int _selectedLocationIndex;
    private string[] _locationNames;

    private void OnEnable()
    {
      SetSelectedLocationIndex(null, Target.ActiveManifest);
      RemoteAuthoringAssistant.ActiveManifestChanged += SetSelectedLocationIndex;
    }

    private void OnDisable()
    {
      RemoteAuthoringAssistant.ActiveManifestChanged -= SetSelectedLocationIndex;
    }

    private void SetSelectedLocationIndex(VPSLocationManifest old, VPSLocationManifest curr)
    {
      _locationNames = Target.AllManifests.Select(m => m.LocationName).ToArray();

      if (curr != null)
      {
        var activeManifestName = curr.LocationName;
        _selectedLocationIndex = Array.IndexOf(_locationNames, activeManifestName);
      }
      else
      {
        _selectedLocationIndex = -1;
      }
    }

    private float _fullWidth;
    private float _colOneWidth;
    private float _colTwoWidth;
    private float _colThreeWidth;

    private void RecalculateWidths()
    {
      _fullWidth = GUILayoutUtility.GetLastRect().width;
      _colOneWidth = _fullWidth * 0.25f;
      _colThreeWidth = _fullWidth * 0.1f;
      _colTwoWidth = _fullWidth - _colOneWidth - _colThreeWidth;
    }

    public override void OnInspectorGUI()
    {
      // Invisible label so width can be fetched
      GUILayout.Label("");

      if (Event.current.type == EventType.Repaint)
        RecalculateWidths();

      DrawLocationSelectionGUI();
      DrawImportLocationGUI();

      GUILayout.Space(30);

      if (Target.ActiveManifest != null)
      {
        DrawLocationNameTextGUI();
        DrawActiveManifestGUI();
      }
    }

    private void DrawLocationSelectionGUI()
    {
      using (var scope = new GUILayout.HorizontalScope())
      {
        GUILayout.Label("Selected Location", GUILayout.Width(_colOneWidth));

        var newLocationIndex =
          EditorGUILayout.Popup(_selectedLocationIndex, _locationNames, GUILayout.Width(_colTwoWidth));

        if (newLocationIndex != _selectedLocationIndex)
        {
          _selectedLocationIndex = newLocationIndex;
          Target.OpenLocation(Target.AllManifests[newLocationIndex]);
        }

        if (CommonStyles.RefreshButton() && _selectedLocationIndex >= 0)
          SetSelectedLocationIndex(null, Target.ActiveManifest);
      }
    }

    private void DrawImportLocationGUI()
    {
      using (var scope = new GUILayout.HorizontalScope())
      {
        GUILayout.Space(_colOneWidth + 2);

        if (GUILayout.Button("Import New Location", GUILayout.Width(_colTwoWidth)))
          _VPSLocationImporter.Import();
      }
    }

    private void DrawLocationNameTextGUI()
    {
      using (var scope = new GUILayout.HorizontalScope())
      {
        GUILayout.Label("Location Name", GUILayout.Width(_colOneWidth));

        var newName =
          EditorGUILayout.DelayedTextField
          (
            Target.ActiveManifest.LocationName,
            GUILayout.Width(_colTwoWidth)
          );

        if (!string.IsNullOrEmpty(newName) && !string.Equals(newName, Target.ActiveManifest.LocationName))
        {
          Target.ActiveManifest.LocationName = newName;
          SetSelectedLocationIndex(Target.ActiveManifest, Target.ActiveManifest);
        }
      }
    }

    private void DrawActiveManifestGUI()
    {
      using (var scope = new GUILayout.HorizontalScope())
      {
        GUILayout.Space(_colOneWidth + 2);

        using (var vScope = new GUILayout.VerticalScope())
        {
          if (GUILayout.Button("Inspect Location Manifest"))
          {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = Target.ActiveManifest;
          }

          GUILayout.Space(20);

          using (var hscope = new GUILayout.HorizontalScope())
          {
            var buttonWidth = (_colTwoWidth - 10) / 2;
            if (GUILayout.Button("Create New Anchor", GUILayout.Width(buttonWidth)))
              _AnchorPlacementUtility.AddAnchorFromCamera();

            GUILayout.Space(10);

            if (GUILayout.Button("Save All Anchors", GUILayout.Width(buttonWidth)))
              Target.SaveUnsavedData(false);
          }

          GUILayout.Space(20);

          DrawDeleteLocationGUI();
        }

        GUILayout.Space(_colThreeWidth);
      }
    }

    private void DrawDeleteLocationGUI()
    {
      if (GUILayout.Button("Delete Location"))
      {
        var verified = EditorUtility.DisplayDialog
        (
          RemoteAuthoringAssistant.DIALOG_TITLE,
          "Are you sure you want to remove this location from your project?",
          "Yes",
          "Cancel"
        );

        if (verified)
        {
          AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Target.ActiveManifest));
          SetSelectedLocationIndex(null, null);
        }
      }
    }
  }
}