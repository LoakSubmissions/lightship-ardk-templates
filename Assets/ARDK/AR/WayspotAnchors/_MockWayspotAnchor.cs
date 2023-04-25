// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Editor;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR.Mock;

using UnityEditor;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _MockWayspotAnchor:
    _ThreadCheckedObject,
    IWayspotAnchor,
    _IInternalTrackable
  {
    public ArdkEventHandler<WayspotAnchorResolvedArgs> TrackingStateUpdated { get; set; }

    public event ArdkEventHandler<WayspotAnchorResolvedArgs> TransformUpdated
    {
      add
      {
        _CheckThread();

        _transformUpdated += value;

        if (Status == WayspotAnchorStatusCode.Success)
          value.Invoke(new WayspotAnchorResolvedArgs(ID, LastKnownPosition, LastKnownRotation));
      }
      remove
      {
        _transformUpdated -= value;
      }
    }

    public event ArdkEventHandler<WayspotAnchorStatusUpdate> StatusCodeUpdated
    {
      add
      {
        _CheckThread();

        _statusCodeUpdated += value;
        value.Invoke(new WayspotAnchorStatusUpdate(ID, Status));
      }
      remove
      {
        _statusCodeUpdated -= value;
      }
    }

    public Guid ID { get; private set; }

    /// Whether or not the mock anchor is currently being tracked
    public bool Tracking { get; private set; }

    public WayspotAnchorStatusCode Status { get; private set; } = WayspotAnchorStatusCode.Pending;

    public Vector3 LastKnownPosition { get; private set; }

    public Quaternion LastKnownRotation { get; private set; }

    private Vector3 _resolvedPos;
    private Quaternion _resolvedRot;
    private WayspotAnchorStatusCode _resolvedStatus;
    private float _trackingStart;

    // Sets whether or not the mock anchor should be tracked
    // Part of _IInternalTrackable interface
    public void SetTrackingEnabled(bool tracking)
    {
      _trackingStart = Time.time;
      Tracking = tracking;
    }

    // Part of _IInternalTrackable interface
    public void SetTransform(Vector3 position, Quaternion rotation)
    {
      LastKnownPosition = position;
      LastKnownRotation = rotation;
      _transformUpdated?.Invoke(new WayspotAnchorResolvedArgs(ID, LastKnownPosition, LastKnownRotation));
    }

    // Part of _IInternalTrackable interface
    public void SetStatusCode(WayspotAnchorStatusCode statusCode)
    {
      if (Status != statusCode)
      {
        Status = statusCode;
        _statusCodeUpdated?.Invoke(new WayspotAnchorStatusUpdate(ID, Status));
      }
    }

    public bool TryToResolve(out WayspotAnchorStatusCode status, out Vector3 pos, out Quaternion rot)
    {
      // 1 second delay to resolve
      if (Time.time - _trackingStart < 1f)
      {
        status = WayspotAnchorStatusCode.Pending;
        pos = LastKnownPosition;
        rot = LastKnownRotation;
        return false;
      }

      // Cannot call SetStatusCode and SetTransform from within this method, because it needs to
      // be called by the Controller so the values are aligned with when events are surfaced...
      pos = _resolvedPos;
      rot = _resolvedRot;
      status = _resolvedStatus;
      return true;
    }

    public _MockWayspotAnchor(Guid id, Matrix4x4 localPose)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(_WayspotAnchorFactory));

      ID = id;

      _resolvedPos = localPose.ToPosition();
      _resolvedRot = localPose.ToRotation();
      _resolvedStatus = WayspotAnchorStatusCode.Success;

      LastKnownPosition = _resolvedPos;
      LastKnownRotation = _resolvedRot;
    }

#if UNITY_EDITOR
    private static MockWayspot _mockWayspot;
#endif

    public _MockWayspotAnchor(byte[] data)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(_WayspotAnchorFactory));

      var json = Encoding.UTF8.GetString(data);
      if (json.Contains(nameof(_MockWayspotAnchorData._ID)))
        CreateFromJSON(json);
