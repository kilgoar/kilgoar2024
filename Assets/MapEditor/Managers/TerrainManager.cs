
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;


#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif
using System.Collections;
using System.Threading.Tasks;
using RustMapEditor.Maths;
using RustMapEditor.Variables;
using static WorldConverter;
using static AreaManager;

public static class TerrainManager
{
	
	#region Fields and Properties
    // Terrain References
    public static Terrain Land { get; private set; }
    public static Terrain LandMask { get; private set; }
    public static Terrain Water { get; private set; }
    public static Material WaterMaterial { get; private set; }
    public static Vector3 TerrainSize => Land.terrainData.size;
    public static Vector3 MapOffset => 0.5f * TerrainSize;
    public static Vector3 TerrainPosition => Land.transform.position;
    public static Vector3 TerrainSizeInverse => new Vector3(1f / TerrainSize.x, 1f / TerrainSize.y, 1f / TerrainSize.z);

    // Resolution
    public static int HeightMapRes { get; private set; }
    public static int SplatMapRes { get; private set; }
    public static int AlphaMapRes => HeightMapRes - 1;
    public static float SplatSize => Land.terrainData.size.x / SplatMapRes;
    public static float SplatRatio => Land.terrainData.heightmapResolution / (float)SplatMapRes;

    // Height Data
    public static float[,] Height;
    public static float[,] Slope;
    public static float[,] Curvature;
    private static Vector2 HeightMapCentre => new Vector2(0.5f, 0.5f);

    // Splat Data
    public static float[,,] Ground { get; private set; }
    public static float[,,] Biome { get; private set; }
    public static Vector4[,] BiomeMap { get; private set; }
    public static bool[,] Alpha { get; private set; }
    public static bool[,] AlphaMask { get; private set; }
    public static bool[,] SpawnMap { get; private set; }
    public static float[,] CliffMap { get; private set; }
    public static float[,,] CliffField { get; private set; }
    public static float[][,,] Topology { get; private set; } = new float[TerrainTopology.COUNT][,,];

    // Layer State
    public static LayerType CurrentLayerType { get; private set; }
    public static int TopologyLayer => TerrainTopology.TypeToIndex((int)TopologyLayerEnum);
    public static TerrainTopology.Enum TopologyLayerEnum { get; private set; }
    public static bool LayerDirty { get; private set; }
    public static bool AlphaDirty { get; set; } = true;
    public static int Layers => LayerCount(CurrentLayerType);

    // Textures
    private static Texture FilterTexture;
    private static Texture2D HeightTexture;
    public static RenderTexture HeightSlopeTexture;
    private static RenderTexture AlphaTexture;
    private static RenderTexture BiomeTexture;
    public static Texture2D RuntimeNormalMap { get; private set; }

    // Configuration
    public static TerrainConfig _config;
    public static bool IsLoading { get; private set; }
    public static uint RandomSeed { get; set; }
    public static float MinHeight { get; set; }
    public static float MaxHeight { get; set; }

    // Terrain Layers
    private static TerrainLayer[] GroundLayers;
    private static TerrainLayer[] BiomeLayers;
    private static TerrainLayer[] TopologyLayers;
    private static TerrainLayer[] MaskLayers;
    #endregion

    // Enum for LayerType (kept as is from original)
    public enum LayerType
    {
        Ground,
        Biome,
        Alpha,
        Topology
    }
	
	public enum TerrainType
    {
        Land,
		LandMask,
        Water
    }
	
	#if UNITY_EDITOR
    #region Init
    [InitializeOnLoadMethod]
    private static void Init()
    {	
        TerrainCallbacks.heightmapChanged += HeightMapChanged;
        TerrainCallbacks.textureChanged += SplatMapChanged;
        EditorApplication.update += OnProjectLoad;
		ShowLandMask();
    }

    private static void OnProjectLoad()
    {
			EditorApplication.update -= OnProjectLoad;
			FilterTexture = Resources.Load<Texture>("Textures/Brushes/White128");
			SetTerrainReferences();		
    }
    #endregion
	#endif
	
	
	public static void RuntimeInit()
	{
		TerrainCallbacks.heightmapChanged += HeightMapChanged;
        TerrainCallbacks.textureChanged += SplatMapChanged;
		
		AssetManager.Callbacks.BundlesLoaded += OnBundlesLoaded;
		
		FilterTexture = Resources.Load<Texture>("Textures/Brushes/White128");
        SetTerrainReferences();
		
		/*
		#if UNITY_EDITOR
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.GenerateNormalMap(HeightMapRes - 1, Progress.Start("Generate Normal Map")));
		#else
		CoroutineManager.StartCoroutine(Coroutines.GenerateNormalMap(HeightMapRes - 1, -1)); // No progress ID at runtime
		#endif
		*/
	}
	
	public static void OnBundlesLoaded()
	{		
		_config = Resources.Load<TerrainConfig>("TerrainConfig");
        if (_config == null)
        {
            Debug.LogError("TerrainConfig not found at Resources/TerrainConfig!");
        }

		SetTerrainLayers();
		LoadTerrainAssets();
		
		ConfigureShaderGlobals(Land);
		
		ApplyConfigToTerrain(Land);
	}
	
	public static void PopulateTerrainArrays()
	{
		if (Land == null || Land.terrainData == null)
		{
			Debug.LogError("Cannot populate terrain arrays: Land terrain or its data is null.");
			return;
		}

		// Populate Height array
		Height = Land.terrainData.GetHeights(0, 0, HeightMapRes, HeightMapRes);
		SyncHeightTexture();

		// Populate Alpha array
		Alpha = Land.terrainData.GetHoles(0, 0, AlphaMapRes, AlphaMapRes);


		// Populate Ground and Biome arrays
		Ground = Land.terrainData.GetAlphamaps(0, 0, SplatMapRes, SplatMapRes);
		//Biome = Ground; // Assuming Biome shares the same alphamap data initially; adjust if separate biome data exists
		//SyncBiomeTexture();
		SyncAlphaTexture();
		LayerDirty = false;

	/*
		// Populate Topology arrays
		if (Topology == null || Topology.Length != TerrainTopology.COUNT)
		{
			Topology = new float[TerrainTopology.COUNT][,,];
		}
		for (int i = 0; i < TerrainTopology.COUNT; i++)
		{
			// Assuming TopologyData provides the topology layer data; adjust if sourced differently
			Topology[i] = TopologyData.GetTopologyLayer(TerrainTopology.IndexToType(i));
			if (Topology[i] == null || Topology[i].GetLength(0) != SplatMapRes || Topology[i].GetLength(1) != SplatMapRes)
			{
				Topology[i] = new float[SplatMapRes, SplatMapRes, 2]; // Default to 2 channels (active/inactive)
			}
		}
*/

	}
	
	private static void ApplyConfigToTerrain(Terrain terrain)
	{
		if (_config == null || terrain == null || terrain.terrainData == null)
		{
			Debug.LogError("Cannot apply config: _config or terrain is null.");
			return;
		}

		// Set terrain properties from _config
		terrain.castShadows = _config.CastShadows;
		terrain.materialType = Terrain.MaterialType.Custom;
		terrain.materialTemplate = LoadTerrainMaterial();
		terrain.GetComponent<TerrainCollider>().sharedMaterial = _config.GenericMaterial;

		// Set terrain layers based on _config.Splats
		TerrainLayer[] layers = new TerrainLayer[_config.Splats.Length];
		for (int i = 0; i < _config.Splats.Length; i++)
		{
			SplatType splat = _config.Splats[i];
			TerrainLayer layer = new TerrainLayer
			{
				tileSize = new Vector2(splat.SplatTiling, splat.SplatTiling),
				tileOffset = Vector2.zero,
				specular = splat.TemperateColor,
				metallic = 0f,
				smoothness = splat.TemperateOverlay?.Smoothness ?? 0.5f
				// Textures handled via shader globals
			};
			layers[i] = layer;
		}
		terrain.terrainData.terrainLayers = layers;
	}
	
	
    public static void BytesToAlpha(byte[] data)
    {
        int res = Mathf.RoundToInt(Mathf.Sqrt(data.Length / sizeof(float)));
        if (res != AlphaMapRes)
        {
            Debug.LogWarning($"Alpha data resolution ({res}) does not match AlphaMapRes ({AlphaMapRes}). ");
        }

        Alpha = new bool[AlphaMapRes, AlphaMapRes];
        for (int i = 0; i < AlphaMapRes; i++)
        {
            for (int j = 0; j < AlphaMapRes; j++)
            {
                int dataIndex = i * res + j;
                Alpha[i, j] = (dataIndex < data.Length) ? (BitUtility.Byte2Float(data[dataIndex]) > 0.5f) : true;
            }
        }
        SyncAlphaTexture();
    }

    public static void SyncAlphaTexture()
    {
        if (Alpha == null || Alpha.GetLength(0) != AlphaMapRes)
        {
            Debug.LogError("Alpha data is not initialized or resolution mismatch.");
            return;
        }

        Texture2D tempTexture = new Texture2D(AlphaMapRes, AlphaMapRes, TextureFormat.RGBA32, false, true);
        Color32[] colors = new Color32[AlphaMapRes * AlphaMapRes];

        for (int z = 0; z < AlphaMapRes; z++)
        {
            for (int x = 0; x < AlphaMapRes; x++)
            {
                float value = Alpha[z, x] ? 1f : 0f;
                colors[z * AlphaMapRes + x] = new Color(value, value, value, value);
            }
        }

        tempTexture.SetPixels32(colors);
        tempTexture.Apply(true, false);

        if (AlphaTexture == null || !AlphaTexture.IsCreated())
        {
            AlphaTexture = new RenderTexture(AlphaMapRes, AlphaMapRes, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            AlphaTexture.Create();
        }

        Graphics.Blit(tempTexture, AlphaTexture);
        UnityEngine.Object.Destroy(tempTexture);
        Shader.SetGlobalTexture("Terrain_Alpha", AlphaTexture);
    }
	
    public static void BytesToBiomeTexture(byte[] data)
    {
        int res = Mathf.RoundToInt(Mathf.Sqrt(data.Length / 4f));
        if (res != SplatMapRes)
        {
            Debug.LogWarning($"Biome data resolution ({res}) does not match SplatMapRes ({SplatMapRes}).");
        }
        
        Biome = new float[SplatMapRes, SplatMapRes, 4];
        for (int i = 0; i < SplatMapRes; i++)
        {
            for (int j = 0; j < SplatMapRes; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    int dataIndex = (k * res + i) * res + j;
                    Biome[i, j, k] = (dataIndex < data.Length) ? BitUtility.Byte2Float(data[dataIndex]) : 0f;
                }
            }
        }
        SyncBiomeTexture();
    }

