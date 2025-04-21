#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RustStandardBlendLayerShaderGUI : ShaderGUI
{
    private bool showMain = true;
    private bool showDetail = false;
    private bool showVertex = false;
    private bool showAdvanced = false;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Find Properties
        MaterialProperty _Color = FindProperty("_Color", properties);
        MaterialProperty _MainTex = FindProperty("_MainTex", properties);
        MaterialProperty _Cutoff = FindProperty("_Cutoff", properties);
        MaterialProperty _Glossiness = FindProperty("_Glossiness", properties);
        MaterialProperty _Metallic = FindProperty("_Metallic", properties);
        MaterialProperty _MetallicGlossMap = FindProperty("_MetallicGlossMap", properties);
        MaterialProperty _BumpMap = FindProperty("_BumpMap", properties);
        MaterialProperty _BumpScale = FindProperty("_BumpScale", properties);
        MaterialProperty _OcclusionMap = FindProperty("_OcclusionMap", properties);
        MaterialProperty _OcclusionStrength = FindProperty("_OcclusionStrength", properties);
        MaterialProperty _OcclusionUVSet = FindProperty("_OcclusionUVSet", properties);
        MaterialProperty _EmissionColor = FindProperty("_EmissionColor", properties);
        MaterialProperty _EmissionMap = FindProperty("_EmissionMap", properties);

        MaterialProperty _DetailBlendLayer = FindProperty("_DetailBlendLayer", properties);
        MaterialProperty _DetailAlbedoMap = FindProperty("_DetailAlbedoMap", properties);
        MaterialProperty _DetailMetallicGlossMap = FindProperty("_DetailMetallicGlossMap", properties);
        MaterialProperty _DetailNormalMap = FindProperty("_DetailNormalMap", properties);
        MaterialProperty _DetailNormalMapScale = FindProperty("_DetailNormalMapScale", properties);
        MaterialProperty _DetailTintMap = FindProperty("_DetailTintMap", properties);
        MaterialProperty _DetailBlendMaskMap = FindProperty("_DetailBlendMaskMap", properties);
        MaterialProperty _DetailColor = FindProperty("_DetailColor", properties);
        MaterialProperty _DetailMetallic = FindProperty("_DetailMetallic", properties);
        MaterialProperty _DetailGlossiness = FindProperty("_DetailGlossiness", properties);
        MaterialProperty _DetailBlendFactor = FindProperty("_DetailBlendFactor", properties);
        MaterialProperty _DetailBlendFalloff = FindProperty("_DetailBlendFalloff", properties);
        MaterialProperty _DetailAlbedoMapScroll = FindProperty("_DetailAlbedoMapScroll", properties);
        MaterialProperty _DetailBlendMaskMapScroll = FindProperty("_DetailBlendMaskMapScroll", properties);
        MaterialProperty _DetailBlendMaskMapInvert = FindProperty("_DetailBlendMaskMapInvert", properties);
        MaterialProperty _DetailBlendMaskUVSet = FindProperty("_DetailBlendMaskUVSet", properties);
        MaterialProperty _DetailBlendMaskAddLowFreq = FindProperty("_DetailBlendMaskAddLowFreq", properties);
        MaterialProperty _DetailBlendMaskVertexSource = FindProperty("_DetailBlendMaskVertexSource", properties);
        MaterialProperty _DetailTintSource = FindProperty("_DetailTintSource", properties);
        MaterialProperty _DetailTintBlockSize = FindProperty("_DetailTintBlockSize", properties);
        MaterialProperty _DetailUseWorldXZ = FindProperty("_DetailUseWorldXZ", properties);

        MaterialProperty _BiomeLayer = FindProperty("_BiomeLayer", properties);
        MaterialProperty _BiomeLayer_TintSplatIndex = FindProperty("_BiomeLayer_TintSplatIndex", properties);

        MaterialProperty _ApplyVertexAlpha = FindProperty("_ApplyVertexAlpha", properties);
        MaterialProperty _ApplyVertexAlphaStrength = FindProperty("_ApplyVertexAlphaStrength", properties);
        MaterialProperty _ApplyVertexColor = FindProperty("_ApplyVertexColor", properties);
        MaterialProperty _ApplyVertexColorStrength = FindProperty("_ApplyVertexColorStrength", properties);

        MaterialProperty _MainTexScroll = FindProperty("_MainTexScroll", properties);
        MaterialProperty _UVSec = FindProperty("_UVSec", properties);
        MaterialProperty _Mode = FindProperty("_Mode", properties);
        MaterialProperty _SrcBlend = FindProperty("_SrcBlend", properties);
        MaterialProperty _DstBlend = FindProperty("_DstBlend", properties);
        MaterialProperty _ZWrite = FindProperty("_ZWrite", properties);
        MaterialProperty _Cull = FindProperty("_Cull", properties);
        MaterialProperty _DecalLayerMask = FindProperty("_DecalLayerMask", properties);
        MaterialProperty _EnvReflHorizonFade = FindProperty("_EnvReflHorizonFade", properties);
        MaterialProperty _EnvReflOcclusionStrength = FindProperty("_EnvReflOcclusionStrength", properties);

        // Main Section
        showMain = EditorGUILayout.Foldout(showMain, "Main Properties", true);
        if (showMain)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _MainTex, _Color);
            materialEditor.TexturePropertySingleLine(new GUIContent("Metallic"), _MetallicGlossMap, _Metallic);
            materialEditor.FloatProperty(_Glossiness, "Smoothness");
            materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BumpMap, _BumpScale);
            materialEditor.TexturePropertySingleLine(new GUIContent("Occlusion"), _OcclusionMap, _OcclusionStrength);
            materialEditor.FloatProperty(_OcclusionUVSet, "Occlusion UV Set");
            materialEditor.TexturePropertySingleLine(new GUIContent("Emission"), _EmissionMap, _EmissionColor);
            materialEditor.FloatProperty(_Cutoff, "Alpha Cutoff");
        }

        // Detail Section
        showDetail = EditorGUILayout.Foldout(showDetail, "Detail Properties", true);
        if (showDetail)
        {
            materialEditor.FloatProperty(_DetailBlendLayer, "Enable Detail Layer");
            if (_DetailBlendLayer.floatValue > 0.0f)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Albedo"), _DetailAlbedoMap, _DetailColor);
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Metallic"), _DetailMetallicGlossMap, _DetailMetallic);
                materialEditor.FloatProperty(_DetailGlossiness, "Detail Smoothness");
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Normal"), _DetailNormalMap, _DetailNormalMapScale);
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Tint Map"), _DetailTintMap);
                materialEditor.TexturePropertySingleLine(new GUIContent("Detail Blend Mask"), _DetailBlendMaskMap);
                materialEditor.FloatProperty(_DetailBlendFactor, "Blend Factor");
                materialEditor.FloatProperty(_DetailBlendFalloff, "Blend Falloff");
                materialEditor.VectorProperty(_DetailAlbedoMapScroll, "Albedo Scroll");
                materialEditor.VectorProperty(_DetailBlendMaskMapScroll, "Blend Mask Scroll");
                materialEditor.FloatProperty(_DetailBlendMaskMapInvert, "Invert Blend Mask");
                materialEditor.FloatProperty(_DetailBlendMaskUVSet, "Blend Mask UV Set");
                materialEditor.FloatProperty(_DetailBlendMaskAddLowFreq, "Add Low Frequency");
                materialEditor.FloatProperty(_DetailBlendMaskVertexSource, "Use Vertex Alpha for Mask");
                materialEditor.FloatProperty(_DetailTintSource, "Enable Tint");
                materialEditor.FloatProperty(_DetailTintBlockSize, "Tint Block Size");
                materialEditor.FloatProperty(_DetailUseWorldXZ, "Use World XZ for Detail");
            }
            materialEditor.FloatProperty(_BiomeLayer, "Enable Biome Layer");
            materialEditor.FloatProperty(_BiomeLayer_TintSplatIndex, "Biome Tint Splat Index");
        }

        // Vertex Section
        showVertex = EditorGUILayout.Foldout(showVertex, "Vertex Properties", true);
        if (showVertex)
        {
            materialEditor.FloatProperty(_ApplyVertexAlpha, "Apply Vertex Alpha");
            materialEditor.FloatProperty(_ApplyVertexAlphaStrength, "Vertex Alpha Strength");
            materialEditor.FloatProperty(_ApplyVertexColor, "Apply Vertex Color");
            materialEditor.FloatProperty(_ApplyVertexColorStrength, "Vertex Color Strength");
        }

        // Advanced Section
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Properties", true);
        if (showAdvanced)
        {
            materialEditor.VectorProperty(_MainTexScroll, "Main Tex Scroll");
            materialEditor.FloatProperty(_UVSec, "Secondary UV Set");
            materialEditor.FloatProperty(_Mode, "Rendering Mode (0=Opaque, 1=Cutout)");
            materialEditor.FloatProperty(_SrcBlend, "Source Blend");
            materialEditor.FloatProperty(_DstBlend, "Destination Blend");
            materialEditor.FloatProperty(_ZWrite, "ZWrite");
            materialEditor.FloatProperty(_Cull, "Cull Mode (0=Off, 1=Front, 2=Back)");
            materialEditor.FloatProperty(_DecalLayerMask, "Decal Layer Mask");
            materialEditor.FloatProperty(_EnvReflHorizonFade, "Env Reflection Horizon Fade");
            materialEditor.FloatProperty(_EnvReflOcclusionStrength, "Env Reflection Occlusion Strength");
        }

        // Rendering Mode Handling
        if (_Mode.floatValue == 0.0f)
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        else if (_Mode.floatValue == 1.0f)
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
    }
}
#endif