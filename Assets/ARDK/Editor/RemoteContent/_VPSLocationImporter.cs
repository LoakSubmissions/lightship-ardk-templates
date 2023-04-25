using System;
using System.IO;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _VPSLocationImporter
  {
    [MenuItem("Lightship/ARDK/VPS Authoring Assistant/Add Location", false, 10)]
    public static void Import()
    {
      var sourcePath =
        EditorUtility.OpenFilePanel
        (
          $"Select Download from Geospatial Browser",
          Environment.SpecialFolder.MyDocuments.ToString(),
          "zip"
        );

      if (string.IsNullOrEmpty(sourcePath))
        return;

      var targetPath =
        EditorUtility.SaveFilePanelInProject
        (
          "Save VPS Location Manifest",
          Path.GetFileNameWithoutExtension(sourcePath),
          "zip",
          "Please enter a name for the imported location"
        );

      File.Copy(sourcePath, targetPath, true);
      AssetDatabase.ImportAsset(targetPath);
    }
  }
}
