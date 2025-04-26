using UnityEngine;
using System.Threading.Tasks;
using static TerrainManager;

public static class TopologyData
{
    private static byte[] Data;

    public static TerrainMap<int> GetTerrainMap()
    {
        return new TerrainMap<int>(Data, 1);
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