using System;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors
{
  internal class _VpsDefinitions
  {
    [Serializable]
    public class Transform
    {
      public Transform(Matrix4x4 m)
      {
        translation = m.ToPosition();
        rotation = m.ToRotation();
        scale = 1;
      }

      public Vector3 translation;
      public Quaternion rotation;
      public float scale;
    };

    [Serializable]
    public class Localization
    {
      public Localization(string nid, float c, Transform t)
      {
        nodeIdentifier = nid;
        confidence = c;
        clientTrackingToNodeTransform = t;
      }

      public string nodeIdentifier;
      public float confidence;
      public Transform clientTrackingToNodeTransform;
    };

    [Serializable]
    public class CreationInput
    {
      public CreationInput(string i, Transform t)
      {
        identifier = i;
        managedPoseToClientTracking = t;
      }

      public string identifier;
      public Transform managedPoseToClientTracking;
    };


    [Serializable]
    public class CreateManagedPosesRequest
    {
      public CreateManagedPosesRequest(string rid, Localization[] l, CreationInput[] ci, string metadata)
      {
        requestIdentifier = rid;
        localizations = l;
        creationInputs = ci;
        arCommonMetadata = metadata;
      }

      public string requestIdentifier;
      public Localization[] localizations;
      public CreationInput[] creationInputs;
      public string arCommonMetadata;
    };

    [Serializable]
    public class CreateManagedPosesResponse
    {
      public string requestIdentifier;
      public ManagedPoseCreation[] creations;
      public string statusCode;
    }

    public enum StatusCode
    {
      STATUS_CODE_UNSPECIFIED = 0,
      STATUS_CODE_SUCCESS = 1,
      STATUS_CODE_FAIL = 2, // TODO: Expand on failure possibilities
      STATUS_CODE_LIMITED = 3,
      STATUS_CODE_NOT_FOUND = 4,
      STATUS_CODE_PERMISSION_DENIED = 5,
      STATUS_CODE_INVALID_ARGUMENT = 6,
      STATUS_CODE_INTERNAL = 7
    }

    [Serializable]
    public class ManagedPoseCreation
    {
      public WayspotAnchorBlob managedPose;
    }

    [Serializable]
    public class WayspotAnchorBlob
    {
      public WayspotAnchorBlob(string d)
      {
        data = d;
      }

      public string data;
    }

    [Serializable]
    public class CreateManagedPosesWithOffsetsRequest
    {
      public CreateManagedPosesWithOffsetsRequest(string rid, WayspotAnchorBlob mp, CreationInput[] ci, string metadata)
      {
        requestIdentifier = rid;
        referenceManagedPose = mp;
        creationInputs = ci;
        arCommonMetadata = metadata;
      }

      public string requestIdentifier;
      public WayspotAnchorBlob referenceManagedPose;
      public CreationInput[] creationInputs;
      public string arCommonMetadata;
    };
    
    [Serializable]
    public class ResolveManagedPosesRequest
    {
      public ResolveManagedPosesRequest(string rid, Localization[] l, WayspotAnchorBlob[] mp)
      {
        requestIdentifier = rid;
        localizations = l;
        managedPoses = mp;
      }

      public string requestIdentifier;
      public Localization[] localizations;
      public WayspotAnchorBlob[] managedPoses;
    };
  }
}
