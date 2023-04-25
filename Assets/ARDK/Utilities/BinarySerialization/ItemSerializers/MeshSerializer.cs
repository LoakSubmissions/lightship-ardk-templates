// Copyright 2022 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Extensions;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers
{
  public sealed class MeshSerializer:
    BaseItemSerializer<UnityEngine.Mesh>
  {
    public static readonly MeshSerializer Instance = new MeshSerializer();
    
    // Later versions might write normals, blendshapes, uv sets, etc. as needed.
    private static readonly UInt16 version = 1;
    private MeshSerializer()
    {
    }

    protected override void DoSerialize(BinarySerializer serializer, UnityEngine.Mesh item)
    {
      UInt16Serializer.Instance.Serialize(serializer, version);
      ArraySerializer<Vector3>.Instance.Serialize(serializer, item.vertices);
      ArraySerializer<Int32>.Instance.Serialize(serializer, item.triangles);
      ArraySerializer<Vector2>.Instance.Serialize(serializer, item.uv);
    }
    protected override UnityEngine.Mesh DoDeserialize(BinaryDeserializer deserializer)
    {
      UInt16 deserializationVersion = UInt16Serializer.Instance.Deserialize(deserializer);
      var vertex = ArraySerializer<Vector3>.Instance.Deserialize(deserializer);
      var triangles = ArraySerializer<Int32>.Instance.Deserialize(deserializer);
      var uvs = ArraySerializer<Vector2>.Instance.Deserialize(deserializer);
      Mesh mesh = new Mesh();
      mesh.indexFormat = vertex.Length >= 65536? IndexFormat.UInt32 : IndexFormat.UInt16;
      mesh.SetVertices(vertex);
      mesh.SetTriangles(triangles, 0);
      mesh.SetUVs(0, uvs);
      return mesh;
    }
  }
}
