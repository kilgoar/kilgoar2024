using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewTerrainAsset", menuName = "Terrain/Terrain Asset")]
public class TerrainAsset : ScriptableObject
{
    [SerializeField] private TerrainData terrainData;
    
    // Public access to the data
    public TerrainData Data => terrainData;

    // Nested data class
    [Serializable]
    public class TerrainData
    {
        public GameObjectData m_GameObject;
        public int m_Enabled;
        public ScriptData m_Script;
        public string m_Name;
        public int CastShadows;
        public LayerMaskData GroundMask;
        public LayerMaskData WaterMask;
        public MaterialData GenericMaterial;
        public MaterialData WaterMaterial;
        public MaterialData Material;
        public MaterialData MarginMaterial;
        public TextureData[] AlbedoArrays;
        public TextureData[] NormalArrays;
        public float HeightMapErrorMin;
        public float HeightMapErrorMax;
        public float BaseMapDistanceMin;
        public float BaseMapDistanceMax;
        public float ShaderLodMin;
        public float ShaderLodMax;
        public SplatData[] Splats;
    }

    // Rest of the nested classes remain the same
    [Serializable]
    public class GameObjectData
    {
        public int m_FileID;
        public long m_PathID;
    }

    [Serializable]
    public class ScriptData
    {
        public int m_FileID;
        public int m_PathID;
    }

    [Serializable]
    public class LayerMaskData
    {
        public int m_Bits;
    }

    [Serializable]
    public class MaterialData
    {
        public int m_FileID;
        public long m_PathID;
    }

    [Serializable]
    public class TextureData
    {
        public int m_FileID;
        public long m_PathID;
    }

    [Serializable]
    public class SplatData
    {
        public string Name;
        public ColorData AridColor;
        public OverlayData AridOverlay;
        public ColorData TemperateColor;
        public OverlayData TemperateOverlay;
        public ColorData TundraColor;
        public OverlayData TundraOverlay;
        public ColorData ArcticColor;
        public OverlayData ArcticOverlay;
        public MaterialData Material;
        public float SplatTiling;
        public float UVMIXMult;
        public float UVMIXStart;
        public float UVMIXDist;
    }

    [Serializable]
    public class ColorData
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color ToUnityColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public class OverlayData
    {
        public ColorData Color;
        public float Smoothness;
        public float NormalIntensity;
        public float BlendFactor;
        public float BlendFalloff;
    }
}