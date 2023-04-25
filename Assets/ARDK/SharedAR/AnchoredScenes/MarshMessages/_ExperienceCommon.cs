// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.WayspotAnchors;
using Niantic.ARDK.LocationService;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages
{
  /// @note This is an experimental feature. Experimental features should not be used in
  /// production products as they are subject to breaking changes, not officially supported, and
  /// may be deprecated without notice
  // Internal API to convert AnchoredScenes (public) struct to internal client <-> server messages
  [Serializable]
  internal struct _ExperienceCommon
  {
    // Lower camelCase names to match Json format that Marsh expects
#region APIs to be serialized to Marsh
    // Note - these fields cannot be modified to maintain compatibility with Marsh.
    //  No additional public fields should be added without corresponding server changes
    public string experienceId;
    public string name;
    public string description;
    public int emptyRoomTimeoutSeconds;
    public string initData;
    public string appId;
    public double lat;
    public double lng;
    // End public field portion
#endregion
    
    private const string WayspotAnchorsPrefix = "!WayspotAnchorsPrefix";

    public _ExperienceCommon
    (
      Dictionary<string, byte[]> gameData,
      List<WayspotAnchorPayload> wayspotAnchors
    )
    {
      // Defaults
      experienceId = null;
      name = null;
      description = null;
      emptyRoomTimeoutSeconds = 0;
      appId = null;
      lat = 0;
      lng = 0;
      
      Dictionary<string, byte[]> persistentData;
      if (gameData == null)
      {
        persistentData = new Dictionary<string, byte[]>();
      }
      else
      {
        persistentData = new Dictionary<string, byte[]>(gameData);
      }

      // Only add a WayspotAnchors entry if there are WayspotAnchors
      if (wayspotAnchors != null && wayspotAnchors.Count != 0)
      {
        var wayspotAnchorsData = new WayspotAnchorsData
        {
          Payloads = wayspotAnchors.Select(a => a.Serialize()).ToArray()
        };

        var wayspotAnchorsJson = JsonUtility.ToJson(wayspotAnchorsData);
        
        // Add the wayspot anchor payloads as a serialized json WayspotAnchorsData
        persistentData.Add(WayspotAnchorsPrefix, System.Text.Encoding.UTF8.GetBytes(wayspotAnchorsJson));
      }
      
      initData = persistentData._DictionaryStringByteToJson();
    }

    public static implicit operator _ExperienceCommon(AnchoredScene scene)
    {
      Dictionary<string, byte[]> persistentData;
      if (scene.PersistentGameData == null)
      {
        persistentData = new Dictionary<string, byte[]>();
      }
      else
      {
        persistentData = new Dictionary<string, byte[]>(scene.PersistentGameData);
      }

      // Only add a WayspotAnchors entry if there are WayspotAnchors
      if (scene.WayspotAnchors != null && scene.WayspotAnchors.Count != 0)
      {
        var wayspotAnchorsData = new WayspotAnchorsData
        {
          Payloads = scene.WayspotAnchors.Select(a => a.Serialize()).ToArray()
        };

        var wayspotAnchorsJson = JsonUtility.ToJson(wayspotAnchorsData);
        
        // Add the wayspot anchor payloads as a serialized json WayspotAnchorsData
        persistentData.Add(WayspotAnchorsPrefix, System.Text.Encoding.UTF8.GetBytes(wayspotAnchorsJson));
      }

      var exp = new _ExperienceCommon()
      {
        experienceId = scene.SceneId,
        name = scene.Name,
        description = scene.Kind,
        emptyRoomTimeoutSeconds = 0,
        initData = persistentData._DictionaryStringByteToJson(),
        appId = null,
        lat = scene.Location.Latitude,
        lng = scene.Location.Longitude,
      };

      return exp;
    }
    
    public static implicit operator AnchoredScene(_ExperienceCommon exp)
    {
      Dictionary<string, byte[]> persistentData;
      List<WayspotAnchorPayload> wayspotAnchorList;
      if (exp.initData == null)
      {
        persistentData = new Dictionary<string, byte[]>();
        wayspotAnchorList = new List<WayspotAnchorPayload>();
      }
      else
      {
        persistentData = _DictionaryToJsonHelper._JsonToDictionaryStringByte(exp.initData.ToString());
        wayspotAnchorList = new List<WayspotAnchorPayload>();
        // Get WayspotAnchors out of the initData, if present
        if (persistentData.TryGetValue(WayspotAnchorsPrefix, out var waBytes))
        {
          var wayspotAnchorsData = JsonUtility.FromJson<WayspotAnchorsData>(System.Text.Encoding.UTF8.GetString(waBytes));

          foreach (var wayspotAnchorPayload in wayspotAnchorsData.Payloads)
          {
            var payload = WayspotAnchorPayload.Deserialize(wayspotAnchorPayload);
            wayspotAnchorList.Add(payload);
          }

          persistentData.Remove(WayspotAnchorsPrefix);
        }
      }

      var scene = new AnchoredScene()
      {
        Name = exp.name,
        Kind = exp.description,
        PersistentGameData = persistentData,
        Location = new LatLng(exp.lat, exp.lng),
        WayspotAnchors = wayspotAnchorList,
        SceneId = exp.experienceId
      };

      return scene;
    }

    // Get the Persistent Game Data dictionary out of the init data string
    // Parses the string and generates a new dictionary, so this is rather costly
    public Dictionary<string, byte[]> GetPersistentGameData()
    {
      if (string.IsNullOrEmpty(initData))
        return new Dictionary<string, byte[]>();
      
      var persistentData = _DictionaryToJsonHelper._JsonToDictionaryStringByte(initData);
      persistentData.Remove(WayspotAnchorsPrefix);

      return persistentData;
    }

    // Get the WayspotAnchor List out of the init data string
    // Parses the string and generates a new list, so this is rather costly
    public List<WayspotAnchorPayload> GetWayspotAnchors()
    {
      var wayspotAnchors = new List<WayspotAnchorPayload>();
      if (string.IsNullOrEmpty(initData))
        return wayspotAnchors;
      
      var persistentData = _DictionaryToJsonHelper._JsonToDictionaryStringByte(initData);
      // Get WayspotAnchors out of the initData, if present
      if (persistentData.TryGetValue(WayspotAnchorsPrefix, out var waBytes))
      {
        var wayspotAnchorsData = JsonUtility.FromJson<WayspotAnchorsData>
          (System.Text.Encoding.UTF8.GetString(waBytes));

        foreach (var wayspotAnchorPayload in wayspotAnchorsData.Payloads)
        {
          var payload = WayspotAnchorPayload.Deserialize(wayspotAnchorPayload);
          wayspotAnchors.Add(payload);
        }
      }

      return wayspotAnchors;
    }

    internal static string _ConvertGameDataAndWayspotAnchorsToInitData
    (
      Dictionary<string, byte[]> gameData,
      List<WayspotAnchorPayload> wayspotAnchorPayloads
    )
    {
      var initDataDict = new Dictionary<string, byte[]>();
      // Only add a WayspotAnchors entry if there are WayspotAnchors
      if (wayspotAnchorPayloads != null && wayspotAnchorPayloads.Count != 0)
      {
        var wayspotAnchorsData = new WayspotAnchorsData
        {
          Payloads = wayspotAnchorPayloads.Select(a => a.Serialize()).ToArray()
        };

        var wayspotAnchorsJson = JsonUtility.ToJson(wayspotAnchorsData);
        
        // Add the wayspot anchor payloads as a serialized json WayspotAnchorsData
        initDataDict.Add(WayspotAnchorsPrefix, System.Text.Encoding.UTF8.GetBytes(wayspotAnchorsJson));
      }

      if (gameData != null)
      {
        // Add each of the game state kvps as an entry
        foreach (var kvp in gameData)
        {
          if (kvp.Key.Equals(WayspotAnchorsPrefix))
          {
            ARLog._Warn($"Persistent Game Data is using {WayspotAnchorsPrefix} as a key, ignoring");
            continue;
          }

          initDataDict.Add(kvp.Key, kvp.Value);
        }
      }
      
      return initDataDict._DictionaryStringByteToJson();
    }

    [Serializable]
    private class WayspotAnchorsData
    {
      /// The payloads to save via JsonUtility
      public string[] Payloads = Array.Empty<string>();
    }
  }
}
