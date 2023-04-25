Shader "Custom/PulsePointCloud"

// A point cloud visualization for in-progress scans that is animated,
// with the points changing sizes based on their distance to the camera.

// Set the progress variable to animate the pulse.

{

    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 0.05
        _MaxDistance("Max Distance", Float) = 3
        _Progress("Progress", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ ENABLE_COMPUTE_BUFFERS

            #include "UnityCG.cginc"

            float4x4 _Transform;
            half _PointSize;
            float _Progress;
            float _MaxDistance;
            float4 _Color;

            #if ENABLE_COMPUTE_BUFFERS
                StructuredBuffer<float4> _PositionBuffer;

                // Allowing an alpha channel, just in case.
                StructuredBuffer<float4> _ColorBuffer;
            #else
                Texture2D<float4> _PositionBuffer;
                Texture2D<float4> _ColorBuffer;
            #endif

            struct PointOut
            {
                float4 position : SV_Position;
                half3 color : COLOR;
                float size : PSIZE;

            };

            PointOut Vertex(uint vertexId : SV_VertexID) {
                PointOut po;
                float4 pt = 0;
                #if ENABLE_COMPUTE_BUFFERS
                    pt = _PositionBuffer[vertexId];
                    po.position = UnityObjectToClipPos(mul(_Transform, float4(pt.xyz, 1)));
                    po.color =  half3(_ColorBuffer[vertexId].xyz);
                #else
                    int w, h;
                    _PositionBuffer.GetDimensions(w, h);
                    if (vertexId < w * h)
                    {
                        int x = vertexId / w;
                        int y = vertexId - x * w;
                        uint2 coord = uint2(y, x);
                        pt = _PositionBuffer[coord];
                        po.position = UnityObjectToClipPos(mul(_Transform, float4(pt.xyz, 1)));
                        po.color =  half3(_ColorBuffer[coord].xyz);
                    }
                #endif
                const float scaled_progress = _Progress * _MaxDistance;
                const float distance_to_camera = distance(pt.xyz, _WorldSpaceCameraPos);
                const float distance_factor = max(0, 1 - (abs(distance_to_camera - scaled_progress) * 3));
                po.size = _PointSize * distance_factor / distance_to_camera;
                return po;
            }


             half4 Fragment(PointOut input) : SV_Target {
                 return half4(input.color * _Color, 1);
             }
            ENDCG
        }
    } 
    FallBack "Diffuse"
}
