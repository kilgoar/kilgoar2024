using UnityEditor;
using UnityEngine;

public class RustWaterShaderGUI : ShaderGUI
{
    private static class Styles
    {
        public static readonly GUIContent waterAppearanceLabel = new GUIContent("Water Appearance");
        public static readonly GUIContent waveAnimationLabel = new GUIContent("Wave Animation");
        public static readonly GUIContent renderingLabel = new GUIContent("Rendering");
    }

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
        MaterialProperty cutoff = FindProperty("_Cutoff", properties);
        MaterialProperty mode = FindProperty("_Mode", properties);
        MaterialProperty srcBlend = FindProperty("_SrcBlend", properties);
        MaterialProperty dstBlend = FindProperty("_DstBlend", properties);
        MaterialProperty zWrite = FindProperty("_ZWrite", properties);

        // Water Appearance Section
        EditorGUILayout.LabelField(Styles.waterAppearanceLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        materialEditor.ColorProperty(color, "Water Color");
        materialEditor.RangeProperty(metallic, "Metallic");
        materialEditor.RangeProperty(smoothness, "Smoothness");
        materialEditor.RangeProperty(opacity, "Opacity");
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // Wave Animation Section
        EditorGUILayout.LabelField(Styles.waveAnimationLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        materialEditor.FloatProperty(waveSpeed, "Wave Speed");
        materialEditor.FloatProperty(waveScale, "Wave Scale");
        materialEditor.FloatProperty(waveStrength, "Wave Strength");
        materialEditor.FloatProperty(noiseScale, "Noise Scale");
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // Rendering Section
        EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        materialEditor.RangeProperty(cutoff, "Alpha Cutoff");
        materialEditor.FloatProperty(mode, "Rendering Mode");
        materialEditor.FloatProperty(srcBlend, "Source Blend");
        materialEditor.FloatProperty(dstBlend, "Destination Blend");
        materialEditor.FloatProperty(zWrite, "ZWrite");
        EditorGUI.indentLevel--;

        // Apply changes
        if (GUI.changed)
        {
            materialEditor.PropertiesChanged();
        }
    }
}