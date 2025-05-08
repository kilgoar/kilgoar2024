#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RustStandardTerrainShaderGUI : ShaderGUI
{
    private bool showGlobalTextures = true;  // Foldout for global textures
    private bool showGlobalParameters = true; // Foldout for UV mix and biome colors
    private bool showOverrides = true;       // Foldout for override textures
    private bool showLayers = true;          // Foldout for layer properties
    private bool showWetness = false;        // Foldout for wetness properties
    private bool showDetail = false;         // Foldout for detail properties
    private bool showRendering = false;      // Foldout for rendering properties

    // Foldouts for each texture array
    private bool[] showTextureArrays = new bool[6]; // One for each LOD albedo and normal array

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Find Material Properties
        MaterialProperty _UseTerrainOverrides = FindProperty("_UseTerrainOverrides", properties);
        MaterialProperty _TerrainOverride_Control0 = FindProperty("_TerrainOverride_Control0", properties);
        MaterialProperty _TerrainOverride_Control1 = FindProperty("_TerrainOverride_Control1", properties);
        MaterialProperty _TerrainOverride_Biome = FindProperty("_TerrainOverride_Biome", properties);
        MaterialProperty _TerrainOverride_Normal = FindProperty("_TerrainOverride_Normal", properties);
        MaterialProperty _TerrainOverride_Alpha = FindProperty("_TerrainOverride_Alpha", properties);

        MaterialProperty _WetnessLayer_Mask = FindProperty("_WetnessLayer_Mask", properties);
        MaterialProperty _PotatoDetailTexture = FindProperty("_PotatoDetailTexture", properties);

        MaterialProperty _Layer0_Factor = FindProperty("_Layer0_Factor", properties);
        MaterialProperty _Layer0_Falloff = FindProperty("_Layer0_Falloff", properties);
        MaterialProperty _Layer0_Metallic = FindProperty("_Layer0_Metallic", properties);
        MaterialProperty _Layer0_Smoothness = FindProperty("_Layer0_Smoothness", properties);
        MaterialProperty _Layer0_SpecularReflectivity = FindProperty("_Layer0_SpecularReflectivity", properties);

        MaterialProperty _Layer1_Factor = FindProperty("_Layer1_Factor", properties);
        MaterialProperty _Layer1_Falloff = FindProperty("_Layer1_Falloff", properties);
        MaterialProperty _Layer1_Metallic = FindProperty("_Layer1_Metallic", properties);
        MaterialProperty _Layer1_Smoothness = FindProperty("_Layer1_Smoothness", properties);
        MaterialProperty _Layer1_SpecularReflectivity = FindProperty("_Layer1_SpecularReflectivity", properties);

        MaterialProperty _Layer2_Factor = FindProperty("_Layer2_Factor", properties);
        MaterialProperty _Layer2_Falloff = FindProperty("_Layer2_Falloff", properties);
        MaterialProperty _Layer2_Metallic = FindProperty("_Layer2_Metallic", properties);
        MaterialProperty _Layer2_Smoothness = FindProperty("_Layer2_Smoothness", properties);
        MaterialProperty _Layer2_SpecularReflectivity = FindProperty("_Layer2_SpecularReflectivity", properties);

        MaterialProperty _Layer3_Factor = FindProperty("_Layer3_Factor", properties);
        MaterialProperty _Layer3_Falloff = FindProperty("_Layer3_Falloff", properties);
        MaterialProperty _Layer3_Metallic = FindProperty("_Layer3_Metallic", properties);
        MaterialProperty _Layer3_Smoothness = FindProperty("_Layer3_Smoothness", properties);
        MaterialProperty _Layer3_SpecularReflectivity = FindProperty("_Layer3_SpecularReflectivity", properties);

        MaterialProperty _Layer4_Factor = FindProperty("_Layer4_Factor", properties);
        MaterialProperty _Layer4_Falloff = FindProperty("_Layer4_Falloff", properties);
        MaterialProperty _Layer4_Metallic = FindProperty("_Layer4_Metallic", properties);
        MaterialProperty _Layer4_Smoothness = FindProperty("_Layer4_Smoothness", properties);
        MaterialProperty _Layer4_SpecularReflectivity = FindProperty("_Layer4_SpecularReflectivity", properties);

        MaterialProperty _Layer5_Factor = FindProperty("_Layer5_Factor", properties);
        MaterialProperty _Layer5_Falloff = FindProperty("_Layer5_Falloff", properties);
        MaterialProperty _Layer5_Metallic = FindProperty("_Layer5_Metallic", properties);
        MaterialProperty _Layer5_Smoothness = FindProperty("_Layer5_Smoothness", properties);
        MaterialProperty _Layer5_SpecularReflectivity = FindProperty("_Layer5_SpecularReflectivity", properties);

        MaterialProperty _Layer6_Factor = FindProperty("_Layer6_Factor", properties);
        MaterialProperty _Layer6_Falloff = FindProperty("_Layer6_Falloff", properties);
        MaterialProperty _Layer6_Metallic = FindProperty("_Layer6_Metallic", properties);
        MaterialProperty _Layer6_Smoothness = FindProperty("_Layer6_Smoothness", properties);
        MaterialProperty _Layer6_SpecularReflectivity = FindProperty("_Layer6_SpecularReflectivity", properties);

        MaterialProperty _Layer7_Factor = FindProperty("_Layer7_Factor", properties);
        MaterialProperty _Layer7_Falloff = FindProperty("_Layer7_Falloff", properties);
        MaterialProperty _Layer7_Metallic = FindProperty("_Layer7_Metallic", properties);
        MaterialProperty _Layer7_Smoothness = FindProperty("_Layer7_Smoothness", properties);
        MaterialProperty _Layer7_SpecularReflectivity = FindProperty("_Layer7_SpecularReflectivity", properties);

        MaterialProperty _LayerFallback_Albedo = FindProperty("_LayerFallback_Albedo", properties);
        MaterialProperty _LayerFallback_Metallic = FindProperty("_LayerFallback_Metallic", properties);
        MaterialProperty _LayerFallback_Smoothness = FindProperty("_LayerFallback_Smoothness", properties);

        MaterialProperty _WetnessLayer = FindProperty("_WetnessLayer", properties);
        MaterialProperty _WetnessLayer_Wetness = FindProperty("_WetnessLayer_Wetness", properties);
        MaterialProperty _WetnessLayer_WetAlbedoScale = FindProperty("_WetnessLayer_WetAlbedoScale", properties);
        MaterialProperty _WetnessLayer_WetSmoothness = FindProperty("_WetnessLayer_WetSmoothness", properties);

        MaterialProperty _ShoreWetnessLayer = FindProperty("_ShoreWetnessLayer", properties);
        MaterialProperty _ShoreWetnessLayer_BlendFactor = FindProperty("_ShoreWetnessLayer_BlendFactor", properties);
        MaterialProperty _ShoreWetnessLayer_BlendFalloff = FindProperty("_ShoreWetnessLayer_BlendFalloff", properties);
        MaterialProperty _ShoreWetnessLayer_Range = FindProperty("_ShoreWetnessLayer_Range", properties);
        MaterialProperty _ShoreWetnessLayer_WetAlbedoScale = FindProperty("_ShoreWetnessLayer_WetAlbedoScale", properties);
        MaterialProperty _ShoreWetnessLayer_WetSmoothness = FindProperty("_ShoreWetnessLayer_WetSmoothness", properties);

        MaterialProperty _PotatoDetailWorldUVScale = FindProperty("_PotatoDetailWorldUVScale", properties);

        MaterialProperty _Cutoff = FindProperty("_Cutoff", properties);
        MaterialProperty _CutoffRange = FindProperty("_CutoffRange", properties);
        MaterialProperty _DecalLayerMask = FindProperty("_DecalLayerMask", properties);
        MaterialProperty _Mode = FindProperty("_Mode", properties);
        MaterialProperty _SrcBlend = FindProperty("_SrcBlend", properties);
        MaterialProperty _DstBlend = FindProperty("_DstBlend", properties);
        MaterialProperty _ZWrite = FindProperty("_ZWrite", properties);
        MaterialProperty _TerrainParallax = FindProperty("_TerrainParallax", properties);
        MaterialProperty _Terrain_Type = FindProperty("_Terrain_Type", properties);
        MaterialProperty _LODTransitionDistance = FindProperty("_LODTransitionDistance", properties);

        // New properties
        MaterialProperty _BiomeMode = FindProperty("_BiomeMode", properties);
        MaterialProperty _TopologyMode = FindProperty("_TopologyMode", properties);
        MaterialProperty _PreviewMode = FindProperty("_PreviewMode", properties);

        // Global Textures Section
        showGlobalTextures = EditorGUILayout.Foldout(showGlobalTextures, "Global Textures", true);
        if (showGlobalTextures)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("These textures are set globally via TerrainManager (Shader.SetGlobalTexture) and are read-only.", MessageType.Info);

            // Single textures
            string[] singleTextureNames = new string[]
            {
                "Terrain_Control0",
                "Terrain_Control1",
                "Terrain_HeightTexture",
                "Terrain_Alpha",
                "Terrain_Biome",
                "Terrain_Biome1",
                "Terrain_Topologies", // New texture
                "Terrain_Preview"    // New texture
            };

            foreach (string textureName in singleTextureNames)
            {
                Texture texture = Shader.GetGlobalTexture(textureName);
                EditorGUILayout.ObjectField(textureName, texture, typeof(Texture), false);
            }

            // Texture arrays (albedo and normal)
            string[] textureArrayNames = new string[]
            {
                "Terrain_AlbedoArray_LOD0",
                "Terrain_AlbedoArray_LOD1",
                "Terrain_AlbedoArray_LOD2",
                "Terrain_NormalArray_LOD0",
                "Terrain_NormalArray_LOD1",
                "Terrain_NormalArray_LOD2"
            };

            string[] textureArrayLabels = new string[]
            {
                "Albedo Array LOD0",
                "Albedo Array LOD1",
                "Albedo Array LOD2",
                "Normal Array LOD0",
                "Normal Array LOD1",
                "Normal Array LOD2"
            };

            for (int i = 0; i < textureArrayNames.Length; i++)
            {
                showTextureArrays[i] = EditorGUILayout.Foldout(showTextureArrays[i], textureArrayLabels[i], true);
                if (showTextureArrays[i])
                {
                    EditorGUI.indentLevel++;
                    Texture2DArray textureArray = Shader.GetGlobalTexture(textureArrayNames[i]) as Texture2DArray;
                    if (textureArray != null)
                    {
                        int layerCount = textureArray.depth; // Number of textures in the array
                        for (int layer = 0; layer < Mathf.Min(layerCount, 8); layer++) // Limit to 8 layers
                        {
                            // Use AssetPreview for texture array (note: limited support)
                            Texture2D preview = AssetPreview.GetAssetPreview(textureArray);
                            if (preview == null)
                            {
                                EditorGUILayout.LabelField($"Layer {layer}", "No preview available");
                            }
                            else
                            {
                                EditorGUILayout.ObjectField($"Layer {layer}", preview, typeof(Texture2D), false);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Texture array not assigned");
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }

        // Global Parameters Section (UV Mix and Biome Colors)
        showGlobalParameters = EditorGUILayout.Foldout(showGlobalParameters, "Global Parameters", true);
        if (showGlobalParameters)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("These parameters are set globally via TerrainManager (Shader.SetGlobalVector/Color) and are read-only.", MessageType.Info);

            // UV Mix Parameters
            EditorGUILayout.LabelField("UV Mix Parameters", EditorStyles.boldLabel);
            for (int i = 0; i < 8; i++)
            {
                Vector4 uvMix = Shader.GetGlobalVector($"Splat{i}_UVMIX");
                EditorGUILayout.Vector4Field($"Splat{i}_UVMIX (Mult, Start, Distance, Unused)", uvMix);
            }

            // Biome Colors
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Biome Colors", EditorStyles.boldLabel);
            string[] biomes = new string[] { "Arid", "Temperate", "Tundra", "Arctic", "Jungle" };
            foreach (string biome in biomes)
            {
                EditorGUILayout.LabelField($"{biome} Colors", EditorStyles.miniBoldLabel);
                for (int i = 0; i < 8; i++) // Display 8 colors per biome (0-7)
                {
                    Color color = Shader.GetGlobalColor($"Splat{i}_{biome}Color");
                    Rect rect = EditorGUILayout.GetControlRect();
                    EditorGUI.ColorField(rect, new GUIContent($"Splat{i}_{biome}Color"), color, true, false, false);
                }
            }
            EditorGUI.indentLevel--;
        }

        // Override Section
        showOverrides = EditorGUILayout.Foldout(showOverrides, "Terrain Override Properties", true);
        if (showOverrides)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_UseTerrainOverrides, "Use Terrain Overrides");
            if (_UseTerrainOverrides.floatValue > 0.0f)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Override Control 0 (RG)"), _TerrainOverride_Control0);
                materialEditor.TexturePropertySingleLine(new GUIContent("Override Control 1 (RG)"), _TerrainOverride_Control1);
                materialEditor.TexturePropertySingleLine(new GUIContent("Override Biome"), _TerrainOverride_Biome);
                materialEditor.TexturePropertySingleLine(new GUIContent("Override Normal"), _TerrainOverride_Normal);
                materialEditor.TexturePropertySingleLine(new GUIContent("Override Alpha"), _TerrainOverride_Alpha);
            }
            EditorGUI.indentLevel--;
        }

        // Layer Properties Section
        showLayers = EditorGUILayout.Foldout(showLayers, "Layer Properties", true);
        if (showLayers)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < 8; i++)
            {
                EditorGUILayout.LabelField($"Layer {i}", EditorStyles.boldLabel);
                materialEditor.FloatProperty(FindProperty($"_Layer{i}_Factor", properties), "Factor");
                materialEditor.FloatProperty(FindProperty($"_Layer{i}_Falloff", properties), "Falloff");
                materialEditor.FloatProperty(FindProperty($"_Layer{i}_Metallic", properties), "Metallic");
                materialEditor.FloatProperty(FindProperty($"_Layer{i}_Smoothness", properties), "Smoothness");
                materialEditor.FloatProperty(FindProperty($"_Layer{i}_SpecularReflectivity", properties), "Specular Reflectivity");
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Fallback Layer (Puddle)", EditorStyles.boldLabel);
            materialEditor.ColorProperty(_LayerFallback_Albedo, "Albedo");
            materialEditor.FloatProperty(_LayerFallback_Metallic, "Metallic");
            materialEditor.FloatProperty(_LayerFallback_Smoothness, "Smoothness");
            EditorGUI.indentLevel--;
        }

        // Wetness Section
        showWetness = EditorGUILayout.Foldout(showWetness, "Wetness Properties", true);
        if (showWetness)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("Wetness Mask"), _WetnessLayer_Mask);
            materialEditor.FloatProperty(_WetnessLayer, "Wetness Layer");
            if (_WetnessLayer.floatValue > 0.0f)
            {
                materialEditor.FloatProperty(_WetnessLayer_Wetness, "Wetness Amount");
                materialEditor.FloatProperty(_WetnessLayer_WetAlbedoScale, "Wet Albedo Scale");
                materialEditor.FloatProperty(_WetnessLayer_WetSmoothness, "Wet Smoothness");
            }
            materialEditor.FloatProperty(_ShoreWetnessLayer, "Shore Wetness Layer");
            if (_ShoreWetnessLayer.floatValue > 0.0f)
            {
                materialEditor.FloatProperty(_ShoreWetnessLayer_BlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_ShoreWetnessLayer_BlendFalloff, "Blend Falloff");
                materialEditor.FloatProperty(_ShoreWetnessLayer_Range, "Range");
                materialEditor.FloatProperty(_ShoreWetnessLayer_WetAlbedoScale, "Wet Albedo Scale");
                materialEditor.FloatProperty(_ShoreWetnessLayer_WetSmoothness, "Wet Smoothness");
            }
            EditorGUI.indentLevel--;
        }

        // Detail Section
        showDetail = EditorGUILayout.Foldout(showDetail, "Detail Properties", true);
        if (showDetail)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("Potato Detail Texture"), _PotatoDetailTexture);
            materialEditor.FloatProperty(_PotatoDetailWorldUVScale, "Detail UV Scale");
            EditorGUI.indentLevel--;
        }

        // Rendering Section
        showRendering = EditorGUILayout.Foldout(showRendering, "Rendering Properties", true);
        if (showRendering)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_LODTransitionDistance, "LOD Transition Distance");
            materialEditor.FloatProperty(_Cutoff, "Alpha Cutoff");
            materialEditor.FloatProperty(_CutoffRange, "Cutoff Range");
            materialEditor.FloatProperty(_DecalLayerMask, "Decal Layer Mask");
            materialEditor.FloatProperty(_Mode, "Rendering Mode (0=Opaque, 1=Cutout)");
            materialEditor.FloatProperty(_BiomeMode, "Biome Rendering Mode"); // New property
            materialEditor.FloatProperty(_TopologyMode, "Topology Rendering Mode"); // New property
            materialEditor.FloatProperty(_PreviewMode, "Preview Mode"); // New property

            // Update rendering settings based on _Mode
            if (_Mode.floatValue == 1.0f) // Cutout
            {
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                material.EnableKeyword("ALPHA_TEST");
            }
            else // Opaque
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                material.DisableKeyword("ALPHA_TEST");
            }

            materialEditor.FloatProperty(_SrcBlend, "Source Blend");
            materialEditor.FloatProperty(_DstBlend, "Destination Blend");
            materialEditor.FloatProperty(_ZWrite, "ZWrite");
            materialEditor.FloatProperty(_TerrainParallax, "Terrain Parallax");
            materialEditor.FloatProperty(_Terrain_Type, "Terrain Type");
            EditorGUI.indentLevel--;
        }

        // Terrain Tiling Parameters
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Terrain Tiling Parameters", EditorStyles.boldLabel);
        float[] terrainTiling = Shader.GetGlobalFloatArray("Terrain_Tiling");
        for (int i = 0; i < terrainTiling.Length; i++)
        {
            EditorGUILayout.FloatField($"Terrain_Tiling[{i}]", terrainTiling[i]);
        }

        // Ensure material properties are applied
        materialEditor.PropertiesChanged();
    }
}
#endif