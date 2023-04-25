// Copyright 2022 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEditor;

using Niantic.ARDK.Configuration.Authentication;

#if (UNITY_EDITOR)
namespace Niantic.ARDK.ConfigHelper.Editor
{
  public class ARDKHelperWindow : EditorWindow
  {
    private string _lightshipKey = "";

    public static void ShowHelperWindow()
    {
      GetWindow<ARDKHelperWindow>("Configuration Helper");
    }

    void OnGUI()
    {
      GUILayout.Label("Setup API Key", EditorStyles.boldLabel);

      ArdkAuthConfig auth = AssetDatabase.LoadAssetAtPath<ArdkAuthConfig>("Assets/Resources/ARDK/ArdkAuthConfig.asset");
      string oldKey = "";

      if (auth == null) {
        auth = ScriptableObject.CreateInstance<ArdkAuthConfig>();
        if(!AssetDatabase.IsValidFolder("Assets/Resources")){
          AssetDatabase.CreateFolder("Assets", "Resources");
          if(!AssetDatabase.IsValidFolder("Assets/Resources/ARDK")){
            AssetDatabase.CreateFolder("Assets/Resources", "ARDK");
          }
        }
        AssetDatabase.CreateAsset(auth, "Assets/Resources/ARDK/ArdkAuthConfig.asset");
      }

      SerializedObject sObject = new SerializedObject(auth);
      SerializedProperty sProperty = sObject.FindProperty("_apiKey");
      string currentKey = sProperty.stringValue;

      // Set old key to new ArdkAuthConfig
      if (!oldKey.Equals("") && currentKey.Equals("")) 
      {
        sProperty.stringValue = oldKey;
        sObject.ApplyModifiedProperties();
      }

      _lightshipKey = EditorGUILayout.TextField("API Key", _lightshipKey);
      GUILayout.Label("Current API Key: " + sProperty.stringValue, EditorStyles.label);

      if (GUILayout.Button("Setup"))
      {
        if (!_lightshipKey.Equals(""))
        {
          sProperty.stringValue = _lightshipKey;
          sObject.ApplyModifiedProperties();
          EditorUtility.DisplayDialog("Lightship", "API Key has been set correctly", "Ok");
        }
        else
        {
          EditorUtility.DisplayDialog("Lightship", "Insert a valid API Key and try again", "Ok");
        }
      }
    }
  }
}
#endif
