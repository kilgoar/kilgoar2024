using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using RustMapEditor.Variables;
using static TerrainManager;
using static RustMapEditor.Maths.Array;
using static WorldConverter;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

using System.Collections;

public static class MapManager
{
	#if UNITY_EDITOR
	
    #region Init
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.update += OnProjectLoad;
    }
	
    private static void OnProjectLoad()
    {
        if (Land != null)
        {
            EditorApplication.update -= OnProjectLoad;
            if (!EditorApplication.isPlaying){
                //CreateMap(1000);
			}
        }
    }
	
    #endregion
	
	#endif

    public static class Callbacks
    {
        public delegate void MapManagerCallback(string mapName = "");

        /// <summary>Called after a map has been loaded. Calls on both map loaded and map created.</summary>
        public static event MapManagerCallback MapLoaded;
        /// <summary>Called after map has been saved and written to disk.</summary>
        public static event MapManagerCallback MapSaved;

        public static void OnMapLoaded(string mapName = "") => MapLoaded?.Invoke(mapName);
        public static void OnMapSaved(string mapName = "") => MapSaved?.Invoke(mapName);
    }
    
    public static List<int> GetEnumSelection<T>(T enumGroup)
    {
        var selectedEnums = new List<int>();
        for (int i = 0; i < Enum.GetValues(typeof(T)).Length; i++)
        {
            int layer = 1 << i;
            if ((Convert.ToInt32(enumGroup) & layer) != 0)
                selectedEnums.Add(i);
        }
        return selectedEnums;
    }

    public static void RotateMap(Selections.Objects objectSelection, bool CW)
    {
        foreach (var item in GetEnumSelection(objectSelection))
        {
            switch (item)
            {
                case 0:
                case 1:
                case 2:
                    RotateLayer((LayerType) item, CW);
                    break;
                case 3:
                    RotateTopologyLayers((TerrainTopology.Enum)TerrainTopology.EVERYTHING, CW);
                    break;
                case 4:
                    RotateHeightMap(CW);
                    break;
                case 5:
                    RotateHeightMap(CW, TerrainType.Water);
                    break;
                case 6:
                    PrefabManager.RotatePrefabs(CW);
                    break;
                case 7:
                    PathManager.RotatePaths(CW);
                    break;
            }
        }
    }

    #region SplatMap Methods
    /// <summary>Rotates the selected layer.</summary>
    /// <param name="landLayerToPaint">The LayerType to rotate. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="CW">True = 90°, False = 270°</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void RotateLayer(LayerType landLayerToPaint, bool CW, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:
                SetSplatMap(Rotate(GetSplatMap(landLayerToPaint, topology), CW), landLayerToPaint, topology);
                break;
            case LayerType.Alpha:
                SetAlphaMap(Rotate(GetAlphaMap(), CW));
                break;
        }
    }

    /// <summary>Rotates the selected topologies.</summary>
    /// <param name="topologyLayers">The Topology layers to rotate.</param>
    /// <param name="CW">True = 90°, False = 270°</param>
    public static void RotateTopologyLayers(TerrainTopology.Enum topologyLayers, bool CW)
    {
        List<int> topologyElements = GetEnumSelection(topologyLayers);

		#if UNITY_EDITOR
        int progressId = Progress.Start("Rotating Topologies", null, Progress.Options.Sticky);
		#endif 
		
        for (int i = 0; i < topologyElements.Count; i++)
        {
			#if UNITY_EDITOR
            Progress.Report(progressId, (float)i / topologyElements.Count, "Rotating: " + ((TerrainTopology.Enum)TerrainTopology.IndexToType(i)).ToString());
			#endif
            RotateLayer(LayerType.Topology, CW, i);
        }
		#if UNITY_EDITOR
        Progress.Finish(progressId);
		#endif
    }

    /// <summary>Paints if all the conditions passed in are true.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="texture">The texture to paint.</param>
    /// <param name="conditions">The conditions to check.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintConditional(LayerType landLayerToPaint, int texture, Conditions conditions, int topology = 0)
    {
        int splatRes = SplatMapRes;
        bool[,] conditionsMet = new bool[splatRes, splatRes]; // Paints wherever the conditionsmet is false.

		#if UNITY_EDITOR
        int progressId = Progress.Start("Conditional Paint");
		#endif
        for (int i = 0; i < TerrainSplat.COUNT; i++)
            if (conditions.GroundConditions.CheckLayer[i])
                conditionsMet = CheckConditions(GetSplatMap(LayerType.Ground), conditionsMet, i, conditions.GroundConditions.Weight[i]);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.2f, "Checking Biome");
		#endif
        for (int i = 0; i < TerrainBiome.COUNT; i++)
            if (conditions.BiomeConditions.CheckLayer[i])
                conditionsMet = CheckConditions(GetSplatMap(LayerType.Biome), conditionsMet, i, conditions.BiomeConditions.Weight[i]);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.3f, "Checking Alpha");
		#endif
		if (conditions.AlphaConditions.CheckAlpha)
            conditionsMet = CheckConditions(GetAlphaMap(), conditionsMet, (conditions.AlphaConditions.Texture == 0) ? true : false);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.5f, "Checking Topology");
		#endif
        for (int i = 0; i < TerrainTopology.COUNT; i++)
            if (conditions.TopologyConditions.CheckLayer[i])
                conditionsMet = CheckConditions(GetSplatMap(LayerType.Topology, i), conditionsMet, (int)conditions.TopologyConditions.Texture[i], 0.5f);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.7f, "Checking Heights");
		#endif
        if (conditions.TerrainConditions.CheckHeights)
            conditionsMet = CheckConditions(GetHeights(), conditionsMet, conditions.TerrainConditions.Heights.HeightLow, conditions.TerrainConditions.Heights.HeightHigh);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.8f, "Checking Slopes");
		#endif
        if (conditions.TerrainConditions.CheckSlopes)
            conditionsMet = CheckConditions(GetSlopes(), conditionsMet, conditions.TerrainConditions.Slopes.SlopeLow, conditions.TerrainConditions.Slopes.SlopeHigh);

		#if UNITY_EDITOR
        Progress.Report(progressId, 0.8f, "Painting");
		#endif
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:
                float[,,] splatMapToPaint = GetSplatMap(landLayerToPaint, topology);
                int textureCount = LayerCount(landLayerToPaint);
                Parallel.For(0, splatRes, i =>
                {
                    for (int j = 0; j < splatRes; j++)
                        if (conditionsMet[i, j] == false)
                        {
                            for (int k = 0; k < textureCount; k++)
                                splatMapToPaint[i, j, k] = 0f;
                            splatMapToPaint[i, j, texture] = 1f;
                        }
                });
                SetSplatMap(splatMapToPaint, landLayerToPaint, topology);
                break;
            case LayerType.Alpha:
                bool[,] alphaMapToPaint = GetAlphaMap();
                Parallel.For(0, splatRes, i =>
                {
                    for (int j = 0; j < splatRes; j++)
                        alphaMapToPaint[i, j] = (conditionsMet[i, j] == false) ? conditionsMet[i, j] : alphaMapToPaint[i, j];
                });
                SetAlphaMap(alphaMapToPaint);
                break;
        }
		#if UNITY_EDITOR
        Progress.Finish(progressId);
		#endif
    }

    /// <summary>Paints the layer wherever the height conditions are met.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="heightLow">The minimum height to paint at 100% weight.</param>
    /// <param name="heightHigh">The maximum height to paint at 100% weight.</param>
    /// <param name="t">The texture to paint.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintHeight(LayerType landLayerToPaint, float heightLow, float heightHigh, int t, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:

                SetSplatMap(SetRange(GetSplatMap(landLayerToPaint, topology), HeightToSplat(GetHeights()), t, heightLow, heightHigh), landLayerToPaint, topology);

                break;
            case LayerType.Alpha:
                bool value = (t == 0) ? true : false;
                SetAlphaMap(SetRange(GetAlphaMap(), GetHeights(), value, heightLow, heightHigh));
                break;
        }
    }

    /// <summary>Paints the layer wherever the height conditions are met with a weighting determined by the range the height falls in.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="heightLow">The minimum height to paint at 100% weight.</param>
    /// <param name="heightHigh">The maximum height to paint at 100% weight.</param>
    /// <param name="minBlendLow">The minimum height to start to paint. The texture weight will increase as it gets closer to the heightlow.</param>
    /// <param name="maxBlendHigh">The maximum height to start to paint. The texture weight will increase as it gets closer to the heighthigh.</param>
    /// <param name="t">The texture to paint.</param>
    public static void PaintHeightBlend(LayerType landLayerToPaint, float heightLow, float heightHigh, float minBlendLow, float maxBlendHigh, int t)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:

                SetSplatMap(SetRangeBlend(GetSplatMap(landLayerToPaint), HeightToSplat(GetHeights()), t, heightLow, heightHigh, minBlendLow, maxBlendHigh), landLayerToPaint);

                break;
        }
    }

    /// <summary>Sets whole layer to the active texture.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="t">The texture to paint.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintLayer(LayerType landLayerToPaint, int t, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:
                SetSplatMap(SetValues(GetSplatMap(landLayerToPaint, topology), t), landLayerToPaint, topology);
                break;
            case LayerType.Alpha:
                SetAlphaMap(SetValues(GetAlphaMap(), true));
                break;
        }
    }

    /// <summary>Paints the selected Topology layers.</summary>
    /// <param name="topologyLayers">The Topology layers to clear.</param>
	public static void PaintTopologyLayers(TerrainTopology.Enum topologyLayers)
	{
		List<int> topologyElements = GetEnumSelection(topologyLayers);

		#if UNITY_EDITOR
		int progressId = Progress.Start("Paint Topologies");
		#endif

		for (int i = 0; i < topologyElements.Count; i++)
		{
			#if UNITY_EDITOR
			Progress.Report(progressId, (float)i / topologyElements.Count, "Painting: " + ((TerrainTopology.Enum)TerrainTopology.IndexToType(i)).ToString());
			#endif
			PaintLayer(LayerType.Topology, 0, i);
		}

		#if UNITY_EDITOR
		Progress.Finish(progressId);
		#endif
	}

    /// <summary>Sets whole layer to the inactive texture. Alpha and Topology only.</summary>
    /// <param name="landLayerToPaint">The LayerType to clear. (Alpha, Topology)</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void ClearLayer(LayerType landLayerToPaint, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Topology:
                SetSplatMap(SetValues(GetSplatMap(landLayerToPaint, topology), 1), landLayerToPaint, topology);
                break;
            case LayerType.Alpha:
                SetAlphaMap(SetValues(GetAlphaMap(), false));
                break;
        }
    }

    /// <summary>Clears the selected Topology layers.</summary>
    /// <param name="topologyLayers">The Topology layers to clear.</param>
	public static void ClearTopologyLayers(TerrainTopology.Enum topologyLayers)
	{
		List<int> topologyElements = GetEnumSelection(topologyLayers);

		#if UNITY_EDITOR
		int progressId = Progress.Start("Clear Topologies");
		#endif

		for (int i = 0; i < topologyElements.Count; i++)
		{
			#if UNITY_EDITOR
			Progress.Report(progressId, (float)i / topologyElements.Count, "Clearing: " + ((TerrainTopology.Enum)TerrainTopology.IndexToType(i)).ToString());
			#endif
			ClearLayer(LayerType.Topology, i);
		}

		#if UNITY_EDITOR
		Progress.Finish(progressId);
		#endif
	}

    /// <summary>Inverts the active and inactive textures. Alpha and Topology only.</summary>
    /// <param name="landLayerToPaint">The LayerType to invert. (Alpha, Topology)</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void InvertLayer(LayerType landLayerToPaint, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Topology:
                SetSplatMap(Invert(GetSplatMap(landLayerToPaint, topology)), landLayerToPaint, topology);
                break;
            case LayerType.Alpha:
                SetAlphaMap(Invert(GetAlphaMap()));
                break;
        }
    }

    /// <summary>Inverts the selected Topology layers.</summary>
    /// <param name="topologyLayers">The Topology layers to invert.</param>
    public static void InvertTopologyLayers(TerrainTopology.Enum topologyLayers)
    {
        List<int> topologyElements = GetEnumSelection(topologyLayers);
		#if UNITY_EDITOR
        int progressId = Progress.Start("Invert Topologies");
		#endif
			
        for (int i = 0; i < topologyElements.Count; i++)
        {
			#if UNITY_EDITOR
			Progress.Report(progressId, (float)i / topologyElements.Count, "Inverting: " + ((TerrainTopology.Enum)TerrainTopology.IndexToType(i)).ToString());
			#endif
            InvertLayer(LayerType.Topology, i);
        }
		#if UNITY_EDITOR
        Progress.Finish(progressId);
		#endif
    }

    /// <summary>Paints the layer wherever the slope conditions are met. Includes option to blend.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="slopeLow">The minimum slope to paint at 100% weight.</param>
    /// <param name="slopeHigh">The maximum slope to paint at 100% weight.</param>
    /// <param name="t">The texture to paint.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintSlope(LayerType landLayerToPaint, float slopeLow, float slopeHigh, int t, int topology = 0) // Paints slope based on the current slope input, the slope range is between 0 - 90
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:

                SetSplatMap(SetRange(GetSplatMap(landLayerToPaint, topology), HeightToSplat(GetSlopes()), t, slopeLow, slopeHigh), landLayerToPaint, topology);

                break;
            case LayerType.Alpha:
                bool value = (t == 0) ? true : false;
                SetAlphaMap(SetRange(GetAlphaMap(), GetSlopes(), value, slopeLow, slopeHigh));
                break;
        }
    }

    /// <summary> Paints the layer wherever the slope conditions are met. Includes option to blend.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="slopeLow">The minimum slope to paint at 100% weight.</param>
    /// <param name="slopeHigh">The maximum slope to paint at 100% weight.</param>
    /// <param name="minBlendLow">The minimum slope to start to paint. The texture weight will increase as it gets closer to the slopeLow.</param>
    /// <param name="maxBlendHigh">The maximum slope to start to paint. The texture weight will increase as it gets closer to the slopeHigh.</param>
    /// <param name="t">The texture to paint.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintSlopeBlend(LayerType landLayerToPaint, float slopeLow, float slopeHigh, float minBlendLow, float maxBlendHigh, int t) // Paints slope based on the current slope input, the slope range is between 0 - 90
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:

                SetSplatMap(SetRangeBlend(GetSplatMap(landLayerToPaint),  HeightToSplat(GetSlopes()), t, slopeLow, slopeHigh, minBlendLow, maxBlendHigh), landLayerToPaint);
                break;
        }
    }
	
	/// <summary>Paints the layer wherever the curve conditions are met. Includes option to blend.</summary>
	/// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
	/// <param name="curveLow">The minimum curve to paint at 100% weight.</param>
	/// <param name="curveHigh">The maximum curve to paint at 100% weight.</param>
	/// <param name="t">The texture to paint.</param>
	/// <param name="topology">The Topology layer, if selected.</param>
	public static void PaintCurve(LayerType landLayerToPaint, float curveLow, float curveHigh, int t, int topology = 0)
	{
		TerrainManager.UpdateHeightCache();
		switch (landLayerToPaint)
		{
			case LayerType.Ground:
				SetSplatMap(SetRange(GetSplatMap(landLayerToPaint, topology), HeightToSplat(GetCurves(1000)), t, curveLow, curveHigh), landLayerToPaint, topology);
				break;
		}
	}
	
	public static float[,,] GetCurve(LayerType landLayerToPaint, float curveLow, float curveHigh, int t, int topology = 0)
	{
		TerrainManager.UpdateHeightCache();
		switch (landLayerToPaint)
		{
			case LayerType.Ground:
				return SetRange(GetSplatMap(landLayerToPaint, topology), HeightToSplat(GetCurves(1000)), t, curveLow, curveHigh);
				break;
		}
		return null;
	}

	/// <summary> Paints the layer wherever the curve conditions are met. Includes option to blend.</summary>
	/// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
	/// <param name="curveLow">The minimum curve to paint at 100% weight.</param>
	/// <param name="curveHigh">The maximum curve to paint at 100% weight.</param>
	/// <param name="minBlendLow">The minimum curve to start to paint. The texture weight will increase as it gets closer to the curveLow.</param>
	/// <param name="maxBlendHigh">The maximum curve to start to paint. The texture weight will increase as it gets closer to the curveHigh.</param>
	/// <param name="t">The texture to paint.</param>
	/// <param name="topology">The Topology layer, if selected.</param>
	public static void PaintCurveBlend(LayerType landLayerToPaint, float curveLow, float curveHigh, float minBlendLow, float maxBlendHigh, int t)
	{
		
		TerrainManager.UpdateHeightCache();
		switch (landLayerToPaint)
		{
			case LayerType.Ground:
				SetSplatMap(SetRangeBlend(GetSplatMap(landLayerToPaint), HeightToSplat(GetCurves(1000)), t, curveLow, curveHigh, minBlendLow, maxBlendHigh), landLayerToPaint);
				break;
		}
	}


    /// <summary>Paints the splats wherever the water is above 500 and is above the terrain.</summary>
    /// <param name="landLayerToPaint">The LayerType to paint. (Ground, Biome, Alpha, Topology)</param>
    /// <param name="aboveTerrain">Check if the watermap is above the terrain before painting.</param>
    /// <param name="tex">The texture to paint.</param>
    /// <param name="topology">The Topology layer, if selected.</param>
    public static void PaintRiver(LayerType landLayerToPaint, bool aboveTerrain, int tex, int topology = 0)
    {
        switch (landLayerToPaint)
        {
            case LayerType.Ground:
            case LayerType.Biome:
            case LayerType.Topology:
                SetSplatMap(SetRiver(GetSplatMap(landLayerToPaint, topology), GetHeights(), GetHeights(TerrainManager.TerrainType.Water), aboveTerrain, tex), landLayerToPaint, topology);
                break;
            case LayerType.Alpha:

                SetAlphaMap(SetRiver(GetAlphaMap(), GetHeights(), HeightToSplat(GetHeights(TerrainManager.TerrainType.Water)), aboveTerrain, tex == 0));

                break;
            
        }
    }
    #endregion

    /// <summary>Centres the Prefab and Path parent objects.</summary>
	static void CentreSceneObjects(MapInfo mapInfo)
	{
		Vector3 centerPosition = new Vector3(mapInfo.size.x / 2, 500, mapInfo.size.z / 2);

		PrefabManager.PrefabParent.GetComponent<LockObject>().SetPosition(centerPosition);
		if( PrefabManager.EditorSpace.GetComponent<LockObject>() != null){
			PrefabManager.EditorSpace.GetComponent<LockObject>().SetPosition(centerPosition);
			}
		PathManager.PathParent.GetComponent<LockObject>().SetPosition(centerPosition);
	}

	#if UNITY_EDITOR
    /// <summary>Loads and sets up the map.</summary>
    public static void Load(MapInfo mapInfo, string loadPath = "")
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Load(mapInfo, loadPath));
    }

    /// <summary>Saves the map.</summary>
    /// <param name="path">The path to save to.</param>
    public static void Save(string path)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Save(path));
    }
	
	public static void SaveCustomPrefab(string path)
    {
		//PrefabManager.RenamePrefabCategories(PrefabManager.CurrentMapPrefabs, path.Split('/').Last().Split('.')[0] + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10));
		string name = path.Split('/').Last().Split('.')[0];
		PrefabManager.RenamePrefabCategories(PrefabManager.CurrentMapPrefabs, ":" + name + "::");
		PrefabManager.RenameNPCs(PrefabManager.CurrentMapNPCs, ":" + name + "::");
		Debug.LogError("saving custom prefab");
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SaveCustomPrefab(path));
    }
	#else
	public static void Load(MapInfo mapInfo, string loadPath = "")
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.Load(mapInfo, loadPath));
    }

    /// <summary>Saves the map.</summary>
    /// <param name="path">The path to save to.</param>
    public static void Save(string path)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.Save(path));
    }
	
	public static void SaveCustomPrefab(string path)
    {
		//PrefabManager.RenamePrefabCategories(PrefabManager.CurrentMapPrefabs, path.Split('/').Last().Split('.')[0] + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10));
		string name = path.Split('/').Last().Split('.')[0];
		PrefabManager.RenamePrefabCategories(PrefabManager.CurrentMapPrefabs, ":" + name + "::");
		PrefabManager.RenameNPCs(PrefabManager.CurrentMapNPCs, ":" + name + "::");
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.SaveCustomPrefab(path));
    }
	
	#endif
	public static void SaveJson(string path)
    {
		string name = path.Split('/').Last().Split('.')[0];
		PrefabManager.RenamePrefabCategories(PrefabManager.CurrentMapPrefabs, ":" + name + "::");
		PrefabManager.RenameNPCs(PrefabManager.CurrentMapNPCs, ":" + name + "::");
		TerrainToCustomPrefab((-1, 0)).SavePrefabJSON(path);
		Callbacks.OnMapSaved(path);
    }
	
    /// <summary>Creates a new flat terrain.</summary>
    /// <param name="size">The size of the terrain.</param>
    public static void CreateMap(int size, TerrainSplat.Enum ground, TerrainBiome.Enum biome, float landHeight = 503f)
    {
		#if UNITY_EDITOR
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.CreateMap(size, ground, biome, landHeight));
		#else
		CoroutineManager.Instance.StartCoroutine(Coroutines.CreateMap(size, ground, biome, landHeight));
		#endif
		
    }
	
		public static void MergeREPrefab(MapInfo mapInfo, string loadPath = "")
    {	
		#if UNITY_EDITOR
		int progressID = Progress.Start("Load: " + loadPath.Split('/').Last(), "Preparing Map", Progress.Options.Sticky);
		int spwPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
        int spwCircuit = Progress.Start("Circuits", null, Progress.Options.Sticky, progressID);
		int spwNPCs = Progress.Start("NPCs", null, Progress.Options.Sticky, progressID);
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, spwPrefab);
		PrefabManager.SpawnCircuits(mapInfo.circuitData, spwCircuit);
		PrefabManager.SpawnNPCs(mapInfo.npcData, spwNPCs);
		#else
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, 0);
		PrefabManager.SpawnCircuits(mapInfo.circuitData, 0);
		PrefabManager.SpawnNPCs(mapInfo.npcData, 0);
		#endif
	
    }
	
	public static void MergeOffsetREPrefab(MapInfo mapInfo, Transform parent, string loadPath = "")
    {
		#if UNITY_EDITOR
		int progressID = Progress.Start("Load: " + loadPath.Split('/').Last(),  "Preparing Map", Progress.Options.Sticky);
		int spwPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
		//int spwCircuit = Progress.Start("Circuits", null, Progress.Options.Sticky, progressID);
		
        
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, spwPrefab, parent);
		//PrefabManager.SpawnCircuits(mapInfo.circuitData, spwCircuit);
		#else
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, 0, parent);
		#endif
    }
	


	public static void LoadREPrefab(MapInfo mapInfo, string loadPath = "")
    {
		//EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.LoadREPrefab(mapInfo, loadPath));
		#if UNITY_EDITOR
		int progressID = Progress.Start("Load: " + loadPath.Split('/').Last(), "Preparing Map", Progress.Options.Sticky);
		int spwPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
        int spwCircuit = Progress.Start("Circuits", null, Progress.Options.Sticky, progressID);
		int spwNPCs = Progress.Start("NPCs", null, Progress.Options.Sticky, progressID);
		#endif
		//int heightmap = Progress.Start("Heightmap", null, Progress.Options.Sticky, progressID);
		PrefabManager.DeletePrefabs(PrefabManager.CurrentMapPrefabs);
        PrefabManager.DeleteCircuits(PrefabManager.CurrentMapElectrics);
		PrefabManager.DeleteNPCs(PrefabManager.CurrentMapNPCs);
		PrefabManager.DeleteModifiers(PrefabManager.CurrentModifiers);
		
		#if UNITY_EDITOR
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, spwPrefab);
		PrefabManager.SpawnCircuits(mapInfo.circuitData, spwCircuit);
		PrefabManager.SpawnNPCs(mapInfo.npcData, spwNPCs);
		#else
		PrefabManager.SpawnPrefabs(mapInfo.prefabData, 0);
		PrefabManager.SpawnCircuits(mapInfo.circuitData, 0);
		PrefabManager.SpawnNPCs(mapInfo.npcData, 0);
		#endif
		
		
		PrefabManager.SpawnModifiers(mapInfo.modifierData);
		//SetPrefabHeightmap(PrefabManager.LoadHeightmap(loadPath + ".heights"), heightmap);
		
		
    }

   private class Coroutines
	{
			public static IEnumerator LoadREPrefab(MapInfo mapInfo, string path = "")
			{
		#if UNITY_EDITOR
				ProgressManager.RemoveProgressBars("Load:");

				int progressID = Progress.Start("Load: " + path.Split('/').Last(), "Preparing Map", Progress.Options.Sticky);
				int delPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int spwPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int spwCircuit = Progress.Start("Circuits", null, Progress.Options.Sticky, progressID);
				int spwNPCs = Progress.Start("NPCs", null, Progress.Options.Sticky, progressID);
				yield return null;

				yield return PrefabManager.DeletePrefabs(PrefabManager.CurrentMapPrefabs, delPrefab);
				PrefabManager.DeleteCircuits(PrefabManager.CurrentMapElectrics);
				PrefabManager.DeleteNPCs(PrefabManager.CurrentMapNPCs);

				CentreSceneObjects(mapInfo);
				PrefabManager.SpawnPrefabs(mapInfo.prefabData, spwPrefab);
				PrefabManager.SpawnCircuits(mapInfo.circuitData, spwCircuit);
				PrefabManager.SpawnNPCs(mapInfo.npcData, spwNPCs);

				var sw = new System.Diagnostics.Stopwatch();
				while (Progress.GetProgressById(spwPrefab).running)
				{
					if (sw.Elapsed.TotalMilliseconds > 0.05f)
					{
						sw.Restart();
						yield return null;
					}
				}

				Progress.Report(progressID, 0.99f, "Loaded");
				Progress.Finish(progressID, Progress.Status.Succeeded);

				Callbacks.OnMapLoaded(path);
		#else
				PrefabManager.DeletePrefabs(PrefabManager.CurrentMapPrefabs);
				PrefabManager.DeleteCircuits(PrefabManager.CurrentMapElectrics);
				PrefabManager.DeleteNPCs(PrefabManager.CurrentMapNPCs);
				CentreSceneObjects(mapInfo);
				PrefabManager.SpawnPrefabs(mapInfo.prefabData);
				PrefabManager.SpawnCircuits(mapInfo.circuitData);
				PrefabManager.SpawnNPCs(mapInfo.npcData);
				Callbacks.OnMapLoaded(path);
				yield return null;
		#endif
			}

			public static IEnumerator Load(MapInfo mapInfo, string path = "")
			{
		#if UNITY_EDITOR
				ProgressManager.RemoveProgressBars("Load:");

				int progressID = Progress.Start("Load: " + path.Split('/').Last(), "Preparing Map", Progress.Options.Sticky);
				int delPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int spwPrefab = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int delPath = Progress.Start("Paths", null, Progress.Options.Sticky, progressID);
				int spwPath = Progress.Start("Paths", null, Progress.Options.Sticky, progressID);
				int terrainID = Progress.Start("Terrain", null, Progress.Options.Sticky, progressID);
				yield return null;

				yield return PrefabManager.DeletePrefabs(PrefabManager.CurrentMapPrefabs, delPrefab);
				PathManager.DeletePaths(PathManager.CurrentMapPaths, delPath);
				CentreSceneObjects(mapInfo);
				TerrainManager.Load(mapInfo, terrainID);
				PrefabManager.SpawnPrefabs(mapInfo.prefabData, spwPrefab);
				PathManager.SpawnPaths(mapInfo.pathData, spwPath);

				var sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				while (Progress.GetProgressById(terrainID).progress < 0.99f || Progress.GetProgressById(spwPrefab).running || Progress.GetProgressById(spwPath).running)
				{
					if (sw.Elapsed.TotalMilliseconds > 0.05f)
					{
						sw.Restart();
						yield return null;
					}
				}

				Progress.Report(progressID, 0.99f, "Loaded");
				Progress.Finish(terrainID, Progress.Status.Succeeded);
				Progress.Finish(progressID, Progress.Status.Succeeded);

				Callbacks.OnMapLoaded(path);
		#else
				
												
				yield return PrefabManager.DeletePrefabs(PrefabManager.CurrentMapPrefabs, 0);
				PathManager.DeletePaths(PathManager.CurrentMapPaths);
				CentreSceneObjects(mapInfo);
				TerrainManager.Load(mapInfo);
				PrefabManager.SpawnPrefabs(mapInfo.prefabData);
				PathManager.SpawnPaths(mapInfo.pathData);
				Callbacks.OnMapLoaded(path);
				yield return null;
		#endif
			}

			public static IEnumerator Save(string path)
			{
				#if UNITY_EDITOR
				ProgressManager.RemoveProgressBars("Save:");

				int progressID = Progress.Start("Save: " + path.Split('/').Last(), "Saving Map", Progress.Options.Sticky);
				int prefabID = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int pathID = Progress.Start("Paths", null, Progress.Options.Sticky, progressID);
				int terrainID = Progress.Start("Terrain", null, Progress.Options.Sticky, progressID);
				#endif

				SaveLayer();
				yield return null;
				#if UNITY_EDITOR
				TerrainToWorld(Land, Water, (prefabID, pathID, terrainID)).Save(path);
				#else
				TerrainToWorld(Land, Water, (0, 0, 0)).Save(path);
				#endif

				#if UNITY_EDITOR
				Progress.Report(progressID, 0.99f, "Saved");
				Progress.Finish(prefabID, Progress.Status.Succeeded);
				Progress.Finish(pathID, Progress.Status.Succeeded);
				Progress.Finish(terrainID, Progress.Status.Succeeded);
				Progress.Finish(progressID, Progress.Status.Succeeded);
				#endif

				Callbacks.OnMapSaved(path);


			}

			#if UNITY_EDITOR
			public static IEnumerator CreateMap(int size, TerrainSplat.Enum ground, TerrainBiome.Enum biome, float landHeight = 503f)
			{
				yield return EditorCoroutineUtility.StartCoroutineOwnerless(Load(EmptyMap(size, landHeight, ground, biome), "New Map"));
			}
			#else
			public static IEnumerator CreateMap(int size, TerrainSplat.Enum ground, TerrainBiome.Enum biome, float landHeight = 503f)
			{
				yield return CoroutineManager.Instance.StartCoroutine(Load(EmptyMap(size, landHeight, ground, biome), "New Map"));
			}
			#endif

			public static IEnumerator SaveCustomPrefab(string path)
			{
				#if UNITY_EDITOR
				ProgressManager.RemoveProgressBars("Save:");

				int progressID = Progress.Start("Save: " + path.Split('/').Last(), "Saving Map", Progress.Options.Sticky);
				int prefabID = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
				int circuitID = Progress.Start("Circuits", null, Progress.Options.Sticky, progressID);

				#endif
				
				yield return null;
				
				#if UNITY_EDITOR
				TerrainToCustomPrefab((prefabID, circuitID)).SaveREPrefab(path);
		
				Progress.Report(progressID, 0.99f, "Saved");
				Progress.Finish(prefabID, Progress.Status.Succeeded);
				Progress.Finish(circuitID, Progress.Status.Succeeded);
				Progress.Finish(progressID, Progress.Status.Succeeded);

				Callbacks.OnMapSaved(path);
				#else
				TerrainToCustomPrefab((0, 0)).SaveREPrefab(path);
				Callbacks.OnMapSaved(path);
				#endif
			}
	}

}