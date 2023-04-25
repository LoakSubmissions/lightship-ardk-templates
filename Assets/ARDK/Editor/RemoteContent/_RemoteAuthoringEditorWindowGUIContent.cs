using Niantic.ARDK.AR.WayspotAnchors.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using RAStrings = Niantic.ARDK.AR.WayspotAnchors.Editor._RemoteAuthoringEditorWindowStrings;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal static class _RemoteAuthoringEditorWindowGUIContent
  {
    public static Texture OutLinkTexture
    {
      get
      {
        return EditorGUIUtility.FindTexture(FullIconPathName("OutLink"));
      }
    }
    
    private static Texture _locationTexture;
    public static Texture LocationTexture
    {
      get
      {
        return EditorGUIUtility.FindTexture(FullIconPathName("Location"));
      }
    }
    
    private static GUIContent _renameLocationContent = GUIContent.none;

    public static GUIContent RenameLocationContent
    {
      get
      {
        if (_renameLocationContent == GUIContent.none)
        {
          _renameLocationContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Rename")), RAStrings.RENAME_LOCATION_ICON_TOOLTIP);
        }

        return _renameLocationContent;
      }
    }
    
    private static GUIContent _trashLocationContent = GUIContent.none;

    public static GUIContent TrashLocationContent
    {
      get
      {
        if (_trashLocationContent == GUIContent.none)
        {
          _trashLocationContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Trash")), RAStrings.TRASH_LOCATION_ICON_TOOLTIP);
        }

        return _trashLocationContent;
      }
    }

    public static Texture ImportTexture
    {
      get
      {
        return EditorGUIUtility.FindTexture(FullIconPathName("Import"));
      }
    }

    private static GUIContent _header3wIcon = GUIContent.none;
    public static GUIContent Header3WIcon
    {
      get
      {
        if (_header3wIcon == GUIContent.none)
        {
          _header3wIcon =
            new GUIContent(RAStrings.DATA_MGMT_LABEL_TEXT, EditorGUIUtility.FindTexture(FullIconPathName("Map")));
        }

        return _header3wIcon;
      }
    }
        
    private static GUIContent _inspectButtonContent = GUIContent.none;
    public static GUIContent InspectButtonContent
    {
      get
      {
        if (_inspectButtonContent == GUIContent.none)
        {
          _inspectButtonContent =
            new GUIContent(RAStrings.INSPECT_MANIFEST_BTN_TEXT, RAStrings.INSPECT_MANIFEST_BTN_TOOLTIP);
        }

        return _inspectButtonContent;
      }
    }
    
    private static GUIContent _saveAnchorsButtonContent = GUIContent.none;
    public static GUIContent SaveAnchorsButtonContent
    {
      get
      {
        if (_saveAnchorsButtonContent == GUIContent.none)
          _saveAnchorsButtonContent = new GUIContent(RAStrings.SAVE_ANCHORS_BTN_TEXT);

        return _saveAnchorsButtonContent;
      }
    }
    
    private static GUIContent _header4wIcon = GUIContent.none;
    public static GUIContent Header4WIcon
    {
      get
      {
        if (_header4wIcon == GUIContent.none)
        {
          _header4wIcon =
            new GUIContent(RAStrings.CREATE_SECTION_LABEL, EditorGUIUtility.FindTexture(FullIconPathName("Anchor")));
        }

        return _header4wIcon;
      }
    }
    
    private static GUIContent _createAnchorButtonContent = GUIContent.none;
    public static GUIContent CreateAnchorButtonContent
    {
      get
      {
        if (_createAnchorButtonContent == GUIContent.none)
        {
          _createAnchorButtonContent =
            new GUIContent(RAStrings.CREATE_ANCHOR_LABEL_TEXT, EditorGUIUtility.FindTexture(FullIconPathName("Anchor")));
        }

        return _createAnchorButtonContent;
      }
    }
    
    private static GUIContent _renameAnchorContent = GUIContent.none;
    public static GUIContent RenameAnchorContent
    {
      get
      {
        if (_renameAnchorContent == GUIContent.none)
        {
          _renameAnchorContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Rename")), RAStrings.RENAME_ANCHOR_ICON_TOOLTIP);
        }

        return _renameAnchorContent;
      }
    }
    
    private static GUIContent _trashAnchorContent = GUIContent.none;
    public static GUIContent TrashAnchorContent
    {
      get
      {
        if (_trashAnchorContent == GUIContent.none)
        {
          _trashAnchorContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Trash")), RAStrings.TRASH_ANCHOR_ICON_TOOLTIP);
        }

        return _trashAnchorContent;
      }
    }
    
    private static GUIContent _saveSelectedContent = GUIContent.none;
    public static GUIContent SaveSelectedContent
    {
      get
      {
        if (_saveSelectedContent == GUIContent.none)
        {
          _saveSelectedContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Save")), RAStrings.SAVE_ANCHOR_TOOLTIP);
        }

        return _saveSelectedContent;
      }
    }

    private static GUIContent _discardChangesSelectedContent = GUIContent.none;
    public static GUIContent DiscardChangesSelectedContent
    {
      get
      {
        if (_discardChangesSelectedContent == GUIContent.none)
        {
          _discardChangesSelectedContent =
            new GUIContent(EditorGUIUtility.FindTexture(FullIconPathName("Undo")), RAStrings.UNDO_ANCHOR_CHANGES_TOOLTIP);
        }

        return _discardChangesSelectedContent;
      }
    }
    
    public static Texture SelectedAnchorTexture
    {
      get
      {
        return EditorGUIUtility.FindTexture(FullIconPathName("SelectedAnchor"));
      }
    }

    private static string FullIconPathName(string nm)
    {
      return _RemoteAuthoringEditorWindowStrings.ICON_PATH + nm + _RemoteAuthoringEditorWindowStrings.ICON_EXT;
    }
  }
}