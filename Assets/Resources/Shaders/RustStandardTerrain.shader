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
		_BiomeMode("Biome Rendering mode", Float) = 0.0
		_TopologyMode("Biome Rendering mode", Float) = -1.0
		_PreviewMode("Preview mode", float) = 0.0
		_BrushStrength("Brush Strength", float) = 1.0
		_TerrainTarget("Terrain Target", float) = 0
		
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

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard vertex:vert
        #pragma multi_compile _ ALPHA_TEST
		#define TERRAIN_SPLATMAP_COMMON_CGINC_INCLUDED
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "AutoLight.cginc"
        #include "TerrainSplatmapCommon.cginc"
		

		UNITY_DECLARE_TEX2D(Terrain_Control0);
		UNITY_DECLARE_TEX2D(Terrain_Control1);
		UNITY_DECLARE_TEX2D(Terrain_HeightTexture);
		UNITY_DECLARE_TEX2D(Terrain_Alpha);
		UNITY_DECLARE_TEX2D(Terrain_Biome);
		UNITY_DECLARE_TEX2D(Terrain_Biome1);
		UNITY_DECLARE_TEX2D(Terrain_Topologies);
		UNITY_DECLARE_TEX2D(Terrain_Preview);
		UNITY_DECLARE_TEX2D(Terrain_BlendMap);
		
		UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD0);
		UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD1);
		UNITY_DECLARE_TEX2DARRAY(Terrain_AlbedoArray_LOD2);
		UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD0);
		UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD1);
		UNITY_DECLARE_TEX2DARRAY(Terrain_NormalArray_LOD2);
		
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
		float _BiomeMode;
		float _TopologyMode;
		float _PreviewMode;
		float _BrushStrength;
		float _TerrainTarget;
		

        float4 BiomeColors[40];
        // Arid biome colors (8 layers)
        float4 Splat0_AridColor, Splat1_AridColor, Splat2_AridColor, Splat3_AridColor,
               Splat4_AridColor, Splat5_AridColor, Splat6_AridColor, Splat7_AridColor;
        // Temperate biome colors (8 layers)
        float4 Splat0_TemperateColor, Splat1_TemperateColor, Splat2_TemperateColor, Splat3_TemperateColor,
               Splat4_TemperateColor, Splat5_TemperateColor, Splat6_TemperateColor, Splat7_TemperateColor;
        // Tundra biome colors (8 layers)
        float4 Splat0_TundraColor, Splat1_TundraColor, Splat2_TundraColor, Splat3_TundraColor,
               Splat4_TundraColor, Splat5_TundraColor, Splat6_TundraColor, Splat7_TundraColor;
        // Arctic biome colors (8 layers)
        float4 Splat0_ArcticColor, Splat1_ArcticColor, Splat2_ArcticColor, Splat3_ArcticColor,
               Splat4_ArcticColor, Splat5_ArcticColor, Splat6_ArcticColor, Splat7_ArcticColor;
        // Jungle biome colors (8 layers)
        float4 Splat0_JungleColor, Splat1_JungleColor, Splat2_JungleColor, Splat3_JungleColor,
               Splat4_JungleColor, Splat5_JungleColor, Splat6_JungleColor, Splat7_JungleColor;
			   
			   


		
		float _Layer0_Factor, _Layer0_Falloff, _Layer0_Metallic, _Layer0_Smoothness, _Layer0_SpecularReflectivity;
		float _Layer1_Factor, _Layer1_Falloff, _Layer1_Metallic, _Layer1_Smoothness, _Layer1_SpecularReflectivity;
		float _Layer2_Factor, _Layer2_Falloff, _Layer2_Metallic, _Layer2_Smoothness, _Layer2_SpecularReflectivity;
		float _Layer3_Factor, _Layer3_Falloff, _Layer3_Metallic, _Layer3_Smoothness, _Layer3_SpecularReflectivity;
		float _Layer4_Factor, _Layer4_Falloff, _Layer4_Metallic, _Layer4_Smoothness, _Layer4_SpecularReflectivity;
		float _Layer5_Factor, _Layer5_Falloff, _Layer5_Metallic, _Layer5_Smoothness, _Layer5_SpecularReflectivity;
		float _Layer6_Factor, _Layer6_Falloff, _Layer6_Metallic, _Layer6_Smoothness, _Layer6_SpecularReflectivity;
		float _Layer7_Factor, _Layer7_Falloff, _Layer7_Metallic, _Layer7_Smoothness, _Layer7_SpecularReflectivity;
 
		float LayerFactors[8];
        sampler2D _Control0, _Control1;

		float4 _Terrain_TexelSize0; 
		float4 _Terrain_TexelSize1; 


        struct Input
        {
            float2 tc_Control0;// Terrain control UVs
            float3 worldPos;
            float4 terrainData; // Additional terrain data
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Compute world position
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            o.worldPos = worldPos.xyz;

            // Set up terrain UVs for control textures
            o.tc_Control0 = v.texcoord.xy; // Typically, terrain control UVs are in texcoord0

            // Initialize terrain data to avoid undefined behavior
            o.terrainData = float4(v.texcoord.xy, 0, 0); // Example: store control UVs or placeholder

            // Ensure tangents and normals are passed correctly for lighting
            v.tangent = float4(cross(v.normal, float3(0, 0, 1)), 1.0); // Simple tangent for flat terrain
			
			float preview = 0.0;
			if(_PreviewMode > 1.5){
				 preview = UNITY_SAMPLE_TEX2D_LOD(Terrain_Preview, v.texcoord.xy, 0).r;
			}
			
			//_PreviewMode 1 > no displacement
			
            if (_PreviewMode > 1.5 && _PreviewMode < 2.5) {     //2              
                v.vertex.y += preview * 1000 * _BrushStrength;
            }
			if (_PreviewMode > 2.5 && _PreviewMode < 3.5){ 		//3		
                v.vertex.y -= preview * 1000 * _BrushStrength;
			}
			if (_PreviewMode > 3.5 && _PreviewMode < 4.5){     //4
				

			}
			if (_PreviewMode > 4.5 && _PreviewMode < 5.5){      //5			
				// Lerp vertex y toward _TerrainTarget
				float influence = preview * _BrushStrength * 5.0;
				v.vertex.y = lerp(v.vertex.y, _TerrainTarget * 1000.0, influence); // Scale target to vertex units
			}
			
        }
		
		
        int GetTopologyBitmask(float2 uv)
        {
            float4 color = UNITY_SAMPLE_TEX2D(Terrain_Topologies, uv);
            int r = floor(color.r * 255.0);
            int g = floor(color.g * 255.0);
            int b = floor(color.b * 255.0);
            int a = floor(color.a * 255.0);
            int bitmask = (a << 24) | (b << 16) | (g << 8) | r;
            return bitmask;
        }
		
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
				float3 bio[5];
				bio[0] = float3(1.0,0.6,0.0);
				bio[1] = float3(0.0,0.3,0.0);
				bio[2] = float3(0.7,0.25,0.0);
				bio[3] = float3(1.0,1.0,1.0);
				bio[4] = float3(1.0,0.0,0.0);
			
			float4 control0 = UNITY_SAMPLE_TEX2D(Terrain_Control0, IN.tc_Control0);
			float4 control1 = UNITY_SAMPLE_TEX2D(Terrain_Control1, IN.tc_Control0);
			
			float3 cameraPos = _WorldSpaceCameraPos;
			float distance = length(IN.worldPos - cameraPos);
			
			float controlWeights[8];
			controlWeights[0] = control0.r;
			controlWeights[1] = control0.g;
			controlWeights[2] = control0.b;
			controlWeights[3] = control0.a;
			controlWeights[4] = control1.r;
			controlWeights[5] = control1.g;
			controlWeights[6] = control1.b;
			controlWeights[7] = control1.a;
			
			LayerFactors[0] = _Layer0_Factor;
			LayerFactors[1] = _Layer1_Factor;
			LayerFactors[2] = _Layer2_Factor;
			LayerFactors[3] = _Layer3_Factor;
			LayerFactors[4] = _Layer4_Factor;
			LayerFactors[5] = _Layer5_Factor;
			LayerFactors[6] = _Layer6_Factor;
			LayerFactors[7] = _Layer7_Factor;

	// Biome 0: Arid (8 layers)
	BiomeColors[0] = Splat0_AridColor; BiomeColors[1] = Splat1_AridColor; BiomeColors[2] = Splat2_AridColor;
	BiomeColors[3] = Splat3_AridColor; BiomeColors[4] = Splat4_AridColor; BiomeColors[5] = Splat5_AridColor;
	BiomeColors[6] = Splat6_AridColor; BiomeColors[7] = Splat7_AridColor;
	// Biome 1: Temperate (8 layers)
	BiomeColors[8] = Splat0_TemperateColor; BiomeColors[9] = Splat1_TemperateColor; BiomeColors[10] = Splat2_TemperateColor;
	BiomeColors[11] = Splat3_TemperateColor; BiomeColors[12] = Splat4_TemperateColor; BiomeColors[13] = Splat5_TemperateColor;
	BiomeColors[14] = Splat6_TemperateColor; BiomeColors[15] = Splat7_TemperateColor;
	// Biome 2: Tundra (8 layers)
	BiomeColors[16] = Splat0_TundraColor; BiomeColors[17] = Splat1_TundraColor; BiomeColors[18] = Splat2_TundraColor;
	BiomeColors[19] = Splat3_TundraColor; BiomeColors[20] = Splat4_TundraColor; BiomeColors[21] = Splat5_TundraColor;
	BiomeColors[22] = Splat6_TundraColor; BiomeColors[23] = Splat7_TundraColor;
	// Biome 3: Arctic (8 layers)
	BiomeColors[24] = Splat0_ArcticColor; BiomeColors[25] = Splat1_ArcticColor; BiomeColors[26] = Splat2_ArcticColor;
	BiomeColors[27] = Splat3_ArcticColor; BiomeColors[28] = Splat4_ArcticColor; BiomeColors[29] = Splat5_ArcticColor;
	BiomeColors[30] = Splat6_ArcticColor; BiomeColors[31] = Splat7_ArcticColor;
	// Biome 4: Jungle (8 layers)
	BiomeColors[32] = Splat0_JungleColor; BiomeColors[33] = Splat1_JungleColor; BiomeColors[34] = Splat2_JungleColor;
	BiomeColors[35] = Splat3_JungleColor; BiomeColors[36] = Splat4_JungleColor; BiomeColors[37] = Splat5_JungleColor;
	BiomeColors[38] = Splat6_JungleColor; BiomeColors[39] = Splat7_JungleColor;

		// Initialize output values
		float3 albedo = float3(0, 0, 0);
		float3 normal = float3(0, 0, 0);
		float metallic = 0.0;
		float smoothness = 0.0;
		float specularReflectivity = 0.0;
		float alpha = 1.0;
		float totalWeight = 0.0;
		
		
		            // Sample biome weights from Terrain_Biome and Terrain_Biome1
        float4 biomeWeights0 = UNITY_SAMPLE_TEX2D(Terrain_Biome, IN.tc_Control0); // RGBA for biomes 0-3
        //float jungleWeight = UNITY_SAMPLE_TEX2D(Terrain_Biome1, IN.tc_Control0).r; // R for biome 4 (Jungle)
				
		alpha = UNITY_SAMPLE_TEX2D(Terrain_Alpha, IN.tc_Control0).a;
        
		
		
		
            float biomeWeights[5];
            biomeWeights[0] = biomeWeights0.r; // Arid
            biomeWeights[1] = biomeWeights0.g; // Temperate
            biomeWeights[2] = biomeWeights0.b; // Tundra
            biomeWeights[3] = biomeWeights0.a; // Arctic
			
			float jungleWeight = 1.0 - (biomeWeights[0] + biomeWeights[1] +biomeWeights[2] +biomeWeights[3]);
			
            biomeWeights[4] = jungleWeight;    // Jungle
		
		

		// Determine biome index (assuming terrainData.x encodes biome info, 0-4 for Arid, Temperate, Tundra, Arctic, Jungle)
		int biomeIndex = clamp(floor(IN.terrainData.x * 5.0), 0, 4); // Map to 0-4

		// LOD transition factor
		float lodFactor = saturate(distance / _LODTransitionDistance);
		lodFactor = pow(lodFactor, 2.0); // Smooth transition curve

		// Layer properties array for metallic and smoothness
		float layerMetallic[8] = { _Layer0_Metallic, _Layer1_Metallic, _Layer2_Metallic, _Layer3_Metallic, 
								  _Layer4_Metallic, _Layer5_Metallic, _Layer6_Metallic, _Layer7_Metallic };
		float layerSmoothness[8] = { _Layer0_Smoothness, _Layer1_Smoothness, _Layer2_Smoothness, _Layer3_Smoothness, 
									_Layer4_Smoothness, _Layer5_Smoothness, _Layer6_Smoothness, _Layer7_Smoothness };
		float layerSpecular[8] = { _Layer0_SpecularReflectivity, _Layer1_SpecularReflectivity, _Layer2_SpecularReflectivity, 
								  _Layer3_SpecularReflectivity, _Layer4_SpecularReflectivity, _Layer5_SpecularReflectivity, 
								  _Layer6_SpecularReflectivity, _Layer7_SpecularReflectivity };
		float layerFalloff[8] = { _Layer0_Falloff, _Layer1_Falloff, _Layer2_Falloff, _Layer3_Falloff, 
								 _Layer4_Falloff, _Layer5_Falloff, _Layer6_Falloff, _Layer7_Falloff };

		
		
		
		for (int i = 0; i < 8; i++)
		{
                float2 worldUV = IN.worldPos.xz; // Already in xz plane from vert shader
                float tilingFrequency = Terrain_Tiling[i];
                float2 tiledUV = worldUV / tilingFrequency;	
				
			float3 layerAlbedo;
            
			if (lodFactor <= 0.5)
                    {
                        // LOD0 to LOD1 transition
                        float3 albedoLOD0 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD0, float3(tiledUV, i)).rgb;
                        float3 albedoLOD1 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD1, float3(tiledUV, i)).rgb;
                        layerAlbedo = lerp(albedoLOD0, albedoLOD1, lodFactor * 2.0);
                    }
                    else
                    {
                        // LOD1 to LOD2 transition
                        float3 albedoLOD1 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD1, float3(tiledUV, i)).rgb;
                        float3 albedoLOD2 = UNITY_SAMPLE_TEX2DARRAY(Terrain_AlbedoArray_LOD2, float3(tiledUV, i)).rgb;
                        layerAlbedo = lerp(albedoLOD1, albedoLOD2, (lodFactor - 0.5) * 2.0);
                    }
			
            float3 layerNormal;
					if (lodFactor <= 0.5)
					{
						float4 normalLOD0 = UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD0, float3(tiledUV, i));
						float4 normalLOD1 = UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD1, float3(tiledUV, i));
						layerNormal = UnpackNormal(lerp(normalLOD0, normalLOD1, lodFactor * 2.0));
					}
					else
					{
						float4 normalLOD1 = UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD1, float3(tiledUV, i));
						float4 normalLOD2 = UNITY_SAMPLE_TEX2DARRAY(Terrain_NormalArray_LOD2, float3(tiledUV, i));
						layerNormal = UnpackNormal(lerp(normalLOD1, normalLOD2, (lodFactor - 0.5) * 2.0));
					}
			
			
			float weight = controlWeights[i];
			
			normal += weight * layerNormal;
				
			float3 biomeColor = float3(0, 0, 0);
			float totalBiomeWeight = 0.0;
			for (int b = 0; b < 5; b++)
			{
				biomeColor += biomeWeights[b] * GammaToLinearSpace(BiomeColors[b * 8 + i]);
			}
			
				float3 tintedAlbedo = layerAlbedo * biomeColor;
				albedo += tintedAlbedo * weight;

		}
		
		if(_BiomeMode > 0.5){
			
			albedo = float3(0,0,0);
				for (int b = 0; b < 5; b++)
				{
					albedo += bio[b] * biomeWeights[b];
				}
		}
		
        // Topology mode override
        if (_TopologyMode > -0.5) // Check if topology mode is enabled (>= 0)
        {
            int topologyBitmask = GetTopologyBitmask(IN.tc_Control0);
            int layerIndex = floor(_TopologyMode); // _TopologyMode is the layer index (0-31)
            bool layerPresent = topologyBitmask & (1 << layerIndex);
            if (layerPresent)
            {
                // Apply a green overlay (blend with existing albedo for a friendly effect)
                float3 greenOverlay = float3(0.1, 0.8, 0.0); // Bright green
                albedo = greenOverlay; 
            }
        }
		
		if (_PreviewMode < -.5)		{
			float blendMap = UNITY_SAMPLE_TEX2D(Terrain_BlendMap, IN.tc_Control0);
			albedo.rgb *= blendMap;
		}
		
		if (_PreviewMode > .5 || _PreviewMode < .5)		{
			float preview = UNITY_SAMPLE_TEX2D(Terrain_Preview, IN.tc_Control0);
			
			albedo.rgb = lerp(albedo.rgb, float3(.1, .8, 0), preview * 100 * _BrushStrength);
		}

		
		normalize(normal);
		clip(alpha - _Cutoff);	
        //o.Normal = normal;
		o.Albedo = albedo;
		
		//o.Albedo = control1.rgb;
	}

        ENDCG
    }

    Fallback "Diffuse"
    CustomEditor "RustStandardTerrainShaderGUI"
}