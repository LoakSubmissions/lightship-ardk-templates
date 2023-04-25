#if UNITY_EDITOR
using System;
using UnityEngine;

using PrefabData = Niantic.ARDK.AR.WayspotAnchors.AuthoredWayspotAnchorData.PrefabData;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public partial class EditModeOnlyBehaviour
  {
    [ExecuteInEditMode]
    internal class _VisualizedPrefabTag: MonoBehaviour
    {
      [SerializeField]
      private string _prefabIdentifier;

      public string PrefabIdentifier { get => _prefabIdentifier; }

      public void Initialize(PrefabData prefabData)
      {
        _prefabIdentifier = prefabData.Identifier;
      }
    }
  }
}
#endif