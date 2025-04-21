
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RustStandardShaderGUI : ShaderGUI
{
    private static readonly string[] PassModeNames = { "Base", "Detail", "Particle", "Transmission/Subsurface" };

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Pass Mode Selection
        EditorGUILayout.LabelField("Pass Mode", EditorStyles.boldLabel);
        MaterialProperty passModeProp = FindProperty("_PassMode", properties, false);
        if (passModeProp != null)
        {
            int passMode = (int)passModeProp.floatValue;
            passMode = EditorGUILayout.Popup("Mode", passMode, PassModeNames);
            passModeProp.floatValue = passMode;
        }

        // Main Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__Color");
        DrawProperty(materialEditor, properties, "__MainTex");
        DrawProperty(materialEditor, properties, "__Cutoff");
        DrawProperty(materialEditor, properties, "__Glossiness");
        DrawProperty(materialEditor, properties, "__Metallic");
        DrawProperty(materialEditor, properties, "__MetallicGlossMap");
        DrawProperty(materialEditor, properties, "__SpecGlossMap");
        DrawProperty(materialEditor, properties, "__BumpScale");
        DrawProperty(materialEditor, properties, "__BumpMap");
        DrawProperty(materialEditor, properties, "__TangentMap");
        DrawProperty(materialEditor, properties, "__AnisotropyMap");
        DrawProperty(materialEditor, properties, "__OcclusionMap");
        DrawProperty(materialEditor, properties, "__OcclusionStrength");
        DrawProperty(materialEditor, properties, "__EmissionColor");
        DrawProperty(materialEditor, properties, "__EmissionMap");
        DrawProperty(materialEditor, properties, "__Parallax");
        DrawProperty(materialEditor, properties, "__ParallaxMap");

        // Detail Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Detail Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__DetailMask");
        DrawProperty(materialEditor, properties, "__DetailAlbedoMap");
        DrawProperty(materialEditor, properties, "__DetailMetallicGlossMap");
        DrawProperty(materialEditor, properties, "__DetailSpecGlossMap");
        DrawProperty(materialEditor, properties, "__DetailNormalMap");
        DrawProperty(materialEditor, properties, "__DetailNormalMapScale");
        DrawProperty(materialEditor, properties, "__DetailOcclusionMap");
        DrawProperty(materialEditor, properties, "__DetailOcclusionStrength");

        // Particle Layer
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Particle Layer", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__ParticleLayer_AlbedoMap");
        DrawProperty(materialEditor, properties, "__ParticleLayer_AlbedoColor");
        DrawProperty(materialEditor, properties, "__ParticleLayer_MetallicGlossMap");
        DrawProperty(materialEditor, properties, "__ParticleLayer_SpecGlossMap");
        DrawProperty(materialEditor, properties, "__ParticleLayer_NormalMap");
        DrawProperty(materialEditor, properties, "__ParticleLayer_BlendFactor");
        DrawProperty(materialEditor, properties, "__ParticleLayer_BlendFalloff");
        DrawProperty(materialEditor, properties, "__ParticleLayer_Glossiness");
        DrawProperty(materialEditor, properties, "__ParticleLayer_Metallic");
        DrawProperty(materialEditor, properties, "__ParticleLayer_NormalScale");
        DrawProperty(materialEditor, properties, "__ParticleLayer_Thickness");
        DrawProperty(materialEditor, properties, "__ParticleLayer_SpecColor");
        DrawProperty(materialEditor, properties, "__ParticleLayer_WorldDirection");
        DrawProperty(materialEditor, properties, "__ParticleLayer_MapTiling");

        // Tinting
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tinting", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__ApplyVertexColor");
        DrawProperty(materialEditor, properties, "__ApplyVertexAlpha");
        DrawProperty(materialEditor, properties, "__ApplyVertexColorStrength");
        DrawProperty(materialEditor, properties, "__ApplyVertexAlphaStrength");
        DrawProperty(materialEditor, properties, "__BiomeLayer_TintMask");
        DrawProperty(materialEditor, properties, "__BiomeLayer_TintColor");
        DrawProperty(materialEditor, properties, "__BiomeLayer");
        DrawProperty(materialEditor, properties, "__BiomeLayer_TintSplatIndex");

        // Transmission and Subsurface
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transmission and Subsurface", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__TransmissionMap");
        DrawProperty(materialEditor, properties, "__TransmissionColor");
        DrawProperty(materialEditor, properties, "__TransmissionScale");
        DrawProperty(materialEditor, properties, "__TransmissionMaskMap");
        DrawProperty(materialEditor, properties, "__SubsurfaceMaskMap");
        DrawProperty(materialEditor, properties, "__SubsurfaceScale");
        DrawProperty(materialEditor, properties, "__SubsurfaceProfile");
        DrawProperty(materialEditor, properties, "__TransmittanceColor");
        DrawProperty(materialEditor, properties, "__TransmissionMode");

        // Wetness
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wetness", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__WetnessLayer");
        DrawProperty(materialEditor, properties, "__WetnessLayer_Mask");
        DrawProperty(materialEditor, properties, "__WetnessLayer_WetAlbedoScale");
        DrawProperty(materialEditor, properties, "__WetnessLayer_WetSmoothness");
        DrawProperty(materialEditor, properties, "__WetnessLayer_Wetness");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer_BlendFactor");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer_BlendFalloff");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer_Range");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer_WetAlbedoScale");
        DrawProperty(materialEditor, properties, "__ShoreWetnessLayer_WetSmoothness");

        // Advanced Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Advanced Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "__ATDistance");
        DrawProperty(materialEditor, properties, "__AlphaDither");
        DrawProperty(materialEditor, properties, "__AlphaUVSec");
        DrawProperty(materialEditor, properties, "__Anisotropy");
        DrawProperty(materialEditor, properties, "__Cull");
        DrawProperty(materialEditor, properties, "__DecalLayerMask");
        DrawProperty(materialEditor, properties, "__DetailAlbedoMapScroll");
        DrawProperty(materialEditor, properties, "__DetailBlendFlags");
        DrawProperty(materialEditor, properties, "__DetailBlendType");
        DrawProperty(materialEditor, properties, "__DetailColor");
        DrawProperty(materialEditor, properties, "__DetailLayer");
        DrawProperty(materialEditor, properties, "__DetailOverlayMetallic");
        DrawProperty(materialEditor, properties, "__DetailOverlaySmoothness");
        DrawProperty(materialEditor, properties, "__DetailOverlaySpecular");
        DrawProperty(materialEditor, properties, "__DoubleSided");
        DrawProperty(materialEditor, properties, "__EmissionFresnel");
        DrawProperty(materialEditor, properties, "__EmissionFresnelInvert");
        DrawProperty(materialEditor, properties, "__EmissionFresnelPower");
        DrawProperty(materialEditor, properties, "__EmissionUVSec");
        DrawProperty(materialEditor, properties, "__EnergyConservingSpecularColor");
        DrawProperty(materialEditor, properties, "__EnvReflHorizonFade");
        DrawProperty(materialEditor, properties, "__EnvReflOcclusionStrength");
        DrawProperty(materialEditor, properties, "__Ior");
        DrawProperty(materialEditor, properties, "__MainTexScroll");
        DrawProperty(materialEditor, properties, "__MaterialType");
        DrawProperty(materialEditor, properties, "_Mode");
        DrawProperty(materialEditor, properties, "__OffsetEmissionOnly");
        DrawProperty(materialEditor, properties, "__Refraction");
        DrawProperty(materialEditor, properties, "__ShadowBiasScale");
        DrawProperty(materialEditor, properties, "__SpecColor");
        DrawProperty(materialEditor, properties, "__SrcBlend");
        DrawProperty(materialEditor, properties, "__DstBlend");
        DrawProperty(materialEditor, properties, "__Thickness");
        DrawProperty(materialEditor, properties, "__UVSec");
        DrawProperty(materialEditor, properties, "__Wind");
        DrawProperty(materialEditor, properties, "__WindAmplitude1");
        DrawProperty(materialEditor, properties, "__WindAmplitude2");
        DrawProperty(materialEditor, properties, "__WindFrequency");
        DrawProperty(materialEditor, properties, "__WindNoiseScale");
        DrawProperty(materialEditor, properties, "__WindNormalOffset");
        DrawProperty(materialEditor, properties, "__WindPhase");
        DrawProperty(materialEditor, properties, "__ZWrite");

        // Debug
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_DebugVertexColor");
        DrawProperty(materialEditor, properties, "_DebugAlbedo");

        // Fix material button
        EditorGUILayout.Space();
        if (GUILayout.Button("Fix Material Textures"))
        {
            FixMaterialTextures(material);
        }

        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }

    private void DrawProperty(MaterialEditor editor, MaterialProperty[] properties, string name)
    {
        MaterialProperty prop = FindProperty(name, properties, false);
        if (prop != null)
        {
            editor.ShaderProperty(prop, prop.displayName);
        }
        else
        {
            EditorGUILayout.LabelField($"Property {name} not found");
        }
    }

    private void FixMaterialTextures(Material material)
    {
        if (!material.shader.name.Equals("Custom/RustStandard")) return;

        SetTextureIfNull(material, "__MainTex", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__BumpMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "__MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__SpecGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__OcclusionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__EmissionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__ParallaxMap", Texture2D.blackTexture);
        SetTextureIfNull(material, "__TransmissionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__TransmissionMaskMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__SubsurfaceMaskMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__DetailMask", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__DetailAlbedoMap", Texture2D.grayTexture);
        SetTextureIfNull(material, "__DetailNormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "__DetailMetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__DetailSpecGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__DetailOcclusionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__ParticleLayer_AlbedoMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__ParticleLayer_MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__ParticleLayer_SpecGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__ParticleLayer_NormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "__BiomeLayer_TintMask", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__WetnessLayer_Mask", Texture2D.whiteTexture);
        SetTextureIfNull(material, "__TangentMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "__AnisotropyMap", Texture2D.blackTexture);

        Debug.Log($"Fixed textures for {material.name}");
    }

    private void SetTextureIfNull(Material material, string propertyName, Texture defaultTexture)
    {
        if (!material.HasProperty(propertyName) || material.GetTexture(propertyName) == null)
        {
            material.SetTexture(propertyName, defaultTexture);
        }
    }
}
#endif