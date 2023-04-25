using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;

using UnityEditor;
using UnityEngine;

using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _VPSLocationAssetProcessor: AssetPostprocessor
  {
    [Serializable]
    private struct WayspotData
    {
      public string NodeIdentifier;
      public string AnchorPayload;
      public string LocalizationTargetName;
    }

    private static void OnPostprocessAllAssets
    (
      string[] importedAssets,
      string[] deletedAssets,
      string[] movedAssets,
      string[] movedFromAssetPaths
    )
    {
      if (importedAssets.Length == 0)
        return;

      var zips = importedAssets.Where(a => string.Equals(Path.GetExtension(a), ".zip")).ToArray();
      if (zips.Length == 0)
        return;

      // Have to delay it a frame in order for all imports to work synchronously
      EditorApplication.delayCall += () => ProcessAllImports(zips);
    }

    private static void ProcessAllImports(string[] zips)
    {
      var allManifests = new List<VPSLocationManifest>();
      foreach (var path in zips)
      {
        if (TryCreateLocationManifest(path, out VPSLocationManifest manifest))
          allManifests.Add(manifest);
        else
          break;
      }

      if (allManifests.Count == 0)
        return;

      AssetDatabase.SaveAssets();

      var ra = RemoteAuthoringAssistant.FindSceneInstance();
      if (ra == null)
      {
        var create =
          EditorUtility.DisplayDialog
          (
            RemoteAuthoringAssistant.DIALOG_TITLE,
            "No RemoteAuthoringAssistant was found in the open scene. Would you like to create one?",
            "Yes",
            "No"
          );

        if (create)
        {
          _RemoteAuthoringPresenceManager.AddPresence();
          ra = RemoteAuthoringAssistant.FindSceneInstance();
          ra.OpenLocation(allManifests[0]);
          
          _RemoteAuthoringEditorWindow.ShowWindow();
          return;
        }
      }
      else
      {
        ra.LoadAllManifestsInProject();
        ra.OpenLocation(allManifests[0]);
      }
    }

    private static bool TryCreateLocationManifest(string zipPath, out VPSLocationManifest manifest)
    {
      manifest = null;

      var isValidZip =
        FindArchivedFiles
        (
          zipPath,
          out UnityEngine.Mesh mesh,
          out Texture2D tex,
          out WayspotData wayspotData
        );

      if (!isValidZip)
        return false;

      if (!RemoteAuthoringAssistant._ValidateAPIKey(false))
      {
        ARLog._Error("No ARDK API key found. Please add one and then reimport the .zip file(s)");
        return false;
      }

      ARLog._Debug("Importing: " + zipPath);

      var dir = Path.GetDirectoryName(zipPath);

      var locationName = wayspotData.LocalizationTargetName;
      if (string.IsNullOrEmpty(locationName))
        locationName = "Unnamed";

      var manifestPath = _ProjectBrowserUtilities.BuildAssetPath(locationName + ".asset", dir);
      manifest = CreateManifest(wayspotData, manifestPath);

      try
      {
        AssetDatabase.StartAssetEditing();

        // Need to create a copy in order to organize as sub-asset of the manifest
        var meshCopy = UnityEngine.Object.Instantiate(mesh);
        meshCopy.name = "Mesh";
        AssetDatabase.AddObjectToAsset(meshCopy, manifest);

        AddDefaultAnchor(manifest, meshCopy);

        if (tex != null)
        {
          var texCopy = UnityEngine.Object.Instantiate(tex);
          texCopy.name = "Texture";
          AssetDatabase.AddObjectToAsset(texCopy, manifest);
        }

        // Create the material asset
        Material mat;
        if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
          mat = new Material(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultShader);
        else
          mat = new Material(Shader.Find("Standard"));
        
        mat.name = "Material";

        AssetDatabase.AddObjectToAsset(mat, manifest);
        Selection.activeObject = manifest;

        // When pinged without delay, project browser window is displayed for a moment
        // before elements are alphabetically sorted, potentially leading to objects moving around.
        var createdManifest = manifest;
        EditorApplication.delayCall += () => EditorGUIUtility.PingObject(createdManifest);
      }
      finally
      {
        AssetDatabase.StopAssetEditing();
      }

      // Cleanup
      if (File.Exists(zipPath))
        AssetDatabase.DeleteAsset(zipPath);

      if (mesh != null)
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(mesh));

      if (tex != null)
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tex));

      return isValidZip;
    }

    private static bool FindArchivedFiles
    (
      string zipPath,
      out UnityEngine.Mesh mesh,
      out Texture2D tex,
      out WayspotData wayspotData
    )
    {
      mesh = null;
      tex = null;
      wayspotData = new WayspotData();

      using (var file = File.OpenRead(zipPath))
      {
        using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
        {
          var validEntries = zip.Entries.Where(e => !e.Name.StartsWith("._"));
          var meshEntries = validEntries.Where(e => Path.GetExtension(e.Name).Equals(".fbx"));
          var texEntries = validEntries.Where(e => Path.GetExtension(e.Name).Equals(".jpeg"));
          var wayspotEntries = validEntries.Where(e => Path.GetExtension(e.Name).Equals(".json"));

          if (!(meshEntries.Any() && wayspotEntries.Any()))
            return false;

          mesh = ImportMesh(meshEntries.First());
          wayspotData = ParseWayspotData(wayspotEntries.First());

          // Some nodes do not have textures
          if (texEntries.Any())
            tex = ImportTexture(texEntries.First());

          return !(mesh == null || string.IsNullOrEmpty(wayspotData.AnchorPayload));
        }
      }
    }

    private static WayspotData ParseWayspotData(ZipArchiveEntry entry)
    {
      using (var stream = entry.Open())
      {
        using (var reader = new StreamReader(stream))
        {
          var anchorFileText = reader.ReadToEnd();
          var wayspotData = JsonUtility.FromJson<WayspotData>(anchorFileText);

          return wayspotData;
        }
      }
    }

    private static bool _isImportingMesh;
    private static UnityEngine.Mesh ImportMesh(ZipArchiveEntry entry)
    {
      var absPath = _ProjectBrowserUtilities.BuildAssetPath("VPSLocationMesh.fbx", Application.dataPath);
      var assetPath = FileUtil.GetProjectRelativePath(absPath);

      using (var stream = entry.Open())
        using (var fs = new FileStream(assetPath, FileMode.OpenOrCreate))
          stream.CopyTo(fs);

      _isImportingMesh = true;
      AssetDatabase.ImportAsset(assetPath);
      _isImportingMesh = false;

      return AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(assetPath);
    }

    private static bool _isImportingTex;
    private static Texture2D ImportTexture(ZipArchiveEntry entry)
    {
      var absPath = _ProjectBrowserUtilities.BuildAssetPath(entry.Name, Application.dataPath);
      var assetPath = FileUtil.GetProjectRelativePath(absPath);

      using (var stream = entry.Open())
      {
        using (var ms = new MemoryStream())
        {
          stream.CopyTo(ms);
          var data = ms.ToArray();
          File.WriteAllBytes(assetPath, data);
        }
      }

      _isImportingTex = true;
      AssetDatabase.ImportAsset(assetPath);
      _isImportingTex = false;

      return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private void OnPreprocessTexture()
    {
      if (!_isImportingTex)
        return;

      var textureImporter = assetImporter as TextureImporter;
      textureImporter.isReadable = true; // Unity takes care of resetting this value
    }

    private void OnPreprocessModel()
    {
      if (!_isImportingMesh)
        return;

      var modelImporter = assetImporter as ModelImporter;
      modelImporter.bakeAxisConversion = true;
    }

    private static VPSLocationManifest CreateManifest(WayspotData wayspotData, string assetPath)
    {
      var manifest = ScriptableObject.CreateInstance<VPSLocationManifest>();
      manifest._NodeIdentifier = wayspotData.NodeIdentifier;
      manifest._MeshOriginAnchorPayload = wayspotData.AnchorPayload;
      manifest.LocationName = Path.GetFileNameWithoutExtension(assetPath);

      AssetDatabase.CreateAsset(manifest, assetPath);

      return manifest;
    }

    private static void AddDefaultAnchor(VPSLocationManifest manifest, UnityEngine.Mesh mesh)
    {
      // TODO (AR-13413): Add at mesh center
      var anchorPose = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
      var task = Task.Run(() => _AuthoringUtilities.CreateRelativeToAnchorAtNodeOriginWithFallback(
          anchorPose, manifest._NodeIdentifier, manifest._MeshOriginAnchorPayload));
      task.Wait();

      var (identifier, payload) = task.Result;
      if (string.IsNullOrEmpty(identifier))
      {
        ARLog._WarnRelease("Unable to create default anchor.");
        return;
      }

      manifest._AddAnchorData
      (
        "Authored Anchor (Default)",
        anchorIdentifier: identifier,
        payload: payload,
        position: Vector3.zero,
        rotation: Vector3.zero
      );
    }
  }
}
