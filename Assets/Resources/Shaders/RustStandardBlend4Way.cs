using UnityEditor;
using UnityEngine;

public class RustStandardBlend4WayShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        // Main Properties
        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_Color");
        DrawProperty(materialEditor, properties, "_MainTex");
        DrawProperty(materialEditor, properties, "_Cutoff");
        DrawProperty(materialEditor, properties, "_Glossiness");
        DrawProperty(materialEditor, properties, "_Metallic");
        DrawProperty(materialEditor, properties, "_MetallicGlossMap");
        DrawProperty(materialEditor, properties, "_BumpScale");
        DrawProperty(materialEditor, properties, "_BumpMap");
        DrawProperty(materialEditor, properties, "_OcclusionMap");
        DrawProperty(materialEditor, properties, "_OcclusionStrength");
        DrawProperty(materialEditor, properties, "_EmissionColor");
        DrawProperty(materialEditor, properties, "_EmissionMap");

        // Blend Layer 1
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Blend Layer 1", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_BlendLayer1");
        DrawProperty(materialEditor, properties, "_BlendLayer1_Color");
        DrawProperty(materialEditor, properties, "_BlendLayer1_AlbedoMap");
        DrawProperty(materialEditor, properties, "_BlendLayer1_MetallicGlossMap");
        DrawProperty(materialEditor, properties, "_BlendLayer1_NormalMap");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendMaskMap");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendFactor");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendFalloff");
        DrawProperty(materialEditor, properties, "_BlendLayer1_Glossiness");
        DrawProperty(materialEditor, properties, "_BlendLayer1_Metallic");
        DrawProperty(materialEditor, properties, "_BlendLayer1_NormalMapScale");
        DrawProperty(materialEditor, properties, "_BlendLayer1_AlbedoMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendMaskMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer1_AlbedoTintMask");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendMaskMapInvert");
        DrawProperty(materialEditor, properties, "_BlendLayer1_UVSet");
        DrawProperty(materialEditor, properties, "_BlendLayer1_BlendMaskUVSet");

        // Blend Layer 2
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Blend Layer 2", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_BlendLayer2");
        DrawProperty(materialEditor, properties, "_BlendLayer2_Color");
        DrawProperty(materialEditor, properties, "_BlendLayer2_AlbedoMap");
        DrawProperty(materialEditor, properties, "_BlendLayer2_MetallicGlossMap");
        DrawProperty(materialEditor, properties, "_BlendLayer2_NormalMap");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendMaskMap");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendFactor");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendFalloff");
        DrawProperty(materialEditor, properties, "_BlendLayer2_Glossiness");
        DrawProperty(materialEditor, properties, "_BlendLayer2_Metallic");
        DrawProperty(materialEditor, properties, "_BlendLayer2_NormalMapScale");
        DrawProperty(materialEditor, properties, "_BlendLayer2_AlbedoMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendMaskMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer2_AlbedoTintMask");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendMaskMapInvert");
        DrawProperty(materialEditor, properties, "_BlendLayer2_UVSet");
        DrawProperty(materialEditor, properties, "_BlendLayer2_BlendMaskUVSet");

        // Blend Layer 3
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Blend Layer 3", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_BlendLayer3");
        DrawProperty(materialEditor, properties, "_BlendLayer3_Color");
        DrawProperty(materialEditor, properties, "_BlendLayer3_AlbedoMap");
        DrawProperty(materialEditor, properties, "_BlendLayer3_MetallicGlossMap");
        DrawProperty(materialEditor, properties, "_BlendLayer3_NormalMap");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendMaskMap");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendFactor");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendFalloff");
        DrawProperty(materialEditor, properties, "_BlendLayer3_Glossiness");
        DrawProperty(materialEditor, properties, "_BlendLayer3_Metallic");
        DrawProperty(materialEditor, properties, "_BlendLayer3_NormalMapScale");
        DrawProperty(materialEditor, properties, "_BlendLayer3_AlbedoMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendMaskMapScroll");
        DrawProperty(materialEditor, properties, "_BlendLayer3_AlbedoTintMask");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendMaskMapInvert");
        DrawProperty(materialEditor, properties, "_BlendLayer3_UVSet");
        DrawProperty(materialEditor, properties, "_BlendLayer3_BlendMaskUVSet");

        // Detail Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Detail Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_DetailMask");
        DrawProperty(materialEditor, properties, "_DetailAlbedoMap");
        DrawProperty(materialEditor, properties, "_DetailMetallicGlossMap");
        DrawProperty(materialEditor, properties, "_DetailNormalMap");
        DrawProperty(materialEditor, properties, "_DetailNormalMapScale");
        DrawProperty(materialEditor, properties, "_DetailOcclusionMap");
        DrawProperty(materialEditor, properties, "_DetailOcclusionStrength");
        DrawProperty(materialEditor, properties, "_DetailColor");
        DrawProperty(materialEditor, properties, "_DetailLayer");
        DrawProperty(materialEditor, properties, "_DetailLayer_BlendFactor");
        DrawProperty(materialEditor, properties, "_DetailLayer_BlendFalloff");
        DrawProperty(materialEditor, properties, "_DetailAlbedoMapScroll");
        DrawProperty(materialEditor, properties, "_DetailOverlayMetallic");
        DrawProperty(materialEditor, properties, "_DetailOverlaySmoothness");

        // Biome Tinting
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Biome Tinting", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_BiomeLayer_TintMask");
        DrawProperty(materialEditor, properties, "_BiomeLayer_TintColor");
        DrawProperty(materialEditor, properties, "_BiomeLayer");
        DrawProperty(materialEditor, properties, "_BiomeLayer_TintSplatIndex");
        DrawProperty(materialEditor, properties, "_AlbedoTintMask");

        // Vertex Color/Alpha
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vertex Color/Alpha", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_ApplyVertexAlpha");
        DrawProperty(materialEditor, properties, "_ApplyVertexAlphaStrength");

        // Advanced Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Advanced Properties", EditorStyles.boldLabel);
        DrawProperty(materialEditor, properties, "_MainTexScroll");
        DrawProperty(materialEditor, properties, "_UVSec");
        DrawProperty(materialEditor, properties, "_Mode");
        DrawProperty(materialEditor, properties, "_SrcBlend");
        DrawProperty(materialEditor, properties, "_DstBlend");
        DrawProperty(materialEditor, properties, "_ZWrite");
        DrawProperty(materialEditor, properties, "_Cull");
        DrawProperty(materialEditor, properties, "_DoubleSided");
        DrawProperty(materialEditor, properties, "_DecalLayerMask");
        DrawProperty(materialEditor, properties, "_EnvReflHorizonFade");
        DrawProperty(materialEditor, properties, "_EnvReflOcclusionStrength");
        DrawProperty(materialEditor, properties, "_DetailApplyBeforeBlendLayers");
        DrawProperty(materialEditor, properties, "_DetailBlendType");
        DrawProperty(materialEditor, properties, "_DetailBlendFlags");
        DrawProperty(materialEditor, properties, "_DetailMaskSeparateTilingOffset");

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
        if (!material.shader.name.Equals("Custom/Rust/StandardBlend4Way")) return;

        SetTextureIfNull(material, "_MainTex", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BumpMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "_MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_OcclusionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_EmissionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_DetailMask", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_DetailAlbedoMap", Texture2D.grayTexture);
        SetTextureIfNull(material, "_DetailNormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "_DetailMetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_DetailOcclusionMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BiomeLayer_TintMask", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer1_AlbedoMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer1_MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer1_NormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "_BlendLayer1_BlendMaskMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer2_AlbedoMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer2_MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer2_NormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "_BlendLayer2_BlendMaskMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer3_AlbedoMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer3_MetallicGlossMap", Texture2D.whiteTexture);
        SetTextureIfNull(material, "_BlendLayer3_NormalMap", Texture2D.normalTexture);
        SetTextureIfNull(material, "_BlendLayer3_BlendMaskMap", Texture2D.whiteTexture);

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