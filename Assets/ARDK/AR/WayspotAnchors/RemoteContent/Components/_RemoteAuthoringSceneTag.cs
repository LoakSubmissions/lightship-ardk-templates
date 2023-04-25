using System;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  // Class is not in EditModeOnlyBehaviour because it stays in the scene
  [Obsolete]
  internal class _RemoteAuthoringSceneTag: MonoBehaviour
  {
    private void Reset()
    {
      DestroyImmediate(gameObject);
    }
  }
}
