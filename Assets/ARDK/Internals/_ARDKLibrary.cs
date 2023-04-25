// Copyright 2022 Niantic, Inc. All Rights Reserved.
namespace Niantic.ARDK.Internals
{
  internal static class _ARDKLibrary
  {
#if UNITY_IOS && !UNITY_EDITOR
    internal const string libraryName = "__Internal";
#elif (UNITY_EDITOR && !IN_ROSETTA) || UNITY_STANDALONE_OSX || UNITY_ANDROID
    internal const string libraryName = "ARDK";
#else
    internal const string libraryName = "PLATFORM_NOT_SUPPORTED";
#endif
  }
}
