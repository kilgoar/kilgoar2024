Shader "Custom/Rust/StandardTerrain"
{
    Properties
    {
        // LOD Parameters
        _LODTransitionDistance("LOD Transition Distance", Range(0, 100)) = 50.0

        // Terrain Override Textures (material-specific)
        [Toggle(USE_TERRAIN_OVERRIDES)] _UseTerrainOverrides("Use Terrain Overrides", Float) = 0.0
        _TerrainOverride_Control0("Override Control 0 (RG)", 2D) = "white" {}
        _TerrainOverride_Control1("Override Control 1 (RG)", 2D) = "white" {}
        _TerrainOverride_Biome("Override Biome", 2D) = "white" {}
        _TerrainOverride_Normal("Override Normal", 2D) = "bump" {}
        _TerrainOverride_Alpha("Override Alpha", 2D) = "white" {}

        // Wetness and Detail Textures
        _WetnessLayer_Mask("Wetness Mask", 2D) = "white" {}
        _PotatoDetailTexture("Potato Detail Texture", 2D) = "grey" {}

        // Layer Properties (0-7 for 8 splat layers)
        _Layer0_Factor("Layer 0 Factor", Float) = 1.0
        _Layer0_Falloff("Layer 0 Falloff", Float) = 1.0
        _Layer0_Metallic("Layer 0 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer0_Smoothness("Layer 0 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer0_SpecularReflectivity("Layer 0 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer1_Factor("Layer 1 Factor", Float) = 1.0
        _Layer1_Falloff("Layer 1 Falloff", Float) = 1.0
        _Layer1_Metallic("Layer 1 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer1_Smoothness("Layer 1 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer1_SpecularReflectivity("Layer 1 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer2_Factor("Layer 2 Factor", Float) = 1.0
        _Layer2_Falloff("Layer 2 Falloff", Float) = 1.0
        _Layer2_Metallic("Layer 2 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer2_Smoothness("Layer 2 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer2_SpecularReflectivity("Layer 2 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer3_Factor("Layer 3 Factor", Float) = 1.0
        _Layer3_Falloff("Layer 3 Falloff", Float) = 1.0
        _Layer3_Metallic("Layer 3 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer3_Smoothness("Layer 3 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer3_SpecularReflectivity("Layer 3 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer4_Factor("Layer 4 Factor", Float) = 1.0
        _Layer4_Falloff("Layer 4 Falloff", Float) = 1.0
        _Layer4_Metallic("Layer 4 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer4_Smoothness("Layer 4 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer4_SpecularReflectivity("Layer 4 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer5_Factor("Layer 5 Factor", Float) = 1.0
        _Layer5_Falloff("Layer 5 Falloff", Float) = 1.0
        _Layer5_Metallic("Layer 5 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer5_Smoothness("Layer 5 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer5_SpecularReflectivity("Layer 5 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer6_Factor("Layer 6 Factor", Float) = 1.0
        _Layer6_Falloff("Layer 6 Falloff", Float) = 1.0
        _Layer6_Metallic("Layer 6 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer6_Smoothness("Layer 6 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer6_SpecularReflectivity("Layer 6 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        _Layer7_Factor("Layer 7 Factor", Float) = 1.0
        _Layer7_Falloff("Layer 7 Falloff", Float) = 1.0
        _Layer7_Metallic("Layer 7 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer7_Smoothness("Layer 7 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer7_SpecularReflectivity("Layer 7 Specular Reflectivity", Range(0.0, 1.0)) = 0.0

        // Fallback Layer (Puddle)
        _LayerFallback_Albedo("Fallback Albedo", Color) = (0.5, 0.5, 0.5, 1.0)
        _LayerFallback_Metallic("Fallback Metallic", Range(0.0, 1.0)) = 0.0
        _LayerFallback_Smoothness("Fallback Smoothness", Range(0.0, 1.0)) = 0.0

        // Wetness Properties
        _WetnessLayer("Wetness Layer", Range(0.0, 1.0)) = 0.0
        _WetnessLayer_Wetness("Wetness Amount", Range(0.0, 1.0)) = 0.0
        _WetnessLayer_WetAlbedoScale("Wet Albedo Scale", Range(0.0, 2.0)) = 0.0
        _WetnessLayer_WetSmoothness("Wet Smoothness", Range(0.0, 1.0)) = 0.0

        // Shore Wetness Properties
        _ShoreWetnessLayer("Shore Wetness Layer", Range(0.0, 1.0)) = 0.0
        _ShoreWetnessLayer_BlendFactor("Shore Blend Factor", Float) = 1.0
        _ShoreWetnessLayer_BlendFalloff("Shore Blend Falloff", Float) = 1.0
        _ShoreWetnessLayer_Range("Shore Range", Float) = 1.0
        _ShoreWetnessLayer_WetAlbedoScale("Shore Wet Albedo Scale", Range(0.0, 2.0)) = 0.5
        _ShoreWetnessLayer_WetSmoothness("Shore Wet Smoothness", Range(0.0, 1.0)) = 0.8

        // Detail Properties
        _PotatoDetailWorldUVScale("Potato Detail UV Scale", Float) = 0.0

        // Rendering Properties
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _CutoffRange("Cutoff Range", Range(0.0, 1.0)) = 0.0
        _DecalLayerMask("Decal Layer Mask", Float) = 0.0
        [Enum(Opaque,0,Cutout,1)] _Mode("Rendering Mode", Float) = 0.0
        [HideInInspector] _SrcBlend("Source Blend", Float) = 1.0
        [HideInInspector] _DstBlend("Destination Blend", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        _TerrainParallax("Terrain Parallax", Float) = 0.0
        _Terrain_Type("Terrain Type", Float) = 0.0
		

    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "TerrainCompatible"="True" }
        LOD 300

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows
        #pragma multi_compile _ ALPHA_TEST
        #pragma multi_compile _ USE_TERRAIN_OVERRIDES
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"

		UNITY_DECLARE_TEX2D(Terrain_Control0);
        UNITY_DECLARE_TEX2D(Terrain_Control1);
        UNITY_DECLARE_TEX2D(Terrain_HeightTexture);
        UNITY_DECLARE_TEX2D(Terrain_Alpha);
        UNITY_DECLARE_TEX2D(Terrain_Biome);
        UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD0);
        UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD1);
        UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD2);
        UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD0);
        UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD1);
        UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD2);


        
		
        // Biome Color Placeholders
        float4 Terrain_Arid0, Terrain_Arid1, Terrain_Arid2, Terrain_Arid3;
        float4 Terrain_Temperate0, Terrain_Temperate1, Terrain_Temperate2, Terrain_Temperate3;
        float4 Terrain_Tundra0, Terrain_Tundra1, Terrain_Tundra2, Terrain_Tundra3;
        float4 Terrain_Arctic0, Terrain_Arctic1, Terrain_Arctic2, Terrain_Arctic3;

        // Global UV Mix Parameters
        float4 UVMixParameter0; // For Dirt (layer 0): (mult, start, distance, unused)

        // Properties
		float Terrain_Tiling[8];
		
        float _LODTransitionDistance;
        float _UseTerrainOverrides;
        float _Cutoff;
        float _CutoffRange;
        float _DecalLayerMask;
        float _SrcBlend;
        float _DstBlend;
        float _ZWrite;
        float _TerrainParallax;
        float _Terrain_Type;
        float _Layer0_Factor, _Layer0_Falloff, _Layer0_Metallic, _Layer0_Smoothness, _Layer0_SpecularReflectivity;
        float _Layer1_Factor, _Layer1_Falloff, _Layer1_Metallic, _Layer1_Smoothness, _Layer1_SpecularReflectivity;
        float _Layer2_Factor, _Layer2_Falloff, _Layer2_Metallic, _Layer2_Smoothness, _Layer2_SpecularReflectivity;
        float _Layer3_Factor, _Layer3_Falloff, _Layer3_Metallic, _Layer3_Smoothness, _Layer3_SpecularReflectivity;
        float _Layer4_Factor, _Layer4_Falloff, _Layer4_Metallic, _Layer4_Smoothness, _Layer4_SpecularReflectivity;
        float _Layer5_Factor, _Layer5_Falloff, _Layer5_Metallic, _Layer5_Smoothness, _Layer5_SpecularReflectivity;
        float _Layer6_Factor, _Layer6_Falloff, _Layer6_Metallic, _Layer6_Smoothness, _Layer6_SpecularReflectivity;
        float _Layer7_Factor, _Layer7_Falloff, _Layer7_Metallic, _Layer7_Smoothness, _Layer7_SpecularReflectivity;
        fixed4 _LayerFallback_Albedo;
        float _LayerFallback_Metallic, _LayerFallback_Smoothness;
        float _WetnessLayer, _WetnessLayer_Wetness, _WetnessLayer_WetAlbedoScale, _WetnessLayer_WetSmoothness;
        float _ShoreWetnessLayer, _ShoreWetnessLayer_BlendFactor, _ShoreWetnessLayer_BlendFalloff;
        float _ShoreWetnessLayer_Range, _ShoreWetnessLayer_WetAlbedoScale, _ShoreWetnessLayer_WetSmoothness;
        float _PotatoDetailWorldUVScale;
        float _Mode;

        // Splat tiling scales from TerrainManager
        float4 _Terrain_TexelSize0; // Splat tiling for layers 0-3
        float4 _Terrain_TexelSize1; // Splat tiling for layers 4-7

        struct Input
        {
            float2 uv_Terrain_Control0; // UVs for global textures
            float2 uv_TerrainOverride_Control0; // UVs for override textures
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Calculate distance to camera for LOD
            float3 cameraPos = _WorldSpaceCameraPos;
            float distance = length(IN.worldPos - cameraPos);

            float2 baseUV = IN.worldPos.xz;
            float uvScale = 1.0 / (Terrain_Tiling[0]); // Inverse for texel size or large scale
            float uvOffset = UVMixParameter0.y; // UV offset
            float2 uv = baseUV * uvScale + uvOffset;
			
            // Sample albedo based on LOD
            float3 albedo;
            if (distance < _LODTransitionDistance * 0.5)
            {
                albedo = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD0, float3(uv, 0)).rgb;
            }
            else if (distance < _LODTransitionDistance)
            {
                // Blend LOD0 and LOD1
                float t = (distance - _LODTransitionDistance * 0.5) / (_LODTransitionDistance * 0.5);
                float3 albedo0 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD0, float3(uv, 0)).rgb;
                float3 albedo1 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD1, float3(uv, 0)).rgb;
                albedo = lerp(albedo0, albedo1, t);
            }
            else
            {
                albedo = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD1, float3(uv, 0));
            }

			albedo *= Terrain_Temperate0;
            // Sample normal based on LOD
			/*
            float3 normal;
            if (distance < _LODTransitionDistance * 0.5)
            {
                normal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD0, float3(uv, 0)));
            }
            else if (distance < _LODTransitionDistance)
            {
                float t = (distance - _LODTransitionDistance * 0.5) / (_LODTransitionDistance * 0.5);
                float3 normal0 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD0, float3(uv, 0)));
                float3 normal1 = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD1, float3(uv, 0)));
                normal = normalize(lerp(normal0, normal1, t));
			}
            else
            {
				normal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD1, float3(uv, 0)));
            }
			
			_Layer0_Factor("Layer 0 Factor", Float) = 1.0
			_Layer0_Falloff("Layer 0 Falloff", Float) = 1.0
			_Layer0_Metallic("Layer 0 Metallic", Range(0.0, 1.0)) = 0.0
			_Layer0_Smoothness("Layer 0 Smoothness", Range(0.0, 1.0)) = 0.5
			_Layer0_SpecularReflectivity("Layer 0 Specular Reflectivity", Range(0.0, 1.0)) = 0.0
			
			*/
			float4 normalTex = UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD0, float3(uv, 0));
			float3 normal = normalTex.rgb;
			
            // Output
            o.Albedo = albedo;
            o.Normal = normal;
			o.Metallic = _Layer0_Metallic;
            //o.Smoothness = _Layer0_Smoothness;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    Fallback "Diffuse"
    CustomEditor "RustStandardTerrainShaderGUI"
}