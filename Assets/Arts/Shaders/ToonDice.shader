Shader "Custom/ToonDice"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Texture", 2D) = "white" {}

        [Header(Toon Shading)]
        _ShadeColor ("Shade Color", Color) = (0.65, 0.6, 0.7, 1)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.1
        _ShadeSmooth ("Shade Smoothness", Range(0.001, 0.5)) = 0.05
        _LightDir ("Light Direction (World)", Vector) = (0.3, 0.8, -0.5, 0)

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // Main pass - Toon (cel shaded).
        // No LightMode tag -> SRPDefaultUnlit, so it renders under BOTH the URP 2D Renderer
        // and the 3D Forward Renderer. Lighting is faked from a material light direction
        // so it does not depend on scene lights (the 2D Renderer provides none).
        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _ShadeColor;
                float _ShadeThreshold;
                float _ShadeSmooth;
                float4 _LightDir;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;

                float3 normalWS = normalize(input.normalWS);
                float3 lightDir = normalize(_LightDir.xyz);
                float NdotL = dot(normalWS, lightDir);

                // Cel-shaded step: soft band between shade and lit regions
                float lit = smoothstep(_ShadeThreshold - _ShadeSmooth,
                                       _ShadeThreshold + _ShadeSmooth,
                                       NdotL);

                // Blend between tinted shade and full albedo by the cel band
                half3 shaded = albedo * _ShadeColor.rgb;
                half3 col = lerp(shaded, albedo, lit);

                // Soft rim light for a friendly toon highlight
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(saturate(1.0 - saturate(dot(normalWS, viewDir))), _RimPower);
                rim *= saturate(lit + 0.25) * _RimStrength;
                col += _RimColor.rgb * rim;

                return half4(col, tex.a * _BaseColor.a);
            }
            ENDHLSL
        }

        // Shadow Caster pass - enables shadow casting
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
