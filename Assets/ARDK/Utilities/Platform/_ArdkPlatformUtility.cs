using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.Utilities.Logging;
using UnityEditor;
using UnityEngine;

namespace Niantic.ARDK.Utilities
{
  internal static class _ArdkPlatformUtility
  {
    // only caching this value because remote needs it on a per frame basis
    public static readonly bool AreNativeBinariesAvailable;

    private const string DIALOG_TITLE = "Niantic Lightship";
    const string SAW_PLATFORM_DIALOG_KEY = "SawPlatformSupportDialog";
    
    static _ArdkPlatformUtility()
    {
      AreNativeBinariesAvailable = _IsNativeSupportEnabled();
    }

    private static bool _IsNativeSupportEnabled()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
      // For IOS and Android, we always use the native implementation.
      ARLog._Debug("Native support is enabled for this platform");
      return true;

#else
      var sawDialog = PlayerPrefs.GetInt(SAW_PLATFORM_DIALOG_KEY, 0) == 1;
      
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        if (!_IsOperatingSystemBigSurAndAbove())
        {
          if (sawDialog)
            return false;
          
          // Macbooks which are Catalina and below do not have native support
          const string bigSurMessage =
            "Native ARDK support is unavailable running the Unity Editor on macOS Catalina and older. " +
            "Please upgrade your operating system to macOS Big Sur or newer.";
        
          EditorUtility.DisplayDialog(DIALOG_TITLE, bigSurMessage, "OK");
          PlayerPrefs.SetInt(SAW_PLATFORM_DIALOG_KEY, 1);
          return false;
        }
       
        if (IsUsingRosetta())
        {
          if (sawDialog)
            return false;
          
          // On Apple Silicon, don't use the Rosetta version of Unity but download Apple Silicon version
          const string siliconMessage =
            "For the best ARDK experience we recommend using Native Unity version on Apple silicon devices";
        
          EditorUtility.DisplayDialog(DIALOG_TITLE, siliconMessage, "OK");
          PlayerPrefs.SetInt(SAW_PLATFORM_DIALOG_KEY, 1);
          return false;
        }
        
        ARLog._Debug("Native ARDK support is enabled for this platform");
        return true;
      }

      if (sawDialog)
        return false;
      
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        const string windowsMessage = 
          "Native ARDK support is unavailable running Unity Editor on Windows. " + 
          "Development is supported but certain features will be disabled.";
          
        var openLink = EditorUtility.DisplayDialog(DIALOG_TITLE, windowsMessage, "Learn More", "OK");
        if (openLink)
          Application.OpenURL("https://niantic.dev/docs/ardk/ardk_fundamentals/system_reqs.html#developing-on-windows");
      }
      else
      {
        const string otherPlatformMessage = 
          "Native ARDK support is unavailable running Unity Editor on this platform. " + 
          "Development may be possible but certain features will be disabled.";
          
        var openLink = EditorUtility.DisplayDialog(DIALOG_TITLE, otherPlatformMessage, "Learn More", "OK");
        if (openLink)
          Application.OpenURL("https://niantic.dev/docs/ardk/ardk_fundamentals/system_reqs.html");
      }

      PlayerPrefs.SetInt(SAW_PLATFORM_DIALOG_KEY, 1);
      return false;
#endif
    }

    private static bool _IsOperatingSystemBigSurAndAbove()
    {
      // https://en.wikipedia.org/wiki/Darwin_%28operating_system%29#Release_history
      // 20.0.0 Darwin is the first version of BigSur
      return Environment.OSVersion.Version >= new Version(20, 0, 0);
    }

    public static bool IsUsingRosetta()
    {
      return
        _IsAppleSiliconProcessor() &&
        RuntimeInformation.ProcessArchitecture == Architecture.X64;
    }

    private static bool _IsAppleSiliconProcessor()
    {
      /*
       * https://developer.apple.com/documentation/apple-silicon/about-the-rosetta-translation-environment
       * From sysctl.proc_translated,
       * Intel/iPhone => -1
       * M1 => 0
       */
      int _;
      var size = (IntPtr)4;
      var param = "sysctl.proc_translated";
      var result = sysctlbyname(param, out _, ref size, IntPtr.Zero, (IntPtr)0);

      return result >= 0;
    }

    [DllImport("libSystem.dylib")]
    private static extern int sysctlbyname ([MarshalAs(UnmanagedType.LPStr)]string name, out int int_val, ref IntPtr length, IntPtr newp, IntPtr newlen);
  }
}
