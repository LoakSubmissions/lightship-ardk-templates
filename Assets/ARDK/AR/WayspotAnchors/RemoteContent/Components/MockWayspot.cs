
using System;
using System.Linq;
#if UNITY_EDITOR
using System.Collections.Generic;

using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  public class MockWayspot: MonoBehaviour
  {
    [SerializeField][_ReadOnly]
    private string _wayspotName;

    [SerializeField][_ReadOnly]
    private GameObject _meshObject;

    internal string _WayspotName
    {
      get
      {
        return _wayspotName;
      }
      set
      {
        _wayspotName = value;
      }
    }

    internal GameObject _MeshObject
    {
      get
      {
        return _meshObject;
      }
      set
      {
        _meshObject = value;
      }
    }

    [SerializeField][HideInInspector]
    private VPSLocationManifest _vpsLocationManifest;

    public VPSLocationManifest _VPSLocationManifest
    {
      get => _vpsLocationManifest;
      set => _vpsLocationManifest = value;
    }

    private Dictionary<string, AuthoredWayspotAnchorData> _resolveableAnchors;
    private Dictionary<string, string> _otherWayspotsAnchors;

    private void Awake()
    {
      if (_MeshObject == null)
        ARLog._WarnRelease($"No MeshObject found in the spawned MockWayspot.");
    }

    // @param out identifier
    //   Is set even if anchor was created at some other Wayspot, to replicate native behaviour.
    //   Will be null if no anchor in any VPSLocationManifest has the specified payload.
    // @param out pos
    //    Position of the anchor at this Wayspot. Vector3.zero if anchor can not be resolved.
    // @param out rot
    //    Rotation of the anchor at this Wayspot. Vector3.zero if anchor can not be resolved.
    // @returns
    //   True if the anchor can be resolved at this Wayspot
    internal bool _ResolveAnchor(byte[] payloadBlob, out string identifier, out Vector3 pos, out Vector3 rot)
    {
      // Do a null check here, because method might be invoked before Awake
      if (_resolveableAnchors == null)
      {
        _resolveableAnchors = new Dictionary<string, AuthoredWayspotAnchorData>();

        foreach (var anchor in _VPSLocationManifest.AuthoredAnchorsData)
          _resolveableAnchors.Add(anchor.Payload, anchor);
      }

      var payload = new WayspotAnchorPayload(payloadBlob).Serialize();
      if (_resolveableAnchors.TryGetValue(payload, out AuthoredWayspotAnchorData anchorData))
      {
        identifier = anchorData.Identifier;
        pos = anchorData.Position;
        rot = anchorData.Rotation;
        return true;
      }

      if (_otherWayspotsAnchors == null)
      {
        _otherWayspotsAnchors = new Dictionary<string, string>();
        foreach (var manifest in _AssetDatabaseUtilities.FindAssets<VPSLocationManifest>())
        {
          foreach (var anchor in manifest.AuthoredAnchorsData)
          {
            // The same default anchor may be duplicated across multiple manifests
            if (!_otherWayspotsAnchors.ContainsKey(anchor.Payload))
              _otherWayspotsAnchors.Add(anchor.Payload, anchor.Identifier);
          }
        }
      }

      pos = Vector3.zero;
      rot = Vector3.zero;

      if (_otherWayspotsAnchors.TryGetValue(payload, out string id))
      {
        identifier = id;
        return false;
      }

      ARLog._WarnRelease
      (
        $"The payload {payload.Substring(0, 10)}... could not be parsed, because no " +
        "VPSLocationManifest with this payload was found. If this is a valid payload, it would " +
        "have successfully been resolved on device."
      );

      identifier = null;
      return false;
    }
  }
}
#endif
