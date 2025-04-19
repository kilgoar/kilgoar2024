Shader "Custom/Rust/StandardBlendLayer"
{
    Properties
    {
        // Main Properties
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionUVSet("Occlusion UV Set", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _MainTexScroll("Main Tex Scroll", Vector) = (0,0,0,0)

        // Detail Properties
        _DetailBlendLayer("Enable Detail Layer", Float) = 0.0
        [Toggle] _HasDetailMask("Has Detail Mask", Float) = 0.0
        _DetailAlbedoMap("Detail Albedo", 2D) = "grey" {}
        _DetailMetallicGlossMap("Detail Metallic", 2D) = "white" {}
        _DetailMetallic("Detail Metallic", Range(0.0, 1.0)) = 0.0
        _DetailGlossiness("Detail Smoothness", Range(0.0, 1.0)) = 0.5
        [Normal] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailTintMap("Detail Tint Map", 2D) = "white" {}
        _DetailBlendMaskMap("Detail Blend Mask", 2D) = "white" {}
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _DetailBlendFactor("Detail Blend Factor", Float) = 0.0
        _DetailBlendFalloff("Detail Blend Falloff", Float) = 0.0
        _DetailAlbedoMapScroll("Detail Albedo Scroll", Vector) = (0,0,0,0)
        _DetailBlendMaskMapScroll("Detail Blend Mask Scroll", Vector) = (0,0,0,0)
        _DetailBlendMaskMapInvert("Invert Blend Mask", Range(0.0, 1.0)) = 0.0
        _DetailBlendMaskUVSet("Blend Mask UV Set", Float) = 0.0
        _DetailBlendMaskAddLowFreq("Add Low Frequency", Float) = 0.0
        _DetailBlendMaskVertexSource("Use Vertex Alpha for Mask", Float) = 0.0
        _DetailTintSource("Enable Tint", Float) = 0.0
        _DetailTintBlockSize("Tint Block Size", Float) = 1.0
        _DetailUseWorldXZ("Use World XZ for Detail", Float) = 0.0

        // Biome Properties
        _BiomeLayer("Enable Biome Layer", Float) = 0.0
        _BiomeLayer_TintSplatIndex("Biome Tint Splat Index", Float) = 0.0

        // Vertex Properties
        _ApplyVertexAlpha("Apply Vertex Alpha", Range(0.0, 1.0)) = 0.0
        _ApplyVertexAlphaStrength("Vertex Alpha Strength", Range(0.0, 1.0)) = 0.0
        _ApplyVertexColor("Apply Vertex Color", Range(0.0, 1.0)) = 0.0
        _ApplyVertexColorStrength("Vertex Color Strength", Range(0.0, 1.0)) = 0.0

        // Advanced Properties
        _UVSec("Secondary UV Set", Float) = 0.0
        [Enum(Opaque,0,Cutout,1)] _Mode("Rendering Mode", Float) = 0.0
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1.0
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        _DecalLayerMask("Decal Layer Mask", Float) = 0.0
        _EnvReflHorizonFade("Env Reflection Horizon Fade", Float) = 0.0
        _EnvReflOcclusionStrength("Env Reflection Occlusion Strength", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #pragma multi_compile _ ALPHA_TEST
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

        // Samplers
        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;
        sampler2D _DetailAlbedoMap;
        sampler2D _DetailMetallicGlossMap;
        sampler2D _DetailNormalMap;
        sampler2D _DetailTintMap;
        sampler2D _DetailBlendMaskMap;

        // Properties
        fixed4 _Color;
        float _Cutoff;
        float _Glossiness;
        float _Metallic;
        float _BumpScale;
        float _OcclusionStrength;
        float _OcclusionUVSet;
        fixed4 _EmissionColor;
        float4 _MainTexScroll;
        float _DetailBlendLayer;
        float _HasDetailMask;
        fixed4 _DetailColor;
        float _DetailMetallic;
        float _DetailGlossiness;
        float _DetailNormalMapScale;
        float _DetailBlendFactor;
        float _DetailBlendFalloff;
        float4 _DetailAlbedoMapScroll;
        float4 _DetailBlendMaskMapScroll;
        float _DetailBlendMaskMapInvert;
        float _DetailBlendMaskUVSet;
        float _DetailBlendMaskAddLowFreq;
        float _DetailBlendMaskVertexSource;
        float _DetailTintSource;
        float _DetailTintBlockSize;
        float _DetailUseWorldXZ;
        float _BiomeLayer;
        float _BiomeLayer_TintSplatIndex;
        float _ApplyVertexAlpha;
        float _ApplyVertexAlphaStrength;
        float _ApplyVertexColor;
        float _ApplyVertexColorStrength;
        float _UVSec;
        float _Mode;
        float _SrcBlend;
        float _DstBlend;
        float _ZWrite;
        float _Cull;
        float _DecalLayerMask;
        float _EnvReflHorizonFade;
        float _EnvReflOcclusionStrength;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_MainTex;
            float3 worldPos;
            fixed4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // UVs
            float2 mainUV = IN.uv_MainTex + _MainTexScroll.xy * _Time.y;
            float2 secondaryUV = IN.uv2_MainTex;
            float2 occlusionUV = (_OcclusionUVSet > 0.0) ? secondaryUV : mainUV;
            float2 detailUV = (_DetailUseWorldXZ > 0.0) ? IN.worldPos.xz : ((_DetailBlendMaskUVSet > 0.0) ? secondaryUV : mainUV);
            detailUV += _DetailAlbedoMapScroll.xy * _Time.y;
            float2 detailMaskUV = (_DetailBlendMaskUVSet > 0.0) ? secondaryUV : mainUV;
            //detailMaskUV += _DetailBlendMaskMapScroll.xy * _Time.y;

			
            // Main Textures
            fixed4 albedo = tex2D(_MainTex, mainUV) * _Color;
            fixed4 metallicGloss = tex2D(_MetallicGlossMap, mainUV);
            float metallic = metallicGloss.r * _Metallic;
            float smoothness = metallicGloss.a * _Glossiness;
            float3 normal = UnpackScaleNormal(tex2D(_BumpMap, mainUV), _BumpScale);
            float occlusion = lerp(1.0, tex2D(_OcclusionMap, occlusionUV).r, _OcclusionStrength);
            fixed3 emission = tex2D(_EmissionMap, mainUV).rgb * _EmissionColor.rgb;

            // Apply Vertex Color
            if (_ApplyVertexColor > 0.0)
            {
                albedo.rgb *= lerp(fixed3(1,1,1), IN.color.rgb, _ApplyVertexColorStrength);
            }

            // Apply Vertex Alpha
            if (_ApplyVertexAlpha > 0.0)
            {
                albedo.a *= lerp(1.0, IN.color.a, _ApplyVertexAlphaStrength);
            }

            // Detail Layer
            if (_DetailBlendLayer > 0.0)
            {
                fixed4 detailAlbedo = tex2D(_DetailAlbedoMap, detailUV) * _DetailColor;
                // Skip default grey texture (RGB ≈ 0.5)

                    float detailMask = 0.0; // Default to full blending
                    detailMask = tex2D(_DetailBlendMaskMap, detailMaskUV).r;
					detailMask = saturate(detailMask / _DetailBlendFactor);
					
					
					if(_DetailBlendMaskMapInvert)
					{
						detailMask = 1.0 - detailMask;
					}


                    // Detail properties
                    fixed4 detailMetallicGloss = tex2D(_DetailMetallicGlossMap, detailUV);
                    float detailMetallic = detailMetallicGloss.r * _DetailMetallic;
                    float detailSmoothness = detailMetallicGloss.a * _DetailGlossiness;
                    float3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalMap, detailUV), _DetailNormalMapScale);
                    // Apply low-frequency mask (placeholder, as implementation is unclear)


					
                    albedo.rgb = lerp(albedo.rgb, detailAlbedo.rgb, detailMask);
                    metallic = lerp(metallic, detailMetallic, detailMask);
                    smoothness = lerp(smoothness, detailSmoothness, detailMask);
                    normal = lerp(normal, detailNormal, detailMask);
                
            }

            // Biome Layer (placeholder, as tint map is missing)
            if (_BiomeLayer > 0.0)
            {
                // TODO: Implement biome tinting (requires tint map or splat logic)
                albedo.rgb = albedo.rgb; // No-op for now
            }

            // Output
            o.Albedo = albedo.rgb;
            o.Metallic = metallic;
            o.Smoothness = smoothness;
            o.Normal = normal;
            o.Occlusion = occlusion;
            o.Emission = emission;
            o.Alpha = albedo.a;

            //clip(albedo.a - _Cutoff);
        }
        ENDCG
    }

    CustomEditor "RustStandardBlendLayerShaderGUI"
}