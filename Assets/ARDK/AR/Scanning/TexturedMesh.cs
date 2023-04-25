// Copyright 2022 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  /// A triangle mesh and its texture. This is the result of processing a scan.
  public class TexturedMesh
  {
    public UnityEngine.Mesh mesh;
    public Texture2D texture;

    internal TexturedMesh(UnityEngine.Mesh mesh, Texture2D texture)
    {
      this.mesh = mesh;
      this.texture = texture;
    }
  }
}
