#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class StandardBlend4WaySpecularShaderGUI : ShaderGUI
{
    private MaterialProperty _Color;
    private MaterialProperty _MainTex;
    private MaterialProperty _Cutoff;
    private MaterialProperty _Glossiness;
    private MaterialProperty _SpecGlossMap;
    private MaterialProperty _BumpMap;
    private MaterialProperty _BumpScale;
    private MaterialProperty _MainTexScroll;

    // Blend Layer 1
    private MaterialProperty _BlendLayer1;
    private MaterialProperty _BlendLayer1_Color;
    private MaterialProperty _BlendLayer1_AlbedoMap;
    private MaterialProperty _BlendLayer1_SpecGlossMap;
    private MaterialProperty _BlendLayer1_NormalMap;
    private MaterialProperty _BlendLayer1_BlendMaskMap;
    private MaterialProperty _BlendLayer1_BlendFactor;
    private MaterialProperty _BlendLayer1_BlendFalloff;
    private MaterialProperty _BlendLayer1_Glossiness;
    private MaterialProperty _BlendLayer1_SpecColor;
    private MaterialProperty _BlendLayer1_NormalMapScale;
    private MaterialProperty _BlendLayer1_AlbedoMapScroll;
    private MaterialProperty _BlendLayer1_BlendMaskMapScroll;
    private MaterialProperty _BlendLayer1_BlendMaskMapInvert;
    private MaterialProperty _BlendLayer1_UVSet;
    private MaterialProperty _BlendLayer1_BlendMaskUVSet;

    // Blend Layer 2
    private MaterialProperty _BlendLayer2;
    private MaterialProperty _BlendLayer2_Color;
    private MaterialProperty _BlendLayer2_AlbedoMap;
    private MaterialProperty _BlendLayer2_SpecGlossMap;
    private MaterialProperty _BlendLayer2_NormalMap;
    private MaterialProperty _BlendLayer2_BlendMaskMap;
    private MaterialProperty _BlendLayer2_BlendFactor;
    private MaterialProperty _BlendLayer2_BlendFalloff;
    private MaterialProperty _BlendLayer2_Glossiness;
    private MaterialProperty _BlendLayer2_SpecColor;
    private MaterialProperty _BlendLayer2_NormalMapScale;
    private MaterialProperty _BlendLayer2_AlbedoMapScroll;
    private MaterialProperty _BlendLayer2_BlendMaskMapScroll;
    private MaterialProperty _BlendLayer2_BlendMaskMapInvert;
    private MaterialProperty _BlendLayer2_UVSet;
    private MaterialProperty _BlendLayer2_BlendMaskUVSet;

    // Blend Layer 3
    private MaterialProperty _BlendLayer3;
    private MaterialProperty _BlendLayer3_Color;
    private MaterialProperty _BlendLayer3_AlbedoMap;
    private MaterialProperty _BlendLayer3_SpecGlossMap;
    private MaterialProperty _BlendLayer3_NormalMap;
    private MaterialProperty _BlendLayer3_BlendMaskMap;
    private MaterialProperty _BlendLayer3_BlendFactor;
    private MaterialProperty _BlendLayer3_BlendFalloff;
    private MaterialProperty _BlendLayer3_Glossiness;
    private MaterialProperty _BlendLayer3_SpecColor;
    private MaterialProperty _BlendLayer3_NormalMapScale;
    private MaterialProperty _BlendLayer3_AlbedoMapScroll;
    private MaterialProperty _BlendLayer3_BlendMaskMapScroll;
    private MaterialProperty _BlendLayer3_BlendMaskMapInvert;
    private MaterialProperty _BlendLayer3_UVSet;
    private MaterialProperty _BlendLayer3_BlendMaskUVSet;

    // Detail Properties
    private MaterialProperty _DetailMask;
    private MaterialProperty _DetailAlbedoMap;
    private MaterialProperty _DetailNormalMap;
    private MaterialProperty _DetailNormalMapScale;
    private MaterialProperty _DetailColor;
    private MaterialProperty _DetailLayer;
    private MaterialProperty _DetailLayer_BlendFactor;
    private MaterialProperty _DetailLayer_BlendFalloff;
    private MaterialProperty _DetailAlbedoMapScroll;

    // Vertex Color/Alpha
    private MaterialProperty _ApplyVertexAlpha;
    private MaterialProperty _ApplyVertexAlphaStrength;

    // Advanced Properties
    private MaterialProperty _UVSec;
    private MaterialProperty _Mode;
    private MaterialProperty _SrcBlend;
    private MaterialProperty _DstBlend;
    private MaterialProperty _ZWrite;
    private MaterialProperty _Cull;

    private bool showBlendLayer1 = false;
    private bool showBlendLayer2 = false;
    private bool showBlendLayer3 = false;
    private bool showDetailLayer = false;
    private bool showAdvanced = false;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);
        Material material = materialEditor.target as Material;

        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _MainTex, _Color);
        materialEditor.FloatProperty(_Cutoff, "Alpha Cutoff");
        materialEditor.TexturePropertySingleLine(new GUIContent("Specular"), _SpecGlossMap);
        materialEditor.FloatProperty(_Glossiness, "Smoothness");
        materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BumpMap);
        materialEditor.FloatProperty(_BumpScale, "Normal Scale");
        materialEditor.VectorProperty(_MainTexScroll, "Main Tex Scroll");

        EditorGUILayout.Space();
        showBlendLayer1 = EditorGUILayout.Foldout(showBlendLayer1, "Blend Layer 1", true, EditorStyles.foldoutHeader);
        if (showBlendLayer1)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_BlendLayer1, "Enable Blend Layer 1");
            if (_BlendLayer1.floatValue > 0)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _BlendLayer1_AlbedoMap, _BlendLayer1_Color);
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular"), _BlendLayer1_SpecGlossMap, _BlendLayer1_SpecColor);
                materialEditor.FloatProperty(_BlendLayer1_Glossiness, "Smoothness");
                materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BlendLayer1_NormalMap);
                materialEditor.FloatProperty(_BlendLayer1_NormalMapScale, "Normal Scale");
                materialEditor.TexturePropertySingleLine(new GUIContent("Blend Mask"), _BlendLayer1_BlendMaskMap);
                materialEditor.FloatProperty(_BlendLayer1_BlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_BlendLayer1_BlendFalloff, "Blend Falloff");
                materialEditor.FloatProperty(_BlendLayer1_BlendMaskMapInvert, "Mask Invert");
                materialEditor.VectorProperty(_BlendLayer1_AlbedoMapScroll, "Albedo Scroll");
                materialEditor.VectorProperty(_BlendLayer1_BlendMaskMapScroll, "Mask Scroll");
                materialEditor.FloatProperty(_BlendLayer1_UVSet, "UV Set (0=Primary, 1=Secondary)");
                materialEditor.FloatProperty(_BlendLayer1_BlendMaskUVSet, "Blend Mask UV Set");
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showBlendLayer2 = EditorGUILayout.Foldout(showBlendLayer2, "Blend Layer 2", true, EditorStyles.foldoutHeader);
        if (showBlendLayer2)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_BlendLayer2, "Enable Blend Layer 2");
            if (_BlendLayer2.floatValue > 0)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _BlendLayer2_AlbedoMap, _BlendLayer2_Color);
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular"), _BlendLayer2_SpecGlossMap, _BlendLayer2_SpecColor);
                materialEditor.FloatProperty(_BlendLayer2_Glossiness, "Smoothness");
                materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BlendLayer2_NormalMap);
                materialEditor.FloatProperty(_BlendLayer2_NormalMapScale, "Normal Scale");
                materialEditor.TexturePropertySingleLine(new GUIContent("Blend Mask"), _BlendLayer2_BlendMaskMap);
                materialEditor.FloatProperty(_BlendLayer2_BlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_BlendLayer2_BlendFalloff, "Blend Falloff");
                materialEditor.FloatProperty(_BlendLayer2_BlendMaskMapInvert, "Mask Invert");
                materialEditor.VectorProperty(_BlendLayer2_AlbedoMapScroll, "Albedo Scroll");
                materialEditor.VectorProperty(_BlendLayer2_BlendMaskMapScroll, "Mask Scroll");
                materialEditor.FloatProperty(_BlendLayer2_UVSet, "UV Set (0=Primary, 1=Secondary)");
                materialEditor.FloatProperty(_BlendLayer2_BlendMaskUVSet, "Blend Mask UV Set");
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showBlendLayer3 = EditorGUILayout.Foldout(showBlendLayer3, "Blend Layer 3", true, EditorStyles.foldoutHeader);
        if (showBlendLayer3)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_BlendLayer3, "Enable Blend Layer 3");
            if (_BlendLayer3.floatValue > 0)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _BlendLayer3_AlbedoMap, _BlendLayer3_Color);
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular"), _BlendLayer3_SpecGlossMap, _BlendLayer3_SpecColor);
                materialEditor.FloatProperty(_BlendLayer3_Glossiness, "Smoothness");
                materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BlendLayer3_NormalMap);
                materialEditor.FloatProperty(_BlendLayer3_NormalMapScale, "Normal Scale");
                materialEditor.TexturePropertySingleLine(new GUIContent("Blend Mask"), _BlendLayer3_BlendMaskMap);
                materialEditor.FloatProperty(_BlendLayer3_BlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_BlendLayer3_BlendFalloff, "Blend Falloff");
                materialEditor.FloatProperty(_BlendLayer3_BlendMaskMapInvert, "Mask Invert");
                materialEditor.VectorProperty(_BlendLayer3_AlbedoMapScroll, "Albedo Scroll");
                materialEditor.VectorProperty(_BlendLayer3_BlendMaskMapScroll, "Mask Scroll");
                materialEditor.FloatProperty(_BlendLayer3_UVSet, "UV Set (0=Primary, 1=Secondary)");
                materialEditor.FloatProperty(_BlendLayer3_BlendMaskUVSet, "Blend Mask UV Set");
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        showDetailLayer = EditorGUILayout.Foldout(showDetailLayer, "Detail Layer", true, EditorStyles.foldoutHeader);
        if (showDetailLayer)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_DetailLayer, "Enable Detail Layer");
            if (_DetailLayer.floatValue > 0)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Mask"), _DetailMask);
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Albedo"), _DetailAlbedoMap, _DetailColor);
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Normal"), _DetailNormalMap);
                materialEditor.FloatProperty(_DetailNormalMapScale, "Normal Scale");
                materialEditor.FloatProperty(_DetailLayer_BlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_DetailLayer_BlendFalloff, "Blend Falloff");
                materialEditor.VectorProperty(_DetailAlbedoMapScroll, "Albedo Scroll");
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vertex Alpha", EditorStyles.boldLabel);
        materialEditor.FloatProperty(_ApplyVertexAlpha, "Apply Vertex Alpha");
        materialEditor.FloatProperty(_ApplyVertexAlphaStrength, "Vertex Alpha Strength");

        EditorGUILayout.Space();
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options", true, EditorStyles.foldoutHeader);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            materialEditor.FloatProperty(_UVSec, "Secondary UV Set");
            materialEditor.FloatProperty(_Mode, "Rendering Mode");
            materialEditor.FloatProperty(_SrcBlend, "Source Blend");
            materialEditor.FloatProperty(_DstBlend, "Destination Blend");
            materialEditor.FloatProperty(_ZWrite, "ZWrite");
            materialEditor.FloatProperty(_Cull, "Cull Mode");
            EditorGUI.indentLevel--;
        }

        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }

    private void FindProperties(MaterialProperty[] properties)
    {
        _Color = FindProperty("_Color", properties);
        _MainTex = FindProperty("_MainTex", properties);
        _Cutoff = FindProperty("_Cutoff", properties);
        _Glossiness = FindProperty("_Glossiness", properties);
        _SpecGlossMap = FindProperty("_SpecGlossMap", properties);
        _BumpMap = FindProperty("_BumpMap", properties);
        _BumpScale = FindProperty("_BumpScale", properties);
        _MainTexScroll = FindProperty("_MainTexScroll", properties);

        _BlendLayer1 = FindProperty("_BlendLayer1", properties);
        _BlendLayer1_Color = FindProperty("_BlendLayer1_Color", properties);
        _BlendLayer1_AlbedoMap = FindProperty("_BlendLayer1_AlbedoMap", properties);
        _BlendLayer1_SpecGlossMap = FindProperty("_BlendLayer1_SpecGlossMap", properties);
        _BlendLayer1_NormalMap = FindProperty("_BlendLayer1_NormalMap", properties);
        _BlendLayer1_BlendMaskMap = FindProperty("_BlendLayer1_BlendMaskMap", properties);
        _BlendLayer1_BlendFactor = FindProperty("_BlendLayer1_BlendFactor", properties);
        _BlendLayer1_BlendFalloff = FindProperty("_BlendLayer1_BlendFalloff", properties);
        _BlendLayer1_Glossiness = FindProperty("_BlendLayer1_Glossiness", properties);
        _BlendLayer1_SpecColor = FindProperty("_BlendLayer1_SpecColor", properties);
        _BlendLayer1_NormalMapScale = FindProperty("_BlendLayer1_NormalMapScale", properties);
        _BlendLayer1_AlbedoMapScroll = FindProperty("_BlendLayer1_AlbedoMapScroll", properties);
        _BlendLayer1_BlendMaskMapScroll = FindProperty("_BlendLayer1_BlendMaskMapScroll", properties);
        _BlendLayer1_BlendMaskMapInvert = FindProperty("_BlendLayer1_BlendMaskMapInvert", properties);
        _BlendLayer1_UVSet = FindProperty("_BlendLayer1_UVSet", properties);
        _BlendLayer1_BlendMaskUVSet = FindProperty("_BlendLayer1_BlendMaskUVSet", properties);

        _BlendLayer2 = FindProperty("_BlendLayer2", properties);
        _BlendLayer2_Color = FindProperty("_BlendLayer2_Color", properties);
        _BlendLayer2_AlbedoMap = FindProperty("_BlendLayer2_AlbedoMap", properties);
        _BlendLayer2_SpecGlossMap = FindProperty("_BlendLayer2_SpecGlossMap", properties);
        _BlendLayer2_NormalMap = FindProperty("_BlendLayer2_NormalMap", properties);
        _BlendLayer2_BlendMaskMap = FindProperty("_BlendLayer2_BlendMaskMap", properties);
        _BlendLayer2_BlendFactor = FindProperty("_BlendLayer2_BlendFactor", properties);
        _BlendLayer2_BlendFalloff = FindProperty("_BlendLayer2_BlendFalloff", properties);
        _BlendLayer2_Glossiness = FindProperty("_BlendLayer2_Glossiness", properties);
        _BlendLayer2_SpecColor = FindProperty("_BlendLayer2_SpecColor", properties);
        _BlendLayer2_NormalMapScale = FindProperty("_BlendLayer2_NormalMapScale", properties);
        _BlendLayer2_AlbedoMapScroll = FindProperty("_BlendLayer2_AlbedoMapScroll", properties);
        _BlendLayer2_BlendMaskMapScroll = FindProperty("_BlendLayer2_BlendMaskMapScroll", properties);
        _BlendLayer2_BlendMaskMapInvert = FindProperty("_BlendLayer2_BlendMaskMapInvert", properties);
        _BlendLayer2_UVSet = FindProperty("_BlendLayer2_UVSet", properties);
        _BlendLayer2_BlendMaskUVSet = FindProperty("_BlendLayer2_BlendMaskUVSet", properties);

        _BlendLayer3 = FindProperty("_BlendLayer3", properties);
        _BlendLayer3_Color = FindProperty("_BlendLayer3_Color", properties);
        _BlendLayer3_AlbedoMap = FindProperty("_BlendLayer3_AlbedoMap", properties);
        _BlendLayer3_SpecGlossMap = FindProperty("_BlendLayer3_SpecGlossMap", properties);
        _BlendLayer3_NormalMap = FindProperty("_BlendLayer3_NormalMap", properties);
        _BlendLayer3_BlendMaskMap = FindProperty("_BlendLayer3_BlendMaskMap", properties);
        _BlendLayer3_BlendFactor = FindProperty("_BlendLayer3_BlendFactor", properties);
        _BlendLayer3_BlendFalloff = FindProperty("_BlendLayer3_BlendFalloff", properties);
        _BlendLayer3_Glossiness = FindProperty("_BlendLayer3_Glossiness", properties);
        _BlendLayer3_SpecColor = FindProperty("_BlendLayer3_SpecColor", properties);
        _BlendLayer3_NormalMapScale = FindProperty("_BlendLayer3_NormalMapScale", properties);
        _BlendLayer3_AlbedoMapScroll = FindProperty("_BlendLayer3_AlbedoMapScroll", properties);
        _BlendLayer3_BlendMaskMapScroll = FindProperty("_BlendLayer3_BlendMaskMapScroll", properties);
        _BlendLayer3_BlendMaskMapInvert = FindProperty("_BlendLayer3_BlendMaskMapInvert", properties);
        _BlendLayer3_UVSet = FindProperty("_BlendLayer3_UVSet", properties);
        _BlendLayer3_BlendMaskUVSet = FindProperty("_BlendLayer3_BlendMaskUVSet", properties);

        _DetailMask = FindProperty("_DetailMask", properties);
        _DetailAlbedoMap = FindProperty("_DetailAlbedoMap", properties);
        _DetailNormalMap = FindProperty("_DetailNormalMap", properties);
        _DetailNormalMapScale = FindProperty("_DetailNormalMapScale", properties);
        _DetailColor = FindProperty("_DetailColor", properties);
        _DetailLayer = FindProperty("_DetailLayer", properties);
        _DetailLayer_BlendFactor = FindProperty("_DetailLayer_BlendFactor", properties);
        _DetailLayer_BlendFalloff = FindProperty("_DetailLayer_BlendFalloff", properties);
        _DetailAlbedoMapScroll = FindProperty("_DetailAlbedoMapScroll", properties);

        _ApplyVertexAlpha = FindProperty("_ApplyVertexAlpha", properties);
        _ApplyVertexAlphaStrength = FindProperty("_ApplyVertexAlphaStrength", properties);

        _UVSec = FindProperty("_UVSec", properties);
        _Mode = FindProperty("_Mode", properties);
        _SrcBlend = FindProperty("_SrcBlend", properties);
        _DstBlend = FindProperty("_DstBlend", properties);
        _ZWrite = FindProperty("_ZWrite", properties);
        _Cull = FindProperty("_Cull", properties);
    }
}
#endif