using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
  internal static class _RemoteAuthoringEditorWindowStyles
  {
    private static Texture2D _backgroundTexture;

    public static Texture2D BackgroundTexture
    {
      get
      {
        if (!_backgroundTexture)
        {
          _backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
          _backgroundTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f));
          _backgroundTexture.Apply();
        }

        return _backgroundTexture;
      }
    }

    public static GUIStyle HeaderSectionStyle
    {
      get
      {
        if (_headerSectionStyle == null)
        {
          _headerSectionStyle = new GUIStyle(EditorStyles.toolbarButton)
          {
            fixedHeight = 140
          };
        }

        return _headerSectionStyle;
      }
    }

    private static GUIStyle _headerSectionStyle;

    public static GUIStyle HeaderTextStyle
    {
      get
      {
        if (_headerTextStyle == null)
        {
          _headerTextStyle = new GUIStyle(EditorStyles.label)
          {
            font = EditorStyles.boldFont,
            fontSize = 18,
            margin = new RectOffset(0, 0, 20, 10),
            padding = new RectOffset(7, 0, 0, 0)
          };
        }

        return _headerTextStyle;
      }
    }

    private static GUIStyle _headerTextStyle;

    public static GUIStyle HeaderSubTextStyle
    {
      get
      {
        if (_headerSubTextStyle == null)
        {
          _headerSubTextStyle = new GUIStyle(EditorStyles.label)
          {
            font = EditorStyles.boldFont,
            fontSize = 18,
            margin = new RectOffset(0, 0, 20, 10),
            padding = new RectOffset(7, 0, 0, 0)
          };
        }

        return _headerSubTextStyle;
      }
    }

    private static GUIStyle _headerSubTextStyle;

    public static GUIStyle SmallSectionStyle
    {
      get
      {
        _smallSectionStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
          fixedHeight = 160
        };
        return _smallSectionStyle;
      }
    }

    private static GUIStyle _smallSectionStyle;

    public static GUIStyle AnchorSelectButtonStyle
    {
      get
      {
        if (_anchorSelectButtonStyle == null)
        {
          _anchorSelectButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
          {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 30f,
            fontSize = 14,
            padding = new RectOffset(5, 0, 5, 5),
            margin = new RectOffset(0, 0, 0, 0),
            richText = true
          };
        }

        return _anchorSelectButtonStyle;
      }
    }

    private static GUIStyle _anchorSelectButtonStyle;

    public static GUIStyle LinkLabelStyle
    {
      get
      {
        if (_linkLabelStyle == null)
        {
          _linkLabelStyle = new GUIStyle(EditorStyles.linkLabel)
          {
            fontSize = 16
          };
        }

        return _linkLabelStyle;
      }
    }

    private static GUIStyle _linkLabelStyle;

    public static GUIStyle LabelStyle
    {
      get
      {
        if (_labelStyle == null)
        {
          _labelStyle = new GUIStyle(EditorStyles.label)
          {
            padding = new RectOffset(15, 0, 0, 0)
          };
        }

        return _labelStyle;
      }
    }

    private static GUIStyle _labelStyle;

    public static GUIStyle CenteredLabelStyle
    {
      get
      {
        if (_centeredLabelStyle == null)
        {
          _centeredLabelStyle = new GUIStyle(EditorStyles.label)
          {
            alignment = TextAnchor.LowerLeft
          };
        }

        return _centeredLabelStyle;
      }
    }

    private static GUIStyle _centeredLabelStyle;

    public static GUIStyle TextFieldStyle
    {
      get
      {
        if (_textFieldStyle == null)
        {
          _textFieldStyle = new GUIStyle(EditorStyles.textField)
          {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 30f,
            fontSize = 14,
            padding = new RectOffset(5, 0, 5, 5),
            margin = new RectOffset(0, 0, 0, 0)
          };
        }

        return _textFieldStyle;
      }
    }
    private static GUIStyle _textFieldStyle;  
    
    public static GUIStyle SmallTextFieldStyle
    {
      get
      {
        if (_smallTextFieldStyle == null)
        {
          _smallTextFieldStyle = new GUIStyle(EditorStyles.textField)
          {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 20f,
            fontSize = 14,
            margin = new RectOffset(0, 0, 0, 0)
          };
        }
        return _smallTextFieldStyle;
      }
    }
    private static GUIStyle _smallTextFieldStyle;  


    public static GUIStyle SelectedAnchorButtonStyle
    {
      get
      {
        if (_selectedAnchorButtonStyle == null)
        {
          _selectedAnchorButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
          {
            margin = new RectOffset(0, 0, 10, 0),
            fixedWidth = 30,
            fixedHeight = 30
          };
        }

        return _selectedAnchorButtonStyle;
      }
    }
    private static GUIStyle _selectedAnchorButtonStyle;

    public static GUIStyle CenteredWhiteLabel
    {
      get
      {
        if (_centeredWhiteLabel == null)
        {
          _centeredWhiteLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
          {
            fontSize = 12,
          };
        }
        return _centeredWhiteLabel;
      }
    }
    private static GUIStyle _centeredWhiteLabel;
    
    public static readonly Color LinkBlue = new Color(120f / 255f, 161f / 255f, 224f / 255f, 1f);
  }

}
