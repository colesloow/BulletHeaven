// This shader draws an overlay on top of the original mesh (two draw calls per enemy).
// Monitor overdraw if many enemies spawn simultaneously.
Shader "BulletHeaven/HologramDissolve"
{
    Properties
    {
        [Header(Hologram)]
        _HologramColor ("Hologram Color", Color) = (0, 0.8, 1, 1)
        _ScanlineFreq ("Scanline Frequency", Float) = 12
        _ScanlineThick ("Scanline Thickness", Range(0, 1)) = 0.35
        _FresnelPower ("Fresnel Power", Float) = 2
        _FresnelIntensity ("Fresnel Intensity", Float) = 1.5
        _FlickerSpeed ("Flicker Speed", Float) = 4
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.15

        [Header(Edge)]
        _EdgeWidth ("Edge Width (normalized)", Range(0, 0.2)) = 0.05
        _EdgeColor ("Edge Color", Color) = (0, 1, 1, 1)
        _EdgeIntensity ("Edge Intensity", Float) = 5

        [Header(Animation set by HologramEffect)]
        _Wave1 ("Phase1 progress (hologram sweeps up)", Range(0, 1)) = 0
        _Wave2 ("Phase2 progress (material reveals up)", Range(0, 1)) = 0
        _WorldYMin ("World Y Min", Float) = -0.5
        _WorldYMax ("World Y Max", Float) = 0.5
    }

    SubShader
    {
        // Queue+1 and Offset -1 ensure the hologram always renders on TOP of the original mesh
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Draws the hologram effect on top of the original mesh.
        // Pixels outside the active band [wave2 .. wave1] are discarded,
        // so the hologram appears and disappears progressively bottom-to-top.
        Pass
        {
            Name "HologramOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Offset -1, 0
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _HologramColor;
                float _ScanlineFreq;
                float _ScanlineThick;
                float _FresnelPower;
                float _FresnelIntensity;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _EdgeWidth;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _Wave1;
                float _Wave2;
                float _WorldYMin;
                float _WorldYMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // Convert pixel height to [0,1] so it can be compared against wave thresholds
                float range = max(_WorldYMax - _WorldYMin, 0.001);
                float normH = (i.positionWS.y - _WorldYMin) / range;

                // Discard pixels outside the active band [wave2-edge -> wave1+edge]
                // clip(x) discards the pixel if x < 0, so this keeps only the band between the two waves.
                // EdgeWidth extends the band slightly so the glow effect isn't clipped at the edges
                clip(normH - (_Wave2 - _EdgeWidth));
                clip((_Wave1 + _EdgeWidth) - normH);

                // Fresnel: surfaces facing away from the camera (grazing angle) get a stronger glow.
                // dot(normal, viewDir) is 0 at grazing and 1 face-on, so (1 - ndotv) inverts that
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndotv = saturate(dot(normalize(i.normalWS), viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower) * _FresnelIntensity;

                // Scanlines use world Y so they remain stable when the mesh rotates.
                // frac() repeats the pattern every unit; step() makes each line a hard threshold.
                float scanline = step(1.0 - _ScanlineThick, frac(i.positionWS.y * _ScanlineFreq));

                // Two overlapping sine waves at different speeds (avoid a too-regular rhythm)
                float t = _Time.y;
                float flicker = 1.0 - _FlickerAmount * abs(sin(t * _FlickerSpeed));
                flicker *= 1.0 - _FlickerAmount * 0.5 * abs(sin(t * _FlickerSpeed * 2.7 + 1.3));

                // Glowing line at each wave boundary, fading to black within EdgeWidth.
                // saturate clamps the sum so the two edges don't exceed 1 when they overlap.
                float topGlow = saturate(1.0 - abs(normH - _Wave1) / max(_EdgeWidth, 0.001));
                float botGlow = saturate(1.0 - abs(normH - _Wave2) / max(_EdgeWidth, 0.001));
                float edgeGlow = saturate(topGlow + botGlow) * _EdgeIntensity;

                float brightness = saturate(fresnel * 0.8 + scanline * 0.4 + 0.15) * flicker;
                float3 col = _HologramColor.rgb * brightness + _EdgeColor.rgb * edgeGlow;

                return float4(col, 1.0);
            }
            ENDHLSL
        }

        // Casts the shadow for the hologram mesh.
        // The shadow only covers the portion already revealed by wave1,
        // so it grows progressively rather than appearing in full from the start.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _HologramColor;
                float _ScanlineFreq;
                float _ScanlineThick;
                float _FresnelPower;
                float _FresnelIntensity;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _EdgeWidth;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _Wave1;
                float _Wave2;
                float _WorldYMin;
                float _WorldYMax;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float normH : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes i)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);

                // ApplyShadowBias offsets the shadow geometry along the light direction
                // to avoid self-shadowing artifacts (shadow acne)
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, lightDir));
                // Clamp to near plane to prevent shadow geometry from being clipped
                // on platforms where the depth buffer is reversed.
                #if UNITY_REVERSED_Z
                    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = clipPos;

                float range = max(_WorldYMax - _WorldYMin, 0.001);
                o.normH = (posWS.y - _WorldYMin) / range;
                return o;
            }

            float4 ShadowFrag(Varyings i) : SV_Target
            {
                // Only the upper bound is clipped: the shadow covers everything below wave1
                // (the portion of the mesh already materialized).
                clip((_Wave1 + _EdgeWidth) - i.normH);
                return 0;
            }
            ENDHLSL
        }
    }
}
