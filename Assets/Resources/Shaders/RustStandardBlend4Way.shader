Shader "Custom/Rust/StandardBlend4Way"
{
    Properties
    {
        // Main Properties
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.3
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0.2,0.2,0.2)
        _EmissionMap("Emission", 2D) = "white" {}

        // Blend Layer 1
        _BlendLayer1("Blend Layer 1", Float) = 0.0
        _BlendLayer1_Color("Blend Layer 1 Color", Color) = (1,1,1,1)
        _BlendLayer1_AlbedoMap("Blend Layer 1 Albedo", 2D) = "white" {}
        _BlendLayer1_MetallicGlossMap("Blend Layer 1 Metallic", 2D) = "white" {}
        [Normal] _BlendLayer1_NormalMap("Blend Layer 1 Normal", 2D) = "bump" {}
        _BlendLayer1_BlendMaskMap("Blend Layer 1 Mask", 2D) = "white" {}
        _BlendLayer1_BlendFactor("Blend Layer 1 Blend Factor", Float) = 0.0
        _BlendLayer1_BlendFalloff("Blend Layer 1 Blend Falloff", Float) = 0.0
        _BlendLayer1_Glossiness("Blend Layer 1 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer1_Metallic("Blend Layer 1 Metallic", Range(0.0, 1.0)) = 0.0
        _BlendLayer1_NormalMapScale("Blend Layer 1 Normal Scale", Float) = 1.0
        _BlendLayer1_AlbedoMapScroll("Blend Layer 1 Albedo Scroll", Vector) = (0,0,0,0)
        _BlendLayer1_BlendMaskMapScroll("Blend Layer 1 Mask Scroll", Vector) = (0,0,0,0)
        _BlendLayer1_AlbedoTintMask("Blend Layer 1 Albedo Tint Mask", Range(0.0, 1.0)) = 0.0
        _BlendLayer1_BlendMaskMapInvert("Blend Layer 1 Mask Invert", Range(0.0, 1.0)) = 0.0
        _BlendLayer1_UVSet("Blend Layer 1 UV Set", Float) = 0.0
        _BlendLayer1_BlendMaskUVSet("Blend Layer 1 Blend Mask UV Set", Float) = 0.0

        // Blend Layer 2
        _BlendLayer2("Blend Layer 2", Float) = 0.0
        _BlendLayer2_Color("Blend Layer 2 Color", Color) = (1,1,1,1)
        _BlendLayer2_AlbedoMap("Blend Layer 2 Albedo", 2D) = "white" {}
        _BlendLayer2_MetallicGlossMap("Blend Layer 2 Metallic", 2D) = "white" {}
        [Normal] _BlendLayer2_NormalMap("Blend Layer 2 Normal", 2D) = "bump" {}
        _BlendLayer2_BlendMaskMap("Blend Layer 2 Mask", 2D) = "white" {}
        _BlendLayer2_BlendFactor("Blend Layer 2 Blend Factor", Float) = 0.0
        _BlendLayer2_BlendFalloff("Blend Layer 2 Blend Falloff", Float) = 0.0
        _BlendLayer2_Glossiness("Blend Layer 2 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer2_Metallic("Blend Layer 2 Metallic", Range(0.0, 1.0)) = 0.0
        _BlendLayer2_NormalMapScale("Blend Layer 2 Normal Scale", Float) = 1.0
        _BlendLayer2_AlbedoMapScroll("Blend Layer 2 Albedo Scroll", Vector) = (0,0,0,0)
        _BlendLayer2_BlendMaskMapScroll("Blend Layer 2 Mask Scroll", Vector) = (0,0,0,0)
        _BlendLayer2_AlbedoTintMask("Blend Layer 2 Albedo Tint Mask", Range(0.0, 1.0)) = 0.0
        _BlendLayer2_BlendMaskMapInvert("Blend Layer 2 Mask Invert", Range(0.0, 1.0)) = 0.0
        _BlendLayer2_UVSet("Blend Layer 2 UV Set", Float) = 0.0
        _BlendLayer2_BlendMaskUVSet("Blend Layer 2 Blend Mask UV Set", Float) = 0.0

        // Blend Layer 3
        _BlendLayer3("Blend Layer 3", Float) = 0.0
        _BlendLayer3_Color("Blend Layer 3 Color", Color) = (1,1,1,1)
        _BlendLayer3_AlbedoMap("Blend Layer 3 Albedo", 2D) = "white" {}
        _BlendLayer3_MetallicGlossMap("Blend Layer 3 Metallic", 2D) = "white" {}
        [Normal] _BlendLayer3_NormalMap("Blend Layer 3 Normal", 2D) = "bump" {}
        _BlendLayer3_BlendMaskMap("Blend Layer 3 Mask", 2D) = "white" {}
        _BlendLayer3_BlendFactor("Blend Layer 3 Blend Factor", Float) = 0.0
        _BlendLayer3_BlendFalloff("Blend Layer 3 Blend Falloff", Float) = 0.0
        _BlendLayer3_Glossiness("Blend Layer 3 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer3_Metallic("Blend Layer 3 Metallic", Range(0.0, 1.0)) = 0.0
        _BlendLayer3_NormalMapScale("Blend Layer 3 Normal Scale", Float) = 1.0
        _BlendLayer3_AlbedoMapScroll("Blend Layer 3 Albedo Scroll", Vector) = (0,0,0,0)
        _BlendLayer3_BlendMaskMapScroll("Blend Layer 3 Mask Scroll", Vector) = (0,0,0,0)
        _BlendLayer3_AlbedoTintMask("Blend Layer 3 Albedo Tint Mask", Range(0.0, 1.0)) = 0.0
        _BlendLayer3_BlendMaskMapInvert("Blend Layer 3 Mask Invert", Range(0.0, 1.0)) = 0.0
        _BlendLayer3_UVSet("Blend Layer 3 UV Set", Float) = 0.0
        _BlendLayer3_BlendMaskUVSet("Blend Layer 3 Blend Mask UV Set", Float) = 0.0

        // Blend Layer 4
        _BlendLayer4("Blend Layer 4", Float) = 0.0
        _BlendLayer4_Color("Blend Layer 4 Color", Color) = (1,1,1,1)
        _BlendLayer4_AlbedoMap("Blend Layer 4 Albedo", 2D) = "white" {}
        _BlendLayer4_MetallicGlossMap("Blend Layer 4 Metallic", 2D) = "white" {}
        [Normal] _BlendLayer4_NormalMap("Blend Layer 4 Normal", 2D) = "bump" {}
        _BlendLayer4_BlendMaskMap("Blend Layer 4 Mask", 2D) = "white" {}
        _BlendLayer4_BlendFactor("Blend Layer 4 Blend Factor", Float) = 0.0
        _BlendLayer4_BlendFalloff("Blend Layer 4 Blend Falloff", Float) = 0.0
        _BlendLayer4_Glossiness("Blend Layer 4 Smoothness", Range(0.0, 1.0)) = 0.5
        _BlendLayer4_Metallic("Blend Layer 4 Metallic", Range(0.0, 1.0)) = 0.0
        _BlendLayer4_NormalMapScale("Blend Layer 4 Normal Scale", Float) = 1.0
        _BlendLayer4_AlbedoMapScroll("Blend Layer 4 Albedo Scroll", Vector) = (0,0,0,0)
        _BlendLayer4_BlendMaskMapScroll("Blend Layer 4 Mask Scroll", Vector) = (0,0,0,0)
        _BlendLayer4_AlbedoTintMask("Blend Layer 4 Albedo Tint Mask", Range(0.0, 1.0)) = 0.0
        _BlendLayer4_BlendMaskMapInvert("Blend Layer 4 Mask Invert", Range(0.0, 1.0)) = 0.0
        _BlendLayer4_UVSet("Blend Layer 4 UV Set", Float) = 0.0
        _BlendLayer4_BlendMaskUVSet("Blend Layer 4 Blend Mask UV Set", Float) = 0.0

        // Detail Properties
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        _DetailMetallicGlossMap("Detail Metallic", 2D) = "white" {}
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailOcclusionMap("Detail Occlusion", 2D) = "white" {}
        _DetailOcclusionStrength("Detail Occlusion Strength", Float) = 0.0
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailLayer("Detail Layer", Float) = 0.0
        _DetailLayer_BlendFactor("Detail Layer Blend Factor", Float) = 0.0
        _DetailLayer_BlendFalloff("Detail Layer Blend Falloff", Float) = 0.0
        _DetailAlbedoMapScroll("Detail Albedo Scroll", Vector) = (0,0,0,0)
        _DetailOverlayMetallic("Detail Overlay Metallic", Range(0.0, 1.0)) = 0.0
        _DetailOverlaySmoothness("Detail Overlay Smoothness", Range(0.0, 1.0)) = 0.0

        // Biome Tinting
        _BiomeLayer_TintMask("Biome Tint Mask", 2D) = "white" {}
        _BiomeLayer_TintColor("Biome Tint Color", Color) = (1,1,1,1)
        _BiomeLayer("Biome Layer", Float) = 0.0
        _BiomeLayer_TintSplatIndex("Biome Tint Splat Index", Float) = 0.0
        _AlbedoTintMask("Albedo Tint Mask", Range(0.0, 1.0)) = 0.0

        // Vertex Color/Alpha
        _ApplyVertexAlpha("Apply Vertex Alpha", Range(0.0, 1.0)) = 0.0
        _ApplyVertexAlphaStrength("Apply Vertex Alpha Strength", Range(0.0, 1.0)) = 0.0

        // Advanced Properties
        _MainTexScroll("Main Tex Scroll", Vector) = (0,0,0,0)
        _UVSec("Secondary UV Set", Float) = 0.0
        _Mode("Rendering Mode", Float) = 0.0
        _SrcBlend("Source Blend", Float) = 1.0
        _DstBlend("Destination Blend", Float) = 0.0
        _ZWrite("ZWrite", Float) = 1.0
        _Cull("Cull Mode", Float) = 2.0
        _DoubleSided("Double Sided", Float) = 0.0
        _DecalLayerMask("Decal Layer Mask", Float) = 0.0
        _EnvReflHorizonFade("Env Reflection Horizon Fade", Float) = 0.0
        _EnvReflOcclusionStrength("Env Reflection Occlusion Strength", Float) = 0.0
        _DetailApplyBeforeBlendLayers("Apply Detail Before Blend Layers", Float) = 0.0
        _DetailBlendType("Detail Blend Type", Float) = 0.0
        _DetailBlendFlags("Detail Blend Flags", Float) = 0.0
        _DetailMaskSeparateTilingOffset("Detail Mask Separate Tiling Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Core samplers
        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        sampler2D _BiomeLayer_TintMask;

        // Blend Layer samplers
        sampler2D _BlendLayer1_AlbedoMap;
        sampler2D _BlendLayer1_MetallicGlossMap;
        sampler2D _BlendLayer1_NormalMap;
        sampler2D _BlendLayer1_BlendMaskMap;
        sampler2D _BlendLayer2_AlbedoMap;
        sampler2D _BlendLayer2_MetallicGlossMap;
        sampler2D _BlendLayer2_NormalMap;
        sampler2D _BlendLayer2_BlendMaskMap;
        sampler2D _BlendLayer3_AlbedoMap;
        sampler2D _BlendLayer3_MetallicGlossMap;
        sampler2D _BlendLayer3_NormalMap;
        sampler2D _BlendLayer3_BlendMaskMap;
        sampler2D _BlendLayer4_AlbedoMap;
        sampler2D _BlendLayer4_MetallicGlossMap;
        sampler2D _BlendLayer4_NormalMap;
        sampler2D _BlendLayer4_BlendMaskMap;

        // Detail samplers
        sampler2D _DetailMask;
        sampler2D _DetailAlbedoMap;
        sampler2D _DetailMetallicGlossMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailOcclusionMap;

        // Properties
        fixed4 _Color;
        float _Cutoff;
        float _Glossiness;
        float _Metallic;
        float _BumpScale;
        float _OcclusionStrength;
        fixed4 _EmissionColor;
        fixed4 _DetailColor;
        float _DetailLayer;
        float _DetailNormalMapScale;
        float _DetailOcclusionStrength;
        float _DetailLayer_BlendFactor;
        float _DetailLayer_BlendFalloff;
        float4 _DetailAlbedoMapScroll;
        float _DetailOverlayMetallic;
        float _DetailOverlaySmoothness;
        float _DetailApplyBeforeBlendLayers;
        float _DetailBlendType;
        float _DetailBlendFlags;
        float _DetailMaskSeparateTilingOffset;
        float _BiomeLayer;
        fixed4 _BiomeLayer_TintColor;
        float _BiomeLayer_TintSplatIndex;
        float _AlbedoTintMask;
        float _ApplyVertexAlpha;
        float _ApplyVertexAlphaStrength;
        float4 _MainTexScroll;
        float _UVSec;
        float _Mode;
        float _SrcBlend;
        float _DstBlend;
        float _ZWrite;
        float _Cull;
        float _DoubleSided;
        float _DecalLayerMask;
        float _EnvReflHorizonFade;
        float _EnvReflOcclusionStrength;

        // Blend Layer 1
        float _BlendLayer1;
        fixed4 _BlendLayer1_Color;
        float _BlendLayer1_BlendFactor;
        float _BlendLayer1_BlendFalloff;
        float _BlendLayer1_Glossiness;
        float _BlendLayer1_Metallic;
        float _BlendLayer1_NormalMapScale;
        float4 _BlendLayer1_AlbedoMapScroll;
        float4 _BlendLayer1_BlendMaskMapScroll;
        float _BlendLayer1_AlbedoTintMask;
        float _BlendLayer1_BlendMaskMapInvert;
        float _BlendLayer1_UVSet;
        float _BlendLayer1_BlendMaskUVSet;

        // Blend Layer 2
        float _BlendLayer2;
        fixed4 _BlendLayer2_Color;
        float _BlendLayer2_BlendFactor;
        float _BlendLayer2_BlendFalloff;
        float _BlendLayer2_Glossiness;
        float _BlendLayer2_Metallic;
        float _BlendLayer2_NormalMapScale;
        float4 _BlendLayer2_AlbedoMapScroll;
        float4 _BlendLayer2_BlendMaskMapScroll;
        float _BlendLayer2_AlbedoTintMask;
        float _BlendLayer2_BlendMaskMapInvert;
        float _BlendLayer2_UVSet;
        float _BlendLayer2_BlendMaskUVSet;

        // Blend Layer 3
        float _BlendLayer3;
        fixed4 _BlendLayer3_Color;
        float _BlendLayer3_BlendFactor;
        float _BlendLayer3_BlendFalloff;
        float _BlendLayer3_Glossiness;
        float _BlendLayer3_Metallic;
        float _BlendLayer3_NormalMapScale;
        float4 _BlendLayer3_AlbedoMapScroll;
        float4 _BlendLayer3_BlendMaskMapScroll;
        float _BlendLayer3_AlbedoTintMask;
        float _BlendLayer3_BlendMaskMapInvert;
        float _BlendLayer3_UVSet;
        float _BlendLayer3_BlendMaskUVSet;

        // Blend Layer 4
        float _BlendLayer4;
        fixed4 _BlendLayer4_Color;
        float _BlendLayer4_BlendFactor;
        float _BlendLayer4_BlendFalloff;
        float _BlendLayer4_Glossiness;
        float _BlendLayer4_Metallic;
        float _BlendLayer4_NormalMapScale;
        float4 _BlendLayer4_AlbedoMapScroll;
        float4 _BlendLayer4_BlendMaskMapScroll;
        float _BlendLayer4_AlbedoTintMask;
        float _BlendLayer4_BlendMaskMapInvert;
        float _BlendLayer4_UVSet;
        float _BlendLayer4_BlendMaskUVSet;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_MainTex;
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            float2 uv2 = IN.uv2_MainTex;
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;

			/*
            // Apply vertex alpha
            if (_ApplyVertexAlpha > 0.0)
            {
                albedo.a *= lerp(1.0, IN.color.a, _ApplyVertexAlphaStrength);
            }
			*/
			
            // Blend Layer 1
            if (_BlendLayer1 > 0.0)
            {
                float2 blend1UV = _BlendLayer1_UVSet == 0.0 ? uv : uv2;
                float2 blend1MaskUV = _BlendLayer1_BlendMaskUVSet == 0.0 ? uv : uv2;
                fixed blendMask1 = tex2D(_BlendLayer1_BlendMaskMap, blend1MaskUV).r;
                blendMask1 = _BlendLayer1_BlendMaskMapInvert > 0.0 ? 1.0 - blendMask1 : blendMask1;
                //blendMask1 = pow(blendMask1, max(0.1, _BlendLayer1_BlendFalloff)); // Apply falloff
                //blendMask1 = blendMask1 * _BlendLayer1_BlendFactor; // Apply intensity and factor
                fixed4 blendAlbedo1 = tex2D(_BlendLayer1_AlbedoMap, blend1UV);
				
                albedo.rgb = lerp(albedo.rgb, blendAlbedo1.rgb, blendMask1);
            }

			/*
            // Blend Layer 2  gives oil rig herpes
            if (_BlendLayer2 > 0.0)  
            {
                float2 blend2UV = _BlendLayer2_UVSet == 0.0 ? uv : uv2;
                float2 blend2MaskUV = _BlendLayer2_BlendMaskUVSet == 0.0 ? uv : uv2;
                fixed blendMask2 = tex2D(_BlendLayer2_BlendMaskMap, blend2MaskUV).r;
                blendMask2 = _BlendLayer2_BlendMaskMapInvert > 0.0 ? 1.0 - blendMask2 : blendMask2;
                //blendMask2 = pow(blendMask2, max(0.1, _BlendLayer2_BlendFalloff)); // Apply falloff
                //blendMask2 = saturate(blendMask2 * _BlendLayer2_BlendFactor); // Apply intensity and factor

                fixed4 blendAlbedo2 = tex2D(_BlendLayer2_AlbedoMap, blend2UV);
                if (blendAlbedo2.r == 1.0 && blendAlbedo2.g == 1.0 && blendAlbedo2.b == 1.0)
                {
                    blendAlbedo2.rgb = _BlendLayer2_Color.rgb;
                }
                else
                {
                    blendAlbedo2.rgb *= _BlendLayer2_Color.rgb;
                }
                albedo.rgb = lerp(albedo.rgb, blendAlbedo2.rgb, blendMask2);
            }
			*/

            // Blend Layer 3
            if (_BlendLayer3 > 0.0)
            {
                float2 blend3UV = _BlendLayer3_UVSet == 0.0 ? uv : uv2;
                float2 blend3MaskUV = _BlendLayer3_BlendMaskUVSet == 0.0 ? uv : uv2;
                fixed blendMask3 = tex2D(_BlendLayer3_BlendMaskMap, blend3MaskUV).r;
                blendMask3 = _BlendLayer3_BlendMaskMapInvert > 0.0 ? 1.0 - blendMask3 : blendMask3;
                //blendMask3 = pow(blendMask3, max(0.1, _BlendLayer3_BlendFalloff)); // Apply falloff
                //blendMask3 = saturate(blendMask3 * _BlendLayer3_BlendFactor); // Apply intensity and factor

                fixed4 blendAlbedo3 = tex2D(_BlendLayer3_AlbedoMap, blend3UV);
                if (blendAlbedo3.r == 1.0 && blendAlbedo3.g == 1.0 && blendAlbedo3.b == 1.0)
                {
                    blendAlbedo3.rgb = _BlendLayer3_Color.rgb;
                }
                else
                {
                    blendAlbedo3.rgb *= _BlendLayer3_Color.rgb;
                }
                albedo.rgb = lerp(albedo.rgb, blendAlbedo3.rgb, blendMask3);
            }

            // Blend Layer 4
            if (_BlendLayer4 > 0.0)
            {
                float2 blend4UV = _BlendLayer4_UVSet == 0.0 ? uv : uv2;
                float2 blend4MaskUV = _BlendLayer4_BlendMaskUVSet == 0.0 ? uv : uv2;
                fixed blendMask4 = tex2D(_BlendLayer4_BlendMaskMap, blend4MaskUV).r;
                blendMask4 = _BlendLayer4_BlendMaskMapInvert > 0.0 ? 1.0 - blendMask4 : blendMask4;
                blendMask4 = pow(blendMask4, max(0.1, _BlendLayer4_BlendFalloff)); // Apply falloff
                blendMask4 = saturate(blendMask4 * _BlendLayer4_BlendFactor); // Apply intensity and factor

                fixed4 blendAlbedo4 = tex2D(_BlendLayer4_AlbedoMap, blend4UV);
                if (blendAlbedo4.r == 1.0 && blendAlbedo4.g == 1.0 && blendAlbedo4.b == 1.0)
                {
                    blendAlbedo4.rgb = _BlendLayer4_Color.rgb;
                }
                else
                {
                    blendAlbedo4.rgb *= _BlendLayer4_Color.rgb;
                }
                albedo.rgb = lerp(albedo.rgb, blendAlbedo4.rgb, blendMask4);
            }

            // Base material properties
            o.Albedo = albedo.rgb;
			o.Emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
            //o.Metallic = _Metallic;
            //o.Smoothness = _Glossiness;
            //o.Alpha = albedo.a;
            //clip(albedo.a - _Cutoff);
        }
        ENDCG
    }
    CustomEditor "RustStandardBlend4WayShaderGUI"
}