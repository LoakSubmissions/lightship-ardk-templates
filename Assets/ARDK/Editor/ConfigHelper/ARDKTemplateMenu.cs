// Copyright 2022 Niantic, Inc. All Rights Reserved.

using UnityEngine;
using UnityEditor;

#if (UNITY_EDITOR)
namespace Niantic.ARDK.ConfigHelper.Editor
{
  public class ARDKTemplatesMenu
  {

    /// Helper window.
    [MenuItem("Lightship/ARDK/Configuration Helper Window", false, 1)]
    private static void OpenHelperWindow()
    {
      ARDKHelperWindow.ShowHelperWindow();
    }
  }
}
#endif
