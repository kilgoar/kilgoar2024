Shader "Custom/Rust/StandardDecal"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission", 2D) = "black" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailOcclusionMap("Detail Occlusion", 2D) = "white" {}
        _DetailOcclusionStrength("Detail Occlusion Strength", Range(0.0, 1.0)) = 0.0
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailLayer("Detail Layer", Float) = 0.0
        _DetailBlendFlags("Detail Blend Flags", Float) = 0.0
        _DetailBlendType("Detail Blend Type", Float) = 0.0
        _DetailOverlayMetallic("Detail Overlay Metallic", Range(0.0, 1.0)) = 0.0
        _DetailOverlaySmoothness("Detail Overlay Smoothness", Range(0.0, 1.0)) = 0.0
        _DetailAlbedoMapScroll("Detail Albedo Map Scroll", Vector) = (0,0,0,0)
        _BiomeLayer_TintMask("Biome Tint Mask", 2D) = "white" {}
        _BiomeLayer("Biome Layer", Float) = 0.0
        _BiomeLayer_TintSplatIndex("Biome Tint Splat Index", Float) = 0.0
        _WetnessLayer("Wetness Layer", Float) = 0.0
        _WetnessLayer_Mask("Wetness Mask", 2D) = "white" {}
        _WetnessLayer_WetAlbedoScale("Wet Albedo Scale", Float) = 0.5
        _WetnessLayer_WetSmoothness("Wet Smoothness", Range(0.0, 1.0)) = 0.8
        _WetnessLayer_Wetness("Wetness", Range(0.0, 1.0)) = 0.0
        _ShoreWetnessLayer("Shore Wetness Layer", Float) = 0.0
        _ShoreWetnessLayer_BlendFactor("Shore Blend Factor", Float) = 0.0
        _ShoreWetnessLayer_BlendFalloff("Shore Blend Falloff", Float) = 0.0
        _ShoreWetnessLayer_Range("Shore Range", Float) = 0.0
        _ShoreWetnessLayer_WetAlbedoScale("Shore Wet Albedo Scale", Float) = 0.5
        _ShoreWetnessLayer_WetSmoothness("Shore Wet Smoothness", Range(0.0, 1.0)) = 0.8
        _ApplyVertexAlpha("Apply Vertex Alpha", Range(0.0, 1.0)) = 0.0
        _ApplyVertexAlphaStrength("Vertex Alpha Strength", Range(0.0, 1.0)) = 1.0
        _ApplyVertexColor("Apply Vertex Color", Range(0.0, 1.0)) = 0.0
        _ApplyVertexColorStrength("Vertex Color Strength", Range(0.0, 1.0)) = 1.0
        _MainTexScroll("Main Tex Scroll", Vector) = (0,0,0,0)
        _UVSec("Secondary UV Set", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        LOD 300
        Cull Off
        ZWrite On
        Blend One Zero

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Properties
        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        sampler2D _DetailMask;
        sampler2D _DetailAlbedoMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailOcclusionMap;
        sampler2D _BiomeLayer_TintMask;
        sampler2D _WetnessLayer_Mask;
        float _ApplyVertexAlpha;
        float _ApplyVertexAlphaStrength;
        float _ApplyVertexColor;
        float _ApplyVertexColorStrength;
        float _BiomeLayer;
        float _BiomeLayer_TintSplatIndex;
        float _BumpScale;
        fixed4 _Color;
        float4 _DetailAlbedoMapScroll;
        float _DetailBlendFlags;
        float _DetailBlendType;
        fixed4 _DetailColor;
        float _DetailLayer;
        float _DetailNormalMapScale;
        float _DetailOcclusionStrength;
        float _DetailOverlayMetallic;
        float _DetailOverlaySmoothness;
        fixed4 _EmissionColor;
        float _Glossiness;
        float4 _MainTexScroll;
        float _Metallic;
        float _OcclusionStrength;
        float _ShoreWetnessLayer;
        float _ShoreWetnessLayer_BlendFactor;
        float _ShoreWetnessLayer_BlendFalloff;
        float _ShoreWetnessLayer_Range;
        float _ShoreWetnessLayer_WetAlbedoScale;
        float _ShoreWetnessLayer_WetSmoothness;
        float _UVSec;
        float _WetnessLayer;
        float _WetnessLayer_WetAlbedoScale;
        float _WetnessLayer_WetSmoothness;
        float _WetnessLayer_Wetness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_BumpMap;
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // UV handling
            float2 uv = (_UVSec == 0) ? IN.uv_MainTex : IN.uv2_BumpMap;
            //uv += _MainTexScroll.xy * _Time.y;

			
            // Base albedo and vertex color/alpha
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;
			
			/*
            if (_ApplyVertexColor > 0.0)
                albedo.rgb *= lerp(fixed3(1,1,1), IN.color.rgb, _ApplyVertexColorStrength);
            if (_ApplyVertexAlpha > 0.0)
                albedo.a *= lerp(1.0, IN.color.a, _ApplyVertexAlphaStrength);
			
		
            // Detail layer
            if (_DetailLayer > 0.0)
            {
                float2 detailUV = uv + _DetailAlbedoMapScroll.xy * _Time.y;
                fixed detailMask = tex2D(_DetailMask, uv).r;
                fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, detailUV) * _DetailColor;
                albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb, detailMask * _DetailLayer);
                //o.Normal = UnpackScaleNormal(tex2D(_DetailNormalMap, detailUV), _DetailNormalMapScale);
                //o.Occlusion = lerp(1.0, tex2D(_DetailOcclusionMap, uv).r, _DetailOcclusionStrength);
                if (_DetailBlendFlags > 0.0)
                {
                    //o.Metallic = lerp(o.Metallic, _DetailOverlayMetallic, detailMask * _DetailLayer);
                    //o.Smoothness = lerp(o.Smoothness, _DetailOverlaySmoothness, detailMask * _DetailLayer);
                }
            }
            else
            {
                //o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
                //o.Occlusion = lerp(1.0, tex2D(_OcclusionMap, uv).r, _OcclusionStrength);
            }

			
            // Biome tint
            if (_BiomeLayer > 0.0)
            {
                fixed biomeMask = tex2D(_BiomeLayer_TintMask, uv).r;
                albedo.rgb *= lerp(fixed3(1,1,1), biomeMask, _BiomeLayer);
            }

			
            // Wetness
            fixed wetness = 0.0;
            if (_WetnessLayer > 0.0)
            {
                wetness = tex2D(_WetnessLayer_Mask, uv).r * _WetnessLayer_Wetness;
                albedo.rgb *= lerp(1.0, _WetnessLayer_WetAlbedoScale, wetness);
                o.Smoothness = lerp(o.Smoothness, _WetnessLayer_WetSmoothness, wetness);
            }
            if (_ShoreWetnessLayer > 0.0)
            {
                fixed shoreWetness = tex2D(_WetnessLayer_Mask, uv).r * _ShoreWetnessLayer_BlendFactor;
                shoreWetness = pow(shoreWetness, _ShoreWetnessLayer_BlendFalloff);
                albedo.rgb *= lerp(1.0, _ShoreWetnessLayer_WetAlbedoScale, shoreWetness);
                o.Smoothness = lerp(o.Smoothness, _ShoreWetnessLayer_WetSmoothness, shoreWetness);
                wetness = max(wetness, shoreWetness);
            }
			*/
			
            // Metallic and smoothness
            //fixed4 metallicGloss = tex2D(_MetallicGlossMap, uv);
            //o.Metallic = metallicGloss.r * _Metallic;
            //o.Smoothness = metallicGloss.a * _Glossiness;

            // Emission
            o.Emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
			
            // Final output
            o.Albedo = albedo.rgb;
            //o.Alpha = albedo.a;
        }
        ENDCG
    }
    CustomEditor "RustStandardDecalShaderGUI"
}