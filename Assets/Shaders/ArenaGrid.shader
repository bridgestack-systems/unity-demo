Shader "NexusArena/ArenaGrid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.05, 0.1, 1)
        _GridColor ("Grid Color", Color) = (0, 0.7, 1, 1)
        _GridScale ("Grid Scale", Float) = 10
        _GridThickness ("Grid Thickness", Range(0.01, 0.2)) = 0.05
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _EmissionStrength ("Emission Strength", Float) = 2.0
        _EdgeGlowWidth ("Edge Glow Width", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ArenaGrid"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GridColor;
                float _GridScale;
                float _GridThickness;
                float _PulseSpeed;
                float _EmissionStrength;
                float _EdgeGlowWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            float GridLine(float coord, float thickness)
            {
                float wrapped = frac(coord);
                float distToLine = min(wrapped, 1.0 - wrapped);
                return 1.0 - saturate(distToLine / thickness);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 gridUV = input.positionWS.xz * _GridScale;

                float gridX = GridLine(gridUV.x, _GridThickness);
                float gridY = GridLine(gridUV.y, _GridThickness);
                float grid = max(gridX, gridY);

                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float animatedGrid = grid * lerp(0.6, 1.0, pulse);

                float2 centeredUV = input.uv - 0.5;
                float distFromCenter = length(centeredUV) * 2.0;
                float edgeGlow = smoothstep(1.0 - _EdgeGlowWidth, 1.0, distFromCenter);

                half3 baseCol = _BaseColor.rgb;
                half3 gridCol = _GridColor.rgb * _EmissionStrength * animatedGrid;
                half3 edgeCol = _GridColor.rgb * _EmissionStrength * edgeGlow * (pulse * 0.3 + 0.7);

                half3 finalColor = baseCol + gridCol + edgeCol;
                float alpha = _BaseColor.a + animatedGrid * 0.5 + edgeGlow * 0.3;
                alpha = saturate(alpha);

                half4 color = half4(finalColor, alpha);
                color.rgb = MixFog(color.rgb, input.fogCoord);

                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
