
using UnityEngine;
using UnityEngine.InputSystem;
using System;
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
		FilterTexture = Resources.Load<Texture>("Textures/Brushes/White128");

        SetTerrainReferences();
		//ShowLandMask();	
		SetTerrainLayers();
	
		SetWaterTransparency(.3f);
		//UpdateHeightCache();
		HideLandMask();
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
    #region Fields
    /// <summary>Ground textures [x, y, texture] use <seealso cref="TerrainSplat.TypeToIndex(int)"/> for texture indexes.</summary>
    /// <value>Strength of texture at <seealso cref="TerrainSplat"/> index, normalised between 0 - 1.</value>
    public static float[,,] Ground { get; private set; }
    /// <summary>Biome textures [x, y, texture] use <seealso cref="TerrainBiome.TypeToIndex(int)"/> for texture indexes.</summary>
    /// <value>Strength of texture at <seealso cref="TerrainBiome"/> index, normalised between 0-1.</value>
    public static float[,,] Biome { get; private set; }
    /// <summary>Alpha/Transparency value of terrain.</summary>
    /// <value>True = Visible / False = Invisible.</value>
    public static bool[,] Alpha { get; private set; }

	public static bool[,] AlphaMask { get; private set; }	
	public static bool[,] SpawnMap { get; private set; }
	
	public static float[,] CliffMap { get; private set; }
	public static float[,,] CliffField { get; private set; }	
	

    /// <summary>Topology layers [topology][x, y, texture] use <seealso cref="TerrainTopology.TypeToIndex(int)"/> for topology layer indexes.</summary>
    /// <value>Texture 0 = Active / Texture 1 = Inactive.</value>
    public static float[][,,] Topology { get; private set; } = new float[TerrainTopology.COUNT][,,];
    /// <summary>Resolution of the splatmap/alphamap.</summary>
    /// <value>Power of ^2, between 512 - 2048.</value>
    public static int SplatMapRes { get; private set; }
    /// <summary>The world size of each splat relative to the terrain size it covers.</summary>
    public static float SplatSize { get => Land.terrainData.size.x / SplatMapRes; }
	public static float SplatRatio { get => Land.terrainData.heightmapResolution / SplatMapRes; }
    public static bool AlphaDirty { get; set; } = true;
    #endregion

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

    public static bool[,] GetAlphaMap()
    {
        if (AlphaDirty)
        {
            Alpha = Land.terrainData.GetHoles(0, 0, AlphaMapRes, AlphaMapRes);
            AlphaDirty = false;
        }
        return Alpha;
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
    /// <summary>The current slope values stored as [x, y].</summary>
    /// <value>Slope angle in degrees, between 0 - 90.</value>

    public static float[,] Slope;
	public static float[,] Curvature;
    /// <summary>The current height values stored as [x, y].</summary>
    /// <value>Height in metres, between 0 - 1000.</value> 
    public static float[,] Height;

    /// <summary>Resolution of the HeightMap.</summary>
    /// <value>Power of ^2 + 1, between 1025 - 4097.</value>
    public static int HeightMapRes { get; private set; }
    /// <summary>Resolution of the AlphaMap.</summary>
    /// <value>Power of ^2, between 1024 - 4096.</value>
    public static int AlphaMapRes { get => HeightMapRes - 1; }

    private static Texture FilterTexture;
    private static Vector2 HeightMapCentre { get => new Vector2(0.5f, 0.5f); }
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

    /// <summary>Callback for whenever the heightmap is updated.</summary>
    private static void HeightMapChanged(Terrain terrain, RectInt heightRegion, bool synched)
    {
		if(terrain.Equals(LandMask))	{
			return;
		}
		
        if (terrain.Equals(Land))
		{
			//FillLandMask();
		}

        ResetHeightCache();
        Callbacks.InvokeHeightMapUpdated(terrain.Equals(Land) ? TerrainType.Land : TerrainType.Water);
		
    }
    #endregion
    #endregion

    #region Terrains
    #region Fields
    /// <summary>The Land terrain in the scene.</summary>
    public static Terrain Land { get; private set; }

	/// <summary>A Terrain for visualizing extra data.</summary>
	public static Terrain LandMask { get; private set; }

    /// <summary>The Water terrain in the scene.</summary>
    public static Terrain Water { get; private set; }
    /// <summary>The material used by the Water terrain object.</summary>
    public static Material WaterMaterial { get; private set; }
    /// <summary>The size of the Land and Water terrains in the scene.</summary>
    public static Vector3 TerrainSize { get => Land.terrainData.size; }
    /// <summary>The offset of the terrain from World Space.</summary>
    public static Vector3 MapOffset { get => 0.5f * TerrainSize; }
    /// <summary>The condition of the current terrain.</summary>
    /// <value>True = Terrain is loading / False = Terrain is loaded.</value>
    public static bool IsLoading { get; private set; } = false;

    /// <summary>Enum of the 2 different terrains in scene. (Land, Water). Required to reference the terrain objects across the Editor.</summary>
    public enum TerrainType
    {
        Land,
		LandMask,

        Water
    }
    #endregion

    #region Methods
    public static void SetTerrainReferences()
    {
        Water = GameObject.FindGameObjectWithTag("Water").GetComponent<Terrain>();
        Land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		LandMask = GameObject.FindGameObjectWithTag("LandMask").GetComponent<Terrain>();
		WaterMaterial = Water.materialTemplate;
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

    private static TerrainLayer[] GroundLayers = null, BiomeLayers = null, TopologyLayers = null, MaskLayers = null;


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
    #region Fields
    /// <summary>The LayerType currently being displayed on the terrain.</summary>
    public static LayerType CurrentLayerType { get; private set; }
    /// <summary>The Topology layer currently being displayed/to be displayed on the terrain when the LayerType is set to topology.</summary>
    public static TerrainTopology.Enum TopologyLayerEnum { get; private set; }
    /// <summary>The Topology layer currently being displayed/to be displayed on the terrain when the LayerType is set to topology.</summary>
    public static int TopologyLayer { get => TerrainTopology.TypeToIndex((int)TopologyLayerEnum); }
    /// <summary>The state of the current layer data.</summary>
    /// <value>True = Layer has been modified and not saved / False = Layer has not been modified since last saved.</value>
    public static bool LayerDirty { get; private set; } = false;
    /// <summary>The amount of TerrainLayers used on the current LayerType.</summary>
    public static int Layers => LayerCount(CurrentLayerType);

    public enum LayerType
    {
        Ground,
        Biome,
        Alpha,
        Topology
    }
    #endregion

    #region Methods
    /// <summary>Saves any changes made to the Alphamaps, including paint operations.</summary>
    public static void SaveLayer()
    {
        SetSplatMap(GetSplatMap(CurrentLayerType, TopologyLayer), CurrentLayerType, TopologyLayer);
        Callbacks.InvokeLayerSaved(CurrentLayerType, TopologyLayer);
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
			
            IsLoading = true;
			yield return SetTerrains(mapInfo, progressID);
            yield return SetSplatMaps(mapInfo, progressID);
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
			#if UNITY_EDITOR
            Progress.Report(progressID, .2f, "Loaded: Water.");
			#endif
           
		   yield return SetupTerrain(mapInfo, Land);
			#if UNITY_EDITOR
            Progress.Report(progressID, .5f, "Loaded: Land.");
			#endif

			yield return SetupTerrain(mapInfo, LandMask);
			#if UNITY_EDITOR
			Progress.Report(progressID, .2f, "Loaded: LandMask.");
			#endif

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