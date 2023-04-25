
using System;
#if UNITY_EDITOR
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public partial class EditModeOnlyBehaviour
  {
    internal class _VisualizedWayspotTag: MonoBehaviour
    {
      private void Reset()
      {
        hideFlags = HideFlags.HideInInspector;
        _SceneHierarchyUtilities.ValidateChildOf<RemoteAuthoringAssistant>(this, true);
      }
    }
  }
}
#endif
