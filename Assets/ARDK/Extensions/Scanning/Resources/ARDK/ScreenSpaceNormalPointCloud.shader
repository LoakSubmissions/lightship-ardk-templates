Shader "Custom/ScreenSpacePointCloud"

// Scan visualization that draws points and their normals based on raycast data on screen space.

{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 0.05
        _Progress("Progress", Float) = 1
        
        _Width("Width", int) = 256
        _Height("Height", int) = 256
        _PositionTex("Position", 2D) = "white"
        _ColorTex("Color", 2D) = "white"
        _NormalTex("Normal", 2D) = "white"
        
        _TrianglesPerDisk("TrianglesPerDisk", int) = 10
        _NormalCalcOffset("NormalCalcOffset", int) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull off
        Pass
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            float4x4 _Transform;
            half _PointSize;
            float _Progress;
            int _Width;
            int _Height;
            int _TrianglesPerDisk;
            int _NormalCalcOffset;
            float4 _Color;
            
            Texture2D<half4> _PositionTex;
            Texture2D<fixed4> _ColorTex;
            Texture2D<fixed4> _NormalTex;

            struct PointOut
            {
                float4 position : SV_Position;
                float4 color : TEXCOORD0;
            };

            PointOut Vertex(uint vertexId : SV_VertexID) {
                // half4 pt = _PositionBuffer[vertexId];

                uint diskVertexId = vertexId % (_TrianglesPerDisk * 3);
                uint diskId = vertexId / (_TrianglesPerDisk * 3);

                // integer math to make sure every point inside a disk has the same coordinate.
                uint pixelId = diskId * _TrianglesPerDisk;
                
                uint diskSliceId = diskVertexId / 3;
                uint triangleId = diskVertexId % 3;
                
                uint x = uint(pixelId % _Width);
                uint y = uint(pixelId / _Width);
                half4 pt = _PositionTex[uint2(x, y)];

                if (pt.x == 0 && pt.y == 0 && pt.z == 0) {
                    PointOut po;
                    po.position = float4(0,0,0,0);
                    po.color = float4(0,0,0,0);
                    return po;
                }
                pt = half4(pt.x, -pt.y, pt.z, 1);
                
                half3 normal = (_NormalTex[uint2(x, y)] - 0.5) * 2;
                half3 up = half3(0, -normal.z, normal.y); 
                half3 right = cross(normal.xyz, up);

                if (triangleId == 2) {
                    diskSliceId += 1;
                }

                float distanceToCamera = distance(pt.xyz, _WorldSpaceCameraPos);
                const float animated_size = diskId % 2 == 0 ? _Progress : 1.6 - _Progress;

                
                if (triangleId != 0) {
                    float angle = 2 * UNITY_PI / _TrianglesPerDisk * diskSliceId;
                    float xOffset;
                    float yOffset;
                    sincos(angle, xOffset, yOffset);
                    pt.xyz = pt.xyz + normalize(up) * 0.008 * xOffset * distanceToCamera * animated_size * _PointSize;
                    pt.xyz = pt.xyz + normalize(right) * 0.008 * yOffset * distanceToCamera * animated_size * _PointSize;
                }

                PointOut po;
                po.position = UnityObjectToClipPos(mul(_Transform, float4(pt.xyz, 1)));
                po.color = float4(_ColorTex[uint2(x, y)].xyz, 1) * _Color;
                return po;
            }
            
             half4 Fragment(PointOut input) : SV_Target {
                 return input.color;
             }
            
            ENDCG
        }
    } 
    FallBack "Diffuse"
}
