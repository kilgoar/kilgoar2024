using UnityEditor;
using UnityEngine;

public class RustOceanShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Find properties
        MaterialProperty color = FindProperty("_Color", properties);
        MaterialProperty metallic = FindProperty("_Metallic", properties);
        MaterialProperty smoothness = FindProperty("_Smoothness", properties);
        MaterialProperty opacity = FindProperty("_Opacity", properties);
        MaterialProperty waveSpeed = FindProperty("_WaveSpeed", properties);
        MaterialProperty waveScale = FindProperty("_WaveScale", properties);
        MaterialProperty waveStrength = FindProperty("_WaveStrength", properties);
        MaterialProperty noiseScale = FindProperty("_NoiseScale", properties);
        MaterialProperty oceanYLevel = FindProperty("_OceanYLevel", properties);
        MaterialProperty topologyBit = FindProperty("_TopologyBit", properties);
        MaterialProperty cutoff = FindProperty("_Cutoff", properties);
        MaterialProperty mode = FindProperty("_Mode", properties);
        MaterialProperty srcBlend = FindProperty("_SrcBlend", properties);
        MaterialProperty dstBlend = FindProperty("_DstBlend", properties);
        MaterialProperty zWrite = FindProperty("_ZWrite", properties);

        // Header styles
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;

        // Appearance Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Appearance", headerStyle);
        materialEditor.ShaderProperty(color, new GUIContent("Water Color", "Base color of the water (RGBA, alpha affects transparency)"));
        materialEditor.ShaderProperty(metallic, new GUIContent("Metallic", "Metallic value for the water surface"));
        materialEditor.ShaderProperty(smoothness, new GUIContent("Smoothness", "Smoothness value for the water surface"));
        materialEditor.ShaderProperty(opacity, new GUIContent("Opacity", "Overall transparency multiplier"));

        // Wave Animation Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wave Animation", headerStyle);
        materialEditor.ShaderProperty(waveSpeed, new GUIContent("Wave Speed", "Speed of the wave animation"));
        materialEditor.ShaderProperty(waveScale, new GUIContent("Wave Scale", "Scale of normal perturbation for waves"));
        materialEditor.ShaderProperty(waveStrength, new GUIContent("Wave Strength", "Height amplitude of the waves"));
        materialEditor.ShaderProperty(noiseScale, new GUIContent("Noise Scale", "Frequency of the Perlin noise pattern"));

        // Topology Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Topology", headerStyle);
        
        // Display global Terrain_Topologies texture
        Texture currentTopologyTexture = Shader.GetGlobalTexture("Terrain_Topologies");
        EditorGUI.BeginChangeCheck();
        Texture newTopologyTexture = EditorGUILayout.ObjectField(
            new GUIContent("Topology Texture", "Global texture defining ocean topology areas (bit 8)"),
            currentTopologyTexture,
            typeof(Texture),
            false
        ) as Texture;
        if (EditorGUI.EndChangeCheck())
        {
            // Set the global texture if the user assigns a new one
            Shader.SetGlobalTexture("Terrain_Topologies", newTopologyTexture);
        }

        materialEditor.ShaderProperty(oceanYLevel, new GUIContent("Ocean Y Level", "World-space Y level for ocean vertices"));
        materialEditor.ShaderProperty(topologyBit, new GUIContent("Topology Bit", "Topology bit for ocean areas (e.g., 8)"));

        // Rendering Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rendering", headerStyle);
        materialEditor.ShaderProperty(cutoff, new GUIContent("Alpha Cutoff", "Threshold for alpha testing (used when Alpha Test is enabled)"));

        // Rendering Mode Dropdown
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        float modeValue = EditorGUILayout.Popup(new GUIContent("Rendering Mode", "Opaque or Transparent rendering"), (int)mode.floatValue, new[] { "Opaque", "Transparent" });
        if (EditorGUI.EndChangeCheck())
        {
            mode.floatValue = modeValue;
            UpdateRenderingSettings(material, modeValue);
        }

        // Hidden properties (for completeness)
        materialEditor.ShaderProperty(srcBlend, new GUIContent("Source Blend", "Internal blending mode (usually SrcAlpha for Transparent)"));
        materialEditor.ShaderProperty(dstBlend, new GUIContent("Destination Blend", "Internal blending mode (usually OneMinusSrcAlpha for Transparent)"));
        materialEditor.ShaderProperty(zWrite, new GUIContent("ZWrite", "Depth buffer writing (usually Off for Transparent)"));

        // Ensure material keywords and render queue are updated
        UpdateMaterialKeywords(material);
    }

    private void UpdateRenderingSettings(Material material, float mode)
    {
        if (mode == 0) // Opaque
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        else // Transparent
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private void UpdateMaterialKeywords(Material material)
    {
        // Enable/disable ALPHA_TEST keyword based on mode
        if (material.GetFloat("_Mode") == 1) // Transparent
        {
            material.DisableKeyword("ALPHA_TEST");
        }
        else
        {
            material.EnableKeyword("ALPHA_TEST");
        }
    }
}