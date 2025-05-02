using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
[CreateAssetMenu(menuName = "Rust/Terrain Config", fileName = "TerrainConfig")]
public class TerrainConfig : ScriptableObject
{
    [SerializeField] private bool castShadows = true;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask waterMask;
    [SerializeField] private PhysicMaterial genericMaterial;
    [SerializeField] private PhysicMaterial waterMaterial;
    [SerializeField] private Material material;
    [SerializeField] private Material marginMaterial;
    [SerializeField] private Texture[] albedoArrays = new Texture[3];
    [SerializeField] private Texture[] normalArrays = new Texture[3];
    [SerializeField] private float heightMapErrorMin;
    [SerializeField] private float heightMapErrorMax;
    [SerializeField] private float baseMapDistanceMin;
    [SerializeField] private float baseMapDistanceMax;
    [SerializeField] private float shaderLodMin;
    [SerializeField] private float shaderLodMax;
    [SerializeField] private SplatType[] splats = new SplatType[8];
    [SerializeField] private string[] splatNames; 
    [SerializeField] private string groundMaskName; 
    [SerializeField] private string waterMaskName;
    [SerializeField] private string[] topologyNames; 
    [SerializeField] private string genericMaterialName; 

    // Properties for direct access
    public bool CastShadows { get => castShadows; set => castShadows = value; }
    public LayerMask GroundMask { get => groundMask; set => groundMask = value; }
    public LayerMask WaterMask { get => waterMask; set => waterMask = value; }
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

    // LOD-aware texture getters
    public Texture AlbedoArray => albedoArrays[Mathf.Clamp(QualitySettings.masterTextureLimit, 0, albedoArrays.Length - 1)];
    public Texture NormalArray => normalArrays[Mathf.Clamp(QualitySettings.masterTextureLimit, 0, normalArrays.Length - 1)];
    public float GetTextureArrayWidth() => AlbedoArray.width; 

    public void LoadTextureArrays()
    {
        // Paths for each LOD level
        string[] albedoPaths = new string[]
        {
            "assets/content/nature/terrain/atlas/terrain4_albedo_array.asset",      // LOD 0
            "assets/content/nature/terrain/atlas/terrain4_albedo_array_lod1.asset", // LOD 1
            "assets/content/nature/terrain/atlas/terrain4_albedo_array_lod2.asset"  // LOD 2
        };

        string[] normalPaths = new string[]
        {
            "assets/content/nature/terrain/atlas/terrain4_normal_array.asset",      // LOD 0
            "assets/content/nature/terrain/atlas/terrain4_normal_array_lod1.asset", // LOD 1
            "assets/content/nature/terrain/atlas/terrain4_normal_array_lod2.asset"  // LOD 2
        };

        // Load albedo arrays
        for (int i = 0; i < albedoArrays.Length; i++)
        {
            if (albedoArrays[i] == null)
            {
                albedoArrays[i] = AssetManager.LoadAsset<Texture2DArray>(albedoPaths[i]);
                if (albedoArrays[i] == null)
                {
                    Debug.LogError($"Failed to load Terrain4_AlbedoArray LOD {i} from AssetManager at path: {albedoPaths[i]}");
                }
                else
                {
                    Debug.Log($"Successfully loaded Terrain4_AlbedoArray LOD {i} from {albedoPaths[i]}.");
                }
            }
        }

        // Load normal arrays
        for (int i = 0; i < normalArrays.Length; i++)
        {
            if (normalArrays[i] == null)
            {
                normalArrays[i] = AssetManager.LoadAsset<Texture2DArray>(normalPaths[i]);
                if (normalArrays[i] == null)
                {
                    Debug.LogError($"Failed to load Terrain4_NormalArray LOD {i} from AssetManager at path: {normalPaths[i]}");
                }
                else
                {
                    Debug.Log($"Successfully loaded Terrain4_NormalArray LOD {i} from {normalPaths[i]}.");
                }
            }
        }
    }

    private void OnEnable()
    {
        AssetManager.Callbacks.BundlesLoaded += OnBundlesLoaded;

    }
	
