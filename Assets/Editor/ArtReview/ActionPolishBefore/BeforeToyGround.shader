Shader "Hidden/ArtReview/BeforeToyGround"
{
    Properties
    {
        _BaseColor("Sage clearing",Color)=(.58,.71,.55,1)
        _LightPatchColor("Light meadow",Color)=(.70,.79,.55,1)
        _OlivePatchColor("Olive meadow",Color)=(.64,.74,.50,1)
        _PatchScale("Broad curved patch scale",Float)=.22
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _LightPatchColor;
                half4 _OlivePatchColor;
                float _PatchScale;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
            V Vert(A v){V o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;}
            half4 Frag(V i):SV_Target
            {
                // Low-frequency contour bands form large clean colour regions. There
                // is no high-frequency noise, mottling, or baked scenery shadow layer.
                float2 p=i.positionWS.xz*_PatchScale;
                float contour=sin(p.x+1.12*sin(p.y*.68))+.58*cos(p.y*1.13+.48*sin(p.x*.71));
                float edge=max(.012,fwidth(contour)*.8);
                float meadow=smoothstep(.10-edge,.10+edge,contour);
                float olive=smoothstep(1.02-edge,1.02+edge,contour);
                half3 albedo=lerp(_BaseColor.rgb,_LightPatchColor.rgb,meadow);
                albedo=lerp(albedo,_OlivePatchColor.rgb,olive);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 shadowTint=lerp(half3(.72h,.79h,.79h),half3(1,1,1),sun.shadowAttenuation);
                return half4(albedo*shadowTint,1);
            }
            ENDHLSL
        }
    }
}