	public static void SyncHeightSlopeTexture(int targetResolution)
    {
        if (Height == null)
        {
            Debug.LogError("[TerrainManager] Height array is null. Cannot sync HeightSlopeTexture.");
            return;
        }

        if (HeightSlopeTexture == null || HeightSlopeTexture.width != targetResolution || !HeightSlopeTexture.IsCreated())
        {
            HeightSlopeTexture = new RenderTexture(targetResolution, targetResolution, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear)
            {
                name = "Terrain_HeightSlope",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = true,
                autoGenerateMips = true
            };
            HeightSlopeTexture.Create();
        }

        var processor = new HeightSlopeProcessor();
        processor.SourceSize = HeightMapRes;
        processor.DestinationSize = targetResolution;
        int sourcePower = Mathf.ClosestPowerOfTwo(processor.SourceSize);
        processor.Pixels = new Color[processor.DestinationSize * processor.DestinationSize];

        Texture2D tempTexture = new Texture2D(processor.DestinationSize, processor.DestinationSize, TextureFormat.RGFloat, false, true)
        {
            name = "Terrain_HeightSlope_Temp",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        processor.BlockSize = sourcePower / processor.DestinationSize;
        float blockScale = 1f / (processor.BlockSize * processor.BlockSize);

        processor.HeightOffset = TerrainPosition.y;
        processor.HeightScale = TerrainSize.y * blockScale * 0.25f * BitUtility.Short2Float(1);
        processor.SlopeScale = blockScale * BitUtility.Short2Float(1) * 0.375f;
        processor.NormalY = TerrainSize.x / TerrainSize.y / processor.SourceSize;

        // Convert Height array (float[,]) to Color32[] for processing
        processor.HeightColors = new Color32[HeightMapRes * HeightMapRes];
        for (int y = 0; y < HeightMapRes; y++)
        {
            for (int x = 0; x < HeightMapRes; x++)
            {
                ushort height = (ushort)(Height[y, x] * 65535f); // Convert 0-1 float to 0-65535 ushort
                processor.HeightColors[y * HeightMapRes + x] = new Color32((byte)(height & 0xFF), 0, (byte)(height >> 8), 255);
            }
        }

        Parallel.For(0, processor.DestinationSize, z => processor.ProcessRow(z));

        tempTexture.SetPixels(processor.Pixels);
        tempTexture.Apply(false);

        Graphics.Blit(tempTexture, HeightSlopeTexture);

        RenderTexture temp1 = RenderTexture.GetTemporary(processor.DestinationSize, processor.DestinationSize, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        RenderTexture temp2 = RenderTexture.GetTemporary(processor.DestinationSize, processor.DestinationSize, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        Material blurMaterial = new Material(AssetManager.LoadAsset<Shader>("assets/content/shaders/resources/separableblur.shader"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Graphics.Blit(tempTexture, temp1);
        float offset = 1f / processor.DestinationSize;
        for (int i = 0; i < 4; i++)
        {
            blurMaterial.SetVector("offsets", new Vector4(offset, 0f, 0f, 0f));
            Graphics.Blit(temp1, temp2, blurMaterial, 2);
            blurMaterial.SetVector("offsets", new Vector4(0f, offset, 0f, 0f));
            Graphics.Blit(temp2, temp1, blurMaterial, 2);
        }

        Graphics.Blit(temp1, HeightSlopeTexture);
        UnityEngine.Object.DestroyImmediate(blurMaterial);
        RenderTexture.ReleaseTemporary(temp1);
        RenderTexture.ReleaseTemporary(temp2);
        UnityEngine.Object.DestroyImmediate(tempTexture);
    }

    private static void SyncBiomeTexture()
    {
        if (Biome == null || Biome.GetLength(0) != SplatMapRes)
        {
            Debug.LogError("Biome data is not initialized or resolution mismatch.");
            return;
        }

        Texture2D tempTexture = new Texture2D(SplatMapRes, SplatMapRes, TextureFormat.RGBA32, false, true);
        Color32[] colors = new Color32[SplatMapRes * SplatMapRes];

        for (int z = 0; z < SplatMapRes; z++)
        {
            for (int x = 0; x < SplatMapRes; x++)
            {
                colors[z * SplatMapRes + x] = new Color(
                    Biome[z, x, 0],
                    Biome[z, x, 1],
                    Biome[z, x, 2],
                    Biome[z, x, 3]
                );
            }
        }

        tempTexture.SetPixels32(colors);
        tempTexture.Apply(true, false);

        if (BiomeTexture == null || !BiomeTexture.IsCreated())
        {
            BiomeTexture = new RenderTexture(SplatMapRes, SplatMapRes, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                wrapMode = TextureWrapMode.Clamp,
                enableRandomWrite = true
            };
            BiomeTexture.Create();
        }

        Graphics.Blit(tempTexture, BiomeTexture);
        UnityEngine.Object.Destroy(tempTexture);
        Shader.SetGlobalTexture("Terrain_Biome", BiomeTexture);
    }
	
	
	public static void LoadTerrainAssets()
	{
		Debug.LogError("load terrain assets working...");
		_config = Resources.Load<TerrainConfig>("TerrainConfig");
		Debug.LogError("config loaded");
		GameObject terrainAssetTextPrefab = AssetManager.LoadAsset<GameObject>("assets/prefabs/engine/testlevel terrain.prefab");
		if (terrainAssetTextPrefab == null)
		{
			Debug.LogError("terrain4 prefab not loading");
			return;
		}
		CreateTerrainConfig();
		Debug.LogError("working...");
		
		_config.AlbedoArrays[0] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_albedo_array.asset");
		_config.AlbedoArrays[1] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_albedo_array_lod1.asset");
		_config.AlbedoArrays[2] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_albedo_array_lod2.asset");
		_config.NormalArrays[0] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_normal_array.asset");
		_config.NormalArrays[1] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_normal_array_lod1.asset");
		_config.NormalArrays[2] = AssetManager.LoadAsset<Texture>("assets/content/nature/terrain/atlas/terrain4_normal_array_lod2.asset");

		// Validate loading
		foreach (var tex in _config.AlbedoArrays.Concat(_config.NormalArrays))
		{
			if (tex == null) Debug.LogWarning("Failed to load a terrain texture array.");
		}
	}
	

    public static void CreateTerrainConfig()
    {
        try
        {
            TerrainConfig config = _config ?? ScriptableObject.CreateInstance<TerrainConfig>();
            config.name = "RebuiltTerrainConfig";
            Debug.Log("Created new TerrainConfig instance: RebuiltTerrainConfig");
            Debug.Log("Hardcoding TerrainConfig from prefab dump (m_PathID = 210)...");

            // Scalars from dump
            config.CastShadows = true;
            Debug.Log($"TerrainConfig.CastShadows set to {config.CastShadows}");

            config.GroundMask = 8388608;
            Debug.Log($"TerrainConfig.GroundMask set to {config.GroundMask}");

            config.WaterMask = 16;
            Debug.Log($"TerrainConfig.WaterMask set to {config.WaterMask}");

            config.HeightMapErrorMin = 10f;
            Debug.Log($"TerrainConfig.HeightMapErrorMin set to {config.HeightMapErrorMin}");

            config.HeightMapErrorMax = 100f;
            Debug.Log($"TerrainConfig.HeightMapErrorMax set to {config.HeightMapErrorMax}");

            config.BaseMapDistanceMin = 100f;
            Debug.Log($"TerrainConfig.BaseMapDistanceMin set to {config.BaseMapDistanceMin}");

            config.BaseMapDistanceMax = 500f;
            Debug.Log($"TerrainConfig.BaseMapDistanceMax set to {config.BaseMapDistanceMax}");

            config.ShaderLodMin = 100f;
            Debug.Log($"TerrainConfig.ShaderLodMin set to {config.ShaderLodMin}");

            config.ShaderLodMax = 600f;
            Debug.Log($"TerrainConfig.ShaderLodMax set to {config.ShaderLodMax}");

            // Splats array from dump
            config.Splats = new SplatType[8]
            {
                new SplatType
                {
                    Name = "Dirt",
                    AridColor = new Color(0.8f, 0.7775281f, 0.7191011f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TemperateColor = new Color(0.7f, 0.6845133f, 0.6597345f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TundraColor = new Color(0.773f, 0.739761f, 0.739761f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(0.1795656f, 0.184f, 0.139656f, 0f), Smoothness = 1f, NormalIntensity = 0f, BlendFactor = 1f, BlendFalloff = 32f },
                    ArcticColor = new Color(0.704098f, 0.7391568f, 0.745f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.97f, 0.9861538f, 1f, 1f), Smoothness = 0.6f, NormalIntensity = 0.282f, BlendFactor = 0.64f, BlendFalloff = 6.1f },
                    SplatTiling = 4.5f,
                    UVMixMult = 0.33f,
                    UVMixStart = 5f,
                    UVMixDist = 100f
                },
                new SplatType
                {
                    Name = "Snow",
                    AridColor = new Color(0.8742f, 0.9035684f, 0.93f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TemperateColor = new Color(0.8742f, 0.9035684f, 0.93f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TundraColor = new Color(0.8742f, 0.9035684f, 0.93f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    ArcticColor = new Color(0.8742f, 0.9035684f, 0.93f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    SplatTiling = 6f,
                    UVMixMult = 0.05f,
                    UVMixStart = 15f,
                    UVMixDist = 50f
                },
                new SplatType
                {
                    Name = "Sand",
                    AridColor = new Color(0.7098039f, 0.6536111f, 0.5749412f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TemperateColor = new Color(0.6588235f, 0.6482823f, 0.6061177f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TundraColor = new Color(0.5f, 0.4901961f, 0.4509804f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    ArcticColor = new Color(0.530689f, 0.5773377f, 0.589f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    SplatTiling = 4.5f,
                    UVMixMult = 0.1f,
                    UVMixStart = 5f,
                    UVMixDist = 50f
                },
                new SplatType
                {
                    Name = "Rock",
                    AridColor = new Color(0.85f, 0.7567085f, 0.61455f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(0.75f, 0.7232143f, 0.6734694f, 1f), Smoothness = 0f, NormalIntensity = 0.5f, BlendFactor = 0.5f, BlendFalloff = 16f },
                    TemperateColor = new Color(0.6509804f, 0.6141859f, 0.566353f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TundraColor = new Color(0.65f, 0.6047468f, 0.5224683f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    ArcticColor = new Color(0.6365f, 0.661625f, 0.67f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.874f, 0.912f, 0.95f, 1f), Smoothness = 0.4f, NormalIntensity = 0.25f, BlendFactor = 0.6f, BlendFalloff = 20f },
                    SplatTiling = 10f,
                    UVMixMult = 0.125f,
                    UVMixStart = 10f,
                    UVMixDist = 50f
                },
                new SplatType
                {
                    Name = "Grass",
                    AridColor = new Color(0.74f, 0.728377f, 0.5850262f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(0.7215686f, 0.675817f, 0.6117647f, 1f), Smoothness = 0.1f, NormalIntensity = 1f, BlendFactor = 0.75f, BlendFalloff = 10f },
                    TemperateColor = new Color(0.4784314f, 0.5803922f, 0.3764706f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(0.4784314f, 0.5803922f, 0.3764706f, 1f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0f, BlendFalloff = 16f },
                    TundraColor = new Color(0.62f, 0.5783009f, 0.372f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 1f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0f, BlendFalloff = 16f },
                    ArcticColor = new Color(0.7588235f, 0.8051961f, 0.8431373f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.892562f, 0.946281f, 1f, 1f), Smoothness = 0.6f, NormalIntensity = 0f, BlendFactor = 0.66f, BlendFalloff = 8f },
                    SplatTiling = 4f,
                    UVMixMult = 0.1f,
                    UVMixStart = 10f,
                    UVMixDist = 150f
                },
                new SplatType
                {
                    Name = "Forest",
                    AridColor = new Color(0.8f, 0.7267974f, 0.5803922f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(0.7f, 0.6613065f, 0.5944723f, 0.9490196f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 16f },
                    TemperateColor = new Color(0.68f, 0.68f, 0.5448193f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(0.3491461f, 0.4509804f, 0f, 0.9019608f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.2f, BlendFalloff = 4f },
                    TundraColor = new Color(0.6f, 0.5228571f, 0.36f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(0.6f, 0.5401961f, 0.3607843f, 0.5019608f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.2f, BlendFalloff = 4f },
                    ArcticColor = new Color(0.851f, 0.8465748f, 0.784622f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.9f, 0.9434782f, 1f, 1f), Smoothness = 0.5f, NormalIntensity = 0.5f, BlendFactor = 0.3f, BlendFalloff = 6f },
                    SplatTiling = 3.5f,
                    UVMixMult = 0.2f,
                    UVMixStart = 5f,
                    UVMixDist = 150f
                },
                new SplatType
                {
                    Name = "Stones",
                    AridColor = new Color(0.8509804f, 0.7333465f, 0.5795177f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TemperateColor = new Color(0.72f, 0.6829715f, 0.6048f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    TundraColor = new Color(0.7f, 0.6488764f, 0.5623596f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    ArcticColor = new Color(0.558f, 0.58425f, 0.6f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.93f, 0.9608f, 1f, 0.8039216f), Smoothness = 0f, NormalIntensity = 0.5f, BlendFactor = 0.4f, BlendFalloff = 16f },
                    SplatTiling = 10f,
                    UVMixMult = 0.75f,
                    UVMixStart = 5f,
                    UVMixDist = 200f
                },
                new SplatType
                {
                    Name = "Gravel",
                    AridColor = new Color(0.85f, 0.7416667f, 0.6375f, 1f),
                    AridOverlay = new SplatOverlay { Color = new Color(0.348f, 0.3042722f, 0.2605445f, 1f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.1f, BlendFalloff = 4f },
                    TemperateColor = new Color(0.64f, 0.619085f, 0.5939869f, 1f),
                    TemperateOverlay = new SplatOverlay { Color = new Color(0.638f, 0.6161273f, 0.560802f, 1f), Smoothness = 0.25f, NormalIntensity = 0.25f, BlendFactor = 0f, BlendFalloff = 16f },
                    TundraColor = new Color(0.6f, 0.5746988f, 0.5385543f, 1f),
                    TundraOverlay = new SplatOverlay { Color = new Color(1f, 1f, 1f, 0f), Smoothness = 0f, NormalIntensity = 1f, BlendFactor = 0.5f, BlendFalloff = 0.5f },
                    ArcticColor = new Color(0.65f, 0.65f, 0.65f, 1f),
                    ArcticOverlay = new SplatOverlay { Color = new Color(0.93f, 0.9766666f, 1f, 0.4196078f), Smoothness = 0.6f, NormalIntensity = 1f, BlendFactor = 2f, BlendFalloff = 24f },
                    SplatTiling = 5f,
                    UVMixMult = 0.25f,
                    UVMixStart = 5f,
                    UVMixDist = 75f
                }
            };
            Debug.Log($"TerrainConfig.Splats set with {config.Splats.Length} elements from prefab dump");

            _config = config;
            Debug.Log("Assigned rebuilt config to TerrainManager._config");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to rebuild TerrainConfig: {e.Message}");
        }
    }

	private static void YoinkTerrainTexturing(GameObject sourcePrefab, Terrain targetTerrain)
	{
		if (sourcePrefab == null || targetTerrain == null)
		{
			Debug.LogError("Source prefab or target terrain is null");
			return;
		}

		TerrainTexturing sourceTexturing = sourcePrefab.GetComponent<TerrainTexturing>();
		if (sourceTexturing == null)
		{
			Debug.LogError("TerrainTexturing component not found on source prefab");
			return;
		}

		TerrainTexturing targetTexturing = targetTerrain.gameObject.GetComponent<TerrainTexturing>();
		if (targetTexturing == null)
		{
			targetTexturing = targetTerrain.gameObject.AddComponent<TerrainTexturing>();
		}

		try
		{
			// Fields derived from Land terrain (sizes, resolutions, etc.)
			targetTexturing.terrainMaxDimension = Mathf.Max(targetTerrain.terrainData.size.x, targetTerrain.terrainData.size.z);
			targetTexturing.textureResolution = Mathf.ClosestPowerOfTwo(targetTerrain.terrainData.heightmapResolution) >> 1; // Half the heightmap res, as per original logic
			targetTexturing.pixelSize = targetTexturing.terrainMaxDimension / targetTexturing.textureResolution;

			// Shore data - check if prefab has precomputed data; otherwise, regenerate
			if (sourceTexturing.shoreDistances != null && sourceTexturing.shoreDistances.Length == targetTexturing.textureResolution * targetTexturing.textureResolution)
			{
				targetTexturing.shoreDistances = (float[])sourceTexturing.shoreDistances.Clone();
			}
			else
			{
				targetTexturing.shoreDistances = null; // Will be regenerated in Refresh()
			}

			if (sourceTexturing.shoreVectors != null && sourceTexturing.shoreVectors.Length == targetTexturing.textureResolution * targetTexturing.textureResolution)
			{
				targetTexturing.shoreVectors = (Vector3[])sourceTexturing.shoreVectors.Clone();
			}
			else
			{
				targetTexturing.shoreVectors = null; // Will be regenerated in Refresh()
			}

			// Texture references - copy from prefab if valid, otherwise null (regenerated in Refresh)
			targetTexturing.shoreVectorTexture = sourceTexturing.shoreVectorTexture;
			targetTexturing.baseDiffuseTexture = sourceTexturing.baseDiffuseTexture;
			targetTexturing.baseNormalTexture = sourceTexturing.baseNormalTexture;
			targetTexturing.heightSlopeTexture = sourceTexturing.heightSlopeTexture;

			// Configuration and state fields from prefab
			targetTexturing.debugFoliageDisplacement = sourceTexturing.debugFoliageDisplacement;
			targetTexturing.previousFoliageDebugState = sourceTexturing.previousFoliageDebugState;
			targetTexturing.isInitialized = false; // Force re-initialization with Land data
			targetTexturing.baseTextureState = sourceTexturing.baseTextureState;
			targetTexturing.heightSlopeState = sourceTexturing.heightSlopeState;
			targetTexturing.needsRefresh = true; // Ensure refresh to align with Land

			Debug.Log($"Yoinked TerrainTexturing: Resolution={targetTexturing.textureResolution}, " +
					  $"TerrainMaxDimension={targetTexturing.terrainMaxDimension}, " +
					  $"PixelSize={targetTexturing.pixelSize}, " +
					  $"ShoreDistances={(targetTexturing.shoreDistances != null ? targetTexturing.shoreDistances.Length : 0)}, " +
					  $"Initialized={targetTexturing.isInitialized}");

			// Always refresh to ensure data aligns with Land terrain
			if (targetTerrain == Land)
			{
				targetTexturing.Refresh(); // Regenerate runtime data based on Land
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to yoink TerrainTexturing data: {e.Message}");
		}
	}

	
private static void InspectComponent(Component component)
{
    // Get the type of the component
    System.Type type = component.GetType();

    // Get all fields (public, private, instance)
    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (fields.Length == 0)
    {
        Debug.Log("  No fields found on this component.");
        return;
    }

    foreach (FieldInfo field in fields)
    {
        try
        {
            object value = field.GetValue(component);
            string valueString = (value == null) ? "null" : value.ToString();

            // Handle arrays or collections specially
            if (value != null && value.GetType().IsArray)
            {
                System.Array array = (System.Array)value;
                valueString = $"Array[{array.Length}]";
                for (int i = 0; i < array.Length; i++)
                {
                    valueString += $"\n    [{i}]: {(array.GetValue(i) == null ? "null" : array.GetValue(i).ToString())}";
                }
            }
            else if (value is UnityEngine.Object unityObj && unityObj != null)
            {
                valueString = $"{unityObj.name} (Type: {unityObj.GetType().Name})";
            }

            Debug.Log($"  Field: {field.Name} = {valueString} (Type: {field.FieldType.Name})");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"  Failed to inspect field {field.Name}: {e.Message}");
        }
    }

    // Optionally inspect properties too
    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    foreach (PropertyInfo prop in properties)
    {
        if (prop.GetMethod != null && prop.GetMethod.GetParameters().Length == 0) // Only gettable properties with no parameters
        {
            try
            {
                object value = prop.GetValue(component);
                string valueString = (value == null) ? "null" : value.ToString();
                Debug.Log($"  Property: {prop.Name} = {valueString} (Type: {prop.PropertyType.Name})");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"  Failed to inspect property {prop.Name}: {e.Message}");
            }
        }
    }
}

    private static void SetShaderSplatParameters()
    {
        for (int i = 0; i < 8; i++)
        {
            SplatType s = _config.Splats[i];
            Shader.SetGlobalVector($"Splat{i}_UVMIX", new Vector3(s.UVMixMult, s.UVMixStart, 1f / s.UVMixDist));
            Shader.SetGlobalColor($"Splat{i}_AridColor", s.AridColor);
            Shader.SetGlobalColor($"Splat{i}_TemperateColor", s.TemperateColor);
            Shader.SetGlobalColor($"Splat{i}_TundraColor", s.TundraColor);
            Shader.SetGlobalColor($"Splat{i}_ArcticColor", s.ArcticColor);
        }
    }

    public static Vector3 WorldToTerrainUV(Vector3 worldPos)
    {
        return new Vector3(
            (worldPos.x - TerrainPosition.x) * TerrainSizeInverse.x,
            (worldPos.y - TerrainPosition.y) * TerrainSizeInverse.y,
            (worldPos.z - TerrainPosition.z) * TerrainSizeInverse.z);
    }

    public static Vector3 TerrainUVToWorld(Vector3 terrainUV)
    {
        return new Vector3(
            TerrainPosition.x + terrainUV.x * TerrainSize.x,
            TerrainPosition.y + terrainUV.y * TerrainSize.y,
            TerrainPosition.z + terrainUV.z * TerrainSize.z);
    }

    public static float ToTerrainX(float worldX) => (worldX - TerrainPosition.x) * TerrainSizeInverse.x * SplatMapRes;
    public static float ToTerrainZ(float worldZ) => (worldZ - TerrainPosition.z) * TerrainSizeInverse.z * SplatMapRes;
    public static float ToWorldX(float terrainX) => TerrainPosition.x + (terrainX / SplatMapRes) * TerrainSize.x;
    public static float ToWorldZ(float terrainZ) => TerrainPosition.z + (terrainZ / SplatMapRes) * TerrainSize.z;


    public static float GetHeightAtPosition(Vector3 position)
    {
        Vector3 uv = WorldToTerrainUV(position);
        return TerrainPosition.y + Land.terrainData.GetInterpolatedHeight(uv.x, uv.z) * TerrainSize.y;
    }

    public static float GetHeightAtUV(float uvX, float uvZ)
    {
        return TerrainPosition.y + Land.terrainData.GetInterpolatedHeight(uvX, uvZ) * TerrainSize.y;
    }

    public static float GetHeightAtGrid(int gridX, int gridZ)
    {
        return TerrainPosition.y + Land.terrainData.GetHeight(gridX, gridZ) * TerrainSize.y;
    }

    public static bool IsOutsideTerrain(Vector3 position)
    {
        return position.x < TerrainPosition.x || position.z < TerrainPosition.z ||
               position.x > TerrainPosition.x + TerrainSize.x || position.z > TerrainPosition.z + TerrainSize.z;
    }

    public static bool IsFarOutsideTerrain(Vector3 position)
    {
        const float buffer = 500f;
        return position.x < TerrainPosition.x - buffer || position.z < TerrainPosition.z - buffer ||
               position.x > TerrainPosition.x + TerrainSize.x + buffer || position.z > TerrainPosition.z + TerrainSize.z + buffer ||
               (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z));
    }

	private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }

    private static int GenerateInt(ref uint seed, int min, int max)
    {
        uint range = (uint)(max - min);
        uint randomValue = NextRandom(ref seed);
        return min + (int)(randomValue % range);
    }

    private static int GenerateRandomOffset(ref uint seed)
    {
        uint randomValue = NextRandom(ref seed);
        return (randomValue % 2U == 0U) ? 1 : -1;
    }

    public static uint GenerateRandomSeed()
    {
        RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
        uint seed = RandomSeed;

        int baseHeight = GenerateInt(ref seed, 0, 4) * 90; // 0 to 3 * 90 = 0, 90, 180, 270
        int heightVariation = GenerateInt(ref seed, -45, 46); // -45 to 45
        int heightOffset = GenerateRandomOffset(ref seed); // -1 or 1

        MinHeight = baseHeight;
        MaxHeight = baseHeight + heightVariation + heightOffset * 90;
        return RandomSeed;
    }



    public static string GetTerrainDebugInfo(int gridX, int gridZ)
    {
		return "";
		/*
        float uvX = (float)gridX / SplatMapRes;
        float uvZ = (float)gridZ / SplatMapRes;
        return $"{Ground != null ? "Ground: " + string.Join(", ", Ground.GetRow(gridX, gridZ)) : "Ground: N/A"}\n" +
               $"{Biome != null ? "Biome: " + string.Join(", ", Biome.GetRow(gridX, gridZ)) : "Biome: N/A"}\n" +
               $"{Alpha != null ? "Alpha: " + Alpha[gridX, gridZ] : "Alpha: N/A"}\n" +
               $"{Topology[TopologyLayer] != null ? "Topology: " + string.Join(", ", Topology[TopologyLayer].GetRow(gridX, gridZ)) : "Topology: N/A"}\n" +
               $"Height: {GetHeightAtGrid(gridX, gridZ)}";
		*/
    }

    private static Texture2D ExtractTextureFromArray(Texture[] array, int index)
    {
        if (array == null || index < 0 || index >= array.Length || array[0] == null)
            return null;

        Texture2DArray texArray = array[Mathf.Clamp(QualitySettings.masterTextureLimit, 0, array.Length - 1)] as Texture2DArray;
        if (texArray == null || index >= texArray.depth)
            return null;

        Texture2D tex = new Texture2D(texArray.width, texArray.height, texArray.format, true);
        Graphics.CopyTexture(texArray, index, 0, tex, 0, 0);
        return tex;
    }


    public static Material LoadTerrainMaterial()
    {
        Material material = AssetManager.LoadAsset<Material>("assets/content/nature/terrain/materials/terrain.v3.mat");
		AssetManager.UpdateShader(material);
		
        UnityEngine.Object.DontDestroyOnLoad(material);
        return material;
    }



private static void SetShaderTilingVectors()
{
    Shader.SetGlobalVector("Terrain_TexelSize0", new Vector4(
        1f / TerrainManager._config.Splats[0].SplatTiling,
        1f / TerrainManager._config.Splats[1].SplatTiling,
        1f / TerrainManager._config.Splats[2].SplatTiling,
        1f / TerrainManager._config.Splats[3].SplatTiling
    ));
    Shader.SetGlobalVector("Terrain_TexelSize1", new Vector4(
        1f / TerrainManager._config.Splats[4].SplatTiling,
        1f / TerrainManager._config.Splats[5].SplatTiling,
        1f / TerrainManager._config.Splats[6].SplatTiling,
        1f / TerrainManager._config.Splats[7].SplatTiling
    ));
}

private static void ConfigureShaderGlobals(Terrain terrain)
{
	 
    TerrainTexturing texturing = terrain.gameObject.GetComponent<TerrainTexturing>();
        if (texturing == null)
        {
            Debug.LogWarning("TerrainTexturing component not found on terrain. Adding one now.");
            texturing = terrain.gameObject.AddComponent<TerrainTexturing>();
        }

        // Call TerrainTexturing.Refresh to set up all shader properties
    texturing.Refresh();

	

    // Core terrain data textures from TerrainData
    Shader.SetGlobalTexture("Terrain_HeightTexture", HeightTexture); // Heightmap for elevation
    //Shader.SetGlobalTexture("Terrain_Normal", RuntimeNormalMap);
    Shader.SetGlobalTexture("Terrain_Alpha", AlphaTexture); 
	
    // Splatmap (alphamap) textures for ground control
    Texture2D[] alphamaps = terrain.terrainData.alphamapTextures;
    if (alphamaps.Length > 0) Shader.SetGlobalTexture("Terrain_Control0", alphamaps[0]); // First 4 splat channels
    if (alphamaps.Length > 1) Shader.SetGlobalTexture("Terrain_Control1", alphamaps[1]); // Next 4 splat channels (if 8 splats)

    // Texture arrays from TerrainConfig
    Shader.SetGlobalTexture("Terrain_AlbedoArray_LOD0", TerrainManager._config.AlbedoArrays[0]);
	Shader.SetGlobalTexture("Terrain_AlbedoArray_LOD1", TerrainManager._config.AlbedoArrays[1]);
	Shader.SetGlobalTexture("Terrain_AlbedoArray_LOD2", TerrainManager._config.AlbedoArrays[2]);
    Shader.SetGlobalTexture("Terrain_NormalArray_LOD0", TerrainManager._config.NormalArrays[0]);
	Shader.SetGlobalTexture("Terrain_NormalArray_LOD1", TerrainManager._config.NormalArrays[1]);
	Shader.SetGlobalTexture("Terrain_NormalArray_LOD2", TerrainManager._config.NormalArrays[2]);

	Shader.SetGlobalColor("Terrain_Arid0" , TerrainManager._config.GetAridColors()[0]);
	Shader.SetGlobalColor("Terrain_Arid1" , TerrainManager._config.GetAridColors()[1]);
	Shader.SetGlobalColor("Terrain_Arid2" , TerrainManager._config.GetAridColors()[2]);
	Shader.SetGlobalColor("Terrain_Arid3" , TerrainManager._config.GetAridColors()[3]);
	
	Shader.SetGlobalColor("Terrain_Temperate0" , TerrainManager._config.GetTemperateColors()[0]);
	Shader.SetGlobalColor("Terrain_Temperate1" , TerrainManager._config.GetTemperateColors()[1]);
	Shader.SetGlobalColor("Terrain_Temperate2" , TerrainManager._config.GetTemperateColors()[2]);
	Shader.SetGlobalColor("Terrain_Temperate3" , TerrainManager._config.GetTemperateColors()[3]);
	
	Shader.SetGlobalColor("Terrain_Tundra0" , TerrainManager._config.GetTundraColors()[0]);
	Shader.SetGlobalColor("Terrain_Tundra1" , TerrainManager._config.GetTundraColors()[1]);
	Shader.SetGlobalColor("Terrain_Tundra2" , TerrainManager._config.GetTundraColors()[2]);
	Shader.SetGlobalColor("Terrain_Tundra3" , TerrainManager._config.GetTundraColors()[3]);
	
	Shader.SetGlobalColor("Terrain_Arctic0" , TerrainManager._config.GetArcticColors()[0]);
	Shader.SetGlobalColor("Terrain_Arctic1" , TerrainManager._config.GetArcticColors()[1]);
	Shader.SetGlobalColor("Terrain_Arctic2" , TerrainManager._config.GetArcticColors()[2]);
	Shader.SetGlobalColor("Terrain_Arctic3" , TerrainManager._config.GetArcticColors()[3]);
	
	Shader.SetGlobalVector("UVMixParameter0" , TerrainManager._config.GetUVMIXParameters()[0]);
	Shader.SetGlobalVector("UVMixParameter1" , TerrainManager._config.GetUVMIXParameters()[1]);
	Shader.SetGlobalVector("UVMixParameter2" , TerrainManager._config.GetUVMIXParameters()[2]);
	Shader.SetGlobalVector("UVMixParameter3" , TerrainManager._config.GetUVMIXParameters()[3]);
	Shader.SetGlobalVector("UVMixParameter4" , TerrainManager._config.GetUVMIXParameters()[4]);
	Shader.SetGlobalVector("UVMixParameter5" , TerrainManager._config.GetUVMIXParameters()[5]);
	Shader.SetGlobalVector("UVMixParameter6" , TerrainManager._config.GetUVMIXParameters()[6]);
	Shader.SetGlobalVector("UVMixParameter7" , TerrainManager._config.GetUVMIXParameters()[7]);
	
	Shader.SetGlobalFloatArray("Terrain_Tiling", TerrainManager._config.GetSplatTilings());
	
	// Disable shore vector (already done, but ensure for redundancy)
    Shader.SetGlobalTexture("Terrain_ShoreVector", null);

    // Update material properties to disable Puddle and Wetness Layers
    Material material = terrain.materialTemplate;
    if (material != null)
    {
        // Puddle Layer
        material.SetFloat("_LayerFallback_Metallic", 0f);
        material.SetFloat("_LayerFallback_Smoothness", 0f);
        material.SetColor("_LayerFallback_Albedo", new Color(0.5f, 0.5f, 0.5f, 1f)); // Neutral gray

        // Wetness Layer
        material.SetFloat("_WetnessLayer_Wetness", 0f);
        material.SetFloat("_WetnessLayer_WetAlbedoScale", 0f);
        material.SetFloat("_WetnessLayer_WetSmoothness", 0f);
        Texture2D blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();
        material.SetTexture("_WetnessLayer_Mask", blackTexture);
    }

    // Placeholder for biome and topology (adjust based on your data structure)
    // Assuming these might come from TerrainManager or elsewhere
	Shader.SetGlobalTexture("Terrain_Biome", BiomeTexture);     // Replace with actual biome texture if available
    Shader.SetGlobalTexture("Terrain_Topology", null);  // Replace with actual topology texture if available

    // Texel size for splatmap resolution
    float texelSize = 1f / terrain.terrainData.alphamapResolution;
    Shader.SetGlobalVector("Terrain_TexelSize", new Vector2(texelSize, texelSize));

    // Splat tiling vectors
    SetShaderTilingVectors();

    // Splat UV mix and biome colors
    SetShaderSplatParameters();

    // Terrain position and size
    Shader.SetGlobalVector("Terrain_Position", terrain.gameObject.transform.position);
    Shader.SetGlobalVector("Terrain_Size", terrain.terrainData.size);
    Shader.SetGlobalVector("Terrain_RcpSize", new Vector3(
        1f / terrain.terrainData.size.x,
        1f / terrain.terrainData.size.y,
        1f / terrain.terrainData.size.z
    ));

	
    // Shader keywords (disable unnecessary ones)
    if (terrain.materialTemplate)
    {
        terrain.materialTemplate.DisableKeyword("_TERRAIN_BLEND_LINEAR");
        terrain.materialTemplate.DisableKeyword("_TERRAIN_VERTEX_NORMALS");
    }
}
	
    public static class Callbacks
    {
        public delegate void Layer(LayerType layer, int? topology = null);
        public delegate void HeightMap(TerrainType terrain);

        /// <summary>Called after the active layer is changed. </summary>
        public static event Layer LayerChanged;
        /// <summary>Called after the active layer is saved. </summary>
        public static event Layer LayerSaved;
        /// <summary>Called when the active layer is dirtied/updated.</summary>
        public static event Layer LayerUpdated;
        /// <summary>Called when the Land/Water heightmap is dirtied/updated.</summary>
        public static event HeightMap HeightMapUpdated;

        public static void InvokeLayerChanged(LayerType layer, int topology) => LayerChanged?.Invoke(layer, topology);
        public static void InvokeLayerSaved(LayerType layer, int topology) => LayerSaved?.Invoke(layer, topology);
        public static void InvokeLayerUpdated(LayerType layer, int topology) => LayerUpdated?.Invoke(layer, topology);
        public static void InvokeHeightMapUpdated(TerrainType terrain) => HeightMapUpdated?.Invoke(terrain);
    }

    #region Splats


    #region Methods
	
    /// <summary>Returns the SplatMap at the selected LayerType.</summary>
    /// <param name="layer">The LayerType to return. (Ground, Biome)</param>
    /// <returns>3D float array in Alphamap format. [x, y, Texture]</returns>
    public static float[,,] GetSplatMap(LayerType layer, int topology = -1)
    {
        switch (layer)
        {
            case LayerType.Ground:
                if (CurrentLayerType == layer && LayerDirty)
                {
                    Ground = Land.terrainData.GetAlphamaps(0, 0, SplatMapRes, SplatMapRes);
                    LayerDirty = false;
                }
                return Ground;
            case LayerType.Biome:
                if (CurrentLayerType == layer && LayerDirty)
                {
                    Biome = Land.terrainData.GetAlphamaps(0, 0, SplatMapRes, SplatMapRes);
                    LayerDirty = false;
                }
                return Biome;
            case LayerType.Topology:
                if (topology < 0 || topology >= TerrainTopology.COUNT)
                {
                    Debug.LogError($"GetSplatMap({layer}, {topology}) topology parameter out of bounds. Should be between 0 - {TerrainTopology.COUNT - 1}");
                    return null;
                }
                if (CurrentLayerType == layer && TopologyLayer == topology && LayerDirty)
                {
                    Topology[topology] = Land.terrainData.GetAlphamaps(0, 0, SplatMapRes, SplatMapRes);
                    LayerDirty = false;
                }
                return Topology[topology];
            default:
                Debug.LogError($"GetSplatMap({layer}) cannot return type float[,,].");
                return null;
        }
    }

	// Add this method within the TerrainManager class, under the #region HeightMap -> #region Methods section
	[ConsoleCommand("Raise or lower terrain")]
	public static void AdjustHeight(int heightAdjustment)
	{
		// Validate input range
		if (heightAdjustment < -1000 || heightAdjustment > 1000)
		{
			Debug.LogError($"Height adjustment must be between -1000 and 1000 meters. Received: {heightAdjustment}");
			return;
		}

		// Normalize the adjustment to Unity's heightmap scale (0-1)
		float normalizedAdjustment = heightAdjustment / 1000f;

		// Register undo and apply the offset
		RegisterHeightMapUndo(TerrainType.Land, $"Adjust Height by {heightAdjustment}m");
		Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Offset(GetHeightMap(), normalizedAdjustment, true));

		// Notify listeners of the update
		Callbacks.InvokeHeightMapUpdated(TerrainType.Land);
	}

	[ConsoleCommand("paints borders")]
    public static void PaintBorderLayer(Layers layerData, int radius)
    {
        // Validate radius
        if (radius < 0 || radius >= TerrainManager.SplatMapRes / 2)
        {
            Debug.LogError($"Radius must be between 0 and {TerrainManager.SplatMapRes / 2 - 1} grid units. Received: {radius}");
            return;
        }

        // Determine the layer type and index from the Layers object
        LayerType layerType;
        int layerIndex = -1;
        if (layerData.Ground != 0)
        {
            layerType = LayerType.Ground;
            layerIndex = TerrainSplat.TypeToIndex((int)layerData.Ground);
        }
        else if (layerData.Biome != 0)
        {
            layerType = LayerType.Biome;
            layerIndex = TerrainBiome.TypeToIndex((int)layerData.Biome);
        }
        else if (layerData.Topologies != 0)
        {
            layerType = LayerType.Topology;
            layerIndex = TerrainTopology.TypeToIndex((int)layerData.Topologies);
        }
        else
        {
            Debug.LogError("No valid layer specified in Layers object.");
            return;
        }

        // Get the current layer data
        float[,,] layerMap = TerrainManager.GetLayerData(layerType, layerIndex);
        int res = layerMap.GetLength(0);
        int layerCount = TerrainManager.LayerCount(layerType); // 8 for Ground, 4 for Biome, 2 for Topology

        // Register undo before modifying
        TerrainManager.RegisterSplatMapUndo($"Paint Border Layer {layerType} Index {layerIndex}, Radius {radius}");

        // Paint the borders with blending over the radius
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                // Calculate distance from the nearest edge
                int distToLeft = x;
                int distToRight = res - 1 - x;
                int distToBottom = z;
                int distToTop = res - 1 - z;
                int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

                // If within radius, apply blending
                if (minDist <= radius)
                {
                    // Blend factor: 1 at edge (minDist = 0), 0 at radius (minDist = radius)
                    float strength = Mathf.InverseLerp(radius, 0, minDist);

                    // Calculate total strength of other layers
                    float totalOtherStrength = 0f;
                    for (int k = 0; k < layerCount; k++)
                    {
                        if (k != layerIndex)
                        {
                            totalOtherStrength += layerMap[x, z, k];
                        }
                    }

                    // Apply strength to the target layer and adjust others
                    float remainingStrength = 1f - strength;
                    for (int k = 0; k < layerCount; k++)
                    {
                        if (k == layerIndex)
                        {
                            layerMap[x, z, k] = strength; // Target layer gets the blended strength
                        }
                        else if (totalOtherStrength > 0)
                        {
                            // Distribute remaining strength proportionally among other layers
                            layerMap[x, z, k] = (layerMap[x, z, k] / totalOtherStrength) * remainingStrength;
                        }
                        else
                        {
                            // If no other strength existed, set evenly or to 0
                            layerMap[x, z, k] = (layerCount > 1) ? (remainingStrength / (layerCount - 1)) : 0f;
                        }
                    }
                }
                // Interior beyond radius remains unchanged
            }
        }

        // Apply the updated layer back to TerrainManager
        TerrainManager.SetLayerData(layerMap, layerType, layerIndex);

        // Notify listeners of the update
        TerrainManager.Callbacks.InvokeLayerUpdated(layerType, layerIndex);
    }

	
	public static Vector4[,] GetBiomeMap()
    {
        if (BiomeMap == null || LayerDirty || CurrentLayerType == LayerType.Biome)
        {
            float[,,] biomeSplat = GetSplatMap(LayerType.Biome);
            if (biomeSplat == null) return null;

            int res = SplatMapRes;
            BiomeMap = new Vector4[res, res];
            for (int x = 0; x < res; x++)
            {
                for (int z = 0; z < res; z++)
                {
                    BiomeMap[z, x] = new Vector4(
                        biomeSplat[x, z, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Arid)],
                        biomeSplat[x, z, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Temperate)],
                        biomeSplat[x, z, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Tundra)],
                        biomeSplat[x, z, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Arctic)]
                    );
                }
            }
            if (CurrentLayerType == LayerType.Biome) LayerDirty = false;
        }
        return BiomeMap;
    }

    /// <summary>Sets a region of the biome map.</summary>
    public static void SetBiomeMapRegion(Vector4[,] array, int x, int y, int width, int height)
    {
        if (array == null || array.GetLength(0) != height || array.GetLength(1) != width)
        {
            Debug.LogError($"SetBiomeMapRegion: Invalid array dimensions. Expected [{height}, {width}], got [{array?.GetLength(0)}, {array?.GetLength(1)}]");
            return;
        }

        Vector4[,] fullMap = GetBiomeMap();
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (x + j < SplatMapRes && y + i < SplatMapRes)
                {
                    fullMap[y + i, x + j] = array[i, j];
                }
            }
        }

        // Convert back to float[,,] for TerrainData
        float[,,] splatMap = new float[SplatMapRes, SplatMapRes, 4];
        for (int i = 0; i < SplatMapRes; i++)
        {
            for (int j = 0; j < SplatMapRes; j++)
            {
                splatMap[i, j, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Arid)] = fullMap[i, j].x;
                splatMap[i, j, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Temperate)] = fullMap[i, j].y;
                splatMap[i, j, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Tundra)] = fullMap[i, j].z;
                splatMap[i, j, TerrainBiome.TypeToIndex((int)TerrainBiome.Enum.Arctic)] = fullMap[i, j].w;
            }
        }

        RegisterSplatMapUndo("Set Biome Map Region");
        SetSplatMap(splatMap, LayerType.Biome);
    }

    /// <summary>Gets the topology map for a specific layer as an int[,] array.</summary>
    public static int[,] GetTopologyMap(int layer = -1)
    {
        if (layer < 0) layer = TopologyLayer;
        if (layer < 0 || layer >= TerrainTopology.COUNT)
        {
            Debug.LogError($"GetTopologyMap: Invalid layer index {layer}. Must be between 0 and {TerrainTopology.COUNT - 1}");
            return null;
        }

        float[,,] topologySplat = GetSplatMap(LayerType.Topology, layer);
        if (topologySplat == null) return null;

        int res = SplatMapRes;
        int[,] topologyMap = new int[res, res];
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                topologyMap[z, x] = topologySplat[x, z, 0] > 0.5f ? TerrainTopology.IndexToType(layer) : 0;
            }
        }
        return topologyMap;
    }

    /// <summary>Sets a region of the topology map.</summary>
    public static void SetTopologyMapRegion(int[,] array, int x, int y, int width, int height, int layer = -1)
    {
        if (array == null || array.GetLength(0) != height || array.GetLength(1) != width)
        {
            Debug.LogError($"SetTopologyMapRegion: Invalid array dimensions. Expected [{height}, {width}], got [{array?.GetLength(0)}, {array?.GetLength(1)}]");
            return;
        }

        if (layer < 0) layer = TopologyLayer;
        if (layer < 0 || layer >= TerrainTopology.COUNT)
        {
            Debug.LogError($"SetTopologyMapRegion: Invalid layer index {layer}. Must be between 0 and {TerrainTopology.COUNT - 1}");
            return;
        }

        float[,,] fullMap = GetSplatMap(LayerType.Topology, layer);
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (x + j < SplatMapRes && y + i < SplatMapRes)
                {
                    fullMap[y + i, x + j, 0] = array[i, j] != 0 ? 1f : 0f;
                    fullMap[y + i, x + j, 1] = array[i, j] == 0 ? 1f : 0f;
                }
            }
        }

        RegisterSplatMapUndo($"Set Topology Map Region Layer {layer}");
        SetSplatMap(fullMap, LayerType.Topology, layer);
    }

    /// <summary>Gets the alpha map as a float[,] for blending purposes (0 = hole, 1 = visible).</summary>
    public static float[,] GetAlphaMapFloat()
    {
        bool[,] alpha = GetAlphaMap();
        if (alpha == null) return null;

        int res = AlphaMapRes;
        float[,] alphaFloat = new float[res, res];
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                alphaFloat[z, x] = alpha[x, z] ? 1f : 0f;
            }
        }
        return alphaFloat;
    }

    /// <summary>Sets a region of the alpha map from a float[,] array.</summary>
    public static void SetAlphaMapRegion(float[,] array, int x, int y, int width, int height)
    {
        if (array == null || array.GetLength(0) != height || array.GetLength(1) != width)
        {
            Debug.LogError($"SetAlphaMapRegion: Invalid array dimensions. Expected [{height}, {width}], got [{array?.GetLength(0)}, {array?.GetLength(1)}]");
            return;
        }

        bool[,] regionAlpha = new bool[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                regionAlpha[i, j] = array[i, j] > 0.5f; // Threshold at 0.5 for binary conversion
            }
        }

        SetAlphaMap(regionAlpha, x, y, width, height);
    }

    // Override the existing GetAlphaMap to clarify it's for holes
    public static bool[,] GetAlphaMap()
    {
        if (AlphaDirty || Alpha == null)
        {
            Alpha = Land.terrainData.GetHoles(0, 0, AlphaMapRes, AlphaMapRes);
            AlphaDirty = false;
        }
        return Alpha;
    }
	
	public static float ToWorldX(int terrainX)
    {
        Vector3 terrainPos = Land.transform.position;
        return terrainPos.x + (terrainX / (float)HeightMapRes) * Land.terrainData.size.x;
    }

    public static float ToWorldZ(int terrainZ)
    {
        Vector3 terrainPos = Land.transform.position;
        return terrainPos.z + (terrainZ / (float)HeightMapRes) * Land.terrainData.size.z;
    }
	
	
[ConsoleCommand("flattens map borders")]
public static void BorderTuck(int targetHeight, int radius, int padding)
{
    // Validate inputs
    if (targetHeight < 0 || targetHeight > 1000)
    {
        Debug.LogError($"Target height must be between 0 and 1000 meters. Received: {targetHeight}");
        return;
    }
    if (radius < 0 || radius >= HeightMapRes / 2)
    {
        Debug.LogError($"Radius must be between 0 and {HeightMapRes / 2 - 1} grid units. Received: {radius}");
        return;
    }
    if (padding < 0 || padding >= HeightMapRes / 2)
    {
        Debug.LogError($"Padding must be between 0 and {HeightMapRes / 2 - 1} grid units. Received: {padding}");
        return;
    }

    // Normalize target height to Unity's heightmap scale (0-1)
    float normalizedHeight = targetHeight / 1000f;

    // Get the current heightmap
    float[,] heightMap = GetHeightMap(TerrainType.Land);
    int res = HeightMapRes;

    // Create a copy of the original heightmap for blending
    float[,] originalHeightMap = (float[,])heightMap.Clone();

    // Register undo before modifying
    RegisterHeightMapUndo(TerrainType.Land, $"Border Tuck to {targetHeight}m, Radius {radius}, Padding {padding}");

    // Apply padding and S-shaped blending
    for (int x = 0; x < res; x++)
    {
        for (int z = 0; z < res; z++)
        {
            // Calculate distances to edges
            float distToLeft = x;
            float distToRight = res - 1 - x;
            float distToBottom = z;
            float distToTop = res - 1 - z;

            // Minimum distance to any edge (for straight edges)
            float minEdgeDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);

            // Check if within padding area
            if (minEdgeDist < padding)
            {
                heightMap[x, z] = normalizedHeight; // Set outer padding to target height
                continue; // Skip blending for padding area
            }

            // Adjust edge distance for blending inside padding
            float adjustedEdgeDist = minEdgeDist - padding;

            // Check if in a corner quadrant and calculate distance to inward-offset center
            bool isInCornerQuadrant = false;
            float cornerDist = 0f;
            if (x < radius + padding && z < radius + padding) // Bottom-left corner, center at (radius + padding, radius + padding)
            {
                cornerDist = Mathf.Sqrt((x - (radius + padding)) * (x - (radius + padding)) + (z - (radius + padding)) * (z - (radius + padding)));
                isInCornerQuadrant = true;
            }
            else if (x > res - 1 - (radius + padding) && z < radius + padding) // Bottom-right corner, center at (res-1-(radius + padding), radius + padding)
            {
                cornerDist = Mathf.Sqrt((x - (res - 1 - (radius + padding))) * (x - (res - 1 - (radius + padding))) + (z - (radius + padding)) * (z - (radius + padding)));
                isInCornerQuadrant = true;
            }
            else if (x < radius + padding && z > res - 1 - (radius + padding)) // Top-left corner, center at (radius + padding, res-1-(radius + padding))
            {
                cornerDist = Mathf.Sqrt((x - (radius + padding)) * (x - (radius + padding)) + (z - (res - 1 - (radius + padding))) * (z - (res - 1 - (radius + padding))));
                isInCornerQuadrant = true;
            }
            else if (x > res - 1 - (radius + padding) && z > res - 1 - (radius + padding)) // Top-right corner, center at (res-1-(radius + padding), res-1-(radius + padding))
            {
                cornerDist = Mathf.Sqrt((x - (res - 1 - (radius + padding))) * (x - (res - 1 - (radius + padding))) + (z - (res - 1 - (radius + padding))) * (z - (res - 1 - (radius + padding))));
                isInCornerQuadrant = true;
            }

            // Apply blending inside padded area
            if (isInCornerQuadrant)
            {
                if (cornerDist <= radius) // Within corner radius from adjusted center
                {
                    // Inverted for corners: 0 at center, 1 at corner
                    float t = Mathf.Clamp01(cornerDist / radius);
                    float blendFactor = Mathf.SmoothStep(0f, 1f, t); // Heights decrease toward corner
                    heightMap[x, z] = Mathf.Lerp(originalHeightMap[x, z], normalizedHeight, blendFactor);
                }
                else if (minEdgeDist < radius + padding) // Inside corner quadrant but outside radius
                {
                    heightMap[x, z] = normalizedHeight;
                }
            }
            else if (adjustedEdgeDist <= radius) // Along edges, inside padding
            {
                // Non-inverted for edges: 1 at inner padding edge, 0 at radius boundary
                float t = Mathf.Clamp01(1f - (adjustedEdgeDist / radius));
                float blendFactor = Mathf.SmoothStep(0f, 1f, t); // Heights decrease toward padding edge
                heightMap[x, z] = Mathf.Lerp(originalHeightMap[x, z], normalizedHeight, blendFactor);
            }
            // Interior beyond radius + padding remains unchanged
        }
    }

    // Apply the modified heightmap
    Land.terrainData.SetHeights(0, 0, heightMap);

    // Notify listeners of the update
    Callbacks.InvokeHeightMapUpdated(TerrainType.Land);
}
	
	public static bool[,] GetAlphaMap(int x, int y, int width, int height)
	{
		if (AlphaDirty)
		{
			Alpha = new bool[AlphaMapRes, AlphaMapRes]; // Assuming AlphaMapRes is the size of the entire map
			Land.terrainData.GetHoles(0, 0, AlphaMapRes, AlphaMapRes);
			AlphaDirty = false;
		}

		// Create a new array for the specified region
		bool[,] regionAlpha = new bool[height, width];

		// Copy the relevant part of Alpha to regionAlpha
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				if (x + j < AlphaMapRes && y + i < AlphaMapRes)
				{
					regionAlpha[i, j] = Alpha[y + i, x + j];
				}
				else
				{
					// If we're asking for an area outside the terrain, we'll default to false (no hole)
					regionAlpha[i, j] = false;
				}
			}
		}

		return regionAlpha;
	}
	
	public static void SetPreviewHoles(bool hole)
	{
		// Get the actual dimensions of the terrain's hole map
		int res = AlphaMapRes;

		bool[,] holes = LandMask.terrainData.GetHoles(0, 0, res, res);

		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				holes[j, i] = hole;
			}
		}
		
		LandMask.terrainData.SetHoles(0, 0, holes);
	}
	
	public static void SetCliffMap(bool[,] spawns)
	{
		SpawnMap = spawns;		
		
		//CliffField = RustMapEditor.Maths.Array.HeightToSplat(CliffMap, SpawnMap, LandMask.terrainData.alphamapResolution);
		//LandMask.terrainData.SetAlphamaps(0, 0, CliffField);
		
	}

    public static void GetTerrainCoordinates(RaycastHit hit, int brushSize, out int x, out int z)
    {
        int num = brushSize / 2;
        Vector3 vector = hit.point - hit.transform.position;
        Vector3 vector2 = new Vector3(vector.x / Land.terrainData.size.x, vector.y / Land.terrainData.size.y, vector.z / Land.terrainData.size.z);
        Vector3 vector3 = new Vector3(vector2.x * Land.terrainData.heightmapResolution, 0, vector2.z * Land.terrainData.heightmapResolution);
        x = ((int)vector3.x) - num;
        z = ((int)vector3.z) - num;
    }

	public static void SetCliffMap(float[,] gradient, bool[,] spawns)
	{
		SpawnMap = spawns;
		
		CliffMap = RustMapEditor.Maths.Array.Normalise(gradient, 0f, 2f, Area.HeightMapDimensions());
		CliffField = RustMapEditor.Maths.Array.HeightToSplat(CliffMap, SpawnMap, LandMask.terrainData.alphamapResolution);
		LandMask.terrainData.SetAlphamaps(0, 0, CliffField);
	}
	
	public static void CopyHeights()
	{
		if (Land == null || LandMask == null)
		{
			Debug.LogError("Terrain or LandMask is null.");
			return;
		}

		// Assuming both terrains have the same resolution for simplicity
		float[,] heights = Land.terrainData.GetHeights(0, 0, Land.terrainData.heightmapResolution, Land.terrainData.heightmapResolution);
		LandMask.terrainData.SetHeights(0, 0, heights);
	}

	public static void FillLandMaskAlpha(){
		SetHoleMap(true);
	}
	

	public static void SetHoleMap(bool hole)
		{			
			TerrainData terrainData = LandMask.terrainData;
			int resolution = terrainData.heightmapResolution-1;

			bool[,] holes = new bool[resolution, resolution];

			for (int y = 0; y < resolution; y++)
			{
				for (int x = 0; x < resolution; x++)
				{
					holes[x, y] = hole;
				}
			}

			terrainData.SetHoles(0, 0, holes);
		}
	
	public static void SetBitMap(bool[,] bitmap)
	{
		if (LandMask == null)
		{
			Debug.LogError("LandMask GameObject is null.");
			return;
		}

		if (bitmap == null)
		{
			Debug.LogError("Bitmap is null.");
			return;
		}

		// Check if the bitmap dimensions match the expected alpha map resolution
		if (bitmap.GetLength(0) != AlphaMapRes || bitmap.GetLength(1) != AlphaMapRes)
		{
			Debug.LogError($"Bitmap dimensions do not match AlphaMap resolution. Expected {AlphaMapRes}x{AlphaMapRes}, got {bitmap.GetLength(0)}x{bitmap.GetLength(1)}.");
			return;
		}

		LandMask.terrainData.SetHoles(0, 0, bitmap);
	}
	

	
	public static void SetHeightPreview()
	{
		if (Height == null)
		{
			UpdateHeights();
		}

		int width = Height.GetLength(0);
		int height = Height.GetLength(1);
		float[,] result = new float[width, height];

		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < height; z++)
			{
				// Subtract from height map and ensure the result isn't negative
				result[x, z] = Height[x, z] / 1000f - 0.001f;
			}
		}

		// Set the land mask with the newly computed height map
		LandMask.terrainData.SetHeightsDelayLOD(0, 0, result);
	}


	public static void SetLandMask(float[,] array)
	{
		LandMask.terrainData.SetHeights(0, 0, array);
	}
	
	public static void SetLandMask(bool[,] array){
		int size = array.GetLength(0) - 1;
		bool[,] small = new bool[size, size];

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				small[i, j] = array[i, j];
			}
		}

		LandMask.terrainData.SetHoles(0, 0, small);
	}

	// -1 shows alpha layer, others show topologies
	public static void BitView(int index)
	{
		int res = AlphaMapRes;
		if (index == -1)
		{
			LandMask.terrainData.SetHoles(0, 0, Land.terrainData.GetHoles(0, 0, res, res));
			return;
		}

		bool[,] screwed = TopologyData.GetTopologyBitmap(TerrainTopology.IndexToType(index));

		int splatRatio = (int)TerrainManager.SplatRatio;
		bool[,] upscaled = UpscaleBitmap(screwed);

		LandMask.terrainData.SetHoles(0, 0, upscaled);
	}



public static bool[,] GetTopologyBitview(int layer, int x, int y, int width, int height)
{
    // Multiply x, y, width, and height by SplatRatio
    int scaledX = (int)(x / SplatRatio);
    int scaledY = (int)(y / SplatRatio);
    int scaledWidth = (int)(width / SplatRatio);
    int scaledHeight = (int)(height / SplatRatio);

    return UpscaleBitmap(TopologyData.GetTopology(layer, scaledX, scaledY, scaledWidth, scaledHeight));
}

public static void SetTopologyBitview(int layer, int x, int y, int width, int height, bool[,] bitmap)
{
    // Divide x, y, width, and height by SplatRatio
    int scaledX = (int)(x / SplatRatio);
    int scaledY = (int)(y / SplatRatio);
    int scaledWidth = (int)(width / SplatRatio);
    int scaledHeight = (int)(height / SplatRatio);

    TopologyData.SetTopology(layer, scaledX, scaledY, scaledWidth, scaledHeight, DownscaleBitmap(bitmap));
}

public static bool[,] DownscaleBitmap(bool[,] source)
{
    int splatRatio = (int)SplatRatio;
    int sourceWidth = source.GetLength(0);
    int sourceHeight = source.GetLength(1);
    
    int targetWidth = sourceWidth / splatRatio;
    int targetHeight = sourceHeight / splatRatio;
    
    bool[,] downscaled = new bool[targetWidth, targetHeight];

    for (int i = 0; i < targetWidth; i++)
    {
        for (int j = 0; j < targetHeight; j++)
        {
            // Determine the area to check for each downscaled pixel
            bool anyTrue = false;
            for (int x = 0; x < splatRatio; x++)
            {
                for (int y = 0; y < splatRatio; y++)
                {
                    int sourceX = i * splatRatio + x;
                    int sourceY = j * splatRatio + y;
                    if (sourceX < sourceWidth && sourceY < sourceHeight)
                    {
                        anyTrue |= source[sourceX, sourceY]; // Use OR to check if any pixel in the area is true
                    }
                }
            }
            downscaled[i, j] = anyTrue;
        }
    }

    return downscaled;
}



public static bool[,] UpscaleBitmap(bool[,] source)
{
    int sourceWidth = source.GetLength(0);
    int sourceHeight = source.GetLength(1);
    
	int targetWidth = sourceWidth*(int)SplatRatio;
	int targetHeight = sourceHeight*(int)SplatRatio;
	
    bool[,] upscaled = new bool[targetWidth, targetHeight];

    for (int i = 0; i < targetWidth; i++)
    {
        for (int j = 0; j < targetHeight; j++)
        {
            int sourceI = i / (int)SplatRatio;
            int sourceJ = j / (int)SplatRatio;
            if (sourceI < sourceWidth && sourceJ < sourceHeight)
            {
                upscaled[i, j] = source[sourceI, sourceJ];
            }
            else
            {
                upscaled[i, j] = false; // Default to false if out of bounds
            }
        }
    }

    return upscaled;
}

	public static void FillLandMask()
	{
		float[,] heights = Land.terrainData.GetHeights(0, 0, AlphaMapRes, AlphaMapRes);
		LandMask.terrainData.SetHeights(0, 0, heights);
	}
	
	public static void SetLandMaskArea(int x, int y, int brushSize, float offset)
	{
		TerrainData terrainData = LandMask.terrainData;
		int width = terrainData.heightmapResolution;
		int height = terrainData.heightmapResolution;

		// Calculate the bounds for the area to modify, ensuring they're within the map dimensions
		int startX = Mathf.Max(0, x - brushSize / 2);
		int startY = Mathf.Max(0, y - brushSize / 2);
		int endX = Mathf.Min(width, x + brushSize / 2);
		int endY = Mathf.Min(height, y + brushSize / 2);

		// Get only the part of the height map we're interested in modifying for better performance
		float[,] heightMap = terrainData.GetHeights(startX, startY, endX - startX, endY - startY);

		// Modify the height map within the brush area
		for (int i = 0; i < heightMap.GetLength(0); i++)
		{
			for (int j = 0; j < heightMap.GetLength(1); j++)
			{
				heightMap[i, j] = Mathf.Max(0, heightMap[i, j] - offset / 1000f); // Convert offset to match height map scale
			}
		}

		// Set the modified heights back to the terrain data
		terrainData.SetHeightsDelayLOD(startX, startY, heightMap);
	}
	
	public static void ShowLandMask()
	{	
		if(LandMask!=null){	
			LandMask.gameObject.SetActive(true);
		}
		
	}
	
	public static void HideLandMask()
	{
		LandMask.gameObject.SetActive(false);
	}
	
	public static float[,,] GetLayerData(LayerType layer, int topology = -1)
    {
        switch (layer)
        {
            case LayerType.Ground:

                return Ground;
            case LayerType.Biome:

                return Biome;
            case LayerType.Topology:
                if (topology < 0 || topology >= TerrainTopology.COUNT)
                {
                    Debug.LogError($"GetSplatMap({layer}, {topology}) topology parameter out of bounds. Should be between 0 - {TerrainTopology.COUNT - 1}");
                    return null;
                }

                return Topology[topology];
            default:
                Debug.LogError($"GetSplatMap({layer}) cannot return type float[,,].");
                return null;
        }
    }
	
		
	public static void SetLayerData(float[,,] array, LayerType layer, int topology = -1)
	{
		if (array == null)
		{
			Debug.LogWarning($"SetLayerArray(array) is null.");
			return;
		}

		if (layer == LayerType.Alpha)
		{
			Debug.LogWarning($"SetLayerArray(float[,,], {layer}) is not a valid layer to set. Use SetAlphaMap(bool[,]) to set {layer}.");
			return;
		}

		// Check for array dimensions not matching expected size
		if (array.GetLength(0) != SplatMapRes || array.GetLength(1) != SplatMapRes || array.GetLength(2) != LayerCount(layer))
		{
			Debug.LogError($"SetLayerArray(array[{array.GetLength(0)}, {array.GetLength(1)}, {array.GetLength(2)}]) dimensions invalid, should be " +
				$"array[{SplatMapRes}, {SplatMapRes}, {LayerCount(layer)}].");
			return;
		}

		// Update the internal data
		switch (layer)
		{
			case LayerType.Ground:
				Ground = array;
				RegisterSplatMapUndo($"Set {layer} Layer Array");
				TerrainManager.ChangeLayer(LayerType.Ground, 0);
				Land.terrainData.SetAlphamaps(0, 0, array);
				LayerDirty = false;
				break;
			case LayerType.Biome:
				Biome = array;
				RegisterSplatMapUndo($"Set {layer} Layer Array");
				TerrainManager.ChangeLayer(LayerType.Biome, 0);
				Land.terrainData.SetAlphamaps(0, 0, array);
				LayerDirty = false;
				break;
			case LayerType.Topology:
			Debug.LogError("boom boom mancini");
				if (topology < 0 || topology >= TerrainTopology.COUNT)
				{
					Debug.LogError($"SetLayerArray({layer}, {topology}) topology parameter out of bounds. Should be between 0 - {TerrainTopology.COUNT - 1}");
					return;
				}
				Topology[topology] = array;
				// Convert float[,,] to bool[,] for TopologyData
				bool[,] bitmap = ConvertSplatToBitmap(array);
				TopologyData.SetTopology(TerrainTopology.IndexToType(topology), 0, 0, SplatMapRes, SplatMapRes, bitmap);
				break;
		}

		// Notify listeners of the update
		//Callbacks.InvokeLayerUpdated(layer, topology);
	}

	// Helper method to convert float[,,] to bool[,] for topology
	public static bool[,] ConvertSplatToBitmap(float[,,] splatMap)
	{
		bool[,] bitmap = new bool[splatMap.GetLength(0), splatMap.GetLength(1)];
		for (int x = 0; x < splatMap.GetLength(0); x++)
		{
			for (int y = 0; y < splatMap.GetLength(1); y++)
			{
				bitmap[x, y] = splatMap[x, y, 0] > 0.5f; // Texture 0 is active, threshold at 0.5
			}
		}
		return bitmap;
	}
	/// <summary>Sets SplatMap of the selected LayerType.</summary>

    /// <param name="layer">The layer to set the data to.</param>
    /// <param name="topology">The topology layer if layer is topology.</param>
    public static void SetSplatMap(float[,,] array, LayerType layer, int topology = -1)
    {
        if (array == null)
        {
            Debug.LogWarning($"SetSplatMap(array) is null.");
            return;
        }

        if (layer == LayerType.Alpha)
        {
            Debug.LogWarning($"SetSplatMap(float[,,], {layer}) is not a valid layer to set. Use SetAlphaMap(bool[,]) to set {layer}.");
            return;
        }

        // Check for array dimensions not matching alphamap.
        if (array.GetLength(0) != SplatMapRes || array.GetLength(1) != SplatMapRes || array.GetLength(2) != LayerCount(layer))
        {
            Debug.LogError($"SetSplatMap(array[{array.GetLength(0)}, {array.GetLength(1)}, {LayerCount(layer)}]) dimensions invalid, should be " +
                $"array[{ SplatMapRes}, { SplatMapRes}, {LayerCount(layer)}].");
            return;
        }

        switch (layer)
        {
            case LayerType.Ground:
                Ground = array;
                break;
            case LayerType.Biome:
                Biome = array;
                break;
            case LayerType.Topology:
                if (topology < 0 || topology >= TerrainTopology.COUNT)
                {
                    Debug.LogError($"SetSplatMap({layer}, {topology}) topology parameter out of bounds. Should be between 0 - {TerrainTopology.COUNT - 1}");
                    return;
                }
                Topology[topology] = array;
                break;
        }

        if (CurrentLayerType == layer)
        {
            if (CurrentLayerType == LayerType.Topology && TopologyLayer != topology)
                return;
            if (!GetTerrainLayers().Equals(Land.terrainData.terrainLayers))
                Land.terrainData.terrainLayers = GetTerrainLayers();

            RegisterSplatMapUndo($"{layer}");
            Land.terrainData.SetAlphamaps(0, 0, array);
            LayerDirty = false;
        }
    }

    /// <summary>Sets the AlphaMap (Holes) of the terrain.</summary>
    public static void SetAlphaMap(bool[,] array)
    {
        if (array == null)
        {
            Debug.LogError($"SetAlphaMap(array) is null.");
            return;
        }

        // Check for array dimensions not matching alphamap.
        if (array.GetLength(0) != AlphaMapRes || array.GetLength(1) != AlphaMapRes)
        {
            // Special case for converting Alphamaps from the Rust resolution to the Unity Editor resolution. 
            if (array.GetLength(0) == SplatMapRes && array.GetLength(1) == SplatMapRes)
            {
                if (Alpha == null || Alpha.GetLength(0) != AlphaMapRes)
                    Alpha = new bool[AlphaMapRes, AlphaMapRes];

                Parallel.For(0, AlphaMapRes, i =>
                {
                    for (int j = 0; j < AlphaMapRes; j++)
                        Alpha[i, j] = array[i / 2, j / 2];
                });

                Land.terrainData.SetHoles(0, 0, Alpha);
                AlphaDirty = false;
                return;
            }

            else
            {
                Debug.LogError($"SetAlphaMap(array[{array.GetLength(0)}, {array.GetLength(1)}]) dimensions invalid, should be array[{AlphaMapRes}, {AlphaMapRes}].");
                return;
            }
        }

        Alpha = array;
        Land.terrainData.SetHoles(0, 0, Alpha);
        AlphaDirty = false;
    }

	/// <summary>Sets a specific region of the AlphaMap (Holes) of the terrain with only the changed areas.</summary>
	public static void SetAlphaMap(bool[,] array, int x, int y, int width, int height)
	{
		if (array == null)
		{
			Debug.LogError($"SetAlphaMap(array, x: {x}, y: {y}, width: {width}, height: {height}) is null.");
			return;
		}

		// Check if the provided array dimensions match the intended region size
		if (array.GetLength(0) != height || array.GetLength(1) != width)
		{
			Debug.LogError($"SetAlphaMap(array[{array.GetLength(0)}, {array.GetLength(1)}], x: {x}, y: {y}, width: {width}, height: {height}) dimensions invalid, should be array[{height}, {width}].");
			return;
		}

		// Ensure Alpha has the size of the entire map
		if (Alpha == null || Alpha.GetLength(0) != AlphaMapRes || Alpha.GetLength(1) != AlphaMapRes)
		{
			Alpha = new bool[AlphaMapRes, AlphaMapRes];
		}

		// Update Alpha with new data
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				// Check if the coordinate is within the bounds of the terrain
				if (x + j < AlphaMapRes && y + i < AlphaMapRes)
				{
					Alpha[y + i, x + j] = array[i, j];
				}
				else
				{
					Debug.LogWarning($"Attempted to set a hole outside of terrain bounds at ({x + j}, {y + i}).");
				}
			}
		}


		// Set only the changed region of the holes
		Land.terrainData.SetHoles(x, y, array);
		AlphaDirty = false;
	}

    private static void SplatMapChanged(Terrain terrain, string textureName, RectInt texelRegion, bool synched)
    {
		if(terrain==LandMask){
			return;
		}
		
		//SyncAlphaTexture();		
        //SyncBiomeTexture();
		
        if (!IsLoading && Land.Equals(terrain) && Mouse.current.leftButton.isPressed)
        {
            switch (textureName)
            {
                case "holes":
                    AlphaDirty = true;
                    Callbacks.InvokeLayerUpdated(LayerType.Alpha, TopologyLayer);
                    break;
                case "alphamap":
                    LayerDirty = true;
                    Callbacks.InvokeLayerUpdated(CurrentLayerType, TopologyLayer);
                    break;
            }
        }
    }
    #endregion
    #endregion

    #region HeightMap
    #region Fields
 
    #endregion

    #region Methods
    public static void ResetHeightCache()
    {
		Curvature = null;
        Slope = null;
        Height = null;
    }

    public static void UpdateHeightCache()
    {
		UpdateHeights();
        UpdateSlopes();
        UpdateCurves();
    }
	

    /// <summary>Checks if selected Alphamap index is a valid coord.</summary>
    /// <returns>True if Alphamap index is valid, false otherwise.</returns>
    public static bool IsValidIndex(int x, int y)
    {
        if (x < 0 || y < 0 || x >= SplatMapRes || y >= SplatMapRes)
            return false;

        return true;
    }

    /// <summary>Returns the slope of the HeightMap at the selected coords.</summary>
    /// <returns>Float within the range 0° - 90°. Null if out of bounds.</returns>
    public static float? GetSlope(int x, int y)
    {
        if (!IsValidIndex(x, y))
        {
            Debug.LogError($"Index: {x},{y} is out of bounds for GetSlope.");
            return null;
        }

        if (Slope == null)
            return Land.terrainData.GetSteepness((float)y / SplatMapRes, (float)x / SplatMapRes);

        return Slope[x, y];
    }

    /// <summary>Returns a 2D array of the slope values.</summary>
    /// <returns>Floats within the range 0° - 90°.</returns>
    public static float[,] GetSlopes()
    {
        if (Slope != null)
            return Slope;
        if (Slope == null)

            Slope = new float[HeightMapRes, HeightMapRes];

        for (int x = 0; x < HeightMapRes; x++)
            for (int y = 0; y < HeightMapRes; y++)
                Slope[x, y] = Land.terrainData.GetSteepness((float)y / HeightMapRes, (float)x / HeightMapRes);

        return Slope;
    }
	
	public static float[,] GetSlopes(int length)
    {
        if (Slope != null)
            return Slope;
        if (Slope == null)
            Slope = new float[length, length];

        for (int x = 0; x < length; x++)
            for (int y = 0; y < length; y++)
                Slope[x, y] = Land.terrainData.GetSteepness((float)y / HeightMapRes, (float)x / HeightMapRes);

        return Slope;
    }
	
	public static float[,] GetCurves()
    {
		float changeInSlopeX, changeInSlopeY, changeInSlopeXY, changeInSlopeYX;
        if (Curvature != null)
            return Curvature;
        if (Curvature == null)
            Curvature = new float[HeightMapRes, HeightMapRes];

        for (int i = 2; i < HeightMapRes-2; i++)
		{
            for (int j = 2; j < HeightMapRes-2; j++)
            {
					changeInSlopeX = (Height[i + 2, j] + Height[i - 2, j]) - (2 * Height[i, j]);
					changeInSlopeY = (Height[i, j + 2] + Height[i, j - 2]) - (2 * Height[i, j]);
					changeInSlopeXY = (Height[i + 2, j + 2] + Height[i - 2, j - 2]) - (2 * Height[i, j]);
					changeInSlopeYX = (Height[i - 2, j + 2] + Height[i + 2, j - 2]) - (2 * Height[i, j]);
					Curvature[i, j] =  ((changeInSlopeX + changeInSlopeY + changeInSlopeXY + changeInSlopeYX) / 4);	
			}
		}
			
        return Curvature;
    }
	
	public static float[,] GetCurves(int power)
    {
		float changeInSlopeX, changeInSlopeY, changeInSlopeXY, changeInSlopeYX;
        if (Curvature != null)
            return Curvature;
        if (Curvature == null)
            Curvature = new float[HeightMapRes, HeightMapRes];

        for (int i = 2; i < HeightMapRes-2; i++)
		{
            for (int j = 2; j < HeightMapRes-2; j++)
            {
					changeInSlopeX = (Height[i + 2, j] + Height[i - 2, j]) - (2 * Height[i, j]);
					changeInSlopeY = (Height[i, j + 2] + Height[i, j - 2]) - (2 * Height[i, j]);
					changeInSlopeXY = (Height[i + 2, j + 2] + Height[i - 2, j - 2]) - (2 * Height[i, j]);
					changeInSlopeYX = (Height[i - 2, j + 2] + Height[i + 2, j - 2]) - (2 * Height[i, j]);
					Curvature[i, j] =  ((changeInSlopeX + changeInSlopeY + changeInSlopeXY + changeInSlopeYX) / 4);
					Curvature[i,j] = Mathf.Abs(Curvature[i,j]) * power;
			}
		}
			
        return Curvature;
    }

    /// <summary>Updates cached Slope values with current.</summary>
    public static void UpdateSlopes() => GetSlopes();
	public static void UpdateCurves() => GetCurves();
	

    /// <summary>Returns the height of the HeightMap at the selected coords.</summary>
    /// <returns>Float within the range 0m - 1000m. Null if out of bounds.</returns>
    public static float? GetHeight(int x, int y)
    {
        if (!IsValidIndex(x, y))
        {
            Debug.LogError($"Index: {x},{y} is out of bounds for GetHeight.");
            return 0;
        }

        if (Height == null)
            return Land.terrainData.GetInterpolatedHeight((float)x / SplatMapRes, (float)y / SplatMapRes); ;

        return Height[x, y];
    }
	

    /// <summary>Returns a 2D array of the height values.</summary>
    /// <returns>Floats within the range 0m - 1000m.</returns>
    public static float[,] GetHeights(TerrainType terrain = TerrainType.Land)
    {
        if (Height != null && terrain == TerrainType.Land)
            return Height;
        if (terrain == TerrainType.Land)
        {
            Height = Land.terrainData.GetInterpolatedHeights(0, 0, HeightMapRes, HeightMapRes, 1f / HeightMapRes, 1f / HeightMapRes);
            return Height;
        }
        return Water.terrainData.GetInterpolatedHeights(0, 0, HeightMapRes, HeightMapRes, 1f / HeightMapRes, 1f / HeightMapRes);

    }

    /// <summary>Updates cached Height values with current.</summary>
    public static void UpdateHeights() => GetHeights();

    /// <summary>Returns the HeightMap array of the selected terrain.</summary>
    /// <param name="terrain">The HeightMap to return.</param>
    public static float[,] GetHeightMap(TerrainType terrain = TerrainType.Land)
    {
        if (terrain == TerrainType.Land)
            return Land.terrainData.GetHeights(0, 0, HeightMapRes, HeightMapRes);
        return Water.terrainData.GetHeights(0, 0, HeightMapRes, HeightMapRes);
    }

    /// <summary>Rotates the HeightMap 90° Clockwise or Counter Clockwise.</summary>
    /// <param name="CW">True = 90°, False = 270°</param>
    public static void RotateHeightMap(bool CW, TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        RegisterHeightMapUndo(terrain, "Rotate HeightMap");
        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Rotate(GetHeightMap(), CW, dmns));
        else
            Water.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Rotate(GetHeightMap(TerrainType.Water), CW, dmns));
    }

    /// <summary>Sets the HeightMap to the height input.</summary>
    /// <param name="height">The height to set.</param>
    public static void SetHeightMapHeight(float height, TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        height /= 1000f; // Normalises user input to a value between 0 - 1f.
        RegisterHeightMapUndo(terrain, "Set HeightMap Height");

        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.SetValues(GetHeightMap(), height, dmns));
        else
            Water.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.SetValues(GetHeightMap(TerrainType.Water), height, dmns));
    }

    /// <summary>Inverts the HeightMap heights.</summary>
    public static void InvertHeightMap(TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        RegisterHeightMapUndo(terrain, "Invert HeightMap");
        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Invert(GetHeightMap(), dmns));
        else
            Water.terrainData.SetHeights(0, 0, GetHeightMap(TerrainType.Water));
    }

    /// <summary> Normalises the HeightMap between two heights.</summary>
    /// <param name="normaliseLow">The lowest height the HeightMap should be.</param>
    /// <param name="normaliseHigh">The highest height the HeightMap should be.</param>

    public static void NormaliseHeightMap(float normaliseLow, float normaliseHigh, TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        normaliseLow /= 1000f; normaliseHigh /= 1000f; // Normalises user input to a value between 0 - 1f.
        RegisterHeightMapUndo(terrain, "Normalise HeightMap");

        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Normalise(GetHeightMap(), normaliseLow, normaliseHigh, dmns));
        else
            Water.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Normalise(GetHeightMap(TerrainType.Water), normaliseLow, normaliseHigh, dmns));
    }
	
	[ConsoleCommand("Squeezes the heightmap to range")]
	public static void SqueezeHeightMap(float normaliseLow, float normaliseHigh)
	{
		normaliseLow /= 1000f;  // Normalize user input from meters to 0-1
		normaliseHigh /= 1000f; // Normalize user input from meters to 0-1

		// Validate input range
		if (normaliseLow >= normaliseHigh)
		{
			Debug.LogError($"Invalid range: normaliseLow ({normaliseLow * 1000f}m) must be less than normaliseHigh ({normaliseHigh * 1000f}m).");
			return;
		}

		// Get current heightmap
		float[,] heights = GetHeightMap(TerrainType.Land);
		int res = HeightMapRes; // Heightmap resolution

		// Get current min/max heights
		float minHeight = float.MaxValue;
		float maxHeight = float.MinValue;
		for (int y = 0; y < res; y++)
		{
			for (int x = 0; x < res; x++)
			{
				float height = heights[x, y];
				if (height < minHeight) minHeight = height;
				if (height > maxHeight) maxHeight = height;
			}
		}

		// Avoid division by zero if flat
		if (Mathf.Approximately(minHeight, maxHeight))
		{
			Debug.LogWarning("Heightmap is flat; setting all heights to normaliseLow.");
			for (int y = 0; y < res; y++)
				for (int x = 0; x < res; x++)
					heights[x, y] = normaliseLow;
		}
		else
		{
			// Remap heights to new range
			float range = maxHeight - minHeight;
			for (int y = 0; y < res; y++)
			{
				for (int x = 0; x < res; x++)
				{
					float normalized = (heights[x, y] - minHeight) / range; // 0-1 based on original range
					heights[x, y] = Mathf.Lerp(normaliseLow, normaliseHigh, normalized); // Remap to new range
				}
			}
		}

		// Apply to Land terrain with undo
		RegisterHeightMapUndo(TerrainType.Land, $"Squeeze HeightMap {normaliseLow * 1000f}m-{normaliseHigh * 1000f}m");
		Land.terrainData.SetHeights(0, 0, heights);
		Callbacks.InvokeHeightMapUpdated(TerrainType.Land); // Notify listeners

		// SplatRatio doesn’t require direct adjustment here since we’re operating on heightmap resolution
		// Ensure splatmaps remain consistent if needed
	}

    /// <summary>Increases or decreases the HeightMap by the offset.</summary>
    /// <param name="offset">The amount to offset by. Negative values offset down.</param>
    /// <param name="clampOffset">Check if offsetting the HeightMap would exceed the min-max values.</param>

	public static void OffsetHeightMap(float offset, bool clampOffset, TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        offset /= 1000f; // Normalises user input to a value between 0 - 1f.
        RegisterHeightMapUndo(terrain, "Offset HeightMap");

        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Offset(GetHeightMap(), offset, clampOffset, dmns));
        else
            Water.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.Offset(GetHeightMap(TerrainType.Water), offset, clampOffset, dmns));
    }
	
	
	
	[ConsoleCommand("Moves heightmap vertically")]
	public static void NudgeHeightMap(float offset)
	{
		offset /= 1000f; // Normalises user input to a value between 0 - 1f.

		// Get current heightmap
		float[,] heights = GetHeightMap(TerrainType.Land);
		int res = HeightMapRes;

		// Offset all heights
		float[,] offsetHeights = new float[res, res];
		for (int y = 0; y < res; y++)
		{
			for (int x = 0; x < res; x++)
			{
				offsetHeights[x, y] = Mathf.Clamp01(heights[x, y] + offset); // Apply offset and clamp to 0-1
			}
		}

		// Apply to terrain with undo
		RegisterHeightMapUndo(TerrainType.Land, "Offset HeightMap");
		Land.terrainData.SetHeights(0, 0, offsetHeights);
		Callbacks.InvokeHeightMapUpdated(TerrainType.Land); // Notify listeners
	}
    /// <summary>Sets the HeightMap level to the minimum if it's below.</summary>
    /// <param name="minimumHeight">The minimum height to set.</param>
    /// <param name="maximumHeight">The maximum height to set.</param>
	[ConsoleCommand("flattens heightmap outside of bounds")]
    public static void ClampHeightMap(float minimumHeight, float maximumHeight, TerrainType terrain = TerrainType.Land, Area dmns = null)
    {
        minimumHeight /= 1000f; maximumHeight /= 1000f; // Normalises user input to a value between 0 - 1f.
        RegisterHeightMapUndo(terrain, "Clamp HeightMap");

        if (terrain == TerrainType.Land)
            Land.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.ClampValues(GetHeightMap(), minimumHeight, maximumHeight, dmns));
        else
            Water.terrainData.SetHeights(0, 0, RustMapEditor.Maths.Array.ClampValues(GetHeightMap(TerrainType.Water), minimumHeight, maximumHeight, dmns));
    }

    /// <summary>Terraces the HeightMap.</summary>
    /// <param name="featureSize">The height of each terrace.</param>
    /// <param name="interiorCornerWeight">The weight of the terrace effect.</param>
	[ConsoleCommand("uniform terracing")]
    public static void TerraceErodeHeightMap(float featureSize, float interiorCornerWeight)
    {
        RegisterHeightMapUndo(TerrainType.Land, "Erode HeightMap");

		#if UNITY_EDITOR
        Material mat = new Material((Shader)AssetDatabase.LoadAssetAtPath("Packages/com.unity.terrain-tools/Shaders/TerraceErosion.shader", typeof(Shader)));
		#else
		Material mat = new Material(Resources.Load<Shader>("Shaders/TerraceErosion"));
		#endif
		
        UnityEngine.TerrainTools.BrushTransform brushXform = UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(Land, HeightMapCentre, Land.terrainData.size.x, 0.0f);
        UnityEngine.TerrainTools.PaintContext paintContext = UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(Land, brushXform.GetBrushXYBounds());
        Vector4 brushParams = new Vector4(1.0f, featureSize, interiorCornerWeight, 0.0f);
        mat.SetTexture("_BrushTex", FilterTexture);
        mat.SetVector("_BrushParams", brushParams);
        UnityEngine.TerrainTools.TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
        Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);
        UnityEngine.TerrainTools.TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Filter - TerraceErosion");
    }

    /// <summary>Smooths the HeightMap.</summary>
    /// <param name="filterStrength">The strength of the smoothing.</param>
    /// <param name="blurDirection">The direction the smoothing should preference. Between -1f - 1f.</param>
	[ConsoleCommand("heightmap smoothing")]
    public static void SmoothHeightMap(float filterStrength, float blurDirection)
    {
        RegisterHeightMapUndo(TerrainType.Land, "Smooth HeightMap");

        Material mat = UnityEngine.TerrainTools.TerrainPaintUtility.GetBuiltinPaintMaterial();
        UnityEngine.TerrainTools.BrushTransform brushXform = UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(Land, HeightMapCentre, Land.terrainData.size.x, 0.0f);
        UnityEngine.TerrainTools.PaintContext paintContext = UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(Land, brushXform.GetBrushXYBounds());
        Vector4 brushParams = new Vector4(filterStrength, 0.0f, 0.0f, 0.0f);
        mat.SetTexture("_BrushTex", FilterTexture);
        mat.SetVector("_BrushParams", brushParams);
        Vector4 smoothWeights = new Vector4(Mathf.Clamp01(1.0f - Mathf.Abs(blurDirection)), Mathf.Clamp01(-blurDirection), Mathf.Clamp01(blurDirection), 0.0f);
        mat.SetVector("_SmoothWeights", smoothWeights);
        UnityEngine.TerrainTools.TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);
        Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, (int)UnityEngine.TerrainTools.TerrainBuiltinPaintMaterialPasses.SmoothHeights);
        UnityEngine.TerrainTools.TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Filter - Smooth Heights");
    }

    #endregion
    #endregion

	    private sealed class HeightSlopeProcessor
    {
        public int BlockSize;
        public int DestinationSize;
        public int SourceSize;
        public Color32[] HeightColors;
        public float SlopeScale;
        public float NormalY;
        public float HeightScale;
        public float HeightOffset;
        public Color[] Pixels;

        public void ProcessRow(int z)
        {
            int sourceZ = z * BlockSize;
            int pixelIndex = z * DestinationSize;
            int sourceX = 0;

            for (int x = 0; x < DestinationSize; x++)
            {
                float h00 = 0f, h10 = 0f, h01 = 0f, h11 = 0f;
                int index00 = sourceZ * SourceSize + sourceX;
                int index10 = index00 + 1;
                int index01 = index00 + SourceSize;
                int index11 = index10 + SourceSize;

                for (int by = 0; by < BlockSize; by++)
                {
                    for (int bx = 0; bx < BlockSize; bx++)
                    {
                        h00 += (HeightColors[index00 + bx].b << 8 | HeightColors[index00 + bx].r);
                        h10 += (HeightColors[index10 + bx].b << 8 | HeightColors[index10 + bx].r);
                        h01 += (HeightColors[index01 + bx].b << 8 | HeightColors[index01 + bx].r);
                        h11 += (HeightColors[index11 + bx].b << 8 | HeightColors[index11 + bx].r);
                    }
                    index00 += SourceSize;
                    index10 += SourceSize;
                    index01 += SourceSize;
                    index11 += SourceSize;
                }

                float dx = (h10 + h11 - h00 - h01) * SlopeScale;
                float dz = (h01 + h11 - h00 - h10) * SlopeScale;
                Vector3 normal = new Vector3(dx, NormalY, dz).normalized;
                float height = (h00 + h10 + h01 + h11) * HeightScale + HeightOffset;
                float slope = normal.x * normal.x + normal.z * normal.z;

                Pixels[pixelIndex + x] = new Color(height, slope, 0f, 0f);
                sourceX += BlockSize;
            }
        }
    }

    #region Terrains


    #region Methods

	
	public static void SetTerrainReferences()
	{
		
		
		Water = GameObject.FindGameObjectWithTag("Water").GetComponent<Terrain>();
		
		Land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		
		LandMask = GameObject.FindGameObjectWithTag("LandMask").GetComponent<Terrain>();
		
		

	}

    public static void SetWaterTransparency(float alpha)
    {
        Color _color = WaterMaterial.color;
        _color.a = alpha;
        WaterMaterial.color = _color;
    }

    /// <summary>Loads and sets up the terrain and associated splatmaps.</summary>
    /// <param name="mapInfo">Struct containing all info about the map to initialise.</param>
    
	#if UNITY_EDITOR
	public static void Load(MapInfo mapInfo, int progressID = 0)
    {
        if (!IsLoading)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Load(mapInfo, progressID));
    }
	#else
	public static void Load(MapInfo mapInfo, int progressID = 0)
    {

            CoroutineManager.Instance.StartCoroutine(Coroutines.Load(mapInfo, progressID));

    }
	#endif
	
    #endregion
    #endregion

    #region Terrain Layers
    /// <summary>The Terrain layers used by the terrain for paint operations</summary>

    #region Methods
    /// <summary>Sets the unity terrain references if not already set, and returns the current terrain layers.</summary>
    /// <returns>Array of TerrainLayers currently displayed on the Land Terrain.</returns>
    public static TerrainLayer[] GetTerrainLayers()
    {
        if (GroundLayers == null || BiomeLayers == null || TopologyLayers == null)
            SetTerrainLayers();

        return CurrentLayerType switch
        {
            LayerType.Ground => GroundLayers,
            LayerType.Biome => BiomeLayers,
            _ => TopologyLayers
        };
    }

	public static Vector3 GetMapOffset()
    {
        return 0.5f * TerrainSize;
    }
	
    /// <summary>Sets the TerrainLayer references in TerrainManager to the asset on disk.</summary>
    public static void SetTerrainLayers()
    {
        GroundLayers = GetGroundLayers();
        BiomeLayers = GetBiomeLayers();
        TopologyLayers = GetTopologyLayers();
		MaskLayers = GetMaskLayers();

		#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
		#endif
    }

	#if UNITY_EDITOR
    private static TerrainLayer[] GetGroundLayers()
    {
		TerrainLayer[] textures = new TerrainLayer[8];
				if (SettingsManager.application.terrainTextureSet)
				{		
				textures[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Dirt.terrainlayer");
				textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/dirt");
				textures[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Snow.terrainlayer");
				textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/snow");
				textures[2] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Sand.terrainlayer");
				textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/sand");
				textures[3] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Rock.terrainlayer");
				textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/rock");
				textures[4] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Grass.terrainlayer");
				textures[4].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/grass");
				textures[5] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Forest.terrainlayer");
				textures[5].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/forest");
				textures[6] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Stones.terrainlayer");
				textures[6].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/stones");
				textures[7] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground/Gravel.terrainlayer");
				textures[7].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/gravel");
				return textures;
				}
				
				textures[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Dirt.terrainlayer");
				textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/dirt1");
				textures[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Snow.terrainlayer");
				textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/snow1");
				textures[2] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Sand.terrainlayer");
				textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/sand1");
				textures[3] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Rock.terrainlayer");
				textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/rock1");
				textures[4] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Grass.terrainlayer");
				textures[4].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/grass1");
				textures[5] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Forest.terrainlayer");
				textures[5].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/forest1");
				textures[6] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Stones.terrainlayer");
				textures[6].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/stones1");
				textures[7] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Ground1/Gravel.terrainlayer");
				textures[7].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/gravel1");

		
        return textures;
    }

    private static TerrainLayer[] GetBiomeLayers()
    {
        TerrainLayer[] textures = new TerrainLayer[4];
        textures[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Biome/Arid.terrainlayer");
        textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/arid");
        textures[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Biome/Temperate.terrainlayer");
        textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/temperate");
        textures[2] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Biome/Tundra.terrainlayer");
        textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/tundra");
        textures[3] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Biome/Arctic.terrainlayer");
        textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/arctic");
        return textures;
    }

    private static TerrainLayer[] GetTopologyLayers()
    {
        TerrainLayer[] textures = new TerrainLayer[2];
        textures[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Topology/Active.terrainlayer");
        textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Topology/active");
        textures[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Topology/InActive.terrainlayer");
        textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Topology/inactive");
        return textures;
    }

	
	private static TerrainLayer[] GetMaskLayers()
    {
        TerrainLayer[] textures = new TerrainLayer[3];
        textures[0] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Mask/geology.terrainlayer");
        textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/geology");
        textures[1] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Mask/lessgeology.terrainlayer");
        textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/lessgeology");
		textures[2] = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Resources/Textures/Mask/placement.terrainlayer");
        textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/placement");
        return textures;
    }
	
	
	#else
	private static TerrainLayer[] GetGroundLayers()
	{
		TerrainLayer[] textures = new TerrainLayer[8];
		
		if (!SettingsManager.application.terrainTextureSet){
			textures[0] = Resources.Load<TerrainLayer>("Textures/Ground1/Dirt"); 
			textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/dirt1");
			
			textures[1] = Resources.Load<TerrainLayer>("Textures/Ground1/Snow"); 
			textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/snow1");

			textures[2] = Resources.Load<TerrainLayer>("Textures/Ground1/Sand"); 
			textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/sand1");
			
			textures[3] = Resources.Load<TerrainLayer>("Textures/Ground1/Rock"); 
			textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/rock1");

			textures[4] = Resources.Load<TerrainLayer>("Textures/Ground1/Grass"); 
			textures[4].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/grass1");

			textures[5] = Resources.Load<TerrainLayer>("Textures/Ground1/Forest"); 
			textures[5].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/forest1");

			textures[6] = Resources.Load<TerrainLayer>("Textures/Ground1/Stones"); 
			textures[6].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground1/stones1");

			textures[7] = Resources.Load<TerrainLayer>("Textures/Ground1/Gravel"); 
			textures[7].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/gravel1");
			return textures;
		}
			
			
			textures[0] = Resources.Load<TerrainLayer>("Textures/Ground/Dirt"); 
			textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/dirt");
			
			textures[1] = Resources.Load<TerrainLayer>("Textures/Ground/Snow"); 
			textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/snow");

			textures[2] = Resources.Load<TerrainLayer>("Textures/Ground/Sand"); 
			textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/sand");
			
			textures[3] = Resources.Load<TerrainLayer>("Textures/Ground/Rock"); 
			textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/rock");

			textures[4] = Resources.Load<TerrainLayer>("Textures/Ground/Grass"); 
			textures[4].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/grass");

			textures[5] = Resources.Load<TerrainLayer>("Textures/Ground/Forest"); 
			textures[5].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/forest");

			textures[6] = Resources.Load<TerrainLayer>("Textures/Ground/Stones"); 
			textures[6].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/stones");

			textures[7] = Resources.Load<TerrainLayer>("Textures/Ground/Gravel"); 
			textures[7].diffuseTexture = Resources.Load<Texture2D>("Textures/Ground/gravel");
			return textures;
	}

	private static TerrainLayer[] GetBiomeLayers()
	{
		TerrainLayer[] textures = new TerrainLayer[4];
		textures[0] = Resources.Load<TerrainLayer>("Textures/Biome/Arid");
		textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/arid");

		textures[1] = Resources.Load<TerrainLayer>("Textures/Biome/Temperate");
		textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/temperate");

		textures[2] = Resources.Load<TerrainLayer>("Textures/Biome/Tundra");
		textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/tundra");

		textures[3] = Resources.Load<TerrainLayer>("Textures/Biome/Arctic");
		textures[3].diffuseTexture = Resources.Load<Texture2D>("Textures/Biome/arctic");

		return textures;
	}

	private static TerrainLayer[] GetTopologyLayers()
	{
		TerrainLayer[] textures = new TerrainLayer[2];
		textures[0] = Resources.Load<TerrainLayer>("Textures/Topology/Active");
		textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Topology/active");

		textures[1] = Resources.Load<TerrainLayer>("Textures/Topology/Inactive");
		textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Topology/inactive");

		return textures;
	}

	private static TerrainLayer[] GetMaskLayers()
	{
		TerrainLayer[] textures = new TerrainLayer[3];
		textures[0] = Resources.Load<TerrainLayer>("Textures/Mask/Geology");
		textures[0].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/geology");

		textures[1] = Resources.Load<TerrainLayer>("Textures/Mask/LessGeology");
		textures[1].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/lessgeology");

		textures[2] = Resources.Load<TerrainLayer>("Textures/Mask/Placement");
		textures[2].diffuseTexture = Resources.Load<Texture2D>("Textures/Mask/placement");

		return textures;
	}
	
	#endif

    #endregion
    #endregion

    #region Layers

    public static void SetHeightMapRegion(float[,] array, int x, int y, int width, int height, TerrainType terrain = TerrainType.Land)
    {
        float[,] fullMap = GetHeightMap(terrain);
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (x + j < HeightMapRes && y + i < HeightMapRes)
                {
                    fullMap[y + i, x + j] = array[i, j];
                    if (terrain == TerrainType.Land)
                    {
                        Height[y + i, x + j] = array[i, j]; // Sync Height array
                    }
                }
            }
        }
        if (terrain == TerrainType.Land)
        {
            Land.terrainData.SetHeights(x, y, array);
            SyncHeightTexture(); 
            Callbacks.InvokeHeightMapUpdated(TerrainType.Land);
        }
        else
        {
            Water.terrainData.SetHeights(x, y, array);
            Callbacks.InvokeHeightMapUpdated(TerrainType.Water);
        }
    }


    private static void HeightMapChanged(Terrain terrain, RectInt heightRegion, bool synched)
    {
		if (terrain.Equals(LandMask))
        {
            return;
        }
		
		if (terrain == Land)
		{
		#if UNITY_EDITOR
				EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.GenerateNormalMap(HeightMapRes - 1, Progress.Start("Regenerate Normal Map")));
		#else
				Land.StartCoroutine(Coroutines.GenerateNormalMap(HeightMapRes - 1, -1));
		#endif
				Callbacks.InvokeHeightMapUpdated(TerrainType.Land);
		}
		

        if (terrain.Equals(Land))
        {
            SyncHeightTexture();
        }
        ResetHeightCache();
        Callbacks.InvokeHeightMapUpdated(terrain.Equals(Land) ? TerrainType.Land : TerrainType.Water);
    }
	
    public static void SyncHeightTexture()
    {
        if (Height == null || Height.GetLength(0) != HeightMapRes)
        {
            Debug.LogError("Height data is not initialized or resolution mismatch." + Height.GetLength(0) + " " + HeightMapRes);
            return;
        }

        Texture2D tempTexture = new Texture2D(HeightMapRes, HeightMapRes, TextureFormat.RGBA32, false, true);
        Color32[] colors = new Color32[HeightMapRes * HeightMapRes];

        for (int z = 0; z < HeightMapRes; z++)
        {
            for (int x = 0; x < HeightMapRes; x++)
            {
                float height = Height[x, z]; // Note: x,z order matches Unity’s heightmap convention
                short shortHeight = BitUtility.Float2Short(height);
                colors[z * HeightMapRes + x] = BitUtility.EncodeShort(shortHeight);
            }
        }

        tempTexture.SetPixels32(colors);
        tempTexture.Apply(true, false);

        if (HeightTexture == null || HeightTexture.width != HeightMapRes || HeightTexture.height != HeightMapRes)
        {
            if (HeightTexture != null) UnityEngine.Object.Destroy(HeightTexture);
            HeightTexture = new Texture2D(HeightMapRes, HeightMapRes, TextureFormat.RGBA32, true, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        Graphics.CopyTexture(tempTexture, HeightTexture);
        UnityEngine.Object.Destroy(tempTexture);
        Shader.SetGlobalTexture("Terrain_HeightTexture", HeightTexture);
    }

    #region Methods
    /// <summary>Saves any changes made to the Alphamaps, including paint operations.</summary>
    public static void SaveLayer()
    {
        SetSplatMap(GetSplatMap(CurrentLayerType, TopologyLayer), CurrentLayerType, TopologyLayer);
        Callbacks.InvokeLayerSaved(CurrentLayerType, TopologyLayer);
    }

    public static float ToWorldHeight(float terrainHeight)
    {
        Vector3 terrainPos = Land.transform.position;
        return terrainPos.y + terrainHeight * Land.terrainData.size.y;
    }

    // Region-specific splatmap update
    public static void SetSplatMapRegion(float[,,] array, LayerType layer, int x, int y, int width, int height, int topology = -1)
    {
        float[,,] fullMap = GetSplatMap(layer, topology);
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                if (x + j < SplatMapRes && y + i < SplatMapRes)
                    for (int k = 0; k < LayerCount(layer); k++)
                        fullMap[y + i, x + j, k] = array[i, j, k];
        SetSplatMap(fullMap, layer, topology);
    }

    // Convert world corners to terrain grid bounds
    public static int[] WorldCornersToGrid(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight)
    {
        int minX = Mathf.FloorToInt(ToTerrainX(bottomLeft.x));
        int minZ = Mathf.FloorToInt(ToTerrainZ(bottomLeft.z));
        int maxX = Mathf.CeilToInt(ToTerrainX(topRight.x));
        int maxZ = Mathf.CeilToInt(ToTerrainZ(topRight.z));
        return new[] { minX, minZ, maxX, maxZ };
    }

    /// <summary>Changes the active Land and Topology Layers.</summary>
    /// <param name="layer">The LayerType to change to.</param>
    /// <param name="topology">The Topology layer to change to.</param>
	public static void ChangeLayer(LayerType layer, int topology = -1)
	{
		if (layer == LayerType.Alpha)
			return;

		if (layer == LayerType.Topology && (topology < 0 || topology >= TerrainTopology.COUNT))
		{
			Debug.LogError($"ChangeLayer({layer}, {topology}) topology parameter out of bounds. Should be between 0 - {TerrainTopology.COUNT - 1}");
			return;
		}

		if (LayerDirty)
		{
			SaveLayer();
		}

		CurrentLayerType = layer;
		// Check if TerrainTopology.IndexToType returns a valid enum before casting
		int typeIndex = TerrainTopology.IndexToType(topology);
		if (Enum.IsDefined(typeof(TerrainTopology.Enum), typeIndex))
		{
			TopologyLayerEnum = (TerrainTopology.Enum)typeIndex;
		}
		else
		{
			Debug.LogError($"Invalid TerrainTopology.Enum for topology index: {topology}");
		}

		// Assuming GetSplatMap returns a non-null value or has its own null check
		var splatMap = GetSplatMap(layer, topology);
		SetSplatMap(splatMap, layer, topology);
		
		// Check if ClearSplatMapUndo method exists before calling

		ClearSplatMapUndo();
		

		Callbacks.InvokeLayerChanged(layer, topology);
		
	}

    /// <summary>Layer count in layer chosen, used for determining the size of the splatmap array.</summary>
    /// <param name="layer">The LayerType to return the texture count from. (Ground, Biome or Topology)</param>
    public static int LayerCount(LayerType layer)
    {
        return layer switch
        {
            LayerType.Ground => 8,
            LayerType.Biome => 4,
            _ => 2
        };
    }
    #endregion
    #endregion

    #region Other
	#if UNITY_EDITOR
	
    /// <summary>Registers changes made to the HeightMap after the function is called.</summary>
    /// <param name="terrain">HeightMap to record.</param>
    /// <param name="name">Name of the Undo object on the stack.</param>
	public static void RegisterHeightMapUndo(TerrainType terrain, string name)
    {
        Undo.RegisterCompleteObjectUndo(terrain == TerrainType.Land ? Land.terrainData.heightmapTexture : Water.terrainData.heightmapTexture, name);
    }

    /// <summary>Registers changes made to the SplatMap after the function is called.</summary>
    /// <param name="terrain">SplatMap to record.</param>
    /// <param name="name">Name of the Undo object on the stack.</param>
    public static void RegisterSplatMapUndo(string name) => Undo.RegisterCompleteObjectUndo(Land.terrainData.alphamapTextures, name);

    /// <summary>Clears all undo operations on the currently displayed SplatMap.</summary>
	public static void ClearSplatMapUndo()
	{
		// Check if Land or terrainData is null before accessing alphamapTextures
		if (Land != null && Land.terrainData != null)
		{
			foreach (var tex in Land.terrainData.alphamapTextures)
			{
				// Check if tex is not null before calling Undo.ClearUndo
				if (tex != null)
				{
					Undo.ClearUndo(tex);
				}
			}
		}
		else
		{
			Debug.LogError("Land or Land.terrainData is null. Cannot clear splat map undo.");
		}
	}
	#else
	public static void RegisterHeightMapUndo(TerrainType terrain, string name) {  }
	public static void RegisterSplatMapUndo(string name) { }
	public static void ClearSplatMapUndo() { }
	#endif
    #endregion

    private class Coroutines
    {

        /// <summary>Loads and sets up the terrain and associated splatmaps.</summary>
        /// <param name="mapInfo">Struct containing all info about the map to initialise.</param>
        public static IEnumerator Load(MapInfo mapInfo, int progressID)
        {
			
			HeightMapRes = mapInfo.terrainRes;
			SplatMapRes = mapInfo.splatRes;

			// Initialize arrays directly from MapInfo
			Height = mapInfo.land.heights;
			Alpha = mapInfo.alphaMap;
			Ground = mapInfo.splatMap;
			Biome = mapInfo.biomeMap;
			TopologyData.Set(mapInfo.topology);
			if (Topology == null || Topology.Length != TerrainTopology.COUNT)
			{
				Topology = new float[TerrainTopology.COUNT][,,];
			}
			for (int i = 0; i < TerrainTopology.COUNT; i++)
			{
				Topology[i] = TopologyData.GetTopologyLayer(TerrainTopology.IndexToType(i));
			}
		
            IsLoading = true;
			yield return SetTerrains(mapInfo, progressID);
            yield return SetSplatMaps(mapInfo, progressID);
			
			//SyncHeightTexture();
			SyncAlphaTexture();
			SyncBiomeTexture();
			//SyncHeightSlopeTexture(Mathf.ClosestPowerOfTwo(HeightMapRes) >> 1);
			AlphaDirty = false;
			LayerDirty = false;
			
            ClearSplatMapUndo();
            AreaManager.Reset();
			
			#if UNITY_EDITOR
            Progress.Report(progressID, .99f, "Loaded Terrain.");
			#endif
            IsLoading = false;
        }

        /// <summary>Loads and sets the Land and Water terrain objects.</summary>
        private static IEnumerator SetTerrains(MapInfo mapInfo, int progressID)
        {
            HeightMapRes = mapInfo.terrainRes;

            yield return SetupTerrain(mapInfo, Water);
			
		    yield return SetupTerrain(mapInfo, Land);

			yield return SetupTerrain(mapInfo, LandMask);

        }


        /// <summary>Sets up the inputted terrain's terraindata.</summary>
        private static IEnumerator SetupTerrain(MapInfo mapInfo, Terrain terrain)
        {
            if (terrain.terrainData.size != mapInfo.size)
            {
                terrain.terrainData.heightmapResolution = mapInfo.terrainRes;
                terrain.terrainData.size = mapInfo.size;
                terrain.terrainData.alphamapResolution = mapInfo.splatRes;
                terrain.terrainData.baseMapResolution = mapInfo.splatRes;
            }
            terrain.terrainData.SetHeights(0, 0, terrain.Equals(Land) ? mapInfo.land.heights : mapInfo.water.heights);
			
            yield return null;
        }

	public static IEnumerator GenerateNormalMap(int resolution, int progressID)
	{
		if (Land == null || Land.terrainData == null)
		{
			Debug.LogError("Cannot generate normal map: Land terrain or its data is null.");
			yield break;
		}

		if (RuntimeNormalMap != null && RuntimeNormalMap.width != resolution)
		{
			UnityEngine.Object.Destroy(RuntimeNormalMap);
			RuntimeNormalMap = null;
		}

		if (RuntimeNormalMap == null)
		{
			RuntimeNormalMap = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true)
			{
				name = "TerrainNormal",
				wrapMode = TextureWrapMode.Clamp
			};
		}

		// Fetch height data on the main thread before processing
		float[,] heights = Land.terrainData.GetHeights(0, 0, resolution + 1, resolution + 1);
		Color32[] normals = new Color32[resolution * resolution];

		int batchSize = 256; // Adjust based on performance needs
		for (int y = 0; y < resolution; y += batchSize)
		{
			int batchHeight = Mathf.Min(batchSize, resolution - y);
			for (int i = 0; i < batchHeight; i++)
			{
				int localY = y + i;
				for (int x = 0; x < resolution; x++)
				{
					float height = heights[localY, x];
					float heightRight = heights[localY, x + 1];
					float heightUp = heights[localY + 1, x];

					// Calculate normal using cross product of height differences
					Vector3 normal = Vector3.Cross(
						new Vector3(1f, (heightRight - height) * Land.terrainData.size.y, 0f),
						new Vector3(0f, (heightUp - height) * Land.terrainData.size.y, 1f)
					).normalized;

					// Convert to Color32 (0-255 range, -1 to 1 mapped to 0-255)
					normals[localY * resolution + x] = new Color32(
						(byte)((normal.x + 1f) * 127.5f),
						(byte)((normal.y + 1f) * 127.5f),
						(byte)((normal.z + 1f) * 127.5f),
						255
					);
				}
			}

			// Yield to allow frame updates
			yield return null;
		}

		RuntimeNormalMap.SetPixels32(normals);
		RuntimeNormalMap.Apply();

		Shader.SetGlobalTexture("Terrain_Normal", RuntimeNormalMap);
	}

        /// <summary>Sets and initialises the Splat/AlphaMaps of all layers from MapInfo. Called when first loading/creating a map.</summary>
        private static IEnumerator SetSplatMaps(MapInfo mapInfo, int progressID)
        {
            SplatMapRes = mapInfo.splatRes;
            SetSplatMap(mapInfo.splatMap, LayerType.Ground);
            SetSplatMap(mapInfo.biomeMap, LayerType.Biome);
            SetAlphaMap(mapInfo.alphaMap);
            yield return null;
			
			#if UNITY_EDITOR
            Progress.Report(progressID, .8f, "Loaded: Splats.");
			#endif

            TopologyData.Set(mapInfo.topology);
            Parallel.For(0, TerrainTopology.COUNT, i =>
            {
                if (CurrentLayerType != LayerType.Topology || TopologyLayer != i)
                    SetSplatMap(TopologyData.GetTopologyLayer(TerrainTopology.IndexToType(i)), LayerType.Topology, i);
            });
            SetSplatMap(TopologyData.GetTopologyLayer(TerrainTopology.IndexToType(TopologyLayer)), LayerType.Topology, TopologyLayer);
            
			#if UNITY_EDITOR
			Progress.Report(progressID, .9f, "Loaded: Topologies.");
			#endif
        }
    }
}