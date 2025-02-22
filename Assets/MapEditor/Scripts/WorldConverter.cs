using System;
using UnityEngine;

using UnityEditor;


using System.Threading.Tasks;
using System.Collections.Generic;
using RustMapEditor.Variables;
using static RustMapEditor.Maths.Array;
using static AreaManager;
using static TerrainManager;
using static WorldSerialization;

public static class WorldConverter
{
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

        terrains.biomeMap = new float[splatRes, splatRes, 4];
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
        return terrains;
    }

    /// <summary>Converts the MapInfo and TerrainMaps into a Unity map format.</summary>
    public static MapInfo ConvertMaps(MapInfo terrains, TerrainMap<byte> splatMap, TerrainMap<byte> biomeMap, TerrainMap<byte> alphaMap)
    {
        terrains.splatMap = new float[splatMap.res, splatMap.res, 8];
        terrains.biomeMap = new float[biomeMap.res, biomeMap.res, 4];
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
            Parallel.For(0, terrains.splatRes, i =>
            {
                for (int j = 0; j < terrains.splatRes; j++)
                    for (int k = 0; k < 4; k++)
                        terrains.biomeMap[i, j, k] = BitUtility.Byte2Float(biomeMap[k, i, j]);
            });
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
        var biomeMap = new TerrainMap<byte>(world.GetMap("biome").data, 4);
        var alphaMap = new TerrainMap<byte>(world.GetMap("alpha").data, 1);
		

		
        terrains.topology = topologyMap;

        terrains.pathData = world.world.paths.ToArray();
        terrains.prefabData = world.world.prefabs.ToArray();

        terrains.terrainRes = heightMap.res;
        terrains.splatRes = splatMap.res;
        terrains.size = terrainSize;
		
		
        var heightTask = Task.Run(() => ShortMapToFloatArray(heightMap));
        var waterTask = Task.Run(() => ShortMapToFloatArray(waterMap));

        terrains = ConvertMaps(terrains, splatMap, biomeMap, alphaMap);

        Task.WaitAll(heightTask, waterTask);
        terrains.land.heights = heightTask.Result;
        terrains.water.heights = waterTask.Result;

			terrains.land.heights = ShortMapToFloatArray(heightMap);
			terrains.water.heights = ShortMapToFloatArray(waterMap);
		    terrains = ConvertMaps(terrains, splatMap, biomeMap, alphaMap);

        return terrains;
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

        byte[] biomeBytes = new byte[textureResolution * textureResolution * 4];
        var biomeMap = new TerrainMap<byte>(biomeBytes, 4);
        var biomeTask = Task.Run(() =>
        {
            Parallel.For(0, 4, i =>
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

        world.AddMap("terrain", landHeightBytes);
        world.AddMap("height", landHeightBytes);
        world.AddMap("water", waterHeightBytes);
        world.AddMap("splat", splatBytes);
        world.AddMap("biome", biomeBytes);
        world.AddMap("alpha", alphaBytes);
        world.AddMap("topology", TopologyData.GetTerrainMap().ToByteArray());
        return world;
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
					p.UpdatePrefabData();
					//p.AlwaysBreakPrefabs(); // Updates the prefabdata before saving.
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