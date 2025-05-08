using UnityEngine;
using System.Threading.Tasks;
using static TerrainManager;

public static class TopologyData
{
    private static byte[] Data;
    public static Texture2D TopologyTexture { get; private set; }
	
    public static TerrainMap<int> GetTerrainMap()
    {
        return new TerrainMap<int>(Data, 1);
    }

    /// <summary>Initializes the TopologyTexture if not already created.</summary>
    public static void InitializeTexture()
    {
        TerrainMap<int> topology = GetTerrainMap();
        int resolution = topology.res;

        if (TopologyTexture == null || TopologyTexture.width != resolution || TopologyTexture.height != resolution || TopologyTexture.format != TextureFormat.RGBA32)
        {
            TopologyTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
			TopologyTexture.filterMode = FilterMode.Point; 
            UpdateTexture(); // Populate initial pixel data
        }
		Shader.SetGlobalTexture("Terrain_Topologies", TopologyTexture);
    }

    /// <summary>Updates the TopologyTexture with the current topology data.</summary>
    public static void UpdateTexture()
    {
        TerrainMap<int> topology = GetTerrainMap();
        int resolution = topology.res;
		
		if (TopologyTexture == null || TopologyTexture.width != resolution || TopologyTexture.height != resolution || TopologyTexture.format != TextureFormat.RGBA32)
        {
            TopologyTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
			TopologyTexture.filterMode = FilterMode.Point; 
            UpdateTexture(); // Populate initial pixel data
        }
		Shader.SetGlobalTexture("Terrain_Topologies", TopologyTexture);

        Color[] pixels = new Color[resolution * resolution];
        Parallel.For(0, resolution, i =>
        {
            for (int j = 0; j < resolution; j++)
            {
                int value = topology[i, j]; // Get the 32-bit bitmask
                float r = ((value >> 0) & 0xFF) / 255f;  // Bits 0-7
                float g = ((value >> 8) & 0xFF) / 255f;  // Bits 8-15
                float b = ((value >> 16) & 0xFF) / 255f; // Bits 16-23
                float a = ((value >> 24) & 0xFF) / 255f; // Bits 24-31
                pixels[i * resolution + j] = new Color(r, g, b, a);
            }
        });

        TopologyTexture.SetPixels(pixels);
        TopologyTexture.Apply();
    }
	

    /// <summary>Returns the Splatmap of the selected Topology Layer.</summary>
    /// <param name="layer">The Topology layer to return.</param>
    public static float[,,] GetTopologyLayer(int layer)
    {
        TerrainMap<int> topology = GetTerrainMap();
        float[,,] splatMap = new float[topology.res, topology.res, 2];
        Parallel.For(0, topology.res, i =>
        {
            for (int j = 0; j < topology.res; j++)
            {
                if ((topology[i, j] & layer) != 0)
                    splatMap[i, j, 0] = 1f;
                else
                    splatMap[i, j, 1] = 1f;
            }
        });
        return splatMap;
    }
	
	public static bool[,] GetTopologyBitmap(int layer)
	{ //scale this up by TerrainMap.SplatRatio
		TerrainMap<int> topology = GetTerrainMap();
		bool[,] bitMap = new bool[topology.res, topology.res];
		Parallel.For(0, topology.res, i =>
		{
			for (int j = 0; j < topology.res; j++)
			{
				if ((topology[i, j] & layer) != 0)
					bitMap[i, j] = true;
			}
		});
		return bitMap;
	}
	
	public static bool[,] GetTopology(int layer, int x, int y, int width, int height)
	{
		TerrainMap<int> topology = GetTerrainMap();
		bool[,] bitMap = new bool[height, width];
		int xMax = Mathf.Min(x + width, topology.res);
		int yMax = Mathf.Min(y + height, topology.res);
		
		Parallel.For(0, Mathf.Min(height, yMax - y), i =>
		{
			for (int j = 0; j < Mathf.Min(width, xMax - x); j++)
			{
				if ((topology[y + i, x + j] & layer) != 0)
				{
					bitMap[i, j] = true;
				}
			}
		});

		return bitMap;
	}

    /// <summary>Converts all the Topology Layer arrays back into a single byte array.</summary>
    public static void SaveTopologyLayers()
    {
        TerrainMap<int> topologyMap = GetTerrainMap();
        Parallel.For(0, TerrainTopology.COUNT, i =>
        {
            Parallel.For(0, topologyMap.res, j =>
            {
                for (int k = 0; k < topologyMap.res; k++)
                {
                    if (Topology[i][j, k, 0] > 0)
                        topologyMap[j, k] = topologyMap[j, k] | TerrainTopology.IndexToType(i);

                    if (Topology[i][j, k, 1] > 0)
                        topologyMap[j, k] = topologyMap[j, k] & ~TerrainTopology.IndexToType(i);
                }
            });
        });
        Data = topologyMap.ToByteArray();
    }
	
