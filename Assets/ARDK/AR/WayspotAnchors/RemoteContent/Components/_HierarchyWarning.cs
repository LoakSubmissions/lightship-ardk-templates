using Niantic.ARDK.Utilities.Editor;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public partial class EditModeOnlyBehaviour
  {
    internal class _HierarchyWarning: MonoBehaviour
    {
      internal static void Create(Scene scene, Color color)
      {
        var warning = new GameObject("Do NOT directly add/delete objects to this scene");
        SceneManager.MoveGameObjectToScene(warning, scene);
        warning.AddComponent<_HierarchyWarning>();

        var pretty = warning.AddComponent<_PrettyHeirarchyItem>();
        pretty.BackgroundColor = color;
      }
    }
  }
}
