// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using UnityEngine;

namespace Niantic.ARDK.AR.Scanning
{
  internal sealed class _MockVoxelBuffer: IVoxelBuffer
  {
    private List<Vector4> _positions;
    private List<Color> _colors;
    
    internal _MockVoxelBuffer(List<Vector4> positions, List<Color> colors)
    {
      this._positions = positions;
      this._colors = colors;
    }
    
    public List<Vector4> GetPositions()
    {
      return this._positions;
    }

    public List<Color> GetColors()
    {
      return this._colors;
    }
  }
}
