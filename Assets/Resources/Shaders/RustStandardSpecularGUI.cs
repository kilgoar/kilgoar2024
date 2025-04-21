#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RustStandardSpecularShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Main Section
        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), FindProperty("_MainTex", properties), FindProperty("_Color", properties));
        materialEditor.FloatProperty(FindProperty("_Cutoff", properties), "Alpha Cutoff");
        materialEditor.TexturePropertySingleLine(new GUIContent("Specular Gloss"), FindProperty("_SpecGlossMap", properties), FindProperty("_SpecColor", properties));
        materialEditor.FloatProperty(FindProperty("_Glossiness", properties), "Smoothness");
        materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), FindProperty("_BumpMap", properties), FindProperty("_BumpScale", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Occlusion"), FindProperty("_OcclusionMap", properties), FindProperty("_OcclusionStrength", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Emission"), FindProperty("_EmissionMap", properties), FindProperty("_EmissionColor", properties));

        EditorGUILayout.Space();

        // Detail Section
        EditorGUILayout.LabelField("Detail Properties", EditorStyles.boldLabel);
        materialEditor.FloatProperty(FindProperty("_DetailLayer", properties), "Detail Layer");
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Mask"), FindProperty("_DetailMask", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Albedo"), FindProperty("_DetailAlbedoMap", properties), FindProperty("_DetailColor", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Specular"), FindProperty("_DetailSpecGlossMap", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Normal"), FindProperty("_DetailNormalMap", properties), FindProperty("_DetailNormalMapScale", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Detail Occlusion"), FindProperty("_DetailOcclusionMap", properties), FindProperty("_DetailOcclusionStrength", properties));
        materialEditor.VectorProperty(FindProperty("_DetailAlbedoMapScroll", properties), "Detail Albedo Scroll");
        materialEditor.FloatProperty(FindProperty("_DetailBlendFlags", properties), "Detail Blend Flags");
        materialEditor.FloatProperty(FindProperty("_DetailBlendType", properties), "Detail Blend Type");
        materialEditor.FloatProperty(FindProperty("_DetailOverlaySmoothness", properties), "Detail Overlay Smoothness");
        materialEditor.FloatProperty(FindProperty("_DetailOverlaySpecular", properties), "Detail Overlay Specular");

        EditorGUILayout.Space();

        // Particle Layer Section
        EditorGUILayout.LabelField("Particle Layer Properties", EditorStyles.boldLabel);
        materialEditor.FloatProperty(FindProperty("_ParticleLayer", properties), "Particle Layer");
        materialEditor.TexturePropertySingleLine(new GUIContent("Particle Albedo"), FindProperty("_ParticleLayer_AlbedoMap", properties), FindProperty("_ParticleLayer_AlbedoColor", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Particle Specular"), FindProperty("_ParticleLayer_SpecGlossMap", properties), FindProperty("_ParticleLayer_SpecColor", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Particle Normal"), FindProperty("_ParticleLayer_NormalMap", properties), FindProperty("_ParticleLayer_NormalScale", properties));
        materialEditor.FloatProperty(FindProperty("_ParticleLayer_Glossiness", properties), "Particle Smoothness");
        materialEditor.FloatProperty(FindProperty("_ParticleLayer_BlendFactor", properties), "Particle Blend Factor");
        materialEditor.FloatProperty(FindProperty("_ParticleLayer_BlendFalloff", properties), "Particle Blend Falloff");
        materialEditor.FloatProperty(FindProperty("_ParticleLayer_Thickness", properties), "Particle Thickness");
        materialEditor.VectorProperty(FindProperty("_ParticleLayer_WorldDirection", properties), "Particle World Direction");
        materialEditor.VectorProperty(FindProperty("_ParticleLayer_MapTiling", properties), "Particle Map Tiling");

        EditorGUILayout.Space();

        // Biome Layer Section
        EditorGUILayout.LabelField("Biome Layer Properties", EditorStyles.boldLabel);
        materialEditor.FloatProperty(FindProperty("_BiomeLayer", properties), "Biome Layer");
        materialEditor.TexturePropertySingleLine(new GUIContent("Biome Tint Mask"), FindProperty("_BiomeLayer_TintMask", properties), FindProperty("_BiomeLayer_TintColor", properties));
        materialEditor.FloatProperty(FindProperty("_BiomeLayer_TintSplatIndex", properties), "Biome Tint Splat Index");

        EditorGUILayout.Space();

        // Wetness Section
        EditorGUILayout.LabelField("Wetness Properties", EditorStyles.boldLabel);
        materialEditor.FloatProperty(FindProperty("_WetnessLayer", properties), "Wetness Layer");
        materialEditor.TexturePropertySingleLine(new GUIContent("Wetness Mask"), FindProperty("_WetnessLayer_Mask", properties));
        materialEditor.FloatProperty(FindProperty("_WetnessLayer_WetAlbedoScale", properties), "Wet Albedo Scale");
        materialEditor.FloatProperty(FindProperty("_WetnessLayer_WetSmoothness", properties), "Wet Smoothness");
        materialEditor.FloatProperty(FindProperty("_WetnessLayer_Wetness", properties), "Wetness");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer", properties), "Shore Wetness Layer");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer_BlendFactor", properties), "Shore Blend Factor");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer_BlendFalloff", properties), "Shore Blend Falloff");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer_Range", properties), "Shore Range");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer_WetAlbedoScale", properties), "Shore Wet Albedo Scale");
        materialEditor.FloatProperty(FindProperty("_ShoreWetnessLayer_WetSmoothness", properties), "Shore Wet Smoothness");

        EditorGUILayout.Space();

        // Advanced Section
        EditorGUILayout.LabelField("Advanced Properties", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Tangent Map"), FindProperty("_TangentMap", properties));
        materialEditor.TexturePropertySingleLine(new GUIContent("Anisotropy Map"), FindProperty("_AnisotropyMap", properties));
        materialEditor.FloatProperty(FindProperty("_ApplyVertexAlpha", properties), "Apply Vertex Alpha");
        materialEditor.FloatProperty(FindProperty("_ApplyVertexAlphaStrength", properties), "Vertex Alpha Strength");
        materialEditor.FloatProperty(FindProperty("_ApplyVertexColor", properties), "Apply Vertex Color");
        materialEditor.FloatProperty(FindProperty("_ApplyVertexColorStrength", properties), "Vertex Color Strength");
        materialEditor.FloatProperty(FindProperty("_Cull", properties), "Cull");
        materialEditor.FloatProperty(FindProperty("_DecalLayerMask", properties), "Decal Layer Mask");
        materialEditor.FloatProperty(FindProperty("_DoubleSided", properties), "Double Sided");
        materialEditor.FloatProperty(FindProperty("_DstBlend", properties), "Destination Blend");
        materialEditor.FloatProperty(FindProperty("_EmissionUVSec", properties), "Emission UV Secondary");
        materialEditor.FloatProperty(FindProperty("_EnvReflHorizonFade", properties), "Env Reflection Horizon Fade");
        materialEditor.FloatProperty(FindProperty("_EnvReflOcclusionStrength", properties), "Env Reflection Occlusion Strength");
        materialEditor.VectorProperty(FindProperty("_MainTexScroll", properties), "Main Tex Scroll");
        materialEditor.FloatProperty(FindProperty("_Mode", properties), "Mode");
        materialEditor.FloatProperty(FindProperty("_OcclusionUVSet", properties), "Occlusion UV Set");
        materialEditor.FloatProperty(FindProperty("_OffsetEmissionOnly", properties), "Offset Emission Only");
        materialEditor.FloatProperty(FindProperty("_ShadowBiasScale", properties), "Shadow Bias Scale");
        materialEditor.FloatProperty(FindProperty("_SrcBlend", properties), "Source Blend");
        materialEditor.FloatProperty(FindProperty("_UVSec", properties), "UV Secondary");
        materialEditor.FloatProperty(FindProperty("_ZWrite", properties), "ZWrite");

        // Render queue and blending options
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }
}
#endif