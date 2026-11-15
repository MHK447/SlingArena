Shader "Treeplla/SpriteOutlineColored"
{
    Properties{
    _MainTex("MainTex",2D) = "white"{}
    _OutlineWidth("Outline Size", Float) = 0.01
    _OutlineColor("Outline Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _OutlineWidth;
            fixed4 _OutlineColor;
            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_Target
            {
                //OUTLINE
                float outlineAlpha = 0.0;
                float angle = 0.0;
                for( int k = 0; k < 32; ++k ){
                    angle += 1.0/(float(32)/2.0) * 3.14159265359;
                    float2 testPoint = i.uv + float2( _OutlineWidth * cos(angle), _OutlineWidth * sin(angle) );
                    float sampledAlpha = tex2D( _MainTex, testPoint ).a;
                    outlineAlpha = max( outlineAlpha, sampledAlpha );
                }
                fixed4 fragColor = lerp( float4(0.0,0.0,0.0,0.0), _OutlineColor, outlineAlpha );

                //TEXTURE (keeps original sprite color, outline drawn behind)
                fixed4 tex0 = tex2D( _MainTex, i.uv ) * i.color;
                fixed4 result = lerp( fragColor, tex0, tex0.a );
                return result;
            }
            ENDCG
        }
    }
}
