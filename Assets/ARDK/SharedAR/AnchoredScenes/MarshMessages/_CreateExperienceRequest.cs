// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  [Serializable]
  internal struct _CreateExperienceRequest
  {
    // Lower camelCase names to match Json format that Marsh expects
#region APIs to be serialized to Marsh
    // Note - these fields cannot be modified to maintain compatibility with Marsh.
    //  No additional public fields should be added without corresponding server changes
    
    // Name of the experience.
    public string name;
    // Optional description of the experience.
    public string description;
    // Key / Value pairs to initialize the rooms with
    // Must be a Json string representing a serialized Dictionary<string, byte[]>
    // Use _DictionaryToJsonHelper to serialize and deserialize the dictionary.
    public string initData;
    // Time until empty rooms are automatically destroyed, default to 600s.
    // Setting to negative value to indicate the room will not timeout, the
    // server will enforce only one room exist at any given time for an experience.
    public int emptyRoomTimeoutSeconds;
    // Latitude of the experience.
    public double lat;
    // Longitude of the experience.
    public double lng;
#endregion
  }
}
