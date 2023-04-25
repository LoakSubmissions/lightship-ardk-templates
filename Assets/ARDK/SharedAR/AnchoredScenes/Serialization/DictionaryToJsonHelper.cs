// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Niantic.ARDK.Utilities.Logging;

namespace Niantic.Experimental.ARDK.SharedAR.AnchoredScenes.MarshMessages
{
  // Unity's built-in JsonUtility does not handle dictionaries
  // Specifically handles string:byte[] dictionaries for Marsh request/responses.
  // @note This class was specifically written to help handle client <-> Marsh requests, there will
  //  be edge cases and this is not meant to be generally used.
  internal static class _DictionaryToJsonHelper
  {
    // Regex to match and grab contents within brackets of the Json and strip spaces
    //  { <content here> }
    private const string BracketsRegexString = "^\\s*{\\s*(.*)\\s*}\\s*$";
    private static Regex _BracketsRegex = new Regex(BracketsRegexString);
    
    // Regex to grab each key:value pair after splitting
    // "(key1)", "(value1)" -> (group1) (group2)
    private const string KeyValuePairRegexString = "\\s*\\\"(.*)\\\"\\s*:\\s*\\\"(.*)\\\"\\s*";
    private static Regex _KeyValuePairRegex = new Regex(KeyValuePairRegexString);

    private const char _CommaDelimiter = ',';
    
    // Helper to serialize a string:byte[] dictionary to Json
    //  The byte[] is represented as a base64 encoded string to match protobuf ByteString 
    //  The serialized Json string is formatted as:
    //    { "key1" : "base64 value1", "key2" : "base64 value2", ... }
    //  No guarantee of order
    // @note This method was specifically written to help handle client <-> Marsh requests, there will
    //  be edge cases and this is not meant to be generally used.
    internal static string _DictionaryStringByteToJson(this Dictionary<string, byte[]> dict)
    {
      if (dict == null || dict.Count == 0)
        return null;

      var stringBuilder = new StringBuilder();
      stringBuilder.Append("{ ");

      foreach (var kvp in dict)
      {
        stringBuilder.Append($"\"{kvp.Key}\": ");
        var valAsBase64 = System.Convert.ToBase64String(kvp.Value);
        stringBuilder.Append($"\"{valAsBase64}\", ");
      }

      // Remove trailing comma and space
      stringBuilder.Remove(stringBuilder.Length - 2, 2);
      stringBuilder.Append("}");

      return stringBuilder.ToString();
    }

    // Helper to deserialize a Json formatted string into a string:byte[] Dictionary
    //  The byte[] is represented as a base64 encoded string to match protobuf ByteString 
    //  The serialized Json string is formatted as:
    //    { "key1" : "base64 value1", "key2" : "base64 value2", ... }
    //  No guarantee of order
    // @note This method was specifically written to help handle client <-> Marsh requests, there will
    //  be edge cases and this is not meant to be generally used.
    internal static Dictionary<string, byte[]> _JsonToDictionaryStringByte(string json)
    {
      var toReturn = new Dictionary<string, byte[]>();
      if (string.IsNullOrEmpty(json))
        return toReturn;
      
      var match = _BracketsRegex.Match(json);
      // 2 Groups - original string + capture
      if (!match.Success || match.Groups.Count != 2)
      {
        ARLog._Error("Could not parse json into dictionary, missing proper brackets");
        return toReturn;
      }

      var bracketlessJson = match.Groups[1].Value;
      var splitString = bracketlessJson.Split(_CommaDelimiter);

      foreach (var kvp in splitString)
      {
        var kvpMatch = _KeyValuePairRegex.Match(kvp);
        // 3 Groups - original kvp string + 2 captures
        if (!kvpMatch.Success || kvpMatch.Groups.Count != 3)
        {
          ARLog._Error("Could not parse entry into string:string kvp");
          // Return empty instead of half filled dictionary
          return new Dictionary<string, byte[]>();
        }

        var key = kvpMatch.Groups[1].Value;
        var val = System.Convert.FromBase64String(kvpMatch.Groups[2].Value);
        
        toReturn.Add(key, val);
      }

      return toReturn;
    }

