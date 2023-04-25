using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using RAGUIContent = Niantic.ARDK.AR.WayspotAnchors.Editor._RemoteAuthoringEditorWindowGUIContent;
using RAStrings = Niantic.ARDK.AR.WayspotAnchors.Editor._RemoteAuthoringEditorWindowStrings;
using RAStyles = Niantic.ARDK.AR.WayspotAnchors.Editor._RemoteAuthoringEditorWindowStyles;

using RemoteAuthoringAssistant = Niantic.ARDK.AR.WayspotAnchors.EditModeOnlyBehaviour.RemoteAuthoringAssistant;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal class _RemoteAuthoringEditorWindow : EditorWindow
  {
    private const string SELECTED_ANCHOR_KEY_INT = "RAA_SelectedAnchor";
    private const string SELECT_EDIT_MODE_ANCHOR_NAME = "RAA_NAME_EDIT_ANCHOR_MODE";
    private const string SELECT_EDIT_MODE_LOCATION_NAME = "RAA_NAME_EDIT_LOCATION_MODE";
    private const float TRASH_BUTTON_WIDTH = 30f;

    private Vector2 _windowScrollPos;
    private Vector2 _anchorsScrollPos;
    private string _revisedLocationName;
    private _AuthoredWayspotAnchorInspector _currentAnchorInspector;
    private EditModeOnlyBehaviour.AuthoredWayspotAnchor _currentAnchorComponent;
    private RemoteAuthoringAssistant _remoteAuthoringAssistant;

    private string[] _locationNames;
    private int _currSelectedLocationIndex;
    private int _currSelectedAnchorIndex = -1;
    private int _prevSelectedAnchorIndex = -1;
    
    private int _SavedSelectedAnchorIndex
    {
      get
      {
        return EditorPrefs.GetInt(SELECTED_ANCHOR_KEY_INT, -1);
      }
      set
      {
        EditorPrefs.SetInt(SELECTED_ANCHOR_KEY_INT, value);
      }
    }

    private void UpdateSavedSelectedAnchorIndex()
    {
      _SavedSelectedAnchorIndex = _currSelectedAnchorIndex;
    }
    
    private RemoteAuthoringAssistant CachedRemoteAuthoringAssistant
    {
      get
      {
        if (!_RemoteAuthoringPresenceManager.CanUseRemoteAuthoring)
          return null;
        
        if (_remoteAuthoringAssistant == null)
          _remoteAuthoringAssistant = RemoteAuthoringAssistant.FindSceneInstance();

        if (_remoteAuthoringAssistant == null)
        {
          _currSelectedAnchorIndex = -1;
          UpdateSavedSelectedAnchorIndex();
          _remoteAuthoringAssistant = _RemoteAuthoringPresenceManager.AddPresence();
        }
        
        return _remoteAuthoringAssistant;
      }
    }

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Lightship/ARDK/VPS Authoring Assistant/Open", false, 0)]
    public static void ShowWindow()
    {
      // Show existing window instance. If one doesn't exist, make one.
      var inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
      GetWindow<_RemoteAuthoringEditorWindow>(RAStrings.EDITOR_WINDOW_TITLE, true, inspectorType);
    }
    
    // Need to check if already closing to avoid destroying same object error
    private static bool _isClosing;
    [MenuItem("Lightship/ARDK/VPS Authoring Assistant/Close", false, 0)]
    public static void CloseWindow()
    {
      if (!_isClosing && HasOpenInstances<_RemoteAuthoringEditorWindow>())
      {
        _isClosing = true;
        GetWindow<_RemoteAuthoringEditorWindow>().Close();
        _isClosing = false;
      }
    }
    
    private void OnEnable()
    {
      RemoteAuthoringAssistant.ActiveManifestChanged += SetSelectedLocationIndex;
      
      if (!_RemoteAuthoringPresenceManager.CanUseRemoteAuthoring || CachedRemoteAuthoringAssistant == null)
      {
        _currSelectedAnchorIndex = -1;
        UpdateSavedSelectedAnchorIndex();
        _remoteAuthoringAssistant = null;
        return;
      }

      ExtractLocationNames();

      var activeManifest = CachedRemoteAuthoringAssistant.ActiveManifest;
      if (activeManifest != null) 
        _currSelectedLocationIndex = Array.IndexOf(_locationNames, activeManifest.LocationName);
      else if (CachedRemoteAuthoringAssistant.AllManifests.Count > 0)
        CachedRemoteAuthoringAssistant.OpenLocation(CachedRemoteAuthoringAssistant.AllManifests[0]);
    }

    private void OnDisable()
    {
      RemoteAuthoringAssistant.ActiveManifestChanged -= SetSelectedLocationIndex;
      _remoteAuthoringAssistant = null;
    }

    private void SetSelectedLocationIndex(VPSLocationManifest old, VPSLocationManifest curr)
    {
      ExtractLocationNames();

      if (curr != null)
      {
        var activeManifestName = curr.LocationName;
        _currSelectedLocationIndex = Array.IndexOf(_locationNames, activeManifestName);

        if (curr.AuthoredAnchorsData.Count > 0)
          _SavedSelectedAnchorIndex = 0;
      }
      else
      {
        _currSelectedLocationIndex = -1;
        _SavedSelectedAnchorIndex = -1;
      }
      
      _currentAnchorComponent = null;
      _currentAnchorInspector = null;
    }

    private void OnDestroy()
    {
      _RemoteAuthoringPresenceManager.RemovePresence();
    }

    private void OnGUI()
    {
      _currSelectedAnchorIndex = _SavedSelectedAnchorIndex;
      var defaultColor = GUI.color;
      //setting the size of commonly used dimensions.  Responds to size of the window
      var locationDropdownWidth = Mathf.Clamp((int)(position.width * .56f), 225, 550);
      var extendedContentWidth = Mathf.Min((int)(position.width * .76f), 650);
      // Draw the background (or in this case, the background color for the editor window)
      GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), RAStyles.BackgroundTexture, ScaleMode.StretchToFill);

      var selectedState = new GUIStyleState();
      selectedState.background = Texture2D.blackTexture;

      if (!_RemoteAuthoringPresenceManager.CanUseRemoteAuthoring)
      {
        //turn off anchor renaming.  Its not needed.
        ResetAnchorNaming();
        DrawRuntime();
        return;
      }

      if (CachedRemoteAuthoringAssistant == null) // Only ever true for a few frames, so not bothering rendering any UI
        return;

      ExtractLocationNames();
      
      _windowScrollPos =
        EditorGUILayout.BeginScrollView
        (
          _windowScrollPos,
          GUI.skin.horizontalScrollbar,
          GUI.skin.verticalScrollbar
        );
      
      DrawLocationAndGSBSection(defaultColor, locationDropdownWidth);
      
      GUILayout.Space(5f);

      DrawDataManagementSection();

      GUILayout.Space(5f);

      _prevSelectedAnchorIndex = _currSelectedAnchorIndex;
      DrawAnchorManagementSection(extendedContentWidth);

      GUILayout.Space(5f);
      
      DrawSelectedAnchorInspector();
      
      EditorGUILayout.EndScrollView();
    }

    private void ExtractLocationNames()
    {
      _locationNames = CachedRemoteAuthoringAssistant.AllManifests.Select(m => m.LocationName).ToArray();
    }
    private void DrawRuntime()
    {
      GUILayout.Label(RAStrings.EDITOR_WINDOW_TITLE, RAStyles.HeaderTextStyle);
      GUILayout.Label("Remote Authoring Editor Window not available outside Edit Mode");
    }

    private void DrawLocationAndGSBSection
    (
      Color defaultColor,
      int locationDropdownWidth
    )
    {
      var editLocationNameSelected = EditorPrefs.GetBool(SELECT_EDIT_MODE_LOCATION_NAME, false);
      var cueImport = false;
      var cueDeleteLocation = false;
      
      void ResetLocationNameState()
      {
          editLocationNameSelected = false;
          _revisedLocationName = string.Empty;
      }
//start with the Location section:  where documentation is linked and locations are described
      //locations can be edited (name), deleted, and imported
      EditorGUILayout.BeginHorizontal(RAStyles.HeaderSectionStyle, GUILayout.ExpandHeight(true));
      EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
      {
        GUILayout.Label(RAStrings.EDITOR_WINDOW_TITLE, RAStyles.HeaderTextStyle);
        EditorGUILayout.BeginHorizontal();
        {
          if (GUILayout.Button(RAStrings.TEXT_LINK_TO_DOCUMENTATION, RAStyles.LinkLabelStyle))
            Application.OpenURL(RAStrings.DOCUMENTATION_LINK_URL);
          
          GUI.color = RAStyles.LinkBlue;
          if (GUILayout.Button(RAGUIContent.OutLinkTexture, GUI.skin.GetStyle("IconButton")))
            Application.OpenURL(RAStrings.DOCUMENTATION_LINK_URL);

          GUI.color = defaultColor;
          GUILayout.FlexibleSpace();

          if (GUILayout.Button(RAStrings.GSB_DESCRIPTION, EditorStyles.linkLabel))
            Application.OpenURL(RAStrings.GEOSPATIAL_BROWSER_URL);

          GUI.color = RAStyles.LinkBlue;
          if (GUILayout.Button(RAGUIContent.LocationTexture, GUI.skin.GetStyle("IconButton")))
            Application.OpenURL(RAStrings.GEOSPATIAL_BROWSER_URL);

          GUI.color = defaultColor;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20f);

        EditorGUILayout.BeginHorizontal();
        {
          if (!editLocationNameSelected)
          {
            if (!string.IsNullOrWhiteSpace(_revisedLocationName))
            {
              CachedRemoteAuthoringAssistant.ActiveManifest.LocationName = _revisedLocationName;
              _revisedLocationName = string.Empty;
            }

            var newLocationIndex =
              EditorGUILayout.Popup
              (
                RAStrings.SELECTED_LOCATION_DROPDOWN_LABEL_TEXT,
                _currSelectedLocationIndex,
                _locationNames,
                GUILayout.Width(locationDropdownWidth)
              );
            
            if (newLocationIndex != _currSelectedLocationIndex)
            {
              ResetLocationNameState();
              CachedRemoteAuthoringAssistant.OpenLocation(CachedRemoteAuthoringAssistant.AllManifests[newLocationIndex]);
            }
          }
          else
          {
            if (string.IsNullOrEmpty(_revisedLocationName) && CachedRemoteAuthoringAssistant.ActiveManifest)
              _revisedLocationName = CachedRemoteAuthoringAssistant.ActiveManifest.LocationName;

            _revisedLocationName =
              EditorGUILayout.TextField
              (
                RAStrings.SELECTED_LOCATION_DROPDOWN_LABEL_TEXT,
                _revisedLocationName,
                RAStyles.SmallTextFieldStyle,
                GUILayout.Width(locationDropdownWidth)
              );
            
            if (Event.current.keyCode == KeyCode.Return)
            {
              editLocationNameSelected = false;
              Repaint();
            }
          }

          editLocationNameSelected = (CachedRemoteAuthoringAssistant.ActiveManifest) && GUILayout.Toggle
          (
            editLocationNameSelected,
            RAGUIContent.RenameLocationContent,
            EditorStyles.miniButton,
            GUILayout.Width(TRASH_BUTTON_WIDTH)
          );

          if (GUILayout.Button(RAGUIContent.TrashLocationContent, EditorStyles.miniButton, GUILayout.Width(TRASH_BUTTON_WIDTH)))
          {
            cueDeleteLocation = true;
            ResetLocationNameState();
          }

          GUILayout.FlexibleSpace();
          
          GUILayout.Label(RAStrings.IMPORT_MESH_LABEL, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100f));
          if (GUILayout.Button(RAGUIContent.ImportTexture, GUILayout.Width(25), GUILayout.Height(19f)))
          {
            //prompt the user to import a zip file
            
            //boot the user out of the naming state if they are in it.
            ResetLocationNameState();
            
            cueImport = true;
          } 
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(25f);
      }
      EditorGUILayout.EndVertical();
      EditorGUILayout.EndHorizontal();
      EditorPrefs.SetBool(SELECT_EDIT_MODE_LOCATION_NAME, editLocationNameSelected);
      
      //Monitor these behaviors.  If we continue to get errors with Begin / End Layout
      //It may make sense to move this functionality out of OnGUI call, or move
      //to the end of layout handling in OnGUI 
      if (cueImport)
      {
        _VPSLocationImporter.Import();
      }

      if (cueDeleteLocation)
      {
        var verified = EditorUtility.DisplayDialog
        (
          RemoteAuthoringAssistant.DIALOG_TITLE,
          RAStrings.REMOVE_LOCATION_PROMPT,
          RAStrings.CONFIRM_TEXT,
          RAStrings.CANCEL_TEXT
        );

        if (verified)
        {
          AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(CachedRemoteAuthoringAssistant.ActiveManifest));
          _currentAnchorComponent = null;
        }
      }
    }

    // Inspect and save manifest content
    private void DrawDataManagementSection()
    {
      EditorGUILayout.BeginHorizontal(_RemoteAuthoringEditorWindowStyles.SmallSectionStyle, GUILayout.ExpandHeight(true));
      EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
      {
        GUILayout.Label(RAGUIContent.Header3WIcon, _RemoteAuthoringEditorWindowStyles.HeaderSubTextStyle);
        
        if (CachedRemoteAuthoringAssistant.ActiveManifest != null)
        {
          EditorGUILayout.BeginHorizontal();
          {
            GUILayout.Space(15f);
            if (GUILayout.Button(RAGUIContent.InspectButtonContent, GUILayout.Width(140f), GUILayout.Height(40f)))
            {
              Selection.activeObject = CachedRemoteAuthoringAssistant.ActiveManifest;
              EditorUtility.FocusProjectWindow();
            }

            GUILayout.Label(RAStrings.SELECTED_LOCATION_DESCRIPTION_TEXT, RAStyles.CenteredLabelStyle);
          }
          EditorGUILayout.EndHorizontal();

          GUILayout.Space(5f);

          EditorGUILayout.BeginHorizontal();
          {
            GUILayout.Space(15f);
            if (GUILayout.Button(RAGUIContent.SaveAnchorsButtonContent, GUILayout.Width(140f), GUILayout.Height(40f)))
            {
              //TODO: Kelly should investigate.  I'm not sure why I have to call this twice to guarantee that it works
              CachedRemoteAuthoringAssistant.SaveUnsavedData(false);
              CachedRemoteAuthoringAssistant.SaveUnsavedData(false);
              Repaint();
            }

            GUILayout.Label(RAStrings.SAVE_DESCRIPTION_TEXT, RAStyles.CenteredLabelStyle);
          }
          EditorGUILayout.EndHorizontal();
        }
        else
        {
          GUILayout.Space(25f);
          GUILayout.Label(RAStrings.SELECT_LOCATION_PROMPT, RAStyles.CenteredWhiteLabel);
        }
      }
      EditorGUILayout.EndVertical();
      EditorGUILayout.EndHorizontal();
    }

    // Create and Modify Anchors
    private void DrawAnchorManagementSection(int extendedContentWidth)
    {
      var cueRepaint = false;
      var editAnchorNameSelected = EditorPrefs.GetBool(SELECT_EDIT_MODE_ANCHOR_NAME, false);
      var style3 = new GUIStyle(EditorStyles.toolbarButton);
      style3.fixedHeight = 240;
      var anchorManagementRect = EditorGUILayout.BeginHorizontal(style3, GUILayout.ExpandHeight(true));
      var isInFocus = 
        Event.current.type == EventType.MouseUp &&
        Event.current.button == 0 &&
        anchorManagementRect.Contains(Event.current.mousePosition);

      EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
      {
        GUILayout.Label(RAGUIContent.Header4WIcon, RAStyles.HeaderSubTextStyle);

        if (CachedRemoteAuthoringAssistant.ActiveManifest != null)
        {
          GUILayout.Label(RAStrings.CREATE_SECTION_DESCRIPTION, RAStyles.LabelStyle);

          EditorGUILayout.BeginHorizontal();
          {
            GUILayout.Space(15f);
            if (GUILayout.Button(RAGUIContent.CreateAnchorButtonContent, GUILayout.Width(180f), GUILayout.Height(40f)))
            {
              var cameraTransform = SceneView.lastActiveSceneView.camera.transform;
              var ray = new Ray(cameraTransform.position, cameraTransform.forward);
              _AnchorPlacementUtility.AddAnchorFromScreenPoint(ray);
              _currentAnchorComponent = null;
              _currentAnchorInspector = null;
            }

            GUILayout.Label(RAStrings.CREATE_ANCHOR_DESCRIPTION_TEXT, RAStyles.LabelStyle);
          }
          EditorGUILayout.EndHorizontal();

          GUILayout.Space(15f);

          EditorGUILayout.BeginHorizontal();
          {
            GUILayout.Space(position.width / 2f - extendedContentWidth / 2f);

            var anchors = CachedRemoteAuthoringAssistant.ActiveAnchors.Reverse().ToArray(); // so new anchors appear on top
            if (anchors.Length == 0)
            {
              GUILayout.Space(25f);
              GUILayout.Label(RAStrings.CREATE_ANCHOR_PROMPT, RAStyles.CenteredWhiteLabel);
            }
            else
            {
              _anchorsScrollPos =
                EditorGUILayout.BeginScrollView
                (
                  _anchorsScrollPos,
                  GUIStyle.none,
                  GUI.skin.verticalScrollbar,
                  GUILayout.Width(extendedContentWidth)
                );

              if (_currSelectedAnchorIndex >= anchors.Length)
              {
                _currSelectedAnchorIndex = CachedRemoteAuthoringAssistant.ActiveAnchors.Count > 0 ? 0 : -1;
                UpdateSavedSelectedAnchorIndex();
              }
              
              // The stuff that is rendered over the scrollable list shouldn't be rendered if there
              // is nothing in the list
              var drawOverlays = anchors.Length != 0;

#region Hack methods
              // HACK: these buttons  (edit and trash) were turned into actions because of a current bug in Unity
              // As of now, the event order and the draw order of buttons in this scroll view are inverted
              // meaning if drawn on top it gets least priority in event firing and vise versa
              // to ensure that the button is both visible AND interactable it must be generated in two places in the code
              // this will hopefully be resolved in later versions of unity
              bool DrawEditButton()
              {
                if (!drawOverlays)
                  return false;

                return
                  GUI.Toggle
                  (
                    new Rect(extendedContentWidth - 75, _currSelectedAnchorIndex * 30 + 8, 32, 30),
                    editAnchorNameSelected,
                    RAGUIContent.RenameAnchorContent,
                    GUI.skin.GetStyle("IconButton")
                  );
              }

              string DrawRenameField()
              {
                if (!drawOverlays)
                  return "";

                return
                  GUI.TextField
                  (
                    new Rect(30, _currSelectedAnchorIndex * 30, extendedContentWidth - 140, 30),
                    anchors[_currSelectedAnchorIndex]._AnchorName,
                    RAStyles.TextFieldStyle
                  );
              }

              bool DrawTrashButton()
              {
                if (!drawOverlays)
                  return false;

                return
                  GUI.Button
                  (
                    new Rect(extendedContentWidth - 50, _currSelectedAnchorIndex * 30 + 8, 32, 30),
                    RAGUIContent.TrashAnchorContent,
                    EditorStyles.miniButton
                  );
              }
#endregion

              // Draw the foreground images
              editAnchorNameSelected = DrawEditButton();

              if (DrawTrashButton())
              {
                var verified =
                  EditorUtility.DisplayDialog
                  (
                    RAStrings.REMOVE_PROMPT_TITLE,
                    RAStrings.REMOTE_PROMPT_DESCRIPTION,
                    RAStrings.CONFIRM_TEXT,
                    RAStrings.CANCEL_TEXT
                  );

                if (verified)
                {
                  CachedRemoteAuthoringAssistant.RemoveAnchor(_currentAnchorComponent);
                  _currSelectedAnchorIndex = Mathf.Max(0, _currSelectedAnchorIndex - 1);
                  UpdateSavedSelectedAnchorIndex();
                  cueRepaint = true;
                }
              }

              var contents = new List<GUIContent>();
              foreach (var anchor in anchors)
              {
                var isDirty = CheckIfDirty(anchor);
                var prepend = (isDirty) ? "   <b>" : "   ";
                var postend = (isDirty) ? "</b>" : "";
                var anchorLabel = prepend + anchor._AnchorName + postend;
                var content = new GUIContent(anchorLabel, EditorGUIUtility.IconContent("DotSelection").image);
                contents.Add(content);
              }

              // Complexity of logic due to having some instances in time (like inspecting a manifest)
              // Where we dont actually want to have anchors in focus
              _currSelectedAnchorIndex =
                GUILayout.SelectionGrid
                (
                  _currSelectedAnchorIndex,
                  contents.ToArray(),
                  1,
                  RAStyles.AnchorSelectButtonStyle,
                  GUILayout.Width(extendedContentWidth)
                );

              UpdateSavedSelectedAnchorIndex();
              
              if (_currSelectedAnchorIndex < anchors.Length && _currSelectedAnchorIndex >= 0)
              {
                try
                {
                  _currentAnchorComponent = anchors[_currSelectedAnchorIndex];
                  if (_currSelectedAnchorIndex != _SavedSelectedAnchorIndex || isInFocus)
                  {
                    // ensures that anchors are selected in the inspector ONLY if the user is actively engaged in
                    // modifying anchors
                    Selection.activeGameObject = _currentAnchorComponent.gameObject;
                  }
                }
                catch (MissingReferenceException)
                {
                  //when moving quickly the editor may not update the anchors list
                  //and delete the associated authored anchor until the follow frame
                  //if the anchor has been deleted, ignore it and put the anchor in focus preceding it
                  _currSelectedAnchorIndex = Mathf.Max(0, _currSelectedAnchorIndex - 1);
                  UpdateSavedSelectedAnchorIndex();
                  
                  if (_currSelectedAnchorIndex < anchors.Length)
                    _currentAnchorComponent = null;
                }
              }
              else
              {
                _currentAnchorComponent = null;
              }

              if (drawOverlays)
              {
                GUI.Box
                (
                  new Rect(4, _currSelectedAnchorIndex * 30 + 6, 32, 30),
                  EditorGUIUtility.IconContent("DotFill").image,
                  GUIStyle.none
                );

#region DUPLICATE_BUTTON_HACK

                // NOTE: It's very important in the current iteration that these repeat calls are kept here.
                // see the HACK note above.  If unity fixes the event order
                // bug then it might be safe to remove this repeat draw
                DrawEditButton();
                DrawTrashButton();

                if (editAnchorNameSelected)
                {
                  GUI.SetNextControlName("NameAdjuster");
                  var anchor = anchors[_currSelectedAnchorIndex];
                  if (anchor != null)
                  {
                    anchor._AnchorName = DrawRenameField();
                    anchor.name = anchors[_currSelectedAnchorIndex]._AnchorName; 
                  }
                  else
                  {
                    //handling the missing reference exception if you try to delete the main anchor 
                    //while edit anchor name is selected.
                    editAnchorNameSelected = false;
                  }
                  var e = Event.current;
                  if (e.keyCode == KeyCode.Return)
                  {
                    editAnchorNameSelected = false;
                    cueRepaint = true;
                  }

                  EditorGUI.FocusTextInControl("NameAdjuster");
                }

#endregion
              }

              EditorGUILayout.EndScrollView();
            }
          }
          EditorGUILayout.EndHorizontal();
        }
        else
        {
          GUILayout.Space(25f);
          GUILayout.Label(RAStrings.SELECT_LOCATION_PROMPT, RAStyles.CenteredWhiteLabel);
        }
      }
      EditorGUILayout.EndVertical();
      EditorGUILayout.EndHorizontal();
      if (cueRepaint)
      {
        Repaint();
      }
      EditorPrefs.SetBool(SELECT_EDIT_MODE_ANCHOR_NAME, editAnchorNameSelected);
    }

    private void ResetAnchorNaming()
    {
      EditorPrefs.SetBool(SELECT_EDIT_MODE_ANCHOR_NAME, false);
    }

    private void DrawSelectedAnchorInspector()
    {
      // The anchor that is currently in focus.  We ensure it isn't null and then draw 
      // information about it at the bottom of the window.
      
      if (_currentAnchorComponent == null)
        return;
      
      GUI.SetNextControlName("AnchorDescription");

      CachedRemoteAuthoringAssistant.ActiveManifest._GetAnchorData
      (
        _currentAnchorComponent._AnchorManifestIdentifier,
        out AuthoredWayspotAnchorData serializedAnchor
      );
        
      _currentAnchorComponent.GetDifferences(serializedAnchor, out bool isBackingAnchorInvalid, out bool isManifestInvalid);
      var isDirty = isBackingAnchorInvalid || isManifestInvalid;
      var dirtyMarking = isDirty ? "*" : "";
        
      EditorGUILayout.BeginVertical();
      {
        EditorGUILayout.BeginHorizontal();
        {
          var header5wIcon =
            new GUIContent
            (
              "   Selected Anchor: " + _currentAnchorComponent._AnchorName + dirtyMarking,
              RAGUIContent.SelectedAnchorTexture
            );

          GUILayout.Label(header5wIcon, RAStyles.HeaderSubTextStyle);

          if (isDirty)
          {
            GUILayout.FlexibleSpace();
              
            if (GUILayout.Button(RAGUIContent.SaveSelectedContent, RAStyles.SelectedAnchorButtonStyle))
              CachedRemoteAuthoringAssistant.UpdateAnchor(_currentAnchorComponent, isBackingAnchorInvalid);

            GUILayout.Space(5f);
              
            if (GUILayout.Button(RAGUIContent.DiscardChangesSelectedContent, RAStyles.SelectedAnchorButtonStyle))
              _currentAnchorComponent._ResetToData(serializedAnchor);

            GUILayout.Space(5f);
          }
        }
        EditorGUILayout.EndHorizontal();
        
        if (isDirty)
          GUILayout.Space(15f);
        
        if (_currentAnchorInspector == null || _currSelectedAnchorIndex != _prevSelectedAnchorIndex)
          _currentAnchorInspector = (_AuthoredWayspotAnchorInspector)UnityEditor.Editor.CreateEditor(_currentAnchorComponent);

        if (_currentAnchorInspector != null)
        {
          try
          {
            //we are protecting against a IMGUI controls argument exception
            //that occurs when a control (the anchor list) is reduced during a repaint
            //the number of anchor GUI objects  (internal to IMGUI) is not consistent
            //to the index of the last object in the list (this seems like an IMGUI bug to me)
            if (_currentAnchorInspector.target != null)
              _currentAnchorInspector.OnInspectorGUI();
            else
              _currentAnchorInspector = null;
          }
          catch (ArgumentException)
          {
            _currentAnchorInspector = null;
            EditorGUILayout.EndVertical();
            return;
          }
          
        }

        GUILayout.Space(5f);
      }
      EditorGUILayout.EndVertical();
    }
    
    private void OnInspectorUpdate()
    {
      if (!_RemoteAuthoringPresenceManager.CanUseRemoteAuthoring || CachedRemoteAuthoringAssistant == null)
        return;
      
      if (_currentAnchorInspector != null)
      {
        // Force the selected anchor to stay in sync with movement
        Repaint();
        
        //In this case the inspector shouldn't be drawn because we dont know which one to draw
        //rather than attempting to draw in the error state, wait for currentInspector to be populated again
        //and then we can move forward in this logic.
        return;
      }

      if (CachedRemoteAuthoringAssistant.ActiveManifest == null || _currentAnchorInspector == null)
        return;
      
      var editors = Resources.FindObjectsOfTypeAll<UnityEditor.Editor>();
      foreach (var editor in editors)
      {
        if (editor is _AuthoredWayspotAnchorInspector inspector)
        {
          // Find all inspector editors that have the current anchor in focus
          // update their background info.
          if (inspector.target == _currentAnchorInspector.target && editor != _currentAnchorInspector)
          {
            inspector.UpdateSafeAssets(_currentAnchorInspector.SafePrefabAssets);
          }
        }
      }
    }

    private bool CheckIfDirty(EditModeOnlyBehaviour.AuthoredWayspotAnchor anchor)
    {
      CachedRemoteAuthoringAssistant.ActiveManifest._GetAnchorData
      (
        anchor._AnchorManifestIdentifier,
        out AuthoredWayspotAnchorData serializedAnchor
      );
      
      anchor.GetDifferences(serializedAnchor, out var isBackingAnchorInvalid, out var isManifestInvalid);
      return (isBackingAnchorInvalid || isManifestInvalid);
    }
  }
}