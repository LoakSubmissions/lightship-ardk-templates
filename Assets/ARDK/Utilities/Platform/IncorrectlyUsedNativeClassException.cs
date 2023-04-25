// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  public sealed class IncorrectlyUsedNativeClassException
    : Exception
  {
    private const string IncorrectNativeClassMessage =
      "Native ARDK support is disabled for this platform.";

    public IncorrectlyUsedNativeClassException()
      : base(IncorrectNativeClassMessage)
    {
    }
  }
}