    // Grpc cannot handle an empty string for initData (it is expecting an empty dictionary {})
    //  Remove the `initData = ""` entry if it exists
    private const string EmptyInitDataString = "\"initData\":\"\",";
    // JsonUtility adds escape symbols to strings, we don't want them (Marsh is expecting a map)
    // Also remove the initData if it is empty.
    // @note This method was specifically written to help handle client <-> Marsh requests, there will
    //  be edge cases and this is not meant to be generally used.
    internal static string _FormatJsonRequestForMarsh(string originalJson)
    {
      if (string.IsNullOrEmpty(originalJson))
        return null;

      var newString = originalJson.Replace(EmptyInitDataString, "");
      newString = newString.Replace("\"{", "{");
      newString = newString.Replace("}\"", "}");
      newString = newString.Replace("\\", "");

      return newString;
    }

    // Capture the dictionary contents within the init data.
    // "initData":<{capture this}>,
    private const string InitDataFromMarshString = "\"initData\":\\s*({\\s*.*?}),";
    private static Regex _InitDataFromMarshCaptureRegex = new Regex(InitDataFromMarshString);

    // If the initData is empty, we get an empty dictionary back `initData:{}`
    // Since JsonUtility is expecting a string, replace the {} with ""
    private const string EmptyDictionaryAsJson = "{}";

    // Re-add escape symbols in from of " (becomes \"). We are getting a map back, so make it a string.
    // Add surrounding quotes around initData string for JsonUtility to parse it as a string
    // @note This method was specifically written to help handle client <-> Marsh requests, there will
    //  be edge cases and this is not meant to be generally used.
    internal static string _FormatJsonResponseFromMarsh(string originalJson)
    {
      if (string.IsNullOrEmpty(originalJson))
        return null;
      
      var match = _InitDataFromMarshCaptureRegex.Match(originalJson);
      if (!match.Success)
        return originalJson;

      var substring = match.Groups[1].Value;
      string newSubstring;
      
      // Since we are getting the response back as a Json, remove the empty dictionary representation
      if (substring.Equals(EmptyDictionaryAsJson))
      {
        newSubstring = "\"\"";
      }
      else
      {
        newSubstring = $"\"{substring.Replace("\"", "\\\"")}\"";
      }

      return originalJson.Replace(substring, newSubstring);
    }

    // Re-add escape symbols in from of " (becomes \"). We are getting a map back, so make it a string.
    // Add surrounding quotes around initData string for JsonUtility to parse it as a string
    // Operates the same as _FormatJsonResponseFromMarsh but handles multiple experiences in one Json
    // @note This method was specifically written to help handle client <-> Marsh requests, there will
    //  be edge cases and this is not meant to be generally used.
    internal static string _FormatMultipleJsonResponsesFromMarsh(string originalJson)
    {
      if (string.IsNullOrEmpty(originalJson))
        return null;

      var match = _InitDataFromMarshCaptureRegex.Match(originalJson);
      var toReturn = originalJson;
      
      // Match along the original Json response until no more groups of "initData" are found.
      // When "initData" is found, either convert the empty dictionary into an empty string, or add
      //  escapes to each ".
      while (match.Success)
      {
        var substring = match.Groups[1].Value;
        string newSubstring;

        // Since we are getting the response back as a Json, remove the empty dictionary representation
        if (substring.Equals(EmptyDictionaryAsJson))
        {
          newSubstring = "\"\"";
        }
        else
        {
          newSubstring = $"\"{substring.Replace("\"", "\\\"")}\"";
        }

        toReturn = toReturn.Replace(substring, newSubstring);

        match = _InitDataFromMarshCaptureRegex.Match(toReturn);
      }

      return toReturn;
    }
  }
}
