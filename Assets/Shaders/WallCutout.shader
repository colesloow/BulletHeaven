Shader "BulletHeaven/WallCutout"
{
    Properties
    {
        _BaseMap ("Colormap Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        [HideInInspector] _Cutoff ("Alpha Cutoff", Float) = 0.5
        [HideInInspector] _Surface ("Surface Type", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // Forward lit pass. Skips pixels where the stencil sphere has
        // written stencil = 1, creating a circular hole in screen space.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Cutoff;
                float _Surface;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 normalWS = normalize(IN.normalWS);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light mainLight = GetMainLight(shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 color = texColor.rgb * mainLight.color * (NdotL * 0.75h + 0.25h);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // Shadow caster pass. Handles both directional and punctual lights.
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Cutoff;
                float _Surface;
            CBUFFER_END

            // Provided by URP during shadow rendering.
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttribs { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct ShadowVaryings { float4 posCS : SV_POSITION; };

            ShadowVaryings ShadowVert(ShadowAttribs IN)
            {
                ShadowVaryings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                posWS = ApplyShadowBias(posWS, normalWS, lightDir);
                float4 posCS = TransformWorldToHClip(posWS);

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.posCS = posCS;
                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthOnly pass intentionally omitted.
        // If the wall wrote its depth during the prepass, the floor behind it
        // would fail ZTest LEqual in the color pass and not render at stencil-masked
        // pixels, producing a black hole. Without this pass the prepass only contains
        // floor depth, so the floor renders correctly through the cutout.
    }
}
