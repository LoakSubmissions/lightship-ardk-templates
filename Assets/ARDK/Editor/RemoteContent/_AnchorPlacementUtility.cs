using System.Linq;

using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _AnchorPlacementUtility: EditorWindow
  {
    public static void AddAnchorFromCamera()
    {
      var ra = EditModeOnlyBehaviour.RemoteAuthoringAssistant.FindSceneInstance();

      var camera = SceneView.lastActiveSceneView.camera;
      if (camera == null)
        ra.AddEmptyAnchorToScene(Vector3.zero, Quaternion.identity.eulerAngles);
      else
        AddAnchorAtMeshCenter();
    }

    // If ray does not intersect with Wayspot Mesh, nothing happens
    public static void AddAnchorFromScreenPoint(Ray ray)
    {
      var hitMesh = HitTestMesh(ray, out Vector3 pos);

      var ra = EditModeOnlyBehaviour.RemoteAuthoringAssistant.FindSceneInstance();
      if (hitMesh)
        ra.AddEmptyAnchorToScene(pos, Quaternion.identity.eulerAngles);
      else
        AddAnchorAtMeshCenter();
    }

    private static void AddAnchorAtMeshCenter()
    {
      var ra = EditModeOnlyBehaviour.RemoteAuthoringAssistant.FindSceneInstance();

      var mesh = _SceneHierarchyUtilities.FindGameObjects<EditModeOnlyBehaviour._VisualizedWayspotTag>(null, ra.transform).First();
      var bounds = mesh.GetComponent<MeshFilter>().sharedMesh.bounds;
      var flooredCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

      ra.AddEmptyAnchorToScene(flooredCenter, Quaternion.identity.eulerAngles);
    }

    private static bool HitTestMesh(Ray ray, out Vector3 hitPosition)
    {
      hitPosition = Vector3.zero;

      var hits = Physics.RaycastAll(ray.origin, ray.direction, 50);
      if (hits.Length == 0)
        return false;

      foreach (var hit in hits)
      {
        var wayspotMesh = hit.collider.gameObject.GetComponent<EditModeOnlyBehaviour._VisualizedWayspotTag>();
        if (wayspotMesh != null)
        {
          hitPosition = hit.point;
          return true;
        }
      }

      return false;
    }
  }
}
