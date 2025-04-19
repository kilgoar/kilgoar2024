using UnityEngine;
using UnityEditor;

public class RustStandardDecalShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Find properties
        MaterialProperty color = FindProperty("_Color", properties);
        MaterialProperty mainTex = FindProperty("_MainTex", properties);
        MaterialProperty cutoff = FindProperty("_Cutoff", properties);
        MaterialProperty glossiness = FindProperty("_Glossiness", properties);
        MaterialProperty metallic = FindProperty("_Metallic", properties);
        MaterialProperty metallicGlossMap = FindProperty("_MetallicGlossMap", properties);
        MaterialProperty bumpMap = FindProperty("_BumpMap", properties);
        MaterialProperty bumpScale = FindProperty("_BumpScale", properties);
        MaterialProperty occlusionMap = FindProperty("_OcclusionMap", properties);
        MaterialProperty occlusionStrength = FindProperty("_OcclusionStrength", properties);
        MaterialProperty emissionColor = FindProperty("_EmissionColor", properties);
        MaterialProperty emissionMap = FindProperty("_EmissionMap", properties);
        MaterialProperty detailMask = FindProperty("_DetailMask", properties);
        MaterialProperty detailAlbedoMap = FindProperty("_DetailAlbedoMap", properties);
        MaterialProperty detailNormalMap = FindProperty("_DetailNormalMap", properties);
        MaterialProperty detailNormalMapScale = FindProperty("_DetailNormalMapScale", properties);
        MaterialProperty detailOcclusionMap = FindProperty("_DetailOcclusionMap", properties);
        MaterialProperty detailOcclusionStrength = FindProperty("_DetailOcclusionStrength", properties);
        MaterialProperty detailColor = FindProperty("_DetailColor", properties);
        MaterialProperty detailLayer = FindProperty("_DetailLayer", properties);
        MaterialProperty biomeLayerTintMask = FindProperty("_BiomeLayer_TintMask", properties);
        MaterialProperty biomeLayer = FindProperty("_BiomeLayer", properties);
        MaterialProperty wetnessLayer = FindProperty("_WetnessLayer", properties);
        MaterialProperty wetnessLayerMask = FindProperty("_WetnessLayer_Mask", properties);
        MaterialProperty wetnessLayerWetAlbedoScale = FindProperty("_WetnessLayer_WetAlbedoScale", properties);
        MaterialProperty wetnessLayerWetSmoothness = FindProperty("_WetnessLayer_WetSmoothness", properties);
        MaterialProperty wetnessLayerWetness = FindProperty("_WetnessLayer_Wetness", properties);
        MaterialProperty shoreWetnessLayer = FindProperty("_ShoreWetnessLayer", properties);
        MaterialProperty shoreWetnessLayerBlendFactor = FindProperty("_ShoreWetnessLayer_BlendFactor", properties);
        MaterialProperty shoreWetnessLayerBlendFalloff = FindProperty("_ShoreWetnessLayer_BlendFalloff", properties);
        MaterialProperty shoreWetnessLayerRange = FindProperty("_ShoreWetnessLayer_Range", properties);
        MaterialProperty shoreWetnessLayerWetAlbedoScale = FindProperty("_ShoreWetnessLayer_WetAlbedoScale", properties);
        MaterialProperty shoreWetnessLayerWetSmoothness = FindProperty("_ShoreWetnessLayer_WetSmoothness", properties);
        MaterialProperty applyVertexAlpha = FindProperty("_ApplyVertexAlpha", properties);
        MaterialProperty applyVertexAlphaStrength = FindProperty("_ApplyVertexAlphaStrength", properties);
        MaterialProperty applyVertexColor = FindProperty("_ApplyVertexColor", properties);
        MaterialProperty applyVertexColorStrength = FindProperty("_ApplyVertexColorStrength", properties);

        // Main Section
        EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), mainTex, color);
        materialEditor.RangeProperty(cutoff, "Alpha Cutoff");
        materialEditor.TexturePropertySingleLine(new GUIContent("Metallic (R) Smoothness (A)"), metallicGlossMap, metallic, glossiness);

        // Normal Map
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Normal Map", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), bumpMap, bumpScale);

        // Occlusion
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Occlusion", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Occlusion"), occlusionMap, occlusionStrength);

        // Emission
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emission", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Emission"), emissionMap, emissionColor);

        // Detail Layer
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Detail Layer", EditorStyles.boldLabel);
        materialEditor.FloatProperty(detailLayer, "Detail Layer");
        if (detailLayer.floatValue > 0)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Detail Mask"), detailMask);
            materialEditor.TexturePropertySingleLine(new GUIContent("Detail Albedo"), detailAlbedoMap, detailColor);
            materialEditor.TexturePropertySingleLine(new GUIContent("Detail Normal"), detailNormalMap, detailNormalMapScale);
            materialEditor.TexturePropertySingleLine(new GUIContent("Detail Occlusion"), detailOcclusionMap, detailOcclusionStrength);
        }

        // Biome Layer
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Biome Layer", EditorStyles.boldLabel);
        materialEditor.FloatProperty(biomeLayer, "Biome Layer");
        if (biomeLayer.floatValue > 0)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Biome Tint Mask"), biomeLayerTintMask);
        }

        // Wetness Layer
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wetness Layer", EditorStyles.boldLabel);
        materialEditor.FloatProperty(wetnessLayer, "Wetness Layer");
        if (wetnessLayer.floatValue > 0)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Wetness Mask"), wetnessLayerMask);
            materialEditor.FloatProperty(wetnessLayerWetAlbedoScale, "Wet Albedo Scale");
            materialEditor.FloatProperty(wetnessLayerWetSmoothness, "Wet Smoothness");
            materialEditor.FloatProperty(wetnessLayerWetness, "Wetness");
        }

        // Shore Wetness Layer
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shore Wetness Layer", EditorStyles.boldLabel);
        materialEditor.FloatProperty(shoreWetnessLayer, "Shore Wetness Layer");
        if (shoreWetnessLayer.floatValue > 0)
        {
            materialEditor.FloatProperty(shoreWetnessLayerBlendFactor, "Blend Factor");
            materialEditor.FloatProperty(shoreWetnessLayerBlendFalloff, "Blend Falloff");
            materialEditor.FloatProperty(shoreWetnessLayerRange, "Range");
            materialEditor.FloatProperty(shoreWetnessLayerWetAlbedoScale, "Wet Albedo Scale");
            materialEditor.FloatProperty(shoreWetnessLayerWetSmoothness, "Wet Smoothness");
        }

        // Vertex Data
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vertex Data", EditorStyles.boldLabel);
        materialEditor.FloatProperty(applyVertexColor, "Apply Vertex Color");
        if (applyVertexColor.floatValue > 0)
            materialEditor.FloatProperty(applyVertexColorStrength, "Vertex Color Strength");
        materialEditor.FloatProperty(applyVertexAlpha, "Apply Vertex Alpha");
        if (applyVertexAlpha.floatValue > 0)
            materialEditor.FloatProperty(applyVertexAlphaStrength, "Vertex Alpha Strength");

        // Render Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }
}