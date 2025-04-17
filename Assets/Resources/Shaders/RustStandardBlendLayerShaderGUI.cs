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
        MaterialProperty _Color = FindProperty("__Color", properties);
        MaterialProperty _MainTex = FindProperty("__MainTex", properties);
        MaterialProperty _Cutoff = FindProperty("__Cutoff", properties);
        MaterialProperty _Glossiness = FindProperty("__Glossiness", properties);
        MaterialProperty _Metallic = FindProperty("__Metallic", properties);
        MaterialProperty _MetallicGlossMap = FindProperty("__MetallicGlossMap", properties);
        MaterialProperty _BumpMap = FindProperty("__BumpMap", properties);
        MaterialProperty _BumpScale = FindProperty("__BumpScale", properties);
        MaterialProperty _OcclusionMap = FindProperty("__OcclusionMap", properties);
        MaterialProperty _OcclusionStrength = FindProperty("__OcclusionStrength", properties);
        MaterialProperty _OcclusionUVSet = FindProperty("__OcclusionUVSet", properties);
        MaterialProperty _EmissionColor = FindProperty("__EmissionColor", properties);
        MaterialProperty _EmissionMap = FindProperty("__EmissionMap", properties);

        MaterialProperty _DetailBlendLayer = FindProperty("__DetailBlendLayer", properties);
        MaterialProperty _DetailAlbedoMap = FindProperty("__DetailAlbedoMap", properties);
        MaterialProperty _DetailMetallicGlossMap = FindProperty("__DetailMetallicGlossMap", properties);
        MaterialProperty _DetailNormalMap = FindProperty("__DetailNormalMap", properties);
        MaterialProperty _DetailNormalMapScale = FindProperty("__DetailNormalMapScale", properties);
        MaterialProperty _DetailTintMap = FindProperty("__DetailTintMap", properties);
        MaterialProperty _DetailBlendMaskMap = FindProperty("__DetailBlendMaskMap", properties);
        MaterialProperty _DetailColor = FindProperty("__DetailColor", properties);
        MaterialProperty _DetailMetallic = FindProperty("__DetailMetallic", properties);
        MaterialProperty _DetailGlossiness = FindProperty("__DetailGlossiness", properties);
        MaterialProperty _DetailBlendFactor = FindProperty("__DetailBlendFactor", properties);
        MaterialProperty _DetailBlendFalloff = FindProperty("__DetailBlendFalloff", properties);
        MaterialProperty _DetailAlbedoMapScroll = FindProperty("__DetailAlbedoMapScroll", properties);
        MaterialProperty _DetailBlendMaskMapScroll = FindProperty("__DetailBlendMaskMapScroll", properties);
        MaterialProperty _DetailBlendMaskMapInvert = FindProperty("__DetailBlendMaskMapInvert", properties);
        MaterialProperty _DetailBlendMaskUVSet = FindProperty("__DetailBlendMaskUVSet", properties);
        MaterialProperty _DetailBlendMaskAddLowFreq = FindProperty("__DetailBlendMaskAddLowFreq", properties);
        MaterialProperty _DetailBlendMaskVertexSource = FindProperty("__DetailBlendMaskVertexSource", properties);
        MaterialProperty _DetailTintSource = FindProperty("__DetailTintSource", properties);
        MaterialProperty _DetailTintBlockSize = FindProperty("__DetailTintBlockSize", properties);
        MaterialProperty _DetailUseWorldXZ = FindProperty("__DetailUseWorldXZ", properties);

        MaterialProperty _BiomeLayer = FindProperty("__BiomeLayer", properties);
        MaterialProperty _BiomeLayer_TintSplatIndex = FindProperty("__BiomeLayer_TintSplatIndex", properties);

        MaterialProperty _ApplyVertexAlpha = FindProperty("__ApplyVertexAlpha", properties);
        MaterialProperty _ApplyVertexAlphaStrength = FindProperty("__ApplyVertexAlphaStrength", properties);
        MaterialProperty _ApplyVertexColor = FindProperty("__ApplyVertexColor", properties);
        MaterialProperty _ApplyVertexColorStrength = FindProperty("__ApplyVertexColorStrength", properties);

        MaterialProperty _MainTexScroll = FindProperty("__MainTexScroll", properties);
        MaterialProperty _UVSec = FindProperty("__UVSec", properties);
        MaterialProperty _Mode = FindProperty("__Mode", properties);
        MaterialProperty _SrcBlend = FindProperty("__SrcBlend", properties);
        MaterialProperty _DstBlend = FindProperty("__DstBlend", properties);
        MaterialProperty _ZWrite = FindProperty("__ZWrite", properties);
        MaterialProperty _Cull = FindProperty("__Cull", properties);
        MaterialProperty _DecalLayerMask = FindProperty("__DecalLayerMask", properties);
        MaterialProperty _EnvReflHorizonFade = FindProperty("__EnvReflHorizonFade", properties);
        MaterialProperty _EnvReflOcclusionStrength = FindProperty("__EnvReflOcclusionStrength", properties);

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