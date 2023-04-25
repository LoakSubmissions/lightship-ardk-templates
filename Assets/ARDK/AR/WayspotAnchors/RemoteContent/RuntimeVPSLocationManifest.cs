using System;
using System.Linq;
using Niantic.ARDK.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  [Serializable]

  public sealed class RuntimeVPSLocationManifest
  {
    public string LocationName;
    public RuntimeAuthoredWayspotAnchorData[] AuthoredAnchors;

#if UNITY_EDITOR
    public RuntimeVPSLocationManifest(VPSLocationManifest manifest)
    {
      LocationName = manifest.LocationName;
      AuthoredAnchors = manifest.AuthoredAnchorsData.Select(a => new RuntimeAuthoredWayspotAnchorData(a)).ToArray();
   }
#endif

    public string ToJson()
    {
      return JsonUtility.ToJson(this);
    }

    public override string ToString()
    {
      return ToJson();
    }
  }
  
}
