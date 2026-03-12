Shader "NexusArena/Hologram"
{
    Properties
    {
        _HoloColor ("Hologram Color", Color) = (0, 0.8, 1, 1)
        _ScanLineSpeed ("Scan Line Speed", Float) = 3.0
        _ScanLineDensity ("Scan Line Density", Float) = 80.0
        _ScanLineIntensity ("Scan Line Intensity", Range(0, 1)) = 0.3
        _FlickerSpeed ("Flicker Speed", Float) = 5.0
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.1
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.7
        _GlitchSpeed ("Glitch Speed", Float) = 2.0
        _GlitchIntensity ("Glitch Intensity", Range(0, 0.1)) = 0.02
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
        Cull Off

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _HoloColor;
                float _ScanLineSpeed;
                float _ScanLineDensity;
                float _ScanLineIntensity;
                float _FlickerSpeed;
                float _FlickerIntensity;
                float _FresnelPower;
                float _FresnelIntensity;
                float _Alpha;
                float _GlitchSpeed;
                float _GlitchIntensity;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float Noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(Hash(i), Hash(i + 1.0), f);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;

                // Glitch vertex displacement
                float glitchTime = floor(_Time.y * _GlitchSpeed * 10.0);
                float glitchStrength = step(0.93, Hash(glitchTime));
                float glitchOffset = (Hash(glitchTime + posOS.y * 100.0) - 0.5) * 2.0;
                posOS.x += glitchOffset * _GlitchIntensity * glitchStrength;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Scan lines
                float scanLineY = input.positionWS.y * _ScanLineDensity + _Time.y * _ScanLineSpeed;
                float scanLine = sin(scanLineY * PI) * 0.5 + 0.5;
                scanLine = lerp(1.0, scanLine, _ScanLineIntensity);

                // Fresnel edge glow
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _FresnelIntensity;

                // Flicker
                float flickerBase = Noise(_Time.y * _FlickerSpeed);
                float flickerHard = step(0.97, Hash(floor(_Time.y * _FlickerSpeed * 3.0)));
                float flicker = 1.0 - (_FlickerIntensity * (1.0 - flickerBase)) - (flickerHard * 0.3);
                flicker = saturate(flicker);

                // Glitch color bands
                float glitchTime = floor(_Time.y * _GlitchSpeed * 10.0);
                float glitchActive = step(0.93, Hash(glitchTime));
                float bandY = floor(input.positionWS.y * 20.0);
                float bandShift = Hash(bandY + glitchTime) * glitchActive;

                half3 color = _HoloColor.rgb;
                // Slight color shift during glitch
                color.r += bandShift * 0.2;
                color.b -= bandShift * 0.1;

                // Combine
                half3 finalColor = color * scanLine * flicker;
                finalColor += color * fresnel;

                float alpha = _Alpha * scanLine * flicker;
                alpha += fresnel * 0.5;
                alpha = saturate(alpha);

                half4 output = half4(finalColor, alpha);
                output.rgb = MixFog(output.rgb, input.fogCoord);

                return output;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
