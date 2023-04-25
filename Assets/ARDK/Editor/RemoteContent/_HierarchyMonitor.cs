using System.Linq;

using Niantic.ARDK.Utilities.Logging;

using UnityEditor;
using UnityEngine.SceneManagement;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _HierarchyMonitor
  {
    private static EditModeOnlyBehaviour.RemoteAuthoringAssistant _ra;
    private static Scene _raScene;
    private static Scene _mainScene;

    // Will be invoked by RemoteAuthoringPresenceManager whenever scripts reload
    public static void Enable(Scene mainScene)
    {
      //EditorApplication.hierarchyChanged += OnHierarchyChanged;

      // _ra = EditModeOnlyBehaviour.RemoteAuthoringAssistant.FindSceneInstance();
      // _raScene = _ra.gameObject.scene;
      // _mainScene = mainScene;
    }

    public static void Disable()
    {
      //EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    static void OnHierarchyChanged()
    {
      if (_raScene.rootCount > 2)
      {
        ARLog._WarnRelease
        (
          "The Remote Authoring scene is temporary. GameObjects can not be added to its " +
          "hierarchy outside of the Remote Authoring flow."
        );

        var rootObjects = _raScene.GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
          if (obj.GetComponent<EditModeOnlyBehaviour.RemoteAuthoringAssistant>() == null &&
              obj.GetComponent<EditModeOnlyBehaviour._HierarchyWarning>() == null)
          {
            SceneManager.MoveGameObjectToScene(obj, _mainScene);
            obj.transform.SetAsLastSibling();
          }
        }

        return;
      }

      var childCount = _ra.transform.childCount;
      var numValidChildren = _ra.ActiveAnchors.Count + 1;

      if (childCount != numValidChildren)
      {
        ARLog._WarnRelease
        (
          "Remote Authoring Assistant hierarchy was externally modified. GameObjects should " +
          "not be added or deleted from this hierarchy outside of the Remote Authoring flow."
        );

        // Can't do anything about deleted objects, but can remove added objects
        if (childCount > numValidChildren)
        {
          var i = 0;
          while (i < _ra.transform.childCount)
          {
            var child = _ra.transform.GetChild(i);
            if (child.GetComponent<EditModeOnlyBehaviour._VisualizedWayspotTag>() == null &&
              child.GetComponent<EditModeOnlyBehaviour.AuthoredWayspotAnchor>() == null)
            {
              child.SetParent(null);
              SceneManager.MoveGameObjectToScene(child.gameObject, _mainScene);
              child.gameObject.transform.SetAsLastSibling();
            }
            else
            {
              i += 1;
            }
          }
        }

        return;
      }

      var anchors = _ra.ActiveAnchors;
      foreach (var anchor in anchors)
      {
        var anchorChildCount = anchor.transform.childCount;
        if ((anchorChildCount == 0 && anchor._Prefab != null) || (anchorChildCount > 0 && anchor._Prefab == null) || anchorChildCount > 1) 
        {
          ARLog._WarnRelease
          (
            "Authored Wayspot Anchor hierarchy was externally modified. GameObjects should " +
            "not be added or deleted from this hierarchy outside of the Remote Authoring flow. " +
            "Associated prefabs can be added through the Authored Wayspot Anchor inspector."
          );
        }
      }
    }
  }
}
