#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;

using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  public partial class EditModeOnlyBehaviour
  {
    /// Backing WayspotAnchor is created in Awake.
    /// Once the device is localized (through the WayspotAnchorService or WayspotAnchorController API)
    /// the VPS will attempt to resolve this anchor.
    [ExecuteInEditMode]
    public class AuthoredWayspotAnchor: MonoBehaviour
    {
      // This class maintains a separate serialization of the properties in AuthoredWayspotAnchorData,
      // so that changes made to this class can either be saved back to the VPSLocationManifest or
      // discarded.
      [SerializeField] [HideInInspector]
      private string _anchorManifestIdentifier;

      [SerializeField] [HideInInspector]
      private string _anchorName;

      [SerializeField] [HideInInspector]
      private string _tags;

      [SerializeField] [HideInInspector]
      private AuthoredWayspotAnchorData.PrefabData _prefab;

      internal string _AnchorManifestIdentifier
      {
        get
        {
          return _anchorManifestIdentifier;
        }
      }

      internal string _AnchorName
      {
        get
        {
          return _anchorName;
        }
        set
        {
          _anchorName = value;
        }
      }

      internal string _Tags
      {
        get
        {
          return _tags;
        }
        set
        {
          _tags = value;
        }
      }

      internal AuthoredWayspotAnchorData.PrefabData _Prefab
      {
        get
        {
          return _prefab;
        }
        private set
        {
          _prefab = value;
        }
      }

      internal static GameObject _Create(AuthoredWayspotAnchorData data)
      {
        var go = new GameObject();
        var raScene = RemoteAuthoringAssistant.FindSceneInstance().gameObject.scene;
        SceneManager.MoveGameObjectToScene(go, raScene);

        var anchor = go.AddComponent<AuthoredWayspotAnchor>();
        anchor._ResetToData(data);

        EditorSceneManager.MarkSceneDirty(go.scene);

        return go;
      }

      private bool _isDestroying;
      internal void Destroy()
      {
        _isDestroying = true;
        DestroyImmediate(gameObject);
      }

      private void OnDestroy()
      {
        var ra = RemoteAuthoringAssistant.FindSceneInstance();
        if (!ra)
        {
          // If destroyed because parent RemoteAuthoringAssistant was destroyed
          // do nothing
          return;
        }

        if (!_isDestroying)
          ra.RefreshActiveAnchors();

        _isDestroying = false;
      }

      private const string ANCHOR_NAME_SUFFIX = " (Authored Anchor)";

      internal void _ResetToData(AuthoredWayspotAnchorData data)
      {
        if (data == null)
        {
          _AnchorName = "UnnamedAnchor" + ANCHOR_NAME_SUFFIX;
          _Tags = String.Empty;
          _Prefab = null;
          return;
        }

        _anchorManifestIdentifier = data._ManifestIdentifier;

        _AnchorName = data.Name;
        _Tags = data.Tags;

        if (data.AssociatedPrefab != null)
          _Prefab = data.AssociatedPrefab.Copy();

        gameObject.name = _anchorName;
        transform.SetPositionAndRotation(data.Position, Quaternion.Euler(data.Rotation));
        transform.localScale = data.Scale;
      }

      internal void GetDifferences(AuthoredWayspotAnchorData data, out bool isBackingAnchorInvalid, out bool isManifestInvalid)
      {
        if (data == null)
        {
          isBackingAnchorInvalid = true;
          isManifestInvalid = true;
          return;
        }

        isManifestInvalid =
          !string.Equals(_AnchorManifestIdentifier, data._ManifestIdentifier) ||
          !string.Equals(_AnchorName, data.Name) ||
          transform.localScale != data.Scale ||
          !string.Equals(_Tags, data.Tags) ||
          (_Prefab == null) != (data.AssociatedPrefab == null) ||
          _Prefab != null && _Prefab.ValuesDifferFrom(data.AssociatedPrefab);
        
        isBackingAnchorInvalid =
          string.IsNullOrEmpty(data.Payload) ||
          transform.position != data.Position ||
          transform.rotation.eulerAngles != data.Rotation;
      }

      private void OnDrawGizmos()
      {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, .1f);
      }
    }
  }
}
#endif
