Shader "Custom/DiceOutline"
{
    Properties
    {
        _BaseColor ("Outline Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "DiceOutline"
            Tags { "LightMode"="UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest Always
            // alpha blend: 뒤 배경색에 영향받지 않도록 색을 그대로 덮어씀.
            // (additive Blend SrcAlpha One은 배경 + outline이라 조명 받은 다이스 본체와 합쳐져 노랑으로 보였음)
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 32
                ReadMask 32
                WriteMask 32
                Comp NotEqual
                Pass Keep
                Fail Keep
            }

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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
