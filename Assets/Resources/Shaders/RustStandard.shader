Shader "Custom/Rust/Standard"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 1.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.3
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _SpecGlossMap("Specular Gloss Map", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _TangentMap("Tangent Map", 2D) = "bump" {}
        _AnisotropyMap("Anisotropy Map", 2D) = "black" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0.2,0.2,0.2)
        _EmissionMap("Emission", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "black" {}
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        _DetailMetallicGlossMap("Detail Metallic", 2D) = "white" {}
        _DetailSpecGlossMap("Detail Specular", 2D) = "white" {}
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailOcclusionMap("Detail Occlusion", 2D) = "white" {}
        _DetailOcclusionStrength("Detail Occlusion Strength", Float) = 0.0
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailLayer("Detail Layer", Float) = 0.0
        _ParticleLayer_AlbedoMap("Particle Albedo", 2D) = "white" {}
        _ParticleLayer_AlbedoColor("Particle Albedo Color", Color) = (1,1,1,1)
        _ParticleLayer_MetallicGlossMap("Particle Metallic", 2D) = "white" {}
        _ParticleLayer_SpecGlossMap("Particle Specular", 2D) = "white" {}
        [Normal] _ParticleLayer_NormalMap("Particle Normal", 2D) = "bump" {}
        _ParticleLayer_BlendFactor("Particle Blend Factor", Float) = 0.0
        _ParticleLayer_BlendFalloff("Particle Blend Falloff", Float) = 0.0
        _ParticleLayer_Glossiness("Particle Smoothness", Range(0.0, 1.0)) = 0.5
        _ParticleLayer_Metallic("Particle Metallic", Range(0.0, 1.0)) = 0.0
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
        // ... other properties unchanged
    }
	

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #pragma shader_feature_local _TANGENTMAP
        #pragma shader_feature_local _ANISOTROPYMAP

        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Core samplers
        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
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
        sampler2D _DetailMetallicGlossMap;
        sampler2D _DetailSpecGlossMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailOcclusionMap;

        // Particle samplers
        sampler2D _ParticleLayer_AlbedoMap;
        sampler2D _ParticleLayer_MetallicGlossMap;
        sampler2D _ParticleLayer_SpecGlossMap;
        sampler2D _ParticleLayer_NormalMap;

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
        fixed4 _ParticleLayer_AlbedoColor;
        float _ParticleLayer_BlendFactor;
        float _ParticleLayer_BlendFalloff;
        float _ParticleLayer_Glossiness;
        float _ParticleLayer_Metallic;
        float _ParticleLayer_NormalScale;
        float _ParticleLayer_Thickness;
        fixed4 _ParticleLayer_SpecColor;
        float4 _ParticleLayer_WorldDirection;
        float4 _ParticleLayer_MapTiling;
        float _ParticleLayer;
        fixed4 _BiomeLayer_TintColor;
        float _BiomeLayer;
        float _BiomeLayer_TintSplatIndex;
        float _WetnessLayer;
        float _WetnessLayer_WetAlbedoScale;
        float _WetnessLayer_WetSmoothness;
        float _WetnessLayer_Wetness;
        float _ShoreWetnessLayer;
        float _ShoreWetnessLayer_BlendFactor;
        float _ShoreWetnessLayer_BlendFalloff;
        float _ShoreWetnessLayer_Range;
        float _ShoreWetnessLayer_WetAlbedoScale;
        float _ShoreWetnessLayer_WetSmoothness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            fixed4 albedo = tex2D(_MainTex, uv) * _Color;


            // Detail pass (controlled by _DetailLayer)
		if (_DetailLayer > 0.0)
		{
			fixed detailMask = tex2D(_DetailMask, uv).rgb;
			fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, uv);
			
			// Check if detailMask is effectively uniform (close to 0 or 1)
			// We'll use a threshold to decide if the mask is "flat"
			if (detailMask == 0) // If detailMask is nearly 0 or 1
			{
			
			}
			else
			{
				albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb * _DetailColor, detailMask);
			}
			
			o.Normal = UnpackScaleNormal(tex2D(_DetailNormalMap, uv), _DetailNormalMapScale);
			//o.Occlusion = lerp(1.0, tex2D(_DetailOcclusionMap, uv).r, _DetailOcclusionStrength);
		}
			

			/*
            // Wetness
            if (_WetnessLayer > 0.0)
            {
                fixed wetness = tex2D(_WetnessLayer_Mask, uv).r * _WetnessLayer_Wetness;
                wetness = wetness > 0 ? wetness : 0.0;
                albedo.rgb *= lerp(1.0, _WetnessLayer_WetAlbedoScale, wetness);
                o.Smoothness = lerp(o.Smoothness, _WetnessLayer_WetSmoothness, wetness);
            }
            if (_ShoreWetnessLayer > 0.0)
            {
                fixed shoreWetness = tex2D(_WetnessLayer_Mask, uv).r * _ShoreWetnessLayer * _ShoreWetnessLayer_BlendFactor;
                shoreWetness = shoreWetness > 0 ? shoreWetness : 0.0;
                albedo.rgb *= lerp(1.0, _ShoreWetnessLayer_WetAlbedoScale, shoreWetness);
                o.Smoothness = lerp(o.Smoothness, _ShoreWetnessLayer_WetSmoothness, shoreWetness);
            }
			*/
			o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
            o.Occlusion = lerp(1.0, tex2D(_DetailOcclusionMap, uv).r, _DetailOcclusionStrength);
            // Use SpecGlossMap for Metallic and Smoothness
            fixed4 specGloss = tex2D(_SpecGlossMap, uv);
            //o.Metallic = specGloss.rgb; // Fully control Metallic with red channel
            //o.Smoothness = specGloss.rgb; // Use alpha channel for Smoothness


            o.Albedo = albedo.rgb;
            o.Emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
            o.Alpha = albedo.a; // Use alpha for blending
			clip(albedo.a - _Cutoff);
        }
        ENDCG
    }
    CustomEditor "RustStandardShaderGUI"
}