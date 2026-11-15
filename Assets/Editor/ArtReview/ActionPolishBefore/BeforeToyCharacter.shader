Shader "Hidden/ArtReview/BeforeToyCharacter"
{
    Properties
    {
        _BaseColor ("Base tint", Color) = (1,1,1,1)
        _GammaCorrectLighting ("Gamma-correct clay lighting", Range(0,1)) = 1
        _TeamColor ("Garment allegiance", Color) = (.4667,.3412,.8392,1)
        _TeamBlend ("Recolor cloth", Range(0,1)) = 0
        _Animated ("Enable all motion", Float) = 1
        _Motion ("Moving", Float) = 0
        _Phase ("Individual phase", Float) = 0
        _Attack ("Attack progress", Float) = 0
        _Hit ("Hit progress", Float) = 0
        _Death ("Death progress", Float) = 0
        _Archetype ("Zombie / skeleton / archer / caster / leader", Float) = 0
        _Summon ("Summon progress", Float) = 0
        _GroundHeight ("Emergence ground height", Float) = 0
        _VisualTime ("Battle animation clock", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _GammaCorrectLighting;
            float _Animated;
            float _GroundHeight;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(PerFigure)
            UNITY_DEFINE_INSTANCED_PROP(float4, _TeamColor)
            UNITY_DEFINE_INSTANCED_PROP(float, _TeamBlend)
            UNITY_DEFINE_INSTANCED_PROP(float, _Motion)
            UNITY_DEFINE_INSTANCED_PROP(float, _Phase)
            UNITY_DEFINE_INSTANCED_PROP(float, _Attack)
            UNITY_DEFINE_INSTANCED_PROP(float, _Hit)
            UNITY_DEFINE_INSTANCED_PROP(float, _Death)
            UNITY_DEFINE_INSTANCED_PROP(float, _Archetype)
            UNITY_DEFINE_INSTANCED_PROP(float, _Summon)
            UNITY_DEFINE_INSTANCED_PROP(float, _VisualTime)
        UNITY_INSTANCING_BUFFER_END(PerFigure)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
            float2 tag : TEXCOORD0;        // side, rigid body part
            float4 pivotGlow : TEXCOORD1; // object-space pivot, emission weight
            float2 rigSize : TEXCOORD2;   // total rest height; static props may omit it
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Pose
        {
            float3 position;
            float3 normal;
            float opacity;
            float flash;
            float charge;
            float groundClip;
        };

        float3 RotateX(float3 p, float angle)
        {
            float s, c; sincos(angle, s, c);
            return float3(p.x, p.y * c - p.z * s, p.y * s + p.z * c);
        }
        float3 RotateY(float3 p, float angle)
        {
            float s, c; sincos(angle, s, c);
            return float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
        }
        float3 RotateZ(float3 p, float angle)
        {
            float s, c; sincos(angle, s, c);
            return float3(p.x * c - p.y * s, p.x * s + p.y * c, p.z);
        }
        void RotatePart(inout float3 p, inout float3 n, float3 pivot, float3 angles)
        {
            p -= pivot;
            p = RotateZ(RotateY(RotateX(p, angles.x), angles.y), angles.z) + pivot;
            n = RotateZ(RotateY(RotateX(n, angles.x), angles.y), angles.z);
        }
        float Pulse(float progress, float peak, float release, float end)
        {
            return smoothstep(0, peak, progress) * (1 - smoothstep(release, end, progress));
        }

        float EmergenceHeight(float progress)
        {
            // Full-size geometry starts entirely buried, rises, then settles from a
            // small overshoot. Progress zero is handled by the caller as inactive.
            float rise = smoothstep(0, .76, progress);
            float settle = sin(3.14159265 * saturate((progress - .62) / .38));
            return -1.06 * (1 - rise) + .045 * settle;
        }

        Pose Animate(Attributes input)
        {
            Pose o;
            o.position = input.positionOS.xyz;
            o.normal = input.normalOS;
            o.opacity = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _TeamColor).a * _BaseColor.a);
            o.flash = 0;
            o.charge = 0;
            o.groundClip = 0;
            // Environment meshes and portrait renders need no rig-size channel and no hover.
            if (_Animated < .001) return o;

            float animated = saturate(_Animated);
            float time = UNITY_ACCESS_INSTANCED_PROP(PerFigure, _VisualTime);
            float phase = UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Phase) * 6.2831853;
            float archetype = UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Archetype);
            float attack = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Attack));
            float hit = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Hit));
            float death = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Death));
            float summon = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Summon));
            float alive = 1 - step(.00001, death);
            float attacking = step(.00001, attack) * (1 - step(.99999, attack));
            float motion = saturate(UNITY_ACCESS_INSTANCED_PROP(PerFigure, _Motion)) * alive * (1 - .65 * attacking);
            float height = max(.01, input.rigSize.x);
            float side = input.tag.x;
            float part = input.tag.y;
            float3 pivot = input.pivotGlow.xyz;
            float3 p = o.position;
            float3 n = o.normal;
            float gait = sin(time * 10.5 + phase);
            float breath = sin(time * 2.7 + phase);
            float3 angles = 0;
            float3 shift = 0;
            float windup = Pulse(attack, .23, .25, .45) * alive;
            float strike = smoothstep(.23, .37, attack) * (1 - smoothstep(.46, 1, attack)) * alive;
            float draw = Pulse(attack, .31, .43, .57) * alive;
            float release = smoothstep(.42, .51, attack) * (1 - smoothstep(.58, 1, attack)) * alive;
            float charge = Pulse(attack, .34, .38, .61) * alive;
            float cast = smoothstep(.36, .51, attack) * (1 - smoothstep(.59, 1, attack)) * alive;

            if (part > .5 && part < 1.5)
            {
                // A complete leg rotates at its hip; the foot never stretches separately.
                angles.x = -.48 * gait * side * motion;
                shift.y = max(0, gait * side) * height * .012 * motion;
            }
            else if (part > 1.5 && part < 2.5)
            {
                angles.x = .34 * gait * side * motion;
                angles.z = side * .035 * breath * alive;
                if (archetype < .5)
                {
                    // Dormant zombies breathe and sway their outstretched arms. Their
                    // torso and feet stay grounded instead of floating like a soul flame.
                    angles.x += .07 * sin(time * 1.9 + phase + side * .5) * alive;
                    angles.z += side * .02 * sin(time * 1.4 + phase) * alive;
                }
                else if (archetype < 1.5)
                {
                    if (side > 0)
                    {
                        angles.x += -1.02 * windup + 1.18 * strike;
                        angles.y += -.28 * windup + .58 * strike;
                        angles.z += -.16 * strike;
                    }
                    else angles.x += .24 * strike;
                }
                else if (archetype > 1.5 && archetype < 2.5)
                {
                    if (side < 0)
                    {
                        angles.x += .08 * draw - .15 * release;
                        shift.z += height * (.025 * draw - .030 * release);
                    }
                    else
                    {
                        angles.y += .24 * draw - .12 * release;
                        shift.z -= height * .10 * draw;
                    }
                }
                else if (archetype > 2.5)
                {
                    angles.x += side > 0 ? -.23 * charge + .48 * cast : .34 * charge + .61 * cast;
                    angles.z += -side * .12 * charge;
                    shift.y += height * .040 * charge;
                    if (side > 0) shift.z += height * .025 * cast;
                }
            }
            else if (part > 2.5 && part < 3.5)
            {
                // Zombie head, ears and face share this rigid breathing pivot.
                shift.y = height * .004 * breath * alive;
                angles.x = .022 * sin(time * 1.8 + phase) * alive;
                angles.y = .035 * sin(time * .9 + phase) * alive;
                angles.z = .030 * sin(time * 1.3 + phase) * alive;
            }
            else if (part > 3.5 && part < 4.5)
            {
                angles.y = .035 * sin(time * 1.8 + phase) * alive;
                angles.z = -.025 * gait * motion;
            }
            else if (part > 4.5)
            {
                angles.x = (.045 * breath + .065 * motion * gait) * alive;
                angles.z = .025 * sin(time * 2 + phase) * alive;
            }
            RotatePart(p, n, pivot, angles);
            p += shift;

            if (archetype > .5)
            {
                p.y += height * (.009 * breath * saturate(input.positionOS.y / (height * .45))
                    + .022 * abs(gait) * motion) * alive;
                float bodyLean = .035 * motion;
                if (archetype < 1.5)
                {
                    bodyLean += -.045 * windup + .095 * strike;
                    p.z += height * (-.025 * windup + .065 * strike);
                }
                else if (archetype < 2.5) bodyLean -= .035 * draw + .035 * release;
                else
                {
                    bodyLean += .055 * cast - .025 * charge;
                    p.y += height * .012 * charge;
                    o.charge = charge * .35;
                }
                RotatePart(p, n, float3(0, height * .35, 0), float3(bodyLean, 0, .012 * gait * motion));
            }

            float recoil = Pulse(hit, .10, .18, .86) * alive;
            p.z -= height * .070 * recoil;
            RotatePart(p, n, float3(0, height * .30, 0), float3(-.15 * recoil, 0, .028 * sin(hit * 37.699) * recoil));
            o.flash = step(.00001, hit) * (1 - smoothstep(.03, .32, hit)) * .80 * alive;

            float summonActive = step(.00001, summon) * alive;
            float rigBlend = lerp(1, smoothstep(.52, .92, summon), summonActive);
            p = lerp(o.position, p, rigBlend);
            n = normalize(lerp(o.normal, n, rigBlend));
            p.y += height * EmergenceHeight(summon) * summonActive;
            o.groundClip = summonActive;

            float deathActive = 1 - alive;
            float compression = sin(3.14159265 * saturate(death / .25)) * .10 * deathActive;
            float3 squash = float3(1 + compression * .4, 1 - compression, 1 + compression * .4);
            p *= squash;
            n /= squash;
            float launch = smoothstep(.10, .80, death);
            float flight = smoothstep(.10, 1, death);
            float shrink = 1 - .78 * smoothstep(.30, 1, death);
            float3 deathPivot = float3(0, height * .38, 0);
            p = deathPivot + (p - deathPivot) * shrink;
            float tumbleSide = sin(phase) >= 0 ? 1 : -1;
            RotatePart(p, n, deathPivot, float3(-1.25 * launch, .25 * launch, tumbleSide * .65 * launch));
            p += height * float3(tumbleSide * .12 * launch, .17 * launch + .24 * sin(flight * 3.14159265), -.48 * launch);
            o.opacity *= 1 - smoothstep(.40, .98, death);

            o.position = lerp(o.position, p, animated);
            o.normal = normalize(lerp(o.normal, n, animated));
            return o;
        }

        // Opaque ordered coverage avoids translucent corpses hiding living troops behind them.
        // World-space cells keep the forward/depth/shadow silhouettes in agreement.
        void ClipVisibility(float opacity, float3 positionWS, float groundClip)
        {
            // The same geometric surface clips color, shadows and depth; buried parts
            // cannot appear through the floor or cast an early full-body silhouette.
            if (groundClip > .5) clip(positionWS.y - _GroundHeight);
            clip(opacity - .003);
            if (opacity < .999)
            {
                float3 cell = floor(positionWS * 43.0);
                float threshold = frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                clip(opacity - threshold);
            }
        }
        ENDHLSL

        Pass
        {
            Name "ToyForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half4 presentation : TEXCOORD2; // opacity, flash, emission, fog
                half groundClip : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                Pose pose = Animate(input);
                output.positionWS = TransformObjectToWorld(pose.position);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(pose.normal);
                output.color = input.color;
                output.presentation = half4(pose.opacity, pose.flash, input.pivotGlow.w * (1 + pose.charge),
                    ComputeFogFactor(output.positionCS.z));
                output.groundClip = pose.groundClip;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                ClipVisibility(input.presentation.x, input.positionWS, input.groundClip);
                half3 albedo = input.color.rgb;
                half3 team = UNITY_ACCESS_INSTANCED_PROP(PerFigure, _TeamColor).rgb;
                team /= max(max(max(team.r, team.g), team.b), .001h);
                half value = max(max(albedo.r, albedo.g), albedo.b);
                half recolor = saturate(input.color.a * UNITY_ACCESS_INSTANCED_PROP(PerFigure, _TeamBlend));
                albedo = lerp(albedo, team * value, recolor) * _BaseColor.rgb;
                // Authored RGB (including baked AO) is display-space color. Decode only
                // for this material's lighting, without changing the project's color space.
                #if defined(UNITY_COLORSPACE_GAMMA)
                bool gammaLighting = _GammaCorrectLighting > .5;
                if (gammaLighting) albedo = SRGBToLinear(albedo);
                #endif

                half3 normal = normalize(input.normalWS);
                half3 view = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                // Broad wrapped key and a cool underside describe the curved clay forms.
                // Vertex RGB already includes the sculpt's light baked ambient occlusion.
                half diffuse = saturate((dot(normal, sun.direction) + .16h) / 1.16h);
                // Bounced light fills a toy's self-shadow; the ground still receives the
                // full cast shadow through its own shader. This avoids hard grey skulls.
                half attenuation = sun.distanceAttenuation * lerp(.35h, 1, sun.shadowAttenuation);
                half sky = smoothstep(-.65h, .85h, normal.y);
                half viewFill = smoothstep(-.2h, 1, dot(normal, view));
                half3 sunColor = min(sun.color, half3(1.25h, 1.25h, 1.25h));
                half3 ambient = lerp(half3(.46h, .48h, .55h), half3(.59h, .61h, .63h), sky);
                half3 lighting = ambient + .09h * viewFill
                    + sunColor * (.38h * diffuse * attenuation);
                half3 halfDirection = SafeNormalize(sun.direction + view);
                half highlight = pow(saturate(dot(normal, halfDirection)), 14) * .065h * attenuation;
                half rim = pow(1 - saturate(dot(normal, view)), 3) * .025h;
                half glow = input.presentation.z;
                half3 color = albedo * lighting + highlight * sunColor + rim * albedo;
                // Only soul details rise above the bloom threshold; ivory clay stays matte.
                color += glow * (albedo * .85h + half3(.025h, .16h, .10h));
                #if defined(UNITY_COLORSPACE_GAMMA)
                // Do not saturate: values above one still feed the subtle soul bloom.
                if (gammaLighting) color = LinearToSRGB(max(color, 0));
                #endif
                color = lerp(color, half3(1, .95h, .84h), input.presentation.y);
                color = MixFog(color, input.presentation.w);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half2 coverage : TEXCOORD1; // opacity, emergence ground clip
                UNITY_VERTEX_OUTPUT_STEREO
            };
            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                Pose pose = Animate(input);
                float3 world = TransformObjectToWorld(pose.position);
                float3 normal = TransformObjectToWorldNormal(pose.normal);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - world);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(world, normal, lightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                output.positionWS = world;
                output.coverage = half2(pose.opacity, pose.groundClip);
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                ClipVisibility(input.coverage.x, input.positionWS, input.coverage.y);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half2 coverage : TEXCOORD1; // opacity, emergence ground clip
                UNITY_VERTEX_OUTPUT_STEREO
            };
            DepthVaryings DepthVert(Attributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                Pose pose = Animate(input);
                output.positionWS = TransformObjectToWorld(pose.position);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.coverage = half2(pose.opacity, pose.groundClip);
                return output;
            }
            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                ClipVisibility(input.coverage.x, input.positionWS, input.coverage.y);
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