	private void OnBundlesLoaded()
    {
		/*
			string materialPath = "assets/content/nature/terrain/materials/terrain.v3.mat";
			Material material = AssetManager.LoadAsset<Material>(materialPath);
			if (material == null)
			{
				Debug.LogError($"[TerrainManager] Failed to load terrain material from {materialPath}.");
				return;
			}
			TerrainManager.Land.materialType = Terrain.MaterialType.Custom;
			TerrainManager.Land.materialTemplate = material;
			
			
            LoadTextureArrays();
		*/

    }




    public void GetSplatColorsAndVectors(int splatIndex, out Color[] colors, out Vector4[] vectors)
    {
        if (splatIndex < 0 || splatIndex >= splats.Length)
        {
            Debug.LogError($"Splat index {splatIndex} out of range (0-{splats.Length - 1})");
            colors = null;
            vectors = null;
            return;
        }

        SplatType splat = splats[splatIndex];
        colors = new Color[] { splat.AridColor, splat.TemperateColor, splat.TundraColor, splat.ArcticColor };
        vectors = new Vector4[] {
            new Vector4(splat.AridOverlay.Smoothness, splat.AridOverlay.NormalIntensity, splat.AridOverlay.BlendFactor, splat.AridOverlay.BlendFalloff),
            new Vector4(splat.TemperateOverlay.Smoothness, splat.TemperateOverlay.NormalIntensity, splat.TemperateOverlay.BlendFactor, splat.TemperateOverlay.BlendFalloff),
            new Vector4(splat.TundraOverlay.Smoothness, splat.TundraOverlay.NormalIntensity, splat.TundraOverlay.BlendFactor, splat.TundraOverlay.BlendFalloff),
            new Vector4(splat.ArcticOverlay.Smoothness, splat.ArcticOverlay.NormalIntensity, splat.ArcticOverlay.BlendFactor, splat.ArcticOverlay.BlendFalloff)
        };
    }

    public Color[] GetSplatColors(int splatIndex)
    {
        if (splatIndex < 0 || splatIndex >= splats.Length)
        {
            Debug.LogError($"Splat index {splatIndex} out of range (0-{splats.Length - 1})");
            return null;
        }
        SplatType splat = splats[splatIndex];
        return new Color[] { splat.AridColor, splat.TemperateColor, splat.TundraColor, splat.ArcticColor };
    }

    public PhysicMaterial[] GetSplatPhysicMaterials()
    {
        PhysicMaterial[] materials = new PhysicMaterial[splats.Length];
        for (int i = 0; i < splats.Length; i++)
        {
            materials[i] = splats[i].Material ?? genericMaterial;
        }
        return materials;
    }

    public float GetSplatTiling(int splatIndex)
    {
        if (splatIndex < 0 || splatIndex >= splats.Length)
        {
            Debug.LogError($"Splat index {splatIndex} out of range (0-{splats.Length - 1})");
            return 5f; // Default tiling
        }
        return splats[splatIndex].SplatTiling;
    }

	public Vector3[] GetSplatUVMixData()
	{
		Vector3[] uvMix = new Vector3[splats.Length];
		for (int i = 0; i < splats.Length; i++)
		{
			uvMix[i] = new Vector3(splats[i].UVMixMult, splats[i].UVMixStart, splats[i].UVMixDist); // No 1f / UVMixDist
		}
		return uvMix;
	}

    public GroundType GetGroundType(bool useRaycast, RaycastHit hit)
    {
        if (!useRaycast || !hit.collider)
        {
            return GroundType.None; // Fallback if no raycast data
        }

        Vector2 uv = hit.textureCoord;
        int x = Mathf.FloorToInt(uv.x * TerrainManager.Land.terrainData.alphamapWidth);
        int y = Mathf.FloorToInt(uv.y * TerrainManager.Land.terrainData.alphamapHeight);
        float[,,] splatMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Ground);
        if (x < 0 || x >= splatMap.GetLength(0) || y < 0 || y >= splatMap.GetLength(1))
        {
            return GroundType.None;
        }

