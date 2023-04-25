// Copyright 2023 Niantic, Inc. All Rights Reserved.

using UnityEngine;


namespace Niantic.ARDK.AR.Scanning
{
  /// Factory used to create instances of <see cref="IScanner"/>.
  public sealed class ScannerFactory
  {

    /// Creates a new <see cref="IScanner"/> instance storing data in the Application's persistent data directory.
    /// @param session the session in which to create the scanner
    public static IScanner Create(IARSession session)
    {
      return Create(session, Application.persistentDataPath);
    }

    /// Creates a new <see cref="IScanner"/> instance storing data in a directory supplied as an argument.
    /// @param session the session in which to create the scanner
    /// @param scanDataPath directory on disk where scans should be stored.
    public static IScanner Create(IARSession session, string scanDataPath)
    {
      if (session.RuntimeEnvironment == RuntimeEnvironment.Mock)
      {
        return new _MockScanner(scanDataPath);
      }
      else
      {
#if UNITY_EDITOR
        return new _MockScanner(scanDataPath);
#else
        return new _NativeScanner(session, scanDataPath);
#endif
      }
    }
  }
}