	public static void SetTopology(int layer, bool[,] bitmap)
	{
		TerrainMap<int> topologyMap = GetTerrainMap();	

		if (bitmap == null)
		{
			return;
		}

		int height = bitmap.GetLength(0);
		int width = bitmap.GetLength(1);

		// Update the entire topology map with the new bitmap data
		Parallel.For(0, height, i =>
		{
			for (int j = 0; j < width; j++)
			{
				// Check if the coordinate is within the bounds of the terrain
				if (j < topologyMap.res && i < topologyMap.res)
				{
					// If bitmap is true, set the bit for the layer; if false, unset it
					if (bitmap[i, j])
					{
						topologyMap[i, j] |= layer; // Set the bit for this layer
					}
					else
					{
						topologyMap[i, j] &= ~layer; // Unset the bit for this layer
					}
				}
			}
		});

		// Convert updated topology back to byte array and update Data
		Data = topologyMap.ToByteArray();
	}
	
	public static void SetTopology(int layer, float[,,] floatMap)
	{
		if (floatMap == null)
		{
			return;
		}
		layer = TerrainTopology.IndexToType(layer);

		TerrainMap<int> topologyMap = GetTerrainMap();
		int height = floatMap.GetLength(0);
		int width = floatMap.GetLength(1);

		// Update topology map directly based on floatMap values
		Parallel.For(0, height, i =>
		{
			for (int j = 0; j < width; j++)
			{
				// Check if the coordinate is within the bounds of the terrain
				if (j < topologyMap.res && i < topologyMap.res)
				{
					// Set or unset the bit based on floatMap value compared to threshold
					if (floatMap[i, j, 1] < .9f)
					{
						topologyMap[i, j] |= layer; // Set the bit for this layer
					}
					else if (floatMap[i,j,1] > .9f)
					{
						topologyMap[i, j] &= ~layer; // Unset the bit for this layer
					}
				}
			}
		});

		// Convert updated topology back to byte array and update Data
		Data = topologyMap.ToByteArray();
	}
	
	public static void SetTopologyRegion(int[,] topologyValues, int x, int y, int width, int height)
	{
		TerrainMap<int> topologyMap = GetTerrainMap();

		if (topologyValues == null)
		{
			Debug.LogError("SetTopologyRegion: Provided topology values array is null.");
			return;
		}

		// Check if the provided array dimensions match the intended region size
		if (topologyValues.GetLength(0) != height || topologyValues.GetLength(1) != width)
		{
			Debug.LogError($"SetTopologyRegion: Invalid array dimensions. Expected [{height}, {width}], got [{topologyValues.GetLength(0)}, {topologyValues.GetLength(1)}]");
			return;
		}

		// Update the specified region of the topology map with the new values additively
		Parallel.For(0, height, i =>
		{
			for (int j = 0; j < width; j++)
			{
				// Check if the coordinate is within the bounds of the terrain
				if (x + j < topologyMap.res && y + i < topologyMap.res)
				{
					topologyMap[y + i, x + j] |= topologyValues[i, j]; // Apply additively
				}
			}
		});

		// Convert updated topology back to byte array and update Data
		Data = topologyMap.ToByteArray();
	}
	
	public static void SetTopology(int layer, int x, int y, int width, int height, bool[,] bitmap)
	{
		TerrainMap<int> topologyMap = GetTerrainMap();	

		
		if (bitmap == null)
		{
			
			return;
		}

		// Check if the provided bitmap dimensions match the intended region size
		if (bitmap.GetLength(0) != height || bitmap.GetLength(1) != width)
		{
			return;
		}



		// Update the specified region of the topology with the new bitmap data
		Parallel.For(0, height, i =>
		{
			for (int j = 0; j < width; j++)
			{
				// Check if the coordinate is within the bounds of the terrain
				if (x + j < topologyMap.res && y + i < topologyMap.res)
				{
					// If bitmap is true, set the bit for the layer; if false, unset it
					if (bitmap[i, j])
					{
						topologyMap[y + i, x + j] |= layer; // Set the bit for this layer
					}
					else
					{
						topologyMap[y + i, x + j] &= ~layer; // Unset the bit for this layer
					}
				}
				else
				{
				}
			}
		});

		// Convert updated topology back to byte array and update Data
		Data = topologyMap.ToByteArray();
	}

    public static void Set(TerrainMap<int> topology) => Data = topology.ToByteArray();
}