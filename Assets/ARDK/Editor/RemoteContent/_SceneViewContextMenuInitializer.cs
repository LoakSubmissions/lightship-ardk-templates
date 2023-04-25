using System;
using System.Collections.Generic;

using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.AR.WayspotAnchors.Editor;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.Editor
{
  [InitializeOnLoad]
  internal class _SceneViewContextMenuInitializer: UnityEditor.Editor
  {
    // Key that's got to be pressed when right clicking, to get a context menu.
    private static readonly EventModifiers _modifier = EventModifiers.Shift;

    static _SceneViewContextMenuInitializer()
    {
      SceneView.duringSceneGui -= OnSceneGUI;
      SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
      var isOnlyModifier = Event.current.modifiers == _modifier;

      if (isOnlyModifier && Event.current.button == 1 && Event.current.type == EventType.MouseDown)
      {
        var go = HandleUtility.PickGameObject(Event.current.mousePosition, true);

        // Show menu.
        if (go.GetComponent<EditModeOnlyBehaviour._VisualizedWayspotTag>() != null)
        {
          var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

          var menu = new GenericMenu();
          menu.AddItem
          (
            new GUIContent("Create Authored Wayspot Anchor"),
            false,
            () => _AnchorPlacementUtility.AddAnchorFromScreenPoint(ray)
          );

          menu.ShowAsContext();
        }
      }
    }
  }
}
