using UnityEngine;

public class TerrainConfigComponent : MonoBehaviour
{
    [SerializeField] public bool CastShadows = true;
    [SerializeField] public LayerMask GroundMask;
    [SerializeField] public LayerMask WaterMask;
    [SerializeField] public PhysicMaterial GenericMaterial;
    [SerializeField] public PhysicMaterial WaterMaterial;
    [SerializeField] public Material Material;
    [SerializeField] public Material MarginMaterial;
    [SerializeField] public Texture[] AlbedoArrays = new Texture[3];
    [SerializeField] public Texture[] NormalArrays = new Texture[3];
    [SerializeField] public float HeightMapErrorMin;
    [SerializeField] public float HeightMapErrorMax;
    [SerializeField] public float BaseMapDistanceMin;
    [SerializeField] public float BaseMapDistanceMax;
    [SerializeField] public float ShaderLodMin;
    [SerializeField] public float ShaderLodMax;
    [SerializeField] public SplatType[] Splats = new SplatType[8];


    [System.Serializable]
    public class SplatOverlay
    {
        public Color Color;
        public float Smoothness;
        public float NormalIntensity;
        public float BlendFactor;
        public float BlendFalloff;
    }
}