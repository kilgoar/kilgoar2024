using UnityEngine;

[System.Serializable]
public class TerrainComponent : MonoBehaviour
{
    [SerializeField] private bool castShadows = true; // UInt8 CastShadows = 1
    [SerializeField] private int groundMask = 8388608; // BitField GroundMask { m_Bits = 8388608 }
    [SerializeField] private int waterMask = 16; // BitField WaterMask { m_Bits = 16 }
    [SerializeField] private PhysicMaterial genericMaterial; // PPtr<$PhysicMaterial> GenericMaterial
    [SerializeField] private PhysicMaterial waterMaterial; // PPtr<$PhysicMaterial> WaterMaterial
    [SerializeField] private Material material; // PPtr<$Material> Material
    [SerializeField] private Material marginMaterial; // PPtr<$Material> MarginMaterial
    [SerializeField] private Texture[] albedoArrays = new Texture[3]; // vector AlbedoArrays [3 entries]
    [SerializeField] private Texture[] normalArrays = new Texture[3]; // vector NormalArrays [3 entries]
    [SerializeField] private float heightMapErrorMin = 10f; // float HeightMapErrorMin = 10
    [SerializeField] private float heightMapErrorMax = 100f; // float HeightMapErrorMax = 100
    [SerializeField] private float baseMapDistanceMin = 100f; // float BaseMapDistanceMin = 100
    [SerializeField] private float baseMapDistanceMax = 500f; // float BaseMapDistanceMax = 500
    [SerializeField] private float shaderLodMin = 100f; // float ShaderLodMin = 100
    [SerializeField] private float shaderLodMax = 600f; // float ShaderLodMax = 600
    [SerializeField] private SplatType[] splats = new SplatType[8]; // SplatType Splats [8 entries]

    // Nested SplatType class
    [System.Serializable]
    public class SplatType
    {
        [SerializeField] private string name;
        [SerializeField] private Color aridColor;
        [SerializeField] private SplatOverlay aridOverlay;
        [SerializeField] private Color temperateColor;
        [SerializeField] private SplatOverlay temperateOverlay;
        [SerializeField] private Color tundraColor;
        [SerializeField] private SplatOverlay tundraOverlay;
        [SerializeField] private Color arcticColor;
        [SerializeField] private SplatOverlay arcticOverlay;
        [SerializeField] private PhysicMaterial material;
        [SerializeField] private float splatTiling;
        [SerializeField] private float uvMixMult;
        [SerializeField] private float uvMixStart;
        [SerializeField] private float uvMixDist;

        // Properties
        public string Name { get => name; set => name = value; }
        public Color AridColor { get => aridColor; set => aridColor = value; }
        public SplatOverlay AridOverlay { get => aridOverlay; set => aridOverlay = value; }
        public Color TemperateColor { get => temperateColor; set => temperateColor = value; }
        public SplatOverlay TemperateOverlay { get => temperateOverlay; set => temperateOverlay = value; }
        public Color TundraColor { get => tundraColor; set => tundraColor = value; }
        public SplatOverlay TundraOverlay { get => tundraOverlay; set => tundraOverlay = value; }
        public Color ArcticColor { get => arcticColor; set => arcticColor = value; }
        public SplatOverlay ArcticOverlay { get => arcticOverlay; set => arcticOverlay = value; }
        public PhysicMaterial Material { get => material; set => material = value; }
        public float SplatTiling { get => splatTiling; set => splatTiling = value; }
        public float UVMixMult { get => uvMixMult; set => uvMixMult = value; }
        public float UVMixStart { get => uvMixStart; set => uvMixStart = value; }
        public float UVMixDist { get => uvMixDist; set => uvMixDist = value; }
    }

    [System.Serializable]
    public class SplatOverlay
    {
        [SerializeField] private Color color;
        [SerializeField] private float smoothness;
        [SerializeField] private float normalIntensity;
        [SerializeField] private float blendFactor;
        [SerializeField] private float blendFalloff;

        public Color Color { get => color; set => color = value; }
        public float Smoothness { get => smoothness; set => smoothness = value; }
        public float NormalIntensity { get => normalIntensity; set => normalIntensity = value; }
        public float BlendFactor { get => blendFactor; set => blendFactor = value; }
        public float BlendFalloff { get => blendFalloff; set => blendFalloff = value; }
    }

    // Properties
    public bool CastShadows { get => castShadows; set => castShadows = value; }
    public int GroundMask { get => groundMask; set => groundMask = value; }
    public int WaterMask { get => waterMask; set => waterMask = value; }
    public PhysicMaterial GenericMaterial { get => genericMaterial; set => genericMaterial = value; }
    public PhysicMaterial WaterMaterial { get => waterMaterial; set => waterMaterial = value; }
    public Material Material { get => material; set => material = value; }
    public Material MarginMaterial { get => marginMaterial; set => marginMaterial = value; }
    public Texture[] AlbedoArrays { get => albedoArrays; set => albedoArrays = value; }
    public Texture[] NormalArrays { get => normalArrays; set => normalArrays = value; }
    public float HeightMapErrorMin { get => heightMapErrorMin; set => heightMapErrorMin = value; }
    public float HeightMapErrorMax { get => heightMapErrorMax; set => heightMapErrorMax = value; }
    public float BaseMapDistanceMin { get => baseMapDistanceMin; set => baseMapDistanceMin = value; }
    public float BaseMapDistanceMax { get => baseMapDistanceMax; set => baseMapDistanceMax = value; }
    public float ShaderLodMin { get => shaderLodMin; set => shaderLodMin = value; }
    public float ShaderLodMax { get => shaderLodMax; set => shaderLodMax = value; }
    public SplatType[] Splats { get => splats; set => splats = value; }
}