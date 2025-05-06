using System;
using UnityEngine;

using UnityEditor;

using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using RustMapEditor.Variables;
using static RustMapEditor.Maths.Array;
using static AreaManager;
using static TerrainManager;
using static WorldSerialization;
using static ModManager;


public static class WorldConverter
{
	public static int BiomeChannels;
	
    public struct MapInfo
    {
        public int terrainRes;
        public int splatRes;
        public Vector3 size;
        public float[,,] splatMap;
        public float[,,] biomeMap;
        public bool[,] alphaMap;
        public TerrainInfo land;
        public TerrainInfo water;
        public TerrainMap<int> topology;
        public PrefabData[] prefabData;
        public PathData[] pathData;
		
		public CircuitDataHolder circuitDataHolder;
		public CircuitData[] circuitData;
		public NPCData[] npcData;
		public ModifierData modifierData;
    }

    public struct TerrainInfo
    {
        public float[,] heights;
    }
    
    public static MapInfo EmptyMap(int size, float landHeight, TerrainSplat.Enum ground = TerrainSplat.Enum.Grass, TerrainBiome.Enum biome = TerrainBiome.Enum.Temperate)
    {		
        MapInfo terrains = new MapInfo();

        int splatRes = Mathf.Clamp(Mathf.NextPowerOfTwo((int)(size * 0.50f)), 16, 2048);

        List<PathData> paths = new List<PathData>();
        List<PrefabData> prefabs = new List<PrefabData>();
		List<CircuitData> circuits = new List<CircuitData>();

        terrains.pathData = paths.ToArray();
        terrains.prefabData = prefabs.ToArray();
		terrains.circuitData = circuits.ToArray();

        terrains.terrainRes = Mathf.NextPowerOfTwo((int)(size * 0.50f)) + 1;
        terrains.size = new Vector3(size, 1000, size);

        terrains.land.heights = SetValues(new float[terrains.terrainRes, terrains.terrainRes], landHeight / 1000f, new Area(0, terrains.terrainRes, 0, terrains.terrainRes));
        terrains.water.heights = SetValues(new float[terrains.terrainRes, terrains.terrainRes], 500f / 1000f, new Area(0, terrains.terrainRes, 0, terrains.terrainRes));

        terrains.splatRes = splatRes;
        terrains.splatMap = new float[splatRes, splatRes, 8];
        int gndIdx = TerrainSplat.TypeToIndex((int)ground);
        Parallel.For(0, splatRes, i =>
        {
            for (int j = 0; j < splatRes; j++)
                terrains.splatMap[i, j, gndIdx] = 1f;
        });

        terrains.biomeMap = new float[splatRes, splatRes, 5];
        int biomeIdx = TerrainBiome.TypeToIndex((int)biome);
        Parallel.For(0, splatRes, i =>
        {
            for (int j = 0; j < splatRes; j++)
                terrains.biomeMap[i, j, biomeIdx] = 1f;
        });

        terrains.alphaMap = new bool[splatRes, splatRes];
        Parallel.For(0, splatRes, i =>
        {
            for (int j = 0; j < splatRes; j++)
                terrains.alphaMap[i, j] = true;
        });
        terrains.topology = new TerrainMap<int>(new byte[(int)Mathf.Pow(splatRes, 2) * 4 * 1], 1);
		
		TerrainManager.InitializeTextures();
        return terrains;
    }


    /// <summary>Converts the MapInfo and TerrainMaps into a Unity map format.</summary>
    public static MapInfo ConvertMaps(MapInfo terrains, TerrainMap<byte> splatMap, TerrainMap<byte> biomeMap, TerrainMap<byte> alphaMap)
    {		
        terrains.splatMap = new float[splatMap.res, splatMap.res, 8];
        terrains.biomeMap = new float[biomeMap.res, biomeMap.res, 5];
        terrains.alphaMap = new bool[alphaMap.res, alphaMap.res];


        var groundTask = Task.Run(() =>
        {
            Parallel.For(0, terrains.splatRes, i =>
            {
                for (int j = 0; j < terrains.splatRes; j++)
                    for (int k = 0; k < 8; k++)
                        terrains.splatMap[i, j, k] = BitUtility.Byte2Float(splatMap[k, i, j]);
            });
        });


    var biomeTask = Task.Run(() =>
    {
        float[] biomeSums = new float[5]; // Track sum of each biome layer
        Parallel.For(0, terrains.splatRes, i =>
        {
            for (int j = 0; j < terrains.splatRes; j++)
            {
                float sum = 0f;
                for (int k = 0; k < BiomeChannels && k < 5; k++)
                {
                    float value = BitUtility.Byte2Float(biomeMap[k, i, j]);
                    terrains.biomeMap[i, j, k] = value;
                    biomeSums[k] += value;
                    sum += value;
                }
                // Fill remaining channels with 0 if BiomeChannels < 5
                for (int k = BiomeChannels; k < 5; k++)
                {
                    terrains.biomeMap[i, j, k] = 0f;
                }
                // Normalize weights to sum to 1
                if (sum > 0)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        terrains.biomeMap[i, j, k] /= sum;
                    }
                }
            }
        });
        Debug.Log($"ConvertMaps: Biome sums: Arid={biomeSums[0]}, Temperate={biomeSums[1]}, Tundra={biomeSums[2]}, Arctic={biomeSums[3]}, Jungle={biomeSums[4]}");
    });

        var alphaTask = Task.Run(() =>
        {
            Parallel.For(0, terrains.splatRes, i =>
            {
                for (int j = 0; j < terrains.splatRes; j++)
                {
                    if (alphaMap[0, i, j] > 0)
                        terrains.alphaMap[i, j] = true;
                    else
                        terrains.alphaMap[i, j] = false;
                }
            });
        });
        Task.WaitAll(groundTask, biomeTask, alphaTask);

        return terrains;
    }

    /// <summary>Parses World Serialization and converts into MapInfo struct.</summary>
    /// <param name="world">Serialization of the map file to parse.</param>
    public static MapInfo WorldToTerrain(WorldSerialization world)
    {		
		
        MapInfo terrains = new MapInfo();

		
        var terrainSize = new Vector3(world.world.size, 1000, world.world.size);
		
		
        var terrainMap = new TerrainMap<short>(world.GetMap("terrain").data, 1);
        var heightMap = new TerrainMap<short>(world.GetMap("height").data, 1);
        var waterMap = new TerrainMap<short>(world.GetMap("water").data, 1);
        var splatMap = new TerrainMap<byte>(world.GetMap("splat").data, 8);
        var topologyMap = new TerrainMap<int>(world.GetMap("topology").data, 1);
		
		int resolution = splatMap.res; 
		Debug.LogError(resolution);
		BiomeChannels = world.GetMap("biome").data.Length / (resolution * resolution); // Calculate channels (4 or 5)
		Debug.LogError(BiomeChannels + " biomes found");
		var biomeMap = new TerrainMap<byte>(world.GetMap("biome").data, BiomeChannels);
		
		    var biomeMapData = world.GetMap("biome")?.data;
			if (biomeMapData == null)
			{
				Debug.LogError("Biome map data is null! Creating default 5-channel map.");
				BiomeChannels = 5;
				biomeMapData = new byte[resolution * resolution * 5];
			}
			else
			{
				BiomeChannels = biomeMapData.Length / (resolution * resolution);
				Debug.Log($"WorldToTerrain: Biome map: Data length={biomeMapData.Length}, Calculated BiomeChannels={BiomeChannels}, Expected=5");
				if (BiomeChannels != 5)
				{
					Debug.LogWarning($"Unexpected BiomeChannels={BiomeChannels}. Expected 5. Jungle layer may be missing.");
				}

				// Sum raw biome data
				long[] channelSums = new long[Math.Min(BiomeChannels, 5)];
				for (int i = 0; i < resolution; i++)
				{
					for (int j = 0; j < resolution; j++)
					{
						for (int k = 0; k < Math.Min(BiomeChannels, 5); k++)
						{
							int index = (k * resolution + i) * resolution + j;
							if (index < biomeMapData.Length)
							{
								channelSums[k] += biomeMapData[index];
							}
						}
					}
				}
				Debug.Log($"WorldToTerrain: Raw biome byte sums: Arid={channelSums[0]}, Temperate={channelSums[1]}, Tundra={channelSums[2]}, Arctic={channelSums[3]}" + (BiomeChannels > 4 ? $", Jungle={channelSums[4]}" : ""));
			}

		
		var alphaMap = new TerrainMap<byte>(world.GetMap("alpha").data, 1);
		
        terrains.topology = topologyMap;

        terrains.pathData = world.world.paths.ToArray();
        terrains.prefabData = world.world.prefabs.ToArray();
        terrains.terrainRes = heightMap.res;
        terrains.splatRes = splatMap.res;
        terrains.size = terrainSize;
		
		
		
			ModManager.ClearModdingData();
			foreach (var name in ModManager.GetKnownDataNames())
			{
				if (name == "buildingblocks")
				{
					WorldSerialization.MapData buildData = world.GetMap(name);
					if (buildData != null)
					{
						ModManager.AddOrUpdateModdingData(name, buildData.data);
					}
					continue;
				}
				
				string hashedName = ModManager.MapDataName(world.world.prefabs.Count, name);
				WorldSerialization.MapData mapData = world.GetMap(hashedName);
				
				if (mapData != null)
				{
					mapData.name = name;
					ModManager.AddOrUpdateModdingData(name, mapData.data); 
				}
			}

			foreach (var data in ModManager.moddingData)
			{
				string topoName = data.name;
				if (topoName.Contains("custom_topology_"))
				{
					WorldSerialization.MapData topoData = world.GetMap(topoName);
					if (topoData != null)
					{
						ModManager.AddOrUpdateModdingData(topoName, topoData.data);
					}
				}
			}
		

        var heightTask = Task.Run(() => ShortMapToFloatArray(heightMap));
        var waterTask = Task.Run(() => ShortMapToFloatArray(waterMap));

        terrains = ConvertMaps(terrains, splatMap, biomeMap, alphaMap);

        Task.WaitAll(heightTask, waterTask);
        terrains.land.heights = heightTask.Result;
        terrains.water.heights = waterTask.Result;

			//terrains.land.heights = ShortMapToFloatArray(heightMap);
			//terrains.water.heights = ShortMapToFloatArray(waterMap);
		    terrains = ConvertMaps(terrains, splatMap, biomeMap, alphaMap);

        return terrains;
    }

	public static WorldSerialization CollectionToREPrefab(Transform parent)
	{
		WorldSerialization world = new WorldSerialization();

		try
		{
			if (parent == null)
			{
				Debug.LogError("Parent Transform is null; no prefabs can be processed.");
				return world;
			}

			// Collect all PrefabDataHolder components in the hierarchy (flattening nesting)
			List<PrefabDataHolder> prefabHolders = new List<PrefabDataHolder>();
			CollectPrefabDataHolders(parent, prefabHolders);

			// Process each PrefabDataHolder and convert to local space relative to the parent
			foreach (PrefabDataHolder holder in prefabHolders)
			{
				if (holder.prefabData != null)
				{
					// Create a copy of the prefab data
					PrefabData localPrefab = holder.prefabData;

					// Get the world position and rotation from the holder's transform
					Vector3 worldPosition = holder.transform.position;
					Quaternion worldRotation = holder.transform.rotation;

					// Convert to local space relative to the parent
					localPrefab.position = parent.InverseTransformPoint(worldPosition);
					localPrefab.rotation = Quaternion.Inverse(parent.rotation) * worldRotation;

					// Add the adjusted prefab to the REPrefab data
					world.rePrefab.prefabs.Add(localPrefab);
				}
			}

			// Initialize empty collections for other REPrefab components
			world.rePrefab.electric.circuitData = new List<CircuitData>();
			world.rePrefab.npcs.bots = new List<NPCData>();
			world.rePrefab.modifiers = new ModifierData();

			return world;
		}
		catch (NullReferenceException err)
		{
			Debug.LogError("Error during prefab conversion: " + err.Message);
			return world;
		}
	}

	/// <summary>Recursively collects all PrefabDataHolder components from a Transform and its children.</summary>
	/// <param name="current">Current Transform to inspect.</param>
	/// <param name="holders">List to store collected PrefabDataHolder components.</param>
	private static void CollectPrefabDataHolders(Transform current, List<PrefabDataHolder> holders)
	{
		// Check if the current object has a PrefabDataHolder
		PrefabDataHolder holder = current.GetComponent<PrefabDataHolder>();
		if (holder != null)
		{
			holders.Add(holder);
		}

		// Recursively process all children
		foreach (Transform child in current)
		{
			CollectPrefabDataHolders(child, holders);
		}
	}



    /// <summary>Converts Unity terrains to WorldSerialization.</summary>
    public static WorldSerialization TerrainToWorld(Terrain land, Terrain water, (int prefab, int path, int terrain) ID = default) 
    {
        WorldSerialization world = new WorldSerialization();
        world.world.size = (uint) land.terrainData.size.x;

        var textureResolution = SplatMapRes;

        byte[] splatBytes = new byte[textureResolution * textureResolution * 8];
        var splatMap = new TerrainMap<byte>(splatBytes, 8);
        var splatTask = Task.Run(() =>
        {
            Parallel.For(0, 8, i =>
            {
                for (int j = 0; j < textureResolution; j++)
                    for (int k = 0; k < textureResolution; k++)
                        splatMap[i, j, k] = BitUtility.Float2Byte(Ground[j, k, i]);
            });
            splatBytes = splatMap.ToByteArray();
        });

        byte[] biomeBytes = new byte[textureResolution * textureResolution * 5];
        var biomeMap = new TerrainMap<byte>(biomeBytes, 5);
        var biomeTask = Task.Run(() =>
        {
            Parallel.For(0, 5, i =>
            {
                for (int j = 0; j < textureResolution; j++)
                    for (int k = 0; k < textureResolution; k++)
                        biomeMap[i, j, k] = BitUtility.Float2Byte(Biome[j, k, i]);
            });
            biomeBytes = biomeMap.ToByteArray();
        });

        byte[] alphaBytes = new byte[textureResolution * textureResolution * 1];
        var alphaMap = new TerrainMap<byte>(alphaBytes, 1);
        bool[,] terrainHoles = GetAlphaMap();
        var alphaTask = Task.Run(() =>
        {
            Parallel.For(0, textureResolution, i =>
            {
                for (int j = 0; j < textureResolution; j++)
                    alphaMap[0, i, j] = BitUtility.Bool2Byte(terrainHoles[i, j]);
            });
            alphaBytes = alphaMap.ToByteArray();
        });

        var topologyTask = Task.Run(() => TopologyData.SaveTopologyLayers());

        foreach (PrefabDataHolder p in PrefabManager.CurrentMapPrefabs)
        {
            if (p.prefabData != null)
            {
                p.UpdatePrefabData(); // Updates the prefabdata before saving.
				p.AlwaysBreakPrefabs();
                world.world.prefabs.Insert(0, p.prefabData);
            }
        }
		
		#if UNITY_EDITOR
        Progress.Report(ID.prefab, 0.99f, "Saved " + PrefabManager.CurrentMapPrefabs.Length + " prefabs.");
		#endif
		
        foreach (PathDataHolder p in PathManager.CurrentMapPaths)
        {
            if (p.pathData != null)
            {
                p.pathData.nodes = new VectorData[p.transform.childCount];
                for (int i = 0; i < p.transform.childCount; i++)
                {
                    Transform g = p.transform.GetChild(i);
                    p.pathData.nodes[i] = g.position - MapOffset;
                }
                world.world.paths.Insert(0, p.pathData);
            }
        }
		
		#if UNITY_EDITOR
        Progress.Report(ID.path, 0.99f, "Saved " + PathManager.CurrentMapPaths.Length + " paths.");
		#endif

        byte[] landHeightBytes = FloatArrayToByteArray(land.terrainData.GetHeights(0, 0, HeightMapRes, HeightMapRes));
        byte[] waterHeightBytes = FloatArrayToByteArray(water.terrainData.GetHeights(0, 0, HeightMapRes, HeightMapRes));

        Task.WaitAll(splatTask, biomeTask, alphaTask, topologyTask);
		
		#if UNITY_EDITOR
        Progress.Report(ID.terrain, 0.99f, "Saved " + TerrainSize.x + " size map.");
		#endif
		
		// Add modding data from ModManager
		var moddingData = ModManager.GetModdingData();
		if (moddingData != null && moddingData.Count > 0)
		{
			foreach (var md in moddingData)
			{
				if (md.name == "buildingblocks" || md.name.Contains("custom_topology_"))
				{
					world.AddMap(md.name, md.data);
				}
				else
				{
					// Normal case: Hash the name
					string hashedName = ModManager.MapDataName(world.world.prefabs.Count, md.name);
					world.AddMap(hashedName, md.data);
				}
			}
		}
		else
		{
		}

        world.AddMap("terrain", landHeightBytes);
        world.AddMap("height", landHeightBytes);
        world.AddMap("water", waterHeightBytes);
        world.AddMap("splat", splatBytes);
        world.AddMap("biome", biomeBytes);
        world.AddMap("alpha", alphaBytes);
        world.AddMap("topology", TopologyData.GetTerrainMap().ToByteArray());
        return world;
    }
	
	/// <summary>Gathers terrain data from the Unity Editor and converts to RMPrefabData with RMMonument terrain data.</summary>
	public static RMPrefabData TerrainToRMPrefab(Terrain land, Terrain water)
	{			
		// Initialize RMPrefabData			
		RMPrefabData rmPrefab = new RMPrefabData();
		try
		{


			// Set modifiers if available
			if (PrefabManager.CurrentModifiers?.modifierData != null)
				rmPrefab.modifiers = PrefabManager.CurrentModifiers.modifierData;

			// Process NPCs
			foreach (NPCDataHolder p in PrefabManager.CurrentMapNPCs)
			{
				if (p.bots != null)
				{
					rmPrefab.npcs.bots.Add(p.bots);
				}
			}

			// Process Prefabs
			foreach (PrefabDataHolder p in PrefabManager.CurrentMapPrefabs)
			{
				if (p.prefabData != null)
				{
					p.AlwaysBreakPrefabs(); // Updates prefab data before saving
					rmPrefab.prefabs.Add(p.prefabData);
				}
			}

			// Process Circuits
			foreach (CircuitDataHolder p in PrefabManager.CurrentMapElectrics)
			{
				if (p.circuitData != null)
				{
					p.UpdateCircuitData(); // Updates circuit data before saving
					rmPrefab.electric.circuitData.Add(p.circuitData);
				}
			}

			// Initialize RMMonument
			RMMonument monument = new RMMonument
			{
				size = new Vector3(land.terrainData.size.x, land.terrainData.size.y, land.terrainData.size.z),
				extents = land.terrainData.size / 2f, // Assuming extents are half the size
				offset = land.transform.position - MapOffset, // Adjust for map offset
				HeightMap = true,
				AlphaMap = true,
				WaterMap = true,
				SplatMask = TerrainSplat.Enum.Grass, // Default splat mask
				BiomeMask = TerrainBiome.Enum.Temperate, // Default biome mask
				TopologyMask = TerrainTopology.Enum.Field // Default topology mask
			};

			// Texture resolution for maps
			var textureResolution = SplatMapRes;
			var heightMapRes = HeightMapRes;

			// Synchronize textures to ensure data is up-to-date
			TerrainManager.SyncSplatTexture();
			TerrainManager.SyncBiomeTexture();
			TerrainManager.SyncAlphaTexture();

			// Process Splat Map (Control0 and Control1 textures)
			Texture2D[] alphamaps = land.terrainData.alphamapTextures;
			if (alphamaps == null || alphamaps.Length < 2)
			{
				Debug.LogError("Terrain alphamap textures (Control0 and Control1) are not available.");
			}
			var splatTask = Task.Run(() =>
			{
				monument.splatmap0 = WorldSerialization.SerializeTexture(alphamaps[0]); // First 4 splat channels
				monument.splatmap1 = WorldSerialization.SerializeTexture(alphamaps[1]); // Next 4 splat channels
			});

			// Process Biome Map (BiomeTexture and Biome1Texture)
			var biomeTask = Task.Run(() =>
			{
				if (TerrainManager.BiomeTexture == null || TerrainManager.Biome1Texture == null)
				{
					Debug.LogError("BiomeTexture or Biome1Texture is not initialized.");
				}
				monument.biomemap = WorldSerialization.SerializeTexture(TerrainManager.BiomeTexture); // Arid, Temperate, Tundra, Arctic

			});

			// Process Alpha Map
			var alphaTask = Task.Run(() =>
			{
				if (TerrainManager.AlphaTexture == null)
				{
					Debug.LogError("AlphaTexture is not initialized.");
				}

				monument.alphamap = WorldSerialization.SerializeTexture(AlphaTexture);
			});

			// Process Blend Map
			var blendTask = Task.Run(() =>
			{
				if (TerrainManager.BlendMapTexture == null)
				{
					Debug.LogWarning("BlendMapTexture is not initialized.");
				}
				
				monument.blendmap = WorldSerialization.SerializeTexture(TerrainManager.BlendMapTexture);
			});

			// Process Topology Map
			var topologyTask = Task.Run(() => TopologyData.SaveTopologyLayers());

			// Process Height and Water Maps (unchanged)
			byte[] heightBytes = FloatArrayToByteArray(land.terrainData.GetHeights(0, 0, heightMapRes, heightMapRes));
			byte[] waterBytes = FloatArrayToByteArray(water.terrainData.GetHeights(0, 0, heightMapRes, heightMapRes));

			// Wait for all tasks to complete
			Task.WaitAll(splatTask, biomeTask, alphaTask, blendTask, topologyTask);

			// Assign height and topology data to RMMonument
			monument.heightmap = heightBytes;
			monument.watermap = waterBytes;
			monument.topologymap = TopologyData.GetTerrainMap().ToByteArray();

			// Assign monument to RMPrefabData
			rmPrefab.monument = monument;

			// To serialize RMPrefabData (including monument), use SavePrefab<RMPrefabData>
			return rmPrefab;
		}
		catch (NullReferenceException err)
		{
			Debug.LogError("Error during RMPrefab conversion: " + err.Message);
			return rmPrefab;
		}
		return rmPrefab;
	}
	
	/// <summary>Attaches a Monument component to a GameObject, populating it with RMMonument data.</summary>
	/// <param name="rmPrefab">The RMPrefabData containing RMMonument data.</param>
	/// <param name="go">The GameObject to attach the Monument component to.</param>
	public static void AttachMonument(RMPrefabData rmPrefab, GameObject go)
	{
		if (rmPrefab == null)
		{
			Debug.LogError("RMPrefabData is null. Cannot attach Monument component.");
			return;
		}
		if (go == null)
		{
			Debug.LogError("GameObject is null. Cannot attach Monument component.");
			return;
		}

		try
		{
			// Add or get the Monument component
			Monument monumentComponent = go.GetComponent<Monument>();
			if (monumentComponent == null)
				monumentComponent = go.AddComponent<Monument>();

			// Populate Monument component with RMMonument data
			if (rmPrefab.monument != null)
			{
				RMMonument rmMonument = rmPrefab.monument;

				// Set basic Monument properties
				monumentComponent.size = rmMonument.size;
				monumentComponent.extents = rmMonument.extents;
				monumentComponent.offset = rmMonument.offset;
				monumentComponent.HeightMap = rmMonument.HeightMap;
				monumentComponent.AlphaMap = rmMonument.AlphaMap;
				monumentComponent.WaterMap = rmMonument.WaterMap;
				monumentComponent.SplatMask = rmMonument.SplatMask;
				monumentComponent.BiomeMask = rmMonument.BiomeMask;
				monumentComponent.TopologyMask = rmMonument.TopologyMask;
				monumentComponent.Radius = rmMonument.size.x / 2f; // Default to half the size.x
				monumentComponent.Fade = Mathf.Min(rmMonument.size.x, rmMonument.size.z) * 0.1f; // Default to 10% of min dimension

				// Deserialize and assign textures
				var textureTasks = new List<Task>();

				// Heightmap
				if (rmMonument.heightmap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D heightTexture = WorldSerialization.DeserializeTexture(
							rmMonument.heightmap,
							TerrainManager.HeightMapRes,
							TerrainManager.HeightMapRes,
							TextureFormat.RGBA32
						);
						if (heightTexture != null)
						{
							monumentComponent.heightmap = new Texture2DRef { cachedInstance = heightTexture };
						}
					}));
				}

				// Splatmap0
				if (rmMonument.splatmap0 != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D splat0Texture = WorldSerialization.DeserializeTexture(
							rmMonument.splatmap0,
							TerrainManager.SplatMapRes,
							TerrainManager.SplatMapRes,
							TextureFormat.RGBA32
						);
						if (splat0Texture != null)
						{
							monumentComponent.splatmap0 = new Texture2DRef { cachedInstance = splat0Texture };
						}
					}));
				}

				// Splatmap1
				if (rmMonument.splatmap1 != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D splat1Texture = WorldSerialization.DeserializeTexture(
							rmMonument.splatmap1,
							TerrainManager.SplatMapRes,
							TerrainManager.SplatMapRes,
							TextureFormat.RGBA32
						);
						if (splat1Texture != null)
						{
							monumentComponent.splatmap1 = new Texture2DRef { cachedInstance = splat1Texture };
						}
					}));
				}

				// Alphamap
				if (rmMonument.alphamap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						RenderTexture alphaTexture = WorldSerialization.DeserializeTexture(
							rmMonument.alphamap,
							TerrainManager.AlphaMapRes,
							TerrainManager.AlphaMapRes,
							RenderTextureFormat.ARGB32
						);
						if (alphaTexture != null)
						{
							// Convert RenderTexture to Texture2D for Texture2DRef
							Texture2D alphaTexture2D = new Texture2D(alphaTexture.width, alphaTexture.height, TextureFormat.RGBA32, false);
							RenderTexture.active = alphaTexture;
							alphaTexture2D.ReadPixels(new Rect(0, 0, alphaTexture.width, alphaTexture.height), 0, 0);
							alphaTexture2D.Apply();
							monumentComponent.alphamap = new Texture2DRef { cachedInstance = alphaTexture2D };
							UnityEngine.Object.Destroy(alphaTexture);
						}
					}));
				}

				// Biomemap
				if (rmMonument.biomemap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D biomeTexture = WorldSerialization.DeserializeTexture(
							rmMonument.biomemap,
							TerrainManager.SplatMapRes,
							TerrainManager.SplatMapRes,
							TextureFormat.RGBA32
						);
						if (biomeTexture != null)
						{
							monumentComponent.biomemap = new Texture2DRef { cachedInstance = biomeTexture };
						}
					}));
				}

				// Topologymap
				if (rmMonument.topologymap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D topologyTexture = WorldSerialization.DeserializeTexture(
							rmMonument.topologymap,
							TerrainManager.SplatMapRes,
							TerrainManager.SplatMapRes,
							TextureFormat.RGBA32
						);
						if (topologyTexture != null)
						{
							monumentComponent.topologymap = new Texture2DRef { cachedInstance = topologyTexture };
						}
					}));
				}

				// Watermap
				if (rmMonument.watermap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D waterTexture = WorldSerialization.DeserializeTexture(
							rmMonument.watermap,
							TerrainManager.HeightMapRes,
							TerrainManager.HeightMapRes,
							TextureFormat.RGBA32
						);
						if (waterTexture != null)
						{
							monumentComponent.watermap = new Texture2DRef { cachedInstance = waterTexture };
						}
					}));
				}

				// Blendmap
				if (rmMonument.blendmap != null)
				{
					textureTasks.Add(Task.Run(() =>
					{
						Texture2D blendTexture = WorldSerialization.DeserializeTexture(
							rmMonument.blendmap,
							TerrainManager.SplatMapRes,
							TerrainManager.SplatMapRes,
							TextureFormat.RGBA32
						);
						if (blendTexture != null)
						{
							monumentComponent.blendmap = new Texture2DRef { cachedInstance = blendTexture };
						}
					}));
				}

				// Wait for all texture deserialization tasks to complete
				Task.WaitAll(textureTasks.ToArray());
			}
			else
			{
				Debug.LogWarning("RMMonument data is null. Monument component will have default values.");
			}

			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(go);
			#endif
		}
		catch (Exception err)
		{
			Debug.LogError($"Error during AttachMonument: {err.Message}");
		}
	}
	
	
	public static MapInfo WorldToREPrefab(WorldSerialization world)
	{
		MapInfo refab = new MapInfo();
		refab.prefabData = world.rePrefab.prefabs.ToArray();
		refab.circuitData = world.rePrefab.electric.circuitData.ToArray();
		refab.npcData = world.rePrefab.npcs.bots.ToArray();
		refab.modifierData = world.rePrefab.modifiers;
		
		for (int k = 0; k < refab.circuitData.Length; k++)
		{
			refab.circuitData[k].connectionsIn = refab.circuitData[k].branchIn.ToArray();
			refab.circuitData[k].connectionsOut = refab.circuitData[k].branchOut.ToArray();
		}
		return refab;
	}
	
	public static MapInfo WorldToRMPrefab(WorldSerialization world)
	{
		MapInfo refab = new MapInfo();
		refab.prefabData = world.rmPrefab.prefabs.ToArray();
		refab.circuitData = world.rmPrefab.electric.circuitData.ToArray();
		refab.npcData = world.rmPrefab.npcs.bots.ToArray();
		refab.modifierData = world.rmPrefab.modifiers;
		
		for (int k = 0; k < refab.circuitData.Length; k++)
		{
			refab.circuitData[k].connectionsIn = refab.circuitData[k].branchIn.ToArray();
			refab.circuitData[k].connectionsOut = refab.circuitData[k].branchOut.ToArray();
		}
		return refab;
	}
	


	
	public static WorldSerialization TerrainToCustomPrefab((int prefab, int circuit) ID) 
    {
		WorldSerialization world = new WorldSerialization();

			try
			{
				
			if (PrefabManager.CurrentModifiers?.modifierData!= null)
				world.rePrefab.modifiers = PrefabManager.CurrentModifiers.modifierData;
			
			
			foreach(NPCDataHolder p in PrefabManager.CurrentMapNPCs)
			{
				if (p.bots != null)
				{
					world.rePrefab.npcs.bots.Insert(0, p.bots);
				}
			}
			
			
			
			foreach (PrefabDataHolder p in PrefabManager.CurrentMapPrefabs)
			{
				if (p.prefabData != null)
				{
					//p.UpdatePrefabData();
					p.AlwaysBreakPrefabs(); // Updates the prefabdata before saving.
					world.rePrefab.prefabs.Add(p.prefabData);
				}
			}
			foreach (CircuitDataHolder p in PrefabManager.CurrentMapElectrics)
			{
				if (p.circuitData != null)
				{
					p.UpdateCircuitData(); // Updates the circuitdata before saving.
					world.rePrefab.electric.circuitData.Insert(0, p.circuitData);
				}
			}
			#if UNITY_EDITOR
			Progress.Report(ID.prefab, 0.99f, "Saved " + PrefabManager.CurrentMapPrefabs.Length + " prefabs.");
			Progress.Report(ID.circuit, 0.99f, "Saved " + PrefabManager.CurrentMapPrefabs.Length + " circuits.");
			#endif

			return world;
			}
			catch(NullReferenceException err)
			{
					Debug.LogError(err.Message);
					return world;
			}
    }
	
}