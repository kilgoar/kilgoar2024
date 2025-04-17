Shader "Custom/Rust/StandardSpecular"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0
        _SpecColor("Specular Color", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap("Specular Gloss Map", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _TangentMap("Tangent Map", 2D) = "bump" {}
        _AnisotropyMap("Anisotropy Map", 2D) = "black" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0.2,0.2,0.2)
        _EmissionMap("Emission", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        _DetailSpecGlossMap("Detail Specular", 2D) = "white" {}
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailOcclusionMap("Detail Occlusion", 2D) = "white" {}
        _DetailOcclusionStrength("Detail Occlusion Strength", Float) = 0.0
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailLayer("Detail Layer", Float) = 0.0
        _ParticleLayer_AlbedoMap("Particle Albedo", 2D) = "white" {}
        _ParticleLayer_AlbedoColor("Particle Albedo Color", Color) = (1,1,1,1)
        _ParticleLayer_SpecGlossMap("Particle Specular", 2D) = "white" {}
        [Normal] _ParticleLayer_NormalMap("Particle Normal", 2D) = "bump" {}
        _ParticleLayer_BlendFactor("Particle Blend Factor", Float) = 0.0
        _ParticleLayer_BlendFalloff("Particle Blend Falloff", Float) = 0.0
        _ParticleLayer_Glossiness("Particle Smoothness", Range(0.0, 1.0)) = 0.5
        _ParticleLayer_NormalScale("Particle Normal Scale", Float) = 1.0
        _ParticleLayer_Thickness("Particle Thickness", Float) = 0.0
        _ParticleLayer_SpecColor("Particle Spec Color", Color) = (0.2,0.2,0.2)
        _ParticleLayer_WorldDirection("Particle World Direction", Vector) = (0,0,0,0)
        _ParticleLayer_MapTiling("Particle Map Tiling", Vector) = (1,1,0,0)
        _ParticleLayer("Particle Layer", Float) = 0.0
        _BiomeLayer_TintMask("Biome Tint Mask", 2D) = "white" {}
        _BiomeLayer_TintColor("Biome Tint Color", Color) = (1,1,1,1)
        _BiomeLayer("Biome Layer", Float) = 0.0
        _BiomeLayer_TintSplatIndex("Biome Tint Splat Index", Float) = 0.0
        _WetnessLayer("Wetness Layer", Float) = 0.0
        _WetnessLayer_Mask("Wetness Mask", 2D) = "white" {}
        _WetnessLayer_WetAlbedoScale("Wet Albedo Scale", Float) = 0.5
        _WetnessLayer_WetSmoothness("Wet Smoothness", Float) = 0.5
        _WetnessLayer_Wetness("Wetness", Float) = 0.0
        _ShoreWetnessLayer("Shore Wetness Layer", Float) = 0.0
        _ShoreWetnessLayer_BlendFactor("Shore Blend Factor", Float) = 0.0
        _ShoreWetnessLayer_BlendFalloff("Shore Blend Falloff", Float) = 0.0
        _ShoreWetnessLayer_Range("Shore Range", Float) = 0.0
        _ShoreWetnessLayer_WetAlbedoScale("Shore Wet Albedo Scale", Float) = 0.5
        _ShoreWetnessLayer_WetSmoothness("Shore Wet Smoothness", Float) = 0.5
        _ApplyVertexAlpha("Apply Vertex Alpha", Float) = 0.0
        _ApplyVertexAlphaStrength("Vertex Alpha Strength", Float) = 1.0
        _ApplyVertexColor("Apply Vertex Color", Float) = 0.0
        _ApplyVertexColorStrength("Vertex Color Strength", Float) = 1.0
        _Cull("Cull", Float) = 2.0
        _DecalLayerMask("Decal Layer Mask", Float) = 0.0
        _DetailAlbedoMapScroll("Detail Albedo Map Scroll", Vector) = (0,0,0,0)
        _DetailBlendFlags("Detail Blend Flags", Float) = 0.0
        _DetailBlendType("Detail Blend Type", Float) = 0.0
        _DetailOverlaySmoothness("Detail Overlay Smoothness", Float) = 0.0
        _DetailOverlaySpecular("Detail Overlay Specular", Float) = 0.0
        _DoubleSided("Double Sided", Float) = 0.0
        _DstBlend("Destination Blend", Float) = 0.0
        _EmissionUVSec("Emission UV Secondary", Float) = 0.0
        _EnvReflHorizonFade("Env Reflection Horizon Fade", Float) = 0.0
        _EnvReflOcclusionStrength("Env Reflection Occlusion Strength", Float) = 0.0
        _MainTexScroll("Main Tex Scroll", Vector) = (0,0,0,0)
        _Mode("Mode", Float) = 0.0
        _OcclusionUVSet("Occlusion UV Set", Float) = 0.0
        _OffsetEmissionOnly("Offset Emission Only", Float) = 0.0
        _ShadowBiasScale("Shadow Bias Scale", Float) = 1.0
        _SrcBlend("Source Blend", Float) = 1.0
        _UVSec("UV Secondary", Float) = 0.0
        _ZWrite("ZWrite", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        LOD 300
        ZWrite [_ZWrite]
        Cull [_Cull]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows alpha:fade
        #pragma shader_feature_local _TANGENTMAP
        #pragma shader_feature_local _ANISOTROPYMAP

        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Core samplers
        sampler2D _MainTex;
        sampler2D _SpecGlossMap;
        sampler2D _BumpMap;
        sampler2D _TangentMap;
        sampler2D _AnisotropyMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        sampler2D _BiomeLayer_TintMask;
        sampler2D _WetnessLayer_Mask;

        // Detail samplers
        sampler2D _DetailMask;
        sampler2D _DetailAlbedoMap;
        sampler2D _DetailSpecGlossMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailOcclusionMap;

        // Particle samplers
        sampler2D _ParticleLayer_AlbedoMap;
        sampler2D _ParticleLayer_SpecGlossMap;
        sampler2D _ParticleLayer_NormalMap;

        // Properties (variables renamed to avoid conflicts)
        fixed4 color;
        float cutoff;
        float glossiness;
        fixed4 specularColor;
        float bumpScale;
        float occlusionStrength;
        fixed4 emissionColor;
        fixed4 detailColor;
        float detailLayer;
        float detailNormalMapScale;
        float detailOcclusionStrength;
        fixed4 particleLayerAlbedoColor;
        float particleLayerBlendFactor;
        float particleLayerBlendFalloff;
        float particleLayerGlossiness;
        float particleLayerNormalScale;
        float particleLayerThickness;
        fixed4 particleLayerSpecColor;
        float4 particleLayerWorldDirection;
        float4 particleLayerMapTiling;
        float particleLayer;
        fixed4 biomeLayerTintColor;
        float biomeLayer;
        float biomeLayerTintSplatIndex;
        float wetnessLayer;
        float wetnessLayerWetAlbedoScale;
        float wetnessLayerWetSmoothness;
        float wetnessLayerWetness;
        float shoreWetnessLayer;
        float shoreWetnessLayerBlendFactor;
        float shoreWetnessLayerBlendFalloff;
        float shoreWetnessLayerRange;
        float shoreWetnessLayerWetAlbedoScale;
        float shoreWetnessLayerWetSmoothness;
        float applyVertexAlpha;
        float applyVertexAlphaStrength;
        float applyVertexColor;
        float applyVertexColorStrength;
        float cull;
        float decalLayerMask;
        float4 detailAlbedoMapScroll;
        float detailBlendFlags;
        float detailBlendType;
        float detailOverlaySmoothness;
        float detailOverlaySpecular;
        float doubleSided;
        float dstBlend;
        float emissionUVSec;
        float envReflHorizonFade;
        float envReflOcclusionStrength;
        float4 mainTexScroll;
        float mode;
        float occlusionUVSet;
        float offsetEmissionOnly;
        float shadowBiasScale;
        float srcBlend;
        float uvSec;
        float zWrite;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            float2 uv = IN.uv_MainTex;
            fixed4 albedo = tex2D(_MainTex, uv) * color;

            // Detail pass
            if (detailLayer > 0.0)
            {
                fixed detailMask = tex2D(_DetailMask, uv).r;
                fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, uv);
                albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb * detailColor.rgb, detailMask);
                o.Specular = tex2D(_DetailSpecGlossMap, uv).rgb * specularColor.rgb;
                o.Smoothness = tex2D(_DetailSpecGlossMap, uv).a * glossiness;
                o.Normal = UnpackScaleNormal(tex2D(_DetailNormalMap, uv), detailNormalMapScale);
                o.Occlusion = lerp(1.0, tex2D(_DetailOcclusionMap, uv).r, detailOcclusionStrength);
            }

            // Particle layer
            if (particleLayer > 0.0)
            {
                float2 particleUV = uv * particleLayerMapTiling.xy + particleLayerMapTiling.zw;
                fixed4 particleAlbedo = tex2D(_ParticleLayer_AlbedoMap, particleUV) * particleLayerAlbedoColor;
                float particleBlend = saturate(pow(particleLayerBlendFactor, particleLayerBlendFalloff));
                albedo.rgb = lerp(albedo.rgb, particleAlbedo.rgb, particleBlend);
                o.Specular = lerp(o.Specular, tex2D(_ParticleLayer_SpecGlossMap, particleUV).rgb * particleLayerSpecColor.rgb, particleBlend);
                o.Smoothness = lerp(o.Smoothness, particleLayerGlossiness, particleBlend);
                o.Normal = lerp(o.Normal, UnpackScaleNormal(tex2D(_ParticleLayer_NormalMap, particleUV), particleLayerNormalScale), particleBlend);
            }

            // Biome tint
            if (biomeLayer > 0.0)
            {
                fixed biomeMask = tex2D(_BiomeLayer_TintMask, uv).r;
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * biomeLayerTintColor.rgb, biomeMask);
            }

            // Wetness
            if (wetnessLayer > 0.0)
            {
                fixed wetness = tex2D(_WetnessLayer_Mask, uv).r * wetnessLayerWetness;
                wetness = max(wetness, 0.0);
                albedo.rgb *= lerp(1.0, wetnessLayerWetAlbedoScale, wetness);
                o.Smoothness = lerp(o.Smoothness, wetnessLayerWetSmoothness, wetness);
            }
            if (shoreWetnessLayer > 0.0)
            {
                fixed shoreWetness = tex2D(_WetnessLayer_Mask, uv).r * shoreWetnessLayer * shoreWetnessLayerBlendFactor;
                shoreWetness = max(shoreWetness, 0.0);
                albedo.rgb *= lerp(1.0, shoreWetnessLayerWetAlbedoScale, shoreWetness);
                o.Smoothness = lerp(o.Smoothness, shoreWetnessLayerWetSmoothness, shoreWetness);
            }

            // Vertex color and alpha (requires vertex input if enabled)
            if (applyVertexColor > 0.0 || applyVertexAlpha > 0.0)
            {
                // Note: Requires fixed4 color : COLOR in Input struct
            }

            o.Albedo = albedo.rgb;
            o.Specular = tex2D(_SpecGlossMap, uv).rgb * specularColor.rgb;
            o.Smoothness = tex2D(_SpecGlossMap, uv).a * glossiness;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), bumpScale);
            o.Occlusion = lerp(1.0, tex2D(_OcclusionMap, uv).r, occlusionStrength);
            o.Emission = tex2D(_EmissionMap, uv).rgb * emissionColor.rgb;
            o.Alpha = albedo.a;

            clip(albedo.a - cutoff);
        }
        ENDCG
    }
    CustomEditor "RustStandardSpecularShaderGUI"
}