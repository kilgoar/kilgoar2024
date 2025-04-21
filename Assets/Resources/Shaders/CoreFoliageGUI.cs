#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CoreFoliageGUI : ShaderGUI
{
    private bool showTextures = true;
    private bool showColors = true;
    private bool showWind = true;
    private bool showDistanceFade = true;
    private bool showDebug = true;
    private bool showAdvanced = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Find all properties
        MaterialProperty baseColorMap = FindProperty("_BaseColorMap", properties);
        MaterialProperty translucencyMap = FindProperty("_TranslucencyMap", properties);
        MaterialProperty normalMap = FindProperty("_NormalMap", properties);
        MaterialProperty tintMask = FindProperty("_TintMask", properties);
        MaterialProperty baseColor = FindProperty("_BaseColor", properties);
        MaterialProperty debugEnabled = FindProperty("_DebugEnabled", properties);
        MaterialProperty debugView = FindProperty("_DebugView", properties);
        MaterialProperty decalLayerMask = FindProperty("_DecalLayerMask", properties);
        MaterialProperty displacementOverride = FindProperty("_DisplacementOverride", properties);
        MaterialProperty displacementStrength = FindProperty("_DisplacementStrength", properties);
        MaterialProperty distanceFadeEnabled = FindProperty("_DistanceFadeEnabled", properties);
        MaterialProperty distanceFadeLength = FindProperty("_DistanceFadeLength", properties);
        MaterialProperty distanceFadeStart = FindProperty("_DistanceFadeStart", properties);
        MaterialProperty distanceFadeToNormal = FindProperty("_DistanceFadeToNormal", properties);
        MaterialProperty distanceFadeToSizeScale = FindProperty("_DistanceFadeToSizeScale", properties);
        MaterialProperty distanceFadeToSmoothnessScale = FindProperty("_DistanceFadeToSmoothnessScale", properties);
        MaterialProperty edgeMaskContrast = FindProperty("_EdgeMaskContrast", properties);
        MaterialProperty edgeMaskMin = FindProperty("_EdgeMaskMin", properties);
        MaterialProperty faceCulling = FindProperty("_FaceCulling", properties);
        MaterialProperty normalScale = FindProperty("_NormalScale", properties);
        MaterialProperty normalScaleFromThicknessEnabled = FindProperty("_NormalScaleFromThicknessEnabled", properties);
        MaterialProperty opacityMaskClip = FindProperty("_OpacityMaskClip", properties);
        MaterialProperty roughness = FindProperty("_Roughness", properties);
        MaterialProperty shadowAlphaLODBias = FindProperty("_ShadowAlphaLODBias", properties);
        MaterialProperty shadowBias = FindProperty("_ShadowBias", properties);
        MaterialProperty shadowIntensity = FindProperty("_ShadowIntensity", properties);
        MaterialProperty specular = FindProperty("_Specular", properties);
        MaterialProperty tintBase1 = FindProperty("_TintBase1", properties);
        MaterialProperty tintBase2 = FindProperty("_TintBase2", properties);
        MaterialProperty tintBiome = FindProperty("_TintBiome", properties);
        MaterialProperty tintColor1 = FindProperty("_TintColor1", properties);
        MaterialProperty tintColor2 = FindProperty("_TintColor2", properties);
        MaterialProperty tintEnabled = FindProperty("_TintEnabled", properties);
        MaterialProperty tintMaskSource = FindProperty("_TintMaskSource", properties);
        MaterialProperty translucency = FindProperty("_Translucency", properties);
        MaterialProperty twoSided = FindProperty("_TwoSided", properties);
        MaterialProperty vertexOcclusionStrength = FindProperty("_VertexOcclusionStrength", properties);
        MaterialProperty wavesEnabled = FindProperty("_WavesEnabled", properties);
        MaterialProperty wavesScale = FindProperty("_WavesScale", properties);
        MaterialProperty windBranchAmplitude = FindProperty("_WindBranchAmplitude", properties);
        MaterialProperty windBranchFrequency = FindProperty("_WindBranchFrequency", properties);
        MaterialProperty windEnabled = FindProperty("_WindEnabled", properties);
        MaterialProperty windFlutterAmplitude = FindProperty("_WindFlutterAmplitude", properties);
        MaterialProperty windFlutterFrequency = FindProperty("_WindFlutterFrequency", properties);
        MaterialProperty windHeightBendAmplitude = FindProperty("_WindHeightBendAmplitude", properties);
        MaterialProperty windHeightBendDirAdherence = FindProperty("_WindHeightBendDirAdherence", properties);
        MaterialProperty windHeightBendExponent = FindProperty("_WindHeightBendExponent", properties);
        MaterialProperty windHeightBendFrequency = FindProperty("_WindHeightBendFrequency", properties);

        // Header
        EditorGUILayout.LabelField("Core Foliage Shader", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Textures Section
        showTextures = EditorGUILayout.Foldout(showTextures, "Textures", true, EditorStyles.foldoutHeader);
        if (showTextures)
        {
            materialEditor.TextureProperty(baseColorMap, "Base Color Map", true);
            materialEditor.TextureProperty(translucencyMap, "Translucency Map", true);
            materialEditor.TextureProperty(normalMap, "Normal Map", true);
            materialEditor.TextureProperty(tintMask, "Tint Mask", true);
        }
        EditorGUILayout.Space();

        // Colors Section
        showColors = EditorGUILayout.Foldout(showColors, "Colors & Tinting", true, EditorStyles.foldoutHeader);
        if (showColors)
        {
            materialEditor.ColorProperty(baseColor, "Base Color");
            materialEditor.FloatProperty(tintEnabled, "Enable Tinting");
            if (tintEnabled.floatValue > 0.5f)
            {
                materialEditor.ColorProperty(tintColor1, "Tint Color 1");
                materialEditor.ColorProperty(tintColor2, "Tint Color 2");
                materialEditor.ColorProperty(tintBase1, "Tint Base 1");
                materialEditor.ColorProperty(tintBase2, "Tint Base 2");
                materialEditor.FloatProperty(tintBiome, "Biome Tint Blend");
                materialEditor.FloatProperty(tintMaskSource, "Tint Mask Source");
            }
            materialEditor.ColorProperty(translucency, "Translucency Color");
        }
        EditorGUILayout.Space();

        // Wind Section
        showWind = EditorGUILayout.Foldout(showWind, "Wind Animation", true, EditorStyles.foldoutHeader);
        if (showWind)
        {
            materialEditor.FloatProperty(windEnabled, "Enable Wind");
            if (windEnabled.floatValue > 0.5f)
            {
                materialEditor.FloatProperty(wavesEnabled, "Enable Waves");
                materialEditor.FloatProperty(wavesScale, "Waves Scale");
                materialEditor.FloatProperty(windBranchAmplitude, "Branch Amplitude");
                materialEditor.FloatProperty(windBranchFrequency, "Branch Frequency");
                materialEditor.FloatProperty(windFlutterAmplitude, "Flutter Amplitude");
                materialEditor.FloatProperty(windFlutterFrequency, "Flutter Frequency");
                materialEditor.FloatProperty(windHeightBendAmplitude, "Height Bend Amplitude");
                materialEditor.FloatProperty(windHeightBendDirAdherence, "Height Bend Direction Adherence");
                materialEditor.FloatProperty(windHeightBendExponent, "Height Bend Exponent");
                materialEditor.FloatProperty(windHeightBendFrequency, "Height Bend Frequency");
            }
        }
        EditorGUILayout.Space();

        // Distance Fade Section
        showDistanceFade = EditorGUILayout.Foldout(showDistanceFade, "Distance Fade", true, EditorStyles.foldoutHeader);
        if (showDistanceFade)
        {
            materialEditor.FloatProperty(distanceFadeEnabled, "Enable Distance Fade");
            if (distanceFadeEnabled.floatValue > 0.5f)
            {
                materialEditor.FloatProperty(distanceFadeStart, "Fade Start Distance");
                materialEditor.FloatProperty(distanceFadeLength, "Fade Length");
                materialEditor.VectorProperty(distanceFadeToNormal, "Fade to Normal");
                materialEditor.FloatProperty(distanceFadeToSizeScale, "Fade to Size Scale");
                materialEditor.FloatProperty(distanceFadeToSmoothnessScale, "Fade to Smoothness Scale");
            }
        }
        EditorGUILayout.Space();

        // Debug Section
        showDebug = EditorGUILayout.Foldout(showDebug, "Debug", true, EditorStyles.foldoutHeader);
        if (showDebug)
        {
            materialEditor.FloatProperty(debugEnabled, "Enable Debug");
            if (debugEnabled.floatValue > 0.5f)
            {
                materialEditor.FloatProperty(debugView, "Debug View Mode");
            }
        }
        EditorGUILayout.Space();

        // Advanced Section
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true, EditorStyles.foldoutHeader);
        if (showAdvanced)
        {
            materialEditor.FloatProperty(decalLayerMask, "Decal Layer Mask");
            materialEditor.FloatProperty(displacementOverride, "Displacement Override");
            materialEditor.FloatProperty(displacementStrength, "Displacement Strength");
            materialEditor.FloatProperty(edgeMaskContrast, "Edge Mask Contrast");
            materialEditor.FloatProperty(edgeMaskMin, "Edge Mask Min");
            materialEditor.FloatProperty(faceCulling, "Face Culling");
            materialEditor.FloatProperty(normalScale, "Normal Scale");
            materialEditor.FloatProperty(normalScaleFromThicknessEnabled, "Normal Scale from Thickness");
            materialEditor.FloatProperty(opacityMaskClip, "Opacity Mask Clip");
            materialEditor.FloatProperty(roughness, "Roughness");
            materialEditor.FloatProperty(shadowAlphaLODBias, "Shadow Alpha LOD Bias");
            materialEditor.FloatProperty(shadowBias, "Shadow Bias");
            materialEditor.FloatProperty(shadowIntensity, "Shadow Intensity");
            materialEditor.FloatProperty(specular, "Specular");
            materialEditor.FloatProperty(twoSided, "Two Sided");
            materialEditor.FloatProperty(vertexOcclusionStrength, "Vertex Occlusion Strength");
        }

        // Standard Unity material options
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }
}
#endif