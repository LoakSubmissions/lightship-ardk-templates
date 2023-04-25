using System;
using System.Linq;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;
using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  [Serializable]
  public class RuntimeAuthoredWayspotAnchorData
  {
    public string Name;
    public string Payload;

#if UNITY_EDITOR
    public RuntimeAuthoredWayspotAnchorData(AuthoredWayspotAnchorData data)
    {
      Name = data.Name;
      Payload = data.Payload;
    }
    
    public GameObject GetAssociatedEditorPrefab(string manifestName, out Vector3 scale)
    {
      //find via manifest name
      VPSLocationManifest[] manifests = _AssetDatabaseUtilities.FindAssets<VPSLocationManifest>();
      foreach (var vpsLocationManifest in manifests)
      {
        if (vpsLocationManifest.LocationName == manifestName)
        {
          AuthoredWayspotAnchorData[] authoredWayspotAnchorDatas = vpsLocationManifest.AuthoredAnchorsData.ToArray();
          foreach (var wayspotAnchorData in authoredWayspotAnchorDatas)
          {
            if (wayspotAnchorData.Name == Name && wayspotAnchorData.AssociatedPrefab != null)
            {
              //get the anchor name that matches
              //return the gameobject 
              scale = wayspotAnchorData.Scale;
              return wayspotAnchorData.AssociatedPrefab.Asset;
            }
          }
        }
      }
      ARLog._WarnRelease("AssociatedPrefab data was not found for "+Name);
      scale = Vector3.one;
      return null;
    }
#endif
  }
}
