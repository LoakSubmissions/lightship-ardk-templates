// Copyright 2022 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Protobuf;
using Niantic.ARDK.Configuration.Authentication;

using Niantic.ARDK.Configuration;
using Niantic.ARDK.Configuration.Internal;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Telemetry;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Niantic.ARDK.Internals
{
  /// Controls the startup systems for ARDK.
  public static class StartupSystems
  {
    // Add a destructor to this class to try and catch editor reloads
    private static readonly _Destructor _ = new _Destructor();

    // The pointer to the C++ NarSystemBase handling functionality at the native level
    private static IntPtr _nativeHandle = IntPtr.Zero;
    
    private static _TelemetryService _telemetryService;

    private static bool _alreadyStarted;

#if UNITY_EDITOR_OSX
    [InitializeOnLoadMethod]
    private static void EditorStartup()
    {
#if !REQUIRE_MANUAL_STARTUP
      _StandardStartup();
#endif
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Startup()
    {
      if (_alreadyStarted)
        return;
      
#if !REQUIRE_MANUAL_STARTUP
      _StandardStartup();
#endif
    }
    
    /// <summary>
    /// Allows users to Manually startup and refresh the underlying implementation when required.
    /// Used by internal teams. DO NOT MAKE PRIVATE
    /// </summary>
    public static void ManualStartup()
    {
      _StandardStartup();
    }

    private static void _StandardStartup()
    {
      if (_alreadyStarted)
        return;
      
      _InitializeTelemetry();
      _InitializeNativeLibraries();
      _SetCSharpInitializationMetadata();
      _alreadyStarted = true;
    }

    /// <summary>
    /// Starts up the ARDK startup systems if they haven't been started yet.
    /// </summary>
    private static void _InitializeNativeLibraries()
    {
#if (AR_NATIVE_SUPPORT || UNITY_EDITOR_OSX)
      try
      {
        // TODO(sxian): Remove the _ROR_CREATE_STARTUP_SYSTEMS() after moving the functionalities to
        // NARSystemBase class.
        // Note, don't put any code before calling _NARSystemBase_Initialize() below, since Narwhal C++
        // _NARSystemBase_Initialize() should be the first API to be called before other components are initialized.
        _ROR_CREATE_STARTUP_SYSTEMS();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to create ARDK startup systems: {0}", false, e);
      }

      if (_nativeHandle == IntPtr.Zero) {
        _nativeHandle = _InitialiseNarBaseSystemBasedOnOS();
        _CallbackQueue.ApplicationWillQuit += OnApplicationQuit;
      } else {
        ARLog._Error("_nativeHandle is not null, _InitializeNativeLibraries is called twice");
      }
      
#endif
    }

    private static void _SetCSharpInitializationMetadata()
    {
      // The initialization of C# components should happen below.
      _SetAuthenticationParameters();
      SetDeviceMetadata();
    }

    private static void OnApplicationQuit()
    {
      if (_nativeHandle != IntPtr.Zero)
      {
        _NARSystemBase_Release(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
      }
    }

    private const string AUTH_DOCS_MSG = "For more information, visit the niantic.dev/docs/authentication.html site.";

    internal static void _SetAuthenticationParameters()
    {
      // We always try to find an api key
      var apiKey = string.Empty;
      var authConfigs = Resources.LoadAll<ArdkAuthConfig>("ARDK/ArdkAuthConfig");

      if (authConfigs.Length > 1)
      {
        var errorMessage = "There are multiple ArdkAuthConfigs in Resources/ARDK/ " +
                           "directories, loading the first API key found. Remove extra" +
                           " ArdkAuthConfigs to prevent API key problems. " + AUTH_DOCS_MSG;
        ARLog._Error(errorMessage);
      }
      else if (authConfigs.Length == 0)
      {
        ARLog._Error
        ($"Could not load an ArdkAuthConfig, please add one in a Resources/ARDK/ directory. {AUTH_DOCS_MSG}");
      }
      else
      {
        var authConfig = authConfigs[0];
        apiKey = authConfig.ApiKey;

        if (!string.IsNullOrEmpty(apiKey))
          ArdkGlobalConfig.SetApiKey(apiKey);
      }

      authConfigs = null;
      Resources.UnloadUnusedAssets();

      //Only continue if needed
      if (!ServerConfiguration.AuthRequired)
        return;

      if (string.IsNullOrEmpty(ServerConfiguration.ApiKey))
      {

        if (!string.IsNullOrEmpty(apiKey))
        {
          ServerConfiguration.ApiKey = apiKey;
        }
        else
        {
          ARLog._Error($"No API Key was found. Add it to an ArdkAuthConfig asset. {AUTH_DOCS_MSG}");
        }
      }

#if UNITY_EDITOR
      if (!string.IsNullOrEmpty(apiKey))
      {
        var authResult = ArdkGlobalConfig._VerifyApiKeyWithFeature("feature:unity_editor", isAsync: false);
        if(authResult == NetworkingErrorCode.Ok)
          ARLog._Debug("Successfully authenticated ARDK Api Key");
        else
        {
          ARLog._Error($"Attempted to authenticate ARDK Api Key, but got error: {authResult}");
        }
      }
#endif

      if (!string.IsNullOrEmpty(apiKey))
        ArdkGlobalConfig._VerifyApiKeyWithFeature(GetInstallMode(), isAsync: true);
    }

    private static string GetInstallMode()
    {
      return $"install_mode:{Application.installMode.ToString()}";
    }

    private static void SetDeviceMetadata()
    {
      ArdkGlobalConfig._Internal.SetApplicationId(Application.identifier);
      ArdkGlobalConfig._Internal.SetArdkInstanceId(_ArdkMetadataConfigExtension._CreateFormattedGuid());
    }

    private static IntPtr _InitialiseNarBaseSystemBasedOnOS()
    {
      if (_ArdkPlatformUtility.AreNativeBinariesAvailable)
      {
        return _NARSystemBase_Initialize(_TelemetryService._OnNativeRecordTelemetry);
      }

      return IntPtr.Zero;
    }
    
    private static void _InitializeTelemetry()
    {
      _telemetryService = _TelemetryService.Instance;
      _telemetryService.Start(Application.persistentDataPath);

      _TelemetryHelper.Start();
    }

    private sealed class _Destructor
    {
      // TODO: Inject telemetry service in ctor and do a Flush() in dispose once Flush() is exposed to us in the library
      ~_Destructor()
      {
        _telemetryService.Stop();
        OnApplicationQuit();
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _ROR_CREATE_STARTUP_SYSTEMS();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARSystemBase_Initialize(_TelemetryService._ARDKTelemetry_Callback callback);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARSystemBase_Release(IntPtr nativeHandle);
  }
}