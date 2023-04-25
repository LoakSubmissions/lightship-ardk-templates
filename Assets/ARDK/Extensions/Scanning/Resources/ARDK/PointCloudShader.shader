Shader "Custom/PointCloud"

// A static point cloud shader to visualize in-progress scans. Detected voxels will be drawn as points in 3D space.

{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 50
        _Progress("Unused", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            float4x4 _Transform;
            half _PointSize;
            StructuredBuffer<float4> _PositionBuffer;

            // Allowing an alpha channel, just in case.
            StructuredBuffer<float4> _ColorBuffer;

            struct PointOut
            {
                float4 position : SV_Position;
                half3 color : COLOR;
                float size : PSIZE;

            };

            PointOut Vertex(uint vertexId : SV_VertexID) {
                float4 pt = _PositionBuffer[vertexId];
                PointOut po;
                po.position = UnityObjectToClipPos(mul(_Transform, float4(pt.xyz, 1)));
                po.color =  half3(_ColorBuffer[vertexId].xyz);
                const float distance_to_camera = distance(pt.xyz, _WorldSpaceCameraPos);

                po.size = _PointSize / distance_to_camera;
                return po;
            }


             half4 Fragment(PointOut input) : SV_Target {
                 return half4(input.color, 1);
             }
            ENDCG
        }
    } 
    FallBack "Diffuse"
}