#if UNITY_EDITOR
      else
        CreateFromPayload(data);
#endif
    }

    private void CreateFromJSON(string json)
    {
      var mockWayspotAnchorData = JsonUtility.FromJson<_MockWayspotAnchorData>(json);

      var success = Guid.TryParse(mockWayspotAnchorData._ID, out Guid identifier);
      if (success)
        ID = identifier;
      else
        throw new ArgumentException("Failed to create wayspot anchor from payload");

      _resolvedPos =
        new Vector3
        (
          mockWayspotAnchorData._XPosition,
          mockWayspotAnchorData._YPosition,
          mockWayspotAnchorData._ZPosition
        );

      var rotationEuler =
        new Vector3
        (
          mockWayspotAnchorData._XRotation,
          mockWayspotAnchorData._YRotation,
          mockWayspotAnchorData._ZRotation
        );

      _resolvedRot = Quaternion.Euler(rotationEuler);

      _resolvedStatus = WayspotAnchorStatusCode.Success;
    }

#if UNITY_EDITOR
    private void CreateFromPayload(byte[] payload)
    {
      if (_mockWayspot == null)
      {
        // Because spawning the mock env prefab depends on the PlayModeStateChanged event,
        // which comes after Awake, can't just use `GameObject.FindObjectOfType<MockWayspot>()`
        // Once the _VirtualStudioLauncher bug is fixed, should come back and clean this
        var mockLauncher = (_MockModeLauncher)_VirtualStudioLauncher.GetOrCreateModeLauncher(RuntimeEnvironment.Mock);
        var mockPrefab = mockLauncher._GetMockScenePrefab();
        if (mockPrefab != null)
          _mockWayspot = mockPrefab.GetComponent<MockWayspot>();
      }

      if (_mockWayspot == null)
      {
        throw new InvalidOperationException
        (
          "No MockWayspot component available to resolve WayspotAnchors in this scene"
        );
      }

      if (_mockWayspot._ResolveAnchor(payload, out string identifier, out Vector3 pos, out Vector3 rot))
      {
        ID = new Guid(identifier);
        _resolvedPos = pos;
        _resolvedRot = Quaternion.Euler(rot);
        _resolvedStatus = WayspotAnchorStatusCode.Success;
      }
      else
      {
        _resolvedStatus = WayspotAnchorStatusCode.Failed;
        if (!string.IsNullOrEmpty(identifier))
          ID = new Guid(identifier);
      }
    }
#endif

    /// Gets the payload of the mock anchor
    /// @note This is a wrapper around the blob of data
    public WayspotAnchorPayload Payload
    {
      get
      {
        string id = ID.ToString();
        var rotation = LastKnownRotation.eulerAngles;
        var mockWayspotAnchorData = new _MockWayspotAnchorData()
        {
          _ID = id,
          _XPosition = LastKnownPosition.x,
          _YPosition = LastKnownPosition.y,
          _ZPosition = LastKnownPosition.z,
          _XRotation = rotation.x,
          _YRotation = rotation.y,
          _ZRotation = rotation.z
        };

        string json = JsonUtility.ToJson(mockWayspotAnchorData);
        byte[] blob = Encoding.UTF8.GetBytes(json);
        var payload = new WayspotAnchorPayload(blob);

        return payload;
      }
    }

    /// The data class used to serialize/deserialize the payload
    [Serializable]
    public class _MockWayspotAnchorData
    {
      public string _ID;
      public float _XPosition;
      public float _YPosition;
      public float _ZPosition;
      public float _XRotation;
      public float _YRotation;
      public float _ZRotation;
    }

    /// Disposes the mock wayspot anchor
    public void Dispose()
    {}

    private event ArdkEventHandler<WayspotAnchorResolvedArgs> _transformUpdated = args => {};
    private event ArdkEventHandler<WayspotAnchorStatusUpdate> _statusCodeUpdated = args => {};
  }
}