        float maxStrength = 0f;
        int dominantIndex = 0;
        for (int i = 0; i < splatMap.GetLength(2); i++)
        {
            if (splatMap[x, y, i] > maxStrength)
            {
                maxStrength = splatMap[x, y, i];
                dominantIndex = i;
            }
        }

        return dominantIndex switch
        {
            0 => GroundType.Dirt,
            1 => GroundType.Snow,
            2 => GroundType.Sand,
            3 => GroundType.HardSurface, // Rock as HardSurface
            4 => GroundType.Grass,
            5 => GroundType.Grass, // Forest as Grass
            6 => GroundType.Gravel, // Stones as Gravel
            7 => GroundType.Gravel,
            _ => GroundType.None
        };
    }

    public enum GroundType
    {
        None,
        HardSurface,
        Grass,
        Sand,
        Snow,
        Dirt,
        Gravel
    }

    public float[] GetSplatTilings() => Splats.Select(s => s.SplatTiling).ToArray();
    public Vector3[] GetUVMIXParameters() => Splats.Select(s => new Vector3(s.UVMixMult, s.UVMixStart, s.UVMixDist)).ToArray(); 
    public Color[] GetAridColors() => Splats.Select(s => s.AridColor).ToArray();
    public Color[] GetTemperateColors() => Splats.Select(s => s.TemperateColor).ToArray();
    public Color[] GetTundraColors() => Splats.Select(s => s.TundraColor).ToArray();
    public Color[] GetArcticColors() => Splats.Select(s => s.ArcticColor).ToArray();
	public Color[] GetJungleColors() => Splats.Select(s => s.JungleColor).ToArray();

    public void GetAridOverlayData(out Color[] colors, out Vector4[] parameters)
    {
        colors = Splats.Select(s => s.AridOverlay?.Color ?? Color.black).ToArray();
        parameters = Splats.Select(s => new Vector4(s.AridOverlay?.Smoothness ?? 0.5f, s.AridOverlay?.NormalIntensity ?? 0f, s.AridOverlay?.BlendFactor ?? 0f, s.AridOverlay?.BlendFalloff ?? 0f)).ToArray();
    }

    public void GetTemperateOverlayData(out Color[] colors, out Vector4[] parameters)
    {
        colors = Splats.Select(s => s.TemperateOverlay?.Color ?? Color.black).ToArray();
        parameters = Splats.Select(s => new Vector4(s.TemperateOverlay?.Smoothness ?? 0.5f, s.TemperateOverlay?.NormalIntensity ?? 0f, s.TemperateOverlay?.BlendFactor ?? 0f, s.TemperateOverlay?.BlendFalloff ?? 0f)).ToArray();
    }

    public void GetTundraOverlayData(out Color[] colors, out Vector4[] parameters)
    {
        colors = Splats.Select(s => s.TundraOverlay?.Color ?? Color.black).ToArray();
        parameters = Splats.Select(s => new Vector4(s.TundraOverlay?.Smoothness ?? 0.5f, s.TundraOverlay?.NormalIntensity ?? 0f, s.TundraOverlay?.BlendFactor ?? 0f, s.TundraOverlay?.BlendFalloff ?? 0f)).ToArray();
    }

    public void GetArcticOverlayData(out Color[] colors, out Vector4[] parameters)
    {
        colors = Splats.Select(s => s.ArcticOverlay?.Color ?? Color.black).ToArray();
        parameters = Splats.Select(s => new Vector4(s.ArcticOverlay?.Smoothness ?? 0.5f, s.ArcticOverlay?.NormalIntensity ?? 0f, s.ArcticOverlay?.BlendFactor ?? 0f, s.ArcticOverlay?.BlendFalloff ?? 0f)).ToArray();
    }
	
	public void GetJungleOverlayData(out Color[] colors, out Vector4[] parameters)
    {
        colors = Splats.Select(s => s.JungleOverlay?.Color ?? Color.black).ToArray();
        parameters = Splats.Select(s => new Vector4(s.JungleOverlay?.Smoothness ?? 0.5f, s.JungleOverlay?.NormalIntensity ?? 0f, s.JungleOverlay?.BlendFactor ?? 0f, s.JungleOverlay?.BlendFalloff ?? 0f)).ToArray();
    }

}