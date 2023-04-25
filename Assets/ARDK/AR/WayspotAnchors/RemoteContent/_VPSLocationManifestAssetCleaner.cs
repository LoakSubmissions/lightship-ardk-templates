#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.VirtualStudio.AR.Mock;
using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _VPSLocationManifestAssetCleaner: UnityEditor.AssetModificationProcessor
  {
    // This is called by Unity when it is about to delete an asset from disk.
    // It allows you to delete the asset yourself.
    // Deletion of a file can be prevented by returning AssetDeleteResult.FailedDelete.
    // You should not call any Unity AssetDatabase api from within this callback,
    // preferably keep to file operations or VCS apis.
    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
      if (!string.Equals(Path.GetExtension(assetPath), ".asset"))
        return AssetDeleteResult.DidNotDelete;
      
      EditorApplication.delayCall += () => CleanupIfManifest(assetPath);

      return AssetDeleteResult.DidNotDelete;
    }

    private static void CleanupIfManifest(string assetPath)
    {
      // Asset itself has been deleted, only info accessible is the assetPath
      var assetName = Path.GetFileNameWithoutExtension(assetPath);
      var mockWayspotPrefabs = _AssetDatabaseUtilities.FindPrefabsWithComponent<MockWayspot>();
      
      foreach (var prefab in mockWayspotPrefabs)
      {
        var path = AssetDatabase.GUIDToAssetPath(prefab.Guid);
        var asset = AssetDatabase.LoadMainAssetAtPath(path);

        MockWayspot m = (asset as GameObject).GetComponent<MockWayspot>();
        if (m && null == m._VPSLocationManifest)
        {
          AssetDatabase.DeleteAsset(path);
        }
      }

      var raa = EditModeOnlyBehaviour.RemoteAuthoringAssistant.FindSceneInstance();
      if (raa != null)
      {
        var oldCount = raa.AllManifests.Count;
        raa.LoadAllManifestsInProject();
        if (oldCount > raa.AllManifests.Count)
        {
          if (raa.ActiveManifest == null)
            raa.OpenLocation(null, false); 
        }
      }
    }
  }
}
#endif
