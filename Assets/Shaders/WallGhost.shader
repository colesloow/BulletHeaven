// Semi-transparent ghost wall rendered inside the stencil cutout sphere.
// Reuses the wall's colormap texture with a hologram tint so the player can see
// where walls are without being fully occluded by them.
Shader "BulletHeaven/WallGhost"
{
    Properties
    {
        _BaseMap ("Colormap Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _GhostAlpha ("Fill Opacity", Range(0,1)) = 0.05
        _GhostColor ("Ghost Tint", Color) = (0, 0.8, 1, 1)
        _FresnelPower ("Fresnel Power", Float) = 4
        _FresnelIntensity ("Fresnel Intensity", Range(0,1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Renders the ghost wall only inside the stencil cutout sphere (Ref=1, Comp Equal).
        // Fill opacity is kept near zero; the fresnel term makes edges glow
        // so the wall silhouette is visible without blocking the view.
        Pass
        {
            Name "WallGhost"
            Tags { "LightMode" = "UniversalForward" }

            // Only draw pixels where the stencil buffer equals 1.
            // The stencil mask sphere (on the player) writes 1 into the stencil buffer,
            // so this pass is only visible inside that sphere.
            Stencil
            {
                Ref 1
                Comp Equal
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _GhostAlpha;
                half4 _GhostColor;
                half _FresnelPower;
                half _FresnelIntensity;
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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                // Computes the vertex position in Clip Space (for screen projection)
                // and World Space (passed to the fragment shader for fresnel)
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                // Converts the normal from Object Space to World Space,
                // correctly handling non-uniform scale via the inverse-transpose matrix
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                // Applies the material's Tiling and Offset to the UV coordinates
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Grazing angles (edges) get a stronger tint, face-on surfaces stay near-invisible
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                half ndotv = saturate(dot(normalize(IN.normalWS), viewDir));
                half fresnel = pow(1.0h - ndotv, _FresnelPower) * _FresnelIntensity;

                half3 col = lerp(texColor.rgb, _GhostColor.rgb, 0.5h);
                half alpha = saturate(_GhostAlpha + fresnel);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
