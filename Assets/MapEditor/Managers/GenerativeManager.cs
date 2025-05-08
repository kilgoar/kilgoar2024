using UnityEngine;

using RustMapEditor.Variables;
using static WorldSerialization;
using static TerrainManager;
using Newtonsoft.Json;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class GenerativeManager
{
	
	public static int GeologySpawns;
	private static Dictionary<string, List<PrefabData>> rayDataCache = new Dictionary<string, List<PrefabData>>();
	private static Coroutine cliffCoroutine;

    #region Noise Generation Fields
    private static readonly double unit = 1.0 / Math.Sqrt(2);
    private static readonly double[,] Gradients = new double[8, 2]
    {
        { unit, unit }, { -unit, unit }, { unit, -unit }, { -unit, -unit },
        { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }
    };

    private static readonly byte[] HashTable = new byte[256]
    {
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
        140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 203, 117, 35, 11, 32, 57,
        177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74,
        165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60,
        211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65,
        25, 63, 161, 1, 216, 80, 73, 209, 76, 187, 208, 89, 18, 169, 200, 196,
        135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 132, 64,
        52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85,
        212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170,
        213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43,
        172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185,
        112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191,
        179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31,
        181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150,
        254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195,
        78, 66, 215, 61, 156, 180, 219
    };
    #endregion

    #region Noise Generation Methods
    private static double DotGridGradient(int ix, int iy, double x, double y, int seed)
    {
        int mx = RobustMod(ix + seed, 256);
        int my = RobustMod(iy + seed, 256);
        int index = RobustMod(mx + HashTable[my], 256);
        int g = HashTable[index] % 8;

        double dx = x - ix;
        double dy = y - iy;

        return dx * Gradients[g, 0] + dy * Gradients[g, 1];
    }

    private static int RobustMod(int x, int m)
    {
        return ((x % m) + m) % m;
    }

    private static double Smoother(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    public static double GetPerlinNoise(double x, double y, int seed)
    {
        int x0 = (int)Math.Floor(x);
        int x1 = x0 + 1;
        int y0 = (int)Math.Floor(y);
        int y1 = y0 + 1;

        double sx = x - x0;
        double sy = y - y0;

        double s = DotGridGradient(x0, y0, x, y, seed);
        double t = DotGridGradient(x1, y0, x, y, seed);
        double u = DotGridGradient(x0, y1, x, y, seed);
        double v = DotGridGradient(x1, y1, x, y, seed);

        double ix0 = Lerp(s, t, Smoother(sx));
        double ix1 = Lerp(u, v, Smoother(sx));
        return Lerp(ix0, ix1, Smoother(sy));
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + t * (b - a);
    }
	
	

    [ConsoleCommand("Diamond method with smoothstep blending")]
    public static void GenerateDiamondHeightmap(int seed, float roughness = 0.5f, float blendWeight = 0.5f)
    {
        Terrain land = GameObject.FindGameObjectWithTag("Land")?.GetComponent<Terrain>();
        if (land == null)
        {
            Debug.LogError("No terrain found with tag 'Land'. Creating a new one.");
            GameObject terrainObj = Terrain.CreateTerrainGameObject(new TerrainData());
            terrainObj.tag = "Land";
            land = terrainObj.GetComponent<Terrain>();
            land.terrainData.heightmapResolution = 513;
        }

        int res = land.terrainData.heightmapResolution;
        float[,] baseMap = land.terrainData.GetHeights(0, 0, res, res);
        float[,] diamondMap = new float[res, res];

        // Initialize corners with random values
        diamondMap[0, 0] = (float)UnityEngine.Random.Range(0.475f, 0.525f);
        diamondMap[res - 1, 0] = (float)UnityEngine.Random.Range(0.475f, 0.525f);
        diamondMap[0, res - 1] = (float)UnityEngine.Random.Range(0.475f, 0.525f);
        diamondMap[res - 1, res - 1] = (float)UnityEngine.Random.Range(0.475f, 0.525f);

        // Diamond-Square algorithm
        float range = 1.0f;
        for (int step = res - 1; step > 1; step /= 2)
        {
            int halfStep = step / 2;

            // Diamond step
            for (int x = 0; x < res - 1; x += step)
            {
                for (int y = 0; y < res - 1; y += step)
                {
                    float avg = (diamondMap[x, y] + diamondMap[x + step, y] + diamondMap[x, y + step] + diamondMap[x + step, y + step]) / 4.0f;
                    diamondMap[x + halfStep, y + halfStep] = avg + (float)(UnityEngine.Random.Range(-range, range) * roughness);
                }
            }

            // Square step
            for (int x = 0; x < res - 1; x += halfStep)
            {
                for (int y = (x + halfStep) % step; y < res - 1; y += step)
                {
                    float avg = (
                        diamondMap[(x - halfStep + res - 1) % (res - 1), y] +
                        diamondMap[(x + halfStep) % (res - 1), y] +
                        diamondMap[x, (y + halfStep) % (res - 1)] +
                        diamondMap[x, (y - halfStep + res - 1) % (res - 1)]
                    ) / 4.0f;
                    diamondMap[x, y] = avg + (float)(UnityEngine.Random.Range(-range, range) * roughness);

                    // Handle edges
                    if (x == 0) diamondMap[res - 1, y] = diamondMap[x, y];
                    if (y == 0) diamondMap[x, res - 1] = diamondMap[x, y];
                }
            }

            range *= (1.0f - roughness); // Reduce range with each iteration
        }

        // Normalize diamondMap to 0-1
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {
                minValue = Mathf.Min(minValue, diamondMap[i, j]);
                maxValue = Mathf.Max(maxValue, diamondMap[i, j]);
            }
        }
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {
                diamondMap[i, j] = (diamondMap[i, j] - minValue) / (maxValue - minValue);
            }
        }

        // Apply smoothstep blending onto the base terrain
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {
                float t = (float)Smoother(blendWeight); // Smoothstep the blend weight
                diamondMap[i, j] = Mathf.Lerp(baseMap[i, j], diamondMap[i, j], t);
            }
        }

        land.terrainData.SetHeights(0, 0, diamondMap);
        Debug.Log($"Generated Diamond-Square heightmap with seed {seed}, roughness {roughness}, blendWeight {blendWeight}. Min value: {minValue}, Max value: {maxValue}");
    }
    #endregion
	
	[ConsoleCommand("Perlin method")]
    public static void GeneratePerlinHeightmap(int seed, float scale = 0.05f, int octaves = 4, float persistence = 0.5f, float lacunarity = 2.0f)
    {
        Terrain land = GameObject.FindGameObjectWithTag("Land")?.GetComponent<Terrain>();
        if (land == null)
        {
            Debug.LogError("No terrain found with tag 'Land'. Creating a new one.");
            GameObject terrainObj = Terrain.CreateTerrainGameObject(new TerrainData());
            terrainObj.tag = "Land";
            land = terrainObj.GetComponent<Terrain>();
            land.terrainData.heightmapResolution = 513;
        }

        int res = land.terrainData.heightmapResolution;
        float[,] heightMap = new float[res, res];

        double minValue = double.MaxValue;
        double maxValue = double.MinValue;

        // Generate Perlin noise with octaves
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {
                double x = (double)i / res * scale;
                double y = (double)j / res * scale;

                double noise = 0.0;
                double amplitude = 1.0;
                double frequency = 1.0;

                for (int octave = 0; octave < octaves; octave++)
                {
                    noise += GetPerlinNoise(x * frequency, y * frequency, seed) * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heightMap[i, j] = (float)noise;
                minValue = Math.Min(minValue, noise);
                maxValue = Math.Max(maxValue, noise);
            }
        }

        // Normalize to 0-1
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {
                heightMap[i, j] = (float)((heightMap[i, j] - minValue) / (maxValue - minValue));
            }
        }

        land.terrainData.SetHeights(0, 0, heightMap);
        Debug.Log($"Generated Perlin heightmap with seed {seed}, scale {scale}, octaves {octaves}, persistence {persistence}, lacunarity {lacunarity}. Min value: {minValue}, Max value: {maxValue}");
    }



[ConsoleCommand("Paints the borders of a specified layer with a blending radius")]
public static void PaintBorder(Layers layerData, int radius)
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
                if (layerData.Topologies != 0)
                {
                    // For topologies, simply fill without blending
                    layerMap[x, z, 0] = 1f;
					layerMap[x, z, 1] = 0f;
                    continue;
                }


                float o = Mathf.Lerp(0.01f, 1f, 1f - (float)minDist / radius); // Smooth taper

                // Apply to the target layer
                if (o > 0f)
                {
                    // Set the target layer to the maximum of current and new strength
                    layerMap[x, z, layerIndex] = Mathf.Max(o, layerMap[x, z, layerIndex]);

                    // Reduce other layers proportionally
                    for (int k = 0; k < layerCount; k++)
                    {
                        if (k != layerIndex)
                        {
                            layerMap[x, z, k] *= (1f - o);
                        }
                    }
                }
            }
        }
    }

    // Apply the updated layer back to TerrainManager
    TerrainManager.SetSplatMap(layerMap, layerType, layerIndex);

    // Notify listeners of the update
    TerrainManager.Callbacks.InvokeLayerUpdated(layerType, layerIndex);
}

	[ConsoleCommand("fills curvature with gradient")]
	public static void paintCurvature(Layers layerData, float minBlend = -0.1f, float min = 0f, float max = 0.1f, float maxBlend = 0.2f)
	{
		// Validate curvature range
		if (!(minBlend < min && min < max && max < maxBlend))
		{
			Debug.LogError($"Invalid curvature range: minBlend ({minBlend}) must be < min ({min}) < max ({max}) < maxBlend ({maxBlend}).");
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

		// Get the curvature data from TerrainManager
		float[,] curvature = TerrainManager.GetCurves();

		// Paint the layer with gradient blending based on curvature
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				float curve = curvature[i, j];
				float strength;

				// Calculate blending strength based on curvature
				if (curve < minBlend)
				{
					strength = 0f; // Below minBlend: fully inactive
				}
				else if (curve < min)
				{
					// Gradient from minBlend (0) to min (1)
					strength = Mathf.InverseLerp(minBlend, min, curve);
				}
				else if (curve <= max)
				{
					strength = 1f; // Between min and max: fully active
				}
				else if (curve < maxBlend)
				{
					// Gradient from max (1) to maxBlend (0)
					strength = Mathf.InverseLerp(maxBlend, max, curve);
				}
				else
				{
					strength = 0f; // Above maxBlend: fully inactive
				}

				// Apply strength to the target layer and adjust others
				float totalOtherStrength = 0f;
				for (int k = 0; k < layerCount; k++)
				{
					if (k != layerIndex)
					{
						totalOtherStrength += layerMap[i, j, k];
					}
				}

				// Normalize the non-target layers' strength to (1 - strength)
				float remainingStrength = 1f - strength;
				for (int k = 0; k < layerCount; k++)
				{
					if (k == layerIndex)
					{
						layerMap[i, j, k] = strength; // Target layer gets the calculated strength
					}
					else if (totalOtherStrength > 0)
					{
						// Distribute remaining strength proportionally among other layers
						layerMap[i, j, k] = (layerMap[i, j, k] / totalOtherStrength) * remainingStrength;
					}
					else
					{
						// If no other strength existed, set evenly or to 0
						layerMap[i, j, k] = (layerCount > 1) ? (remainingStrength / (layerCount - 1)) : 0f;
					}
				}
			}
		}

		// Apply the updated layer back to TerrainManager
		TerrainManager.SetLayerData(layerMap, layerType, layerIndex);
	}


[ConsoleCommand("Paint slopes, heights, or curves range")]
public static void PaintRange(Layers layerData, float minBlend = 20f, float min = 30f, float max = 60f, float maxBlend = 70f, string topography ="slopes")
{
    // Validate slope range
    if (!(minBlend <= min && min < max && max <= maxBlend))
    {
        Debug.LogError($"Invalid slope range: minBlend ({minBlend}) must be < min ({min}) < max ({max}) < maxBlend ({maxBlend}).");
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
    int splatRes = layerMap.GetLength(0); // Splatmap resolution
    int layerCount = TerrainManager.LayerCount(layerType); // 8 for Ground, 4 for Biome, 2 for Topology

    TerrainManager.UpdateHeightCache();
	int heightRes = TerrainManager.HeightMapRes;
	
	float[,] slopes = new float[heightRes,heightRes];
	if(topography.Equals("slopes")){
		slopes = TerrainManager.Slope;}
	
	else if(topography.Equals("heights")){
		slopes = TerrainManager.Land.terrainData.GetHeights(0, 0, heightRes, heightRes);
		Debug.LogError("polling heightmap " + slopes[0,0]);}
		
		
	else if(topography.Equals("curves")){
		slopes = TerrainManager.Curvature;}
	

    float splatRatio = TerrainManager.SplatRatio; // e.g., 2 if 2049 heightmap vs. 1024 splatmap
	float slope;
    // Register undo before modifying
    TerrainManager.RegisterSplatMapUndo($"Paint Slope Blend {layerType} Index {layerIndex}, {minBlend}-{min}-{max}-{maxBlend}");

    for (int i = 0; i < splatRes; i++)
    {
        for (int j = 0; j < splatRes; j++)
        {
            // Map splat coordinates to heightmap space
            int heightX = Mathf.FloorToInt(i * splatRatio);
            int heightY = Mathf.FloorToInt(j * splatRatio);
            heightX = Mathf.Clamp(heightX, 0, heightRes - 1);
            heightY = Mathf.Clamp(heightY, 0, heightRes - 1);

            slope = slopes[heightX, heightY];

            float strength;

            // Calculate blending strength based on slope


                if (slope < minBlend)
                {
                    strength = 0f; // Below minBlend: fully inactive
                }
                else if (slope < min)
                {
                    // Gradient from minBlend to min
                    strength = Mathf.InverseLerp(minBlend, min, slope);
                }
                else if (slope <= max)
                {
                    strength = 1f; // Between min and max: fully active
                }
                else if (slope < maxBlend)
                {
                    // Gradient from max to maxBlend
                    strength = Mathf.InverseLerp(maxBlend, max, slope);
                }
                else
                {
                    strength = 0f; // Above maxBlend: fully inactive
                }
				
				if(layerType == LayerType.Topology){
					if (strength>0f){
						layerMap[i,j,1] = 0f;
						layerMap[i,j,0] = 1f;
					}
					continue;
				}
				else{				
                ApplyLayerBlend(layerMap, i, j, layerIndex, layerCount, strength);
				}

        }
    }

    // Apply the updated layer
    TerrainManager.SetSplatMap(layerMap, layerType, layerIndex);

    // Notify listeners
    TerrainManager.Callbacks.InvokeLayerUpdated(layerType, layerIndex);
}



private static void ApplyLayerBlend(float[,,] layerMap, int x, int z, int layerIndex, int layerCount, float strength)
{
    if (strength > 0f)
    {
        layerMap[x, z, layerIndex] = Mathf.Max(strength, layerMap[x, z, layerIndex]);
        for (int k = 0; k < layerCount; k++)
        {
            if (k != layerIndex)
            {
                layerMap[x, z, k] *= (1f - strength);
            }
        }
    }
}


	[ConsoleCommand("oceans")]
	public static void oceans(OceanPreset ocean)
	{
		
		//should fix with proper inputs
		int radius = ocean.radius;
		int gradient = ocean.gradient;
		float seafloor = ocean.seafloor / 1000f;
		int xOffset = ocean.xOffset;
		int yOffset = ocean.yOffset;
		bool perlin = ocean.perlin;
		int s = ocean.s;
			
				
				float	r = UnityEngine.Random.Range(0,10000)/100f;
				float	r1 =  UnityEngine.Random.Range(0,10000)/100f;
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			
			float[,] perlinShape = new float[baseMap.GetLength(0),baseMap.GetLength(0)];
			
			float[,] puckeredMap = new float[baseMap.GetLength(0),baseMap.GetLength(0)];
			int distance = 0;
			
			Vector2 focusA = new Vector2(baseMap.GetLength(0)/2f+xOffset,baseMap.GetLength(0)/2f+yOffset);
			Vector2 focusB = new Vector2(baseMap.GetLength(0)/2f-xOffset,baseMap.GetLength(0)/2f-yOffset);
			
			
			Vector2 center = new Vector2(baseMap.GetLength(0)/2f,baseMap.GetLength(0)/2f);
			Vector2 scanCord = new Vector2(0f,0f);
			
			int res = baseMap.GetLength(0);
			
				for (int i = 0; i < res; i++)
				{
					//EditorUtility.DisplayProgressBar("Puckering", "making island",(i*1f/res));
					for (int j = 0; j < res; j++)
					{
						scanCord.x = i; scanCord.y = j;
						//circular
						//distance = (int)Vector2.Distance(scanCord,center);
						distance = (int)(Mathf.Pow((Mathf.Pow((scanCord.x - focusA.x),4f) + Mathf.Pow((scanCord.y - focusA.y),4f)),1f/4f));
						
						//distance = (int)Mathf.Sqrt(Vector2.Distance(scanCord,focusA)) + (int)Mathf.Sqrt(Vector2.Distance(scanCord,focusB));
						
						//if distance from center less than radius, value is 1
						if (distance < radius*2f)
						{
							puckeredMap[i,j] = 1f;
						}
						//otherwise the value should proceed to 0
						else if (distance>=radius *2f && distance <=radius*2f + gradient)
						{
							if (perlin)
							{
								perlinShape[i,j] = Mathf.PerlinNoise(i*1f/s+r, j*1f/s+r1)*2f;
							}
							else
							{
								perlinShape[i,j] = 1f;
							}
							
							if (perlinShape[i,j] > 1f)
								perlinShape[i,j] = 1f;
							
							puckeredMap[i,j] = .5f+Mathf.Cos(((distance-radius*2f)/gradient)*Mathf.PI)*.5f - (Mathf.Sin(((distance-radius*2f)/gradient)*Mathf.PI)*perlinShape[i,j]*.5f);
							
							if (puckeredMap[i,j] < 0)
								puckeredMap[i,j] = 0;
						}
						else
						{
							puckeredMap[i,j] = 0f;
						}
						
						puckeredMap[i,j] = Mathf.Lerp(seafloor, baseMap[i,j], puckeredMap[i,j]);
					}
				}
												

						
			
			//EditorUtility.ClearProgressBar();
			land.terrainData.SetHeights(0, 0, puckeredMap);
	}

	[ConsoleCommand("Import .raw heightmap and resample")]
	public static void ImportHeightmap(string path)
	{
		// Construct the full file path
		string fullPath = Path.Combine(SettingsManager.AppDataPath(), "Presets/Scripts", path);
		if (!File.Exists(fullPath))
		{
			Debug.LogError($"Heightmap file not found: {fullPath}");
			return;
		}

		// Read the raw bytes
		byte[] bytes = File.ReadAllBytes(fullPath);

		// Calculate resolution (assuming square heightmap, 16-bit values)
		int totalValues = bytes.Length / 2; // 2 bytes per value
		int sourceRes = (int)Mathf.Sqrt(totalValues);
		if (sourceRes * sourceRes * 2 != bytes.Length)
		{
			Debug.LogError($"Invalid .raw file size: {bytes.Length} bytes does not match a square 16-bit heightmap.");
			return;
		}

		// Get target terrain resolution
		int targetRes = Land.terrainData.heightmapResolution;

		// Convert bytes to source heightmap
		float[,] sourceHeightMap = new float[sourceRes, sourceRes];
		for (int y = 0; y < sourceRes; y++)
		{
			for (int x = 0; x < sourceRes; x++)
			{
				int index = (y * sourceRes + x) * 2;
				ushort value = (ushort)(bytes[index] | (bytes[index + 1] << 8)); // Little-endian 16-bit
				sourceHeightMap[x, y] = value / 65535f; // Normalize to 0-1
			}
		}

		// Resample if resolution doesn't match
		float[,] heightMap;
		if (sourceRes != targetRes)
		{
			Debug.Log($"Resampling heightmap from {sourceRes}x{sourceRes} to {targetRes}x{targetRes}");
			heightMap = ResampleHeightmap(sourceHeightMap, sourceRes, targetRes);
		}
		else
		{
			heightMap = sourceHeightMap;
		}

		// Apply to terrain
		Land.terrainData.SetHeights(0, 0, heightMap);
		Callbacks.InvokeHeightMapUpdated(TerrainType.Land); // Notify listeners
	}

	// Bilinear resampling of heightmap
	private static float[,] ResampleHeightmap(float[,] source, int sourceRes, int targetRes)
	{
		float[,] target = new float[targetRes, targetRes];
		float scaleX = (float)(sourceRes - 1) / (targetRes - 1);
		float scaleY = (float)(sourceRes - 1) / (targetRes - 1);

		for (int y = 0; y < targetRes; y++)
		{
			for (int x = 0; x < targetRes; x++)
			{
				// Map target coordinates to source space
				float srcX = x * scaleX;
				float srcY = y * scaleY;

				// Get integer and fractional parts
				int x0 = Mathf.FloorToInt(srcX);
				int y0 = Mathf.FloorToInt(srcY);
				int x1 = Mathf.Min(x0 + 1, sourceRes - 1);
				int y1 = Mathf.Min(y0 + 1, sourceRes - 1);
				float fx = srcX - x0;
				float fy = srcY - y0;

				// Bilinear interpolation
				float h00 = source[x0, y0];
				float h10 = source[x1, y0];
				float h01 = source[x0, y1];
				float h11 = source[x1, y1];
				float h = h00 * (1 - fx) * (1 - fy) + h10 * fx * (1 - fy) + h01 * (1 - fx) * fy + h11 * fx * fy;
				target[x, y] = h;
			}
		}
		return target;
	}
	
	public static float[,] loadHeightmap(string path)
	{
		
		byte[] sample = File.ReadAllBytes(path);
		Texture2D sampleTexture = new Texture2D(2,2);
		sampleTexture.LoadRawTextureData(sample);
		Color[] colorMap = sampleTexture.GetPixels();
		int res = colorMap.GetLength(0); 
		res=(int)Math.Sqrt(res);
		float[,] heightMap = new float[res,res];
		
		for (int i = 0; i < res; i++)
			{
					
				for (int j = 0; j < res; j++)
				{
					heightMap[i,j] = colorMap[i + j * res].grayscale;
				}
			}
		
		
		return heightMap;
	}
	
	[ConsoleCommand("diamond square heightmap")]
	public static void diamondSquareNoise(int roughness, int height, int weight)
	{
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);			
			int res = baseMap.GetLength(0);
			float[,] newMap = new float[res,res];
			
			//copied from robert stivanson's 'unity-diamond-square'
			//https://github.com/RobertStivanson
			
			//initialize corners
			
			newMap[0,0] = UnityEngine.Random.Range(475,525)/1000f;
			newMap[res-1,0] = UnityEngine.Random.Range(475,525)/1000f;
			newMap[0,res-1] = UnityEngine.Random.Range(475,525)/1000f;
			newMap[res-1, res-1] = UnityEngine.Random.Range(475,525)/1000f;
			
			
			int j, j2, x, y;
			float avg = 0.5f;
			float range = 1f;			
			
			for (j = res - 1; j > 1; j /= 2) 
			{
				j2 = j / 2;
			
				//diamond
				for (x = 0; x < res - 1; x += j) 
				{
					for (y = 0; y < res - 1; y += j) 
					{
						avg = newMap[x, y];
						avg += newMap[x + j, y];
						avg += newMap[x, y + j];
						avg += newMap[x + j, y + j];
						avg /= 4.0f;

						avg += (UnityEngine.Random.Range(0,height)/1000f - height/1500f) * range;
						newMap[x + j2, y + j2] = avg;
					}
				}
				
				//square
				for (x = 0; x < res - 1; x += j2) 
				{
					for (y = (x + j2) % j; y < res - 1; y += j) 
					{
						avg = newMap[(x - j2 + res - 1) % (res - 1), y];
						avg += newMap[(x + j2) % (res - 1), y];
						avg += newMap[x, (y + j2) % (res - 1)];
						avg += newMap[x, (y - j2 + res - 1) % (res - 1)];
						avg /= 4.0f;

						
						avg += (UnityEngine.Random.Range(0,height)/1000f - height/1500f) * range;
						
						
						newMap[x, y] = avg;

						
						if (x == 0)
						{							
							newMap[res - 1, y] = avg;
						}
						

						if (y == 0) 
						{
							newMap[x, res - 1] = avg;
						}
						
	
					}
				}
				
				range -= (float)(Math.Log10(1+roughness/100f)*range);
			
			
			
			}

			
			for(int h = 0; h < res; h++)
			{
				for(int i = 0; i < res; i++)
				{
					//hi
					newMap[h,i] = (newMap[h,i] * (weight/100f)) + (baseMap[h,i] * (1f-(weight/100f)));
				}
			}
	
			land.terrainData.SetHeights(0, 0, newMap);
	
	}	
	
	[ConsoleCommand("generate ridiculous heightmap")]
	public static void perlinRidiculous(PerlinPreset perlin)
	{
			int l = perlin.layers;
			int p = perlin.period;
			int s = perlin.scale;
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			float[,] perlinSum = baseMap;
			
			
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					perlinSum[i,j] = (0);
				}
			}
			
			
			float r = 0;
			float r1 = 0;
			float amplitude = 1f;
			
			
			for (int u = 1; u <= l; u++)
			{
				
				r = UnityEngine.Random.Range(0,10000)/100f;
				r1 =  UnityEngine.Random.Range(0,10000)/100f;
				amplitude *= .3f;
				
				
				
				for (int i = 0; i < baseMap.GetLength(0); i++)
				{
		
					for (int j = 0; j < baseMap.GetLength(0); j++)
					{
						
						perlinSum[i,j] +=  amplitude * Mathf.PerlinNoise((Mathf.PerlinNoise((Mathf.PerlinNoise(Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)), Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)))), (Mathf.PerlinNoise(Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)), Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)))))),(Mathf.PerlinNoise((Mathf.PerlinNoise(Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)), Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)))), (Mathf.PerlinNoise(Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)), Mathf.PerlinNoise(Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r), Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1)))))));
					}
					//EditorUtility.DisplayProgressBar("Generating layer " + u.ToString(), "", (i*1f / baseMap.GetLength(0)*1f));
				}
												
				s = s + p;
				
			}
			//EditorUtility.ClearProgressBar();
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					perlinSum[i,j] = (perlinSum[i,j]/(l)*3f)+.3525f;
				}
			}
			
			
	
			land.terrainData.SetHeights(0, 0, perlinSum);
	
	}	
	
	[ConsoleCommand("generate perlin heightmap")]
	public static void perlinSimple(PerlinPreset perlin)
	{
			int l = perlin.layers;
			int p = perlin.period;
			int s = perlin.scale;
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			float[,] perlinSum = baseMap;
			
			
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					perlinSum[i,j] = (0);
				}
			}
			
			
			float r = 0;
			float amplitude = .5f;
			float height  = .15f;
			
			
			for (int u = 1; u <= l; u++)
			{
				
				r = UnityEngine.Random.Range(0,10000)/100f;
				//r1 =  UnityEngine.Random.Range(0,10000)/100f;
				
				
				
				
				
				for (int i = 0; i < baseMap.GetLength(0); i++)
				{
		
					for (int j = 0; j < baseMap.GetLength(0); j++)
					{
						
						perlinSum[i,j] += Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r)*height + amplitude;
					}
					//EditorUtility.DisplayProgressBar("Generating layer " + u.ToString(), "", (i*1f / baseMap.GetLength(0)*1f));
				}
												
				s = s - p;
				amplitude=0;
				height *= .5f;
				
			}
			//EditorUtility.ClearProgressBar();
			
			
	
			land.terrainData.SetHeights(0, 0, perlinSum);
	
	}	
	
	
	public static Vector2 minmaxHeightmap()
	{
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		Vector2 minmax = new Vector2(0f,1000f);
		for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					minmax.x = Math.Max(minmax.x, baseMap[i,j]);
					minmax.y = Math.Min(minmax.y, baseMap[i,j]);
				}
			}
			return minmax;
	}
	
	[ConsoleCommand("terracing heightmap")]
	public static void randomTerracing(TerracingPreset terracing)
	{
			bool flatten = terracing.flatten;
			bool perlinBanks = terracing.perlinBanks;
			bool circular = terracing.circular;
			float terWeight = terracing.weight;
			int zStart = terracing.zStart;
			int gBot = terracing.gateBottom;
			int gTop = terracing.gateTop;
			int gates = terracing.gates;
			int descaler = terracing.descaleFactor;
			int density = terracing.perlinDensity;
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			float[,] perlinSum = baseMap;
			
			float gateTop = zStart/1000f;
			float gateBottom = .5f;
			float gateRange = 0;
			float gateLoc =0;
			
			float r = 0;
			float r1 = 0;
			int s = density;
	
					
			r = UnityEngine.Random.Range(0,10000)/100f;
			r1 =  UnityEngine.Random.Range(0,10000)/100f;
	
	for (int g = 0; g < gates; g++)
			{
			gateBottom = gateTop*1f;
			gateRange = UnityEngine.Random.Range(gBot,gTop)/1000f;
			gateTop = gateTop + gateRange;
			
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					
					if (flatten && (baseMap[i,j] <= gateBottom) && g==0)
					{
						baseMap[i,j] = baseMap[i,j] / descaler + gateBottom - (gateBottom/descaler);
					}
					

					if ((baseMap[i,j] > gateBottom) && (baseMap[i,j] < gateTop))
					{
						
									gateLoc = (baseMap[i,j]-gateBottom)/(gateTop-gateBottom);
									
									
									
									
									if (circular)
									{
										if (perlinBanks)
										{
											baseMap[i,j] = baseMap[i,j]-(Mathf.Sin(3.12f*gateLoc)* (gateTop-gateBottom)*(perlinSum[i,j] * terWeight* (Mathf.PerlinNoise(i*1f/s+r, j*1f/s+r1)) ));										
										}
										else
										{
											baseMap[i,j] = baseMap[i,j]-(Mathf.Sin(3.12f * gateLoc * .7f) * (gateTop-gateBottom) * terWeight );
										}
									}
									else
									{
										if (perlinBanks)
										{
											baseMap[i,j] = baseMap[i,j]-(gateLoc*.7f) * (gateTop-gateBottom)*(perlinSum[i,j] * terWeight) * (Mathf.PerlinNoise(i*1f/s+r, j*1f/s+r1));										
										}
										else
										{
											baseMap[i,j] = baseMap[i,j]-((gateLoc*.7f) * (gateTop-gateBottom)*terWeight);
										}
									}
					}
	
				}
			}
			
			
			
	
		}
	
		land.terrainData.SetHeights(0, 0, baseMap);
	}
	
	[ConsoleCommand("textures terrain")]
	public static void rippledFiguring(RipplePreset ripple)
	{
			
			int size = ripple.size;
			int density = ripple.density;
			float weight = ripple.weight;
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			float[,] perlinMap = baseMap;
			
				float r = 0;
				float r1 = 0;
				int s = size;
				float field = 0;
				float rippling = 0;
					r = UnityEngine.Random.Range(0,10000)/100f;
					r1 =  UnityEngine.Random.Range(0,10000)/100f;
					
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
					
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					field = (-.5f+2f*(Mathf.PerlinNoise(i*1f/s*density+r, j*1f/s*density+r1)));
					if (field < 0f){field=0f;}
					rippling = -.65f * Mathf.Abs(Mathf.Pow((.011f * (Mathf.PerlinNoise(i * 1.8f/s+r, j*1.8f/s+r1))),3f) - (.011f * Mathf.PerlinNoise(i*1f/s+r, j*1f/s+r1)));
					baseMap[i,j] = baseMap[i,j] - weight * field * rippling;
				}
			}
			
			
			land.terrainData.SetHeights(0, 0, baseMap);
						
	}
	
	[ConsoleCommand("inverts heightmap")]
	public static void FlipHeightmap()
	{
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			for (int i = 0; i < baseMap.GetLength(0); i++)
			{
				for (int j = 0; j < baseMap.GetLength(0); j++)
				{
					baseMap[i,j] = baseMap[i,j]*-1f+1f;
				}
			}
		land.terrainData.SetHeights(0, 0, baseMap);
	}
	
	[ConsoleCommand("renders splat clouds")]
	public static void perlinSplat(PerlinSplatPreset perlinSplat)
	{
		int s = perlinSplat.scale;
		float c = perlinSplat.strength;
		bool invert = perlinSplat.invert;
		bool paintBiome = perlinSplat.paintBiome;
		int t = perlinSplat.splatLayer;
		int index = TerrainBiome.TypeToIndex((int)perlinSplat.biomeLayer);
		
		float[,,] newBiome = TerrainManager.Biome;
		float[,,] newGround = TerrainManager.Ground;
		

		float o = 0;
		float r = UnityEngine.Random.Range(0,10000)/100f;
		float r1 = UnityEngine.Random.Range(0,10000)/100f;
		
		int res = newGround.GetLength(0);
		
		for (int i = 0; i < res; i++)
        {
			//EditorUtility.DisplayProgressBar("Gradient Noise", "Textures",(i*1f/res));
            for (int j = 0; j < res; j++)
            {
					o = Mathf.PerlinNoise(i*1f/s+r,j*1f/s+r1);
					
					o *= c;
					
					if (o > 1f)
						o=1f;
					
					if (invert)
						o = 1f - o; 
					
					
					if (paintBiome)
						o *= (newBiome[i,j, index]);
					
										
					
					if (o > 0f)		{
					newGround[i,j,t] = Math.Max(o, newGround[i,j,t]);
					
					for (int m = 0; m <=7; m++)		{
										if (m!=t)
											newGround[i,j,m] *= 1f-o;
										
									}
					}						
				
            }
        }
		//EditorUtility.ClearProgressBar();
		//dont forget this shit again
		TerrainManager.SetLayerData(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
	
	
	[ConsoleCommand("copy terrain to topology")]
	public static void terrainToTopology(Layers topology, Layers splatLayer, float threshhold)
	{
		float[,,] splatMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)topology.Topologies));
		float[,,] targetGround = TerrainManager.Ground;
		int t = TerrainSplat.TypeToIndex((int)splatLayer.Ground);
		
		int res = targetGround.GetLength(0);
		for (int i = 0; i < res; i++)
        {
			//EditorUtility.DisplayProgressBar("Copying", "Terrains to Topology",(i*1f/res));
            for (int j = 0; j < res; j++)
            {
                if (targetGround[i,j,t] >= threshhold)
				{
					splatMap[i, j, 0] = float.MaxValue;
					splatMap[i, j, 1] = float.MinValue;
				}
            }
        }
		//EditorUtility.ClearProgressBar();
		TerrainManager.SetLayerData(splatMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)topology.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
	}	
	
    [ConsoleCommand("Copy topologies")]
    public static void copyTopologyLayer(Layers source, Layers destination)
    {
        int sourceIndex = TerrainTopology.TypeToIndex((int)source.Topologies);
        int destIndex = TerrainTopology.TypeToIndex((int)destination.Topologies);
        float[,,] sourceArray = TerrainManager.GetLayerData(LayerType.Topology, sourceIndex);
        
        // Validate source array size
        if (sourceArray.GetLength(2) != 2)
        {
            Debug.LogError($"Source topology array has incorrect layer count: {sourceArray.GetLength(2)}. Expected 2 for topology.");
            return;
        }

        TerrainManager.SetLayerData(sourceArray, LayerType.Topology, destIndex);
    }
	
	[ConsoleCommand("topology lake fill")]
	public static void lakeTopologyFill(Layers layer)
	{
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		float[,] heightMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		float[,,] topoMap = TerrainManager.GetLayerData(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		int res = topoMap.GetLength(0);
		int dim = heightMap.GetLength(0);
		float ratio  = 1f*dim/res;
		int xCheck = 0;
		int yCheck = 0;
		float[,,] lakeMap = new float[res,res,2];
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				xCheck = ((int)(1f*i*ratio));
				yCheck =((int)(1f*j*ratio));
				if (heightMap[xCheck,yCheck] < .499f)
				{
					lakeMap[i,j,1] = 0f;
					lakeMap[i,j,0] = 1f;
				}
				else
				{
					lakeMap[i,j,1] = 1f;
					lakeMap[i,j,0] = 0f;
				}
			}
		}
		
		Point lake;
		lake.X = 3;
		lake.Y = 3;

		lakeMap = topoDeleteFill(lake, lakeMap);
		TerrainManager.SetLayerData(lakeMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
		
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));

	}
	
	
	public static int monumentDataLength(monumentData [] array)
	{
		int count=0;
		foreach (monumentData monument in array)
		{
			if (monument.x != 0)
				count++;
		}
		
		return count;
	}

	public static monumentData [] monumentLocations(float [,,] biomeMap)
	{
		int res = biomeMap.GetLength(0);
		float[,,] analysisMap = new float[res,res,2];
		
		monumentData[] monument = new monumentData[300];
		
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				if (biomeMap[i,j,0] != 0)
				{
					analysisMap[i,j,1] = 0f;
					analysisMap[i,j,0] = 1f;
				}
				else
				{
					analysisMap[i,j,1] = 1f;
					analysisMap[i,j,0] = 0f;
				}
			}
		}
		
		Stack<Point> pixels = new Stack<Point>();
		Point p;
		int maxX, maxY, minX, minY;
		int count = -1;
		
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				if (analysisMap[i,j,1] == 0f && count < 299)
				{
							pixels = new Stack<Point>();
							p.X = i;
							p.Y = j;
							
							maxX = i;
							minX = i;
							maxY = j;
							minY = j;
							
							count++;
							
							pixels.Push(p);
							
							float target =0f;
							
							while (pixels.Count != 0)
							{
								
								Point temp = pixels.Pop();
								int y1 = temp.Y;
								
								while (y1 >= 0 && analysisMap[temp.X, y1,1] == target)
								{
									y1--;
								}
								y1++;
								bool spanLeft =false;
								bool spanRight =false;
								while (y1 < res && analysisMap[temp.X,y1,1] == target)
								{
									analysisMap[temp.X, y1, 1] = 1f;
									analysisMap[temp.X, y1, 0] = 0f;
									
									maxX = Math.Max(temp.X, maxX);
									minX = Math.Min(temp.X, minX);
									maxY = Math.Max(y1, maxY);
									minY = Math.Min(y1, minY);
									
									if(!spanLeft && temp.X > 0 && analysisMap[temp.X-1, y1,1] == target)
									{
										pixels.Push(new Point(temp.X -1, y1));
										spanLeft=true;
									}
									else if(spanLeft && temp.X -1 >= 0 && (analysisMap[temp.X-1, y1, 1] != target))
									{
										spanLeft=false;
									}
									if(!spanRight && temp.X < res - 1 && analysisMap[temp.X+1, y1, 1] == target)
									{
										pixels.Push(new Point(temp.X +1, y1));
										spanRight=true;
									}
									else if(spanRight && temp.X < res - 1 && (analysisMap[temp.X+1, y1, 1] != target))
									{
										spanRight=false;
									}
									y1++;
								}
									
							}
							
					monument[count] = new monumentData(minX, minY, maxX-minX, maxY-minY);
				}
			}
		}
		Debug.LogError(count);
		return monument;
		
	}
	
	[ConsoleCommand("topology ocean fill")]
	public static void oceanTopologyFill(Layers layer)
	{
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		float[,] heightMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		float[,,] topoMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		
		
		int res = topoMap.GetLength(0);
		int dim = heightMap.GetLength(0);
		float ratio  = 1f*dim/res;
		float[,,] oceanMap = new float[res,res,2];
		float[,,] lakeMap = new float[res,res,2];
		int xCheck = 0;
		int yCheck = 0;
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				xCheck = ((int)(1f*i*ratio));
				yCheck =((int)(1f*j*ratio));
				
				if (heightMap[xCheck,yCheck] < .499f)
				{
					lakeMap[i,j,1] = 0f;
					lakeMap[i,j,0] = 1f;
					oceanMap[i,j,1] = 0f;
					oceanMap[i,j,0] = 1f;
				}
				else
				{
					lakeMap[i,j,1] = 1f;
					lakeMap[i,j,0] = 0f;
					oceanMap[i,j,1] = 1f;
					oceanMap[i,j,0] = 0f;
				}
			}
		}
		
		Point lake;
		lake.X = 3;
		lake.Y = 3;

		lakeMap = topoDeleteFill(lake, lakeMap);
		
		
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				if (oceanMap[i,j,1] == lakeMap[i,j,1])
				{
					oceanMap[i,j,1] = 1f;
					oceanMap[i,j,0] = 0f;
				}
			}
		}
		
		TerrainManager.SetSplatMap(oceanMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));

	}
	
	public static float [,,] topoDeleteFill(Point p, float[,,] topoMap)
	{

		Stack<Point> pixels = new Stack<Point>();
		int count=0;
		int res = topoMap.GetLength(0);
		pixels.Push(p);
		
		float target =0f;
		
		while (pixels.Count != 0)
		{
			count++;
			Point temp = pixels.Pop();
			int y1 = temp.Y;
			
			while (y1 >= 0 && topoMap[temp.X, y1,1] == target)
			{
				y1--;
			}
			y1++;
			bool spanLeft =false;
			bool spanRight =false;
			while (y1 < res && topoMap[temp.X,y1,1] == target)
			{
				topoMap[temp.X, y1, 1] = 1f;
				topoMap[temp.X, y1, 0] = 0f;
				
				if(!spanLeft && temp.X > 0 && topoMap[temp.X-1, y1,1] == target)
				{
					pixels.Push(new Point(temp.X -1, y1));
					spanLeft=true;
				}
				else if(spanLeft && temp.X -1 >= 0 && (topoMap[temp.X-1, y1, 1] != target))
				{
					spanLeft=false;
				}
				if(!spanRight && temp.X < res - 1 && topoMap[temp.X+1, y1, 1] == target)
				{
					pixels.Push(new Point(temp.X +1, y1));
					spanRight=true;
				}
				else if(spanRight && temp.X < res - 1 && (topoMap[temp.X+1, y1, 1] != target))
				{
					spanRight=false;
				}
				y1++;
			}
				
		}
		Debug.LogError(count);
		return topoMap;
	}
	
	[ConsoleCommand("outline topology")]
	public static void paintTopologyOutline(Layers layer, Layers sourceLayer, int w)
	{
		
		
		float[,,] sourceMap = TerrainManager.GetLayerData(LayerType.Topology, TerrainTopology.TypeToIndex((int)sourceLayer.Topologies));	
		int res = sourceMap.GetLength(0);
		float[,,] splatMap = new float[res,res,2];
		float[,,] scratchMap = new float[res,res,2];
		float[,,] hateMap = new float[res,res,2];
		
		for (int i = 1; i < sourceMap.GetLength(0)-1; i++)
			{
				
				for (int j = 1; j < sourceMap.GetLength(1)-1; j++)
				{
					if (sourceMap[i, j, 0] <= .5f)
								{
									scratchMap[i, j, 0] = float.MaxValue;
									scratchMap[i, j, 1] = float.MinValue;
									hateMap[i, j, 0] = float.MaxValue;
									hateMap[i, j, 1] = float.MinValue;
								}
								else
								{
									scratchMap[i, j, 0] = float.MinValue;
									scratchMap[i, j, 1] = float.MaxValue;
									hateMap[i, j, 0] = float.MinValue;
									hateMap[i, j, 1] = float.MaxValue;
								}
				}
			}
		
		
		for (int n = 1; n <= w; n++)
		{
			
			for (int i = 1; i < sourceMap.GetLength(0)-1; i++)
			{
				//EditorUtility.DisplayProgressBar("Outlining", " Topology",(i*1f/res));
				for (int j = 1; j < sourceMap.GetLength(1)-1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						for (int l = -1; l <= 1; l++)
						{
							if (scratchMap[i-1, j-1, 1] >= 1f
								|| scratchMap[i-1, j, 1] >= 1f
								|| scratchMap[i-1, j+1, 1] >= 1f
								|| scratchMap[i+1, j+1, 1] >= 1f
								|| scratchMap[i+1, j, 1] >= 1f
								|| scratchMap[i+1, j-1, 1] >= 1f
								|| scratchMap[i, j+1, 1] >= 1f
								|| scratchMap[i, j-1, 1] >= 1f
								|| scratchMap[i, j, 1] >= 1f)
								{
									splatMap[i, j, 1] = 1f;
									splatMap[i, j, 0] = 0f;
								}
								else
								{
									splatMap[i, j, 1] = 0f;
									splatMap[i, j, 0] = 1f;
								}
						}					
					}
				}
			}
			
			for (int i = 1; i < sourceMap.GetLength(0)-1; i++)
			{
				for (int j = 1; j < sourceMap.GetLength(1)-1; j++)
				{
					if (splatMap[i,j,1] ==1f)
					{
					scratchMap[i, j, 0] = splatMap[i, j, 0];
					scratchMap[i, j, 1] = splatMap[i, j, 1];
					}
				}
			}
			//EditorUtility.ClearProgressBar();
		}
		
		
		for (int m = 0; m < sourceMap.GetLength(0); m++)
		{
			for (int o = 0; o < sourceMap.GetLength(0); o++)
			{
				if (hateMap[m, o, 0] > 0f  ^ scratchMap[m, o, 0] > 0f)
				{
					splatMap[m, o, 0] = float.MaxValue;
					splatMap[m, o, 1] = float.MinValue;
				}
				else
				{
					splatMap[m, o, 0] = float.MinValue;
					splatMap[m, o, 1] = float.MaxValue;
				}
				
			}
		}
		
        TerrainManager.SetLayerData(splatMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
	}

	public static void fillTopology(Layers topology)
	{
		// Extract the topology index from the Layers object
		int topologyIndex = TerrainTopology.TypeToIndex((int)topology.Topologies);

		// Get the current topology layer data
		float[,,] sourceMap = TerrainManager.GetLayerData(LayerType.Topology, topologyIndex);
		int res = sourceMap.GetLength(0);

		// Fill the layer: set active channel (0) to 1 and inactive channel (1) to 0
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				sourceMap[i, j, 0] = 1f; // Fully active
				sourceMap[i, j, 1] = 0f; // Fully inactive
			}
		}

		// Apply the filled layer back to TerrainManager
		TerrainManager.SetLayerData(sourceMap, LayerType.Topology, topologyIndex);
	}
	
	[ConsoleCommand("Fills layer")]
	public static void fillLayer(Layers layerData)
	{
		TerrainManager.SyncTerrainResolutions();
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
		int layerCount = TerrainManager.LayerCount(layerType); // 8 for Ground, 5 for Biome, 2 for Topology

		// Fill the layer: set the specified layer index to fully active, others to inactive
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				for (int k = 0; k < layerCount; k++)
				{
					layerMap[i, j, k] = (k == layerIndex) ? 1f : 0f; // Target layer active, others inactive
				}
			}
		}

		// Apply the filled layer back to TerrainManager
		//use SetLayerData from now on
		TerrainManager.SetSplatMap(layerMap, layerType, layerIndex);
	}

	[ConsoleCommand("erase overlapping topology")]
	public static void notTopologyLayer(Layers layer, Layers sourceLayer)
	{
		float[,,] sourceMap = TerrainManager.GetLayerData(LayerType.Topology, TerrainTopology.TypeToIndex((int)sourceLayer.Topologies));
		float[,,] splatMap = TerrainManager.GetLayerData(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		int res = sourceMap.GetLength(0);
		
		for (int m = 0; m < res; m++)
		{
			for (int o = 0; o < res; o++)
			{
				if ((splatMap[m, o, 0] > 0f && sourceMap[m, o, 0] > 0f))
				{
					splatMap[m, o, 0] = float.MinValue;
					splatMap[m, o, 1] = float.MaxValue;
				}
								
			}
		}
		
        TerrainManager.SetLayerData(splatMap, LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		
	}

	[ConsoleCommand("randomwalk splat")]
	public static void splatCrazing(CrazingPreset crazing)
	{
		int z = crazing.zones;
		int a = crazing.minSize;
		int b = crazing.maxSize;		
		int t = crazing.splatLayer;
		
		float[,,] newGround = TerrainManager.Ground;

		
		int s = UnityEngine.Random.Range(a, b);
		int uB = newGround.GetLength(0);
		
		for (int i = 0; i < z; i++)
        {
			//EditorUtility.DisplayProgressBar("Painting", "Mottles",(i*1f/z));
			int x = UnityEngine.Random.Range(1, newGround.GetLength(0));
			int y = UnityEngine.Random.Range(1, newGround.GetLength(0));
            for (int j = 0; j < s; j++)
            {
					x = x + UnityEngine.Random.Range(-1,2);
					y = y + UnityEngine.Random.Range(-1,2);

					if (x <= 1)
						x = 2;
					
					if (y <= 1)
						y = 2;
					
					if (x >= uB)
						x = uB-1;
					
					if (y >= uB)
						y = uB-1;
						
					
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 0;
					newGround[x, y, 2] = 0;
					newGround[x, y, 3] = 0;
					newGround[x, y, 4] = 0;
					newGround[x, y, 5] = 0;
					newGround[x, y, 6] = 0;
					newGround[x, y, 7] = 0;
					newGround[x, y, t] = 1;								
				
            }
        }
		//EditorUtility.ClearProgressBar();
		TerrainManager.SetLayerData(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
	
	[ConsoleCommand("dither fill biome layer")]
	public static void DitherFillBiome(int t)
	{
		
		float[,,] newGround = TerrainManager.Biome;
		int dim = newGround.GetLength(0);
		
		for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
				newGround[x, y, 0] = 1;
				newGround[x, y, 1] = 0;
				newGround[x, y, 2] = 0;
				newGround[x, y, 3] = 0;
				
				if(Mathf.PerlinNoise(x*1f/5f*t,y*1f/5f*t) > .666f)
				{
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 1;
					newGround[x, y, 2] = 0;
					newGround[x, y, 3] = 0;
				}
				else if(Mathf.PerlinNoise(x*1f/5f*t,y*1f/5f*t) > .444f)
				{
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 0;
					newGround[x, y, 2] = 1;
					newGround[x, y, 3] = 0;
				}
				
			}
		}
		
		TerrainManager.SetLayerData(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
	
	[ConsoleCommand("dither fill splat layer")]
	public static void splatDitherFill(int t)
	{
		
		float[,,] newGround = TerrainManager.Ground;
		int dim = newGround.GetLength(0);
		
		for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
				if (x%2==0 && y%2 ==0)
				{
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 0;
					newGround[x, y, 2] = 0;
					newGround[x, y, 3] = 0;
					newGround[x, y, 4] = 0;
					newGround[x, y, 5] = 0;
					newGround[x, y, 6] = 0;
					newGround[x, y, 7] = 0;
					newGround[x, y, t] = 1;	
				}
			}
		}
		
		TerrainManager.SetLayerData(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}

	public static void pasteMonument(WorldSerialization blob, int x, int y, float zOffset)
	{
		
		//EditorUtility.DisplayProgressBar("reeeLoading", "Monument File", .75f);
		WorldConverter.MapInfo terrains = WorldConverter.WorldToTerrain(blob);
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		var terrainPosition = 0.5f * terrains.size;
		
		float[,] pasteMap = terrains.land.heights;
		float[,] pasteWater = terrains.water.heights;
		float[,,] pSplat = terrains.splatMap;
		float[,,] pBiome = terrains.biomeMap;
		bool[,] pAlpha = terrains.alphaMap;
		
		TerrainMap<int> pTopoMap = terrains.topology;
		TerrainMap<int> topTerrainMap = TopologyData.GetTerrainMap();
		
		land.transform.position = terrainPosition;
        float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		float ratio = terrains.size.x / (baseMap.GetLength(0));
		
		
		int dim=pSplat.GetLength(0)-4;
		int heightmapDim = baseMap.GetLength(0)-4;
		float ratioMaps = 1f * heightmapDim / dim;
		
		
		float x1 = x/2f;
		float y1 = y/2f;
		x=(int)(x/ratio);
		y=(int)(y/ratio);
		
		float[,,] newGround = TerrainManager.Ground;
		float[,,] newBiome = TerrainManager.Biome;
		bool[,] newAlpha = TerrainManager.Alpha;
		
		float[][,,] topologyArray  = TerrainManager.Topology;
		float[][,,] pTopologyArray =  new float[30][,,];
		
		int res=newGround.GetLength(0)-4;
		int splatMapsX=0;
		int splatMapsY=0;
		float ratioMaps2 = 1f * res / dim;
		
		pTopologyArray = TopomapToArray(pTopoMap,dim);
			
		
		for (int i = 0; i < heightmapDim; i++)
		{
			//EditorUtility.DisplayProgressBar("Loading", "Heightmap", (i*1f/heightmapDim));
			for (int j = 0; j < heightmapDim; j++)
			{
				splatMapsX = (int)(1f* i / ratioMaps);
				splatMapsY = (int)(1f* j / ratioMaps);
				splatMapsX = (int)Mathf.Clamp(splatMapsX, 0f, heightmapDim *1f);
				splatMapsY = (int)Mathf.Clamp(splatMapsY, 0f, heightmapDim *1f);
				baseMap[i + x, j + y] = Mathf.Lerp(baseMap[i+x, j+y], pasteMap[i,j]+zOffset, pBiome[splatMapsX,splatMapsY,0]);
			}
		}
		Debug.LogError("No nulls 1");
		//EditorUtility.ClearProgressBar();
		for (int i = 0; i < res; i++)
		{
			//EditorUtility.DisplayProgressBar("Loading", "Monument Layers", (i*1f/res));
			for (int j = 0; j < res; j++)
			{
				splatMapsX = (int)(1f* i / ratioMaps2);
				splatMapsY = (int)(1f* j / ratioMaps2);
				splatMapsX = (int)Mathf.Clamp(splatMapsX, 0f, heightmapDim *1f);
				splatMapsY = (int)Mathf.Clamp(splatMapsY, 0f, heightmapDim *1f);
				if(i+x  < newGround.GetLength(0) && j+y < newGround.GetLength(0))
				{
					for (int k = 0; k < 8; k++)
					{
						newGround[i + x, j + y, k] = Mathf.Lerp(newGround[i+x,j+y,k], pSplat[splatMapsX,splatMapsY,k], pBiome[splatMapsX,splatMapsY,0]);
					}
					
					if (pBiome[splatMapsX,splatMapsY,0] > .5f)
					{
						for(int k = 0; k < TerrainTopology.COUNT; k++)
						{
							topologyArray[k][i + x, j + y,0] = pTopologyArray[k][splatMapsX, splatMapsY,0];
							topologyArray[k][i + x, j + y,1] = pTopologyArray[k][splatMapsX, splatMapsY,1];
						}
						
						
					}
				}
			}
			
			
        }
		
		//EditorUtility.ClearProgressBar();
		land.terrainData.SetHeights(0,0,baseMap);
		TerrainManager.SetSplatMap(newGround, LayerType.Ground, 0);
		TerrainManager.SetSplatMap(newBiome, LayerType.Biome, 0);
        TerrainManager.SetAlphaMap(newAlpha);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
		
		
		for (int i = 0; i < TerrainTopology.COUNT; i++)
        {
			TerrainManager.SetSplatMap(topologyArray[i], LayerType.Topology, i);
        }
		
		Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		GameObject defaultObj = Resources.Load<GameObject>("Prefabs/DefaultPrefab");
        
		
		//the math here is wrong i believe, probably best to rewrite this entire crap anyway
		for (int i = 0; i < terrains.prefabData.Length; i++)
        {
			terrains.prefabData[i].position.x = terrains.prefabData[i].position.x+y1*2f;
			terrains.prefabData[i].position.z = terrains.prefabData[i].position.z+x1*2f;
			terrains.prefabData[i].position.y = terrains.prefabData[i].position.y + zOffset*1000f;
        }
		PrefabManager.SpawnPrefabs(terrains.prefabData, 0, prefabsParent);
		
		Transform PathParent = GameObject.FindGameObjectWithTag("Paths").transform;
		//int progressID = Progress.Start("Load: " + "", "Preparing Map", Progress.Options.Sticky);
		//int spwPath = Progress.Start("Paths", null, Progress.Options.Sticky, progressID);
		PathManager.SpawnPaths(terrains.pathData,0);
		/*
		Debug.LogError(terrains.pathData.Length);
        for (int i = 0; i < terrains.pathData.Length; i++)
			
        {
            Vector3 averageLocation = Vector3.zero;
            for (int j = 0; j < terrains.pathData[i].nodes.Length; j++)
            {
				
                averageLocation += terrains.pathData[i].nodes[j];
				terrains.pathData[i].nodes[j].x = terrains.pathData[i].nodes[j].x + y1*2f;
				terrains.pathData[i].nodes[j].z = terrains.pathData[i].nodes[j].z + x1*2f;
				terrains.pathData[i].nodes[j].y = terrains.pathData[i].nodes[j].y + zOffset*1000f;
				
            }
            
			averageLocation /= terrains.pathData[i].nodes.Length;
            
			GameObject newObject = GameObject.Instantiate(pathObj, averageLocation + terrainPosition, Quaternion.identity, pathsParent);
            newObject.GetComponent<PathDataHolder>().pathData = terrains.pathData[i];
        }
		*/
		
	}
	
	[ConsoleCommand("create 300 randomly generated buildings")]
	public static void rustBuildings()
	{
		int buildings = 300;
		int dim = (int)Mathf.Sqrt(buildings);
		int maxWidth = 4;
		int maxBreadth = 3;
		int maxHeight = 5;
		
		int start = -1000;
		int buildingSize = Math.Max(maxWidth,maxBreadth)*6+18;
		
		for (int i = 0; i < dim; i++)
		{
			for (int j = 0; j < dim; j++)
			{
				createRustBuilding(i*buildingSize+start,j*buildingSize+start, UnityEngine.Random.Range(1,maxWidth +1),UnityEngine.Random.Range(1,maxBreadth +1),UnityEngine.Random.Range(1,maxHeight +1));
			}
		}
	}
	
	
	public static void createRustBuilding(int x, int y, int width, int breadth, int tallest)
	{
		uint industrialglassnt = 2600790831;
		uint yellow = 2337881356;
		uint sewerCorner = 2032918088;
		int glassScale = 6;
		float z = 9.36f;
		//float yRotation;
		//int height = 0;

		int tallness = 0;
		Vector3 position = new Vector3(0f,0f,0f);
		Vector3 rotation = new Vector3(0f,0f,0f);
		Vector3 containerRotation = new Vector3(0f,0f,0f);
		Vector3 scale = new Vector3(glassScale,glassScale,glassScale);
		
		Vector3 containerScale = new Vector3(.689f, .510f, .675f);
		Vector3 sewerScale = new Vector3(1f,1f,1f);
		Vector3 sewerRotation = new Vector3(0f,0f,0f);
		Vector3 sewerRotation2 = new Vector3(0f,180f,0f);
		float containerYoffset = 1.511f;
		float containerZoffset = 2.03f;
		float foundationOffset = 2.9f;
		//PrefabManager.createPrefab("Decor", foliage, foliageLocation, foliageRotation, foliageScale);
		
		
		for (int i = 0; i < width; i++)
		{
			for(int j = 0; j < breadth; j++)
			{
				tallness = UnityEngine.Random.Range(0, tallest +1);
				for(int k = 0; k<tallness; k++)
				{
					rotation.y = UnityEngine.Random.Range(0, 4) * 90f;
					position.x = i*glassScale+y;
					position.z = j*glassScale+x;
					position.y = k*glassScale + z;
					PrefabManager.createPrefab("Decor", industrialglassnt, position, rotation, scale);
					position.y = position.y + containerYoffset;
					PrefabManager.createPrefab("Decor", yellow, position, containerRotation, containerScale);
					position.z = position.z + containerZoffset;
					PrefabManager.createPrefab("Decor", yellow, position, containerRotation, containerScale);
					position.z = position.z - (containerZoffset * 2f);
					PrefabManager.createPrefab("Decor", yellow, position, containerRotation, containerScale);
					if (k==0)
					{					
					position.x = i*glassScale+y-foundationOffset;
					position.z = j*glassScale+x-foundationOffset;
					position.y = k*glassScale+z-foundationOffset;
					
					PrefabManager.createPrefab("Decor", sewerCorner, position, sewerRotation2, sewerScale);
					
					position.x = i*glassScale+y+foundationOffset;
					position.z = j*glassScale+x+foundationOffset;
					position.y = k*glassScale+z-foundationOffset;
					
					PrefabManager.createPrefab("Decor", sewerCorner, position, sewerRotation, sewerScale);
					
					
					}
				}
			}
		}
		
		
	}
	
	public static void createRustCity(WorldSerialization blob, RustCityPreset city)
	{
		WorldConverter.MapInfo terrains = WorldConverter.WorldToTerrain(blob);
		monumentData [] monuments = monumentLocations(terrains.biomeMap);
		int lane = city.street;
		int height = city.alley;
		int dim = city.size;
		int start = city.start;
		int x = start;
		int y = start;
		city.x = x;
		city.y = y;
		int k = 0;
		int buildings = monumentDataLength(monuments);
		
		
		//EditorUtility.DisplayProgressBar("Generating", "building: " + k, ((y*x*1f)/(dim*dim)));
		while (y < start + dim)
		{
			
			while (x < start + dim)
			{
				
				k = UnityEngine.Random.Range(0,buildings);
				city.x = x;
				city.y = y;
				RustCity(terrains, monuments[k], city);
				x+= (monuments[k].width + lane);
			}
			y += height;
			x = start; 
		}
		//EditorUtility.ClearProgressBar();
		
	}
	
	public static float[][,,] TopomapToArray(TerrainMap<int> pTopoMap, int res)
	{
	float[][,,] pTopologyArray =  new float[TerrainTopology.COUNT][,,];
	
				for(int k = 0; k < TerrainTopology.COUNT;k++)
				{
					pTopologyArray[k] = new float[res,res,2];
				}
		
		for (int i = 0; i < res; i++)
		{
			for (int j = 0; j < res; j++)
			{
				for(int k = 0; k < TerrainTopology.COUNT;k++)
				{
					
					if((pTopoMap[i,j] & TerrainTopology.IndexToType(k)) != 0)
					{
						pTopologyArray[k][i,j,0] = 1f;
						pTopologyArray[k][i,j,1] = 0f;
					}
					else
					{
						pTopologyArray[k][i,j,1] = 1f;
						pTopologyArray[k][i,j,0] = 0f;
					}
				}
			}
		}
		
	return pTopologyArray;
	}
	
	public static void RustCity(WorldConverter.MapInfo terrains, monumentData monument, RustCityPreset city)
	{
		int x = city.x;
		int y = city.y;
		float zOff = city.zOff;
		float steepness = city.flatness;
		float zOffset=0;
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		var terrainPosition = 0.5f * terrains.size;
		
		float[,] pasteMap = terrains.land.heights;
		float[,] pasteWater = terrains.water.heights;
		float[,,] pSplat = terrains.splatMap;
		float[,,] pBiome = terrains.biomeMap;
		bool[,] pAlpha = terrains.alphaMap;
		
		int res = pasteMap.GetLength(0);
		int splatRes = pSplat.GetLength(0);
		
		TerrainMap<int> pTopoMap = terrains.topology;
		float[][,,] pTopologyArray =  new float[TerrainTopology.COUNT][,,];
		pTopologyArray = TopomapToArray(pTopoMap, splatRes);
		float[][,,] topologyArray  = TerrainManager.Topology;
		
		land.transform.position = terrainPosition;
        float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		float ratio = terrains.size.x / (baseMap.GetLength(0));
		
		int dim=baseMap.GetLength(0)-4;
		
		int monumentX = monument.x;
		int monumentY = monument.y;
		int width = monument.width;
		int height = monument.height;
		

		
		float[,,] newGround = TerrainManager.GetSplatMap(LayerType.Ground, 0);
		float[,,] newBiome = TerrainManager.GetSplatMap(LayerType.Biome, 0);
		bool[,] newAlpha = TerrainManager.Alpha;
		
		
		
		float splatRatio = terrains.size.x / pBiome.GetLength(0);
		
		float mapSplatRatio = splatRes / newBiome.GetLength(0);
		
		
		
		//EditorUtility.ClearProgressBar();
		
		float ratioMaps = 1f * res / splatRes;
		int heightmapDim = baseMap.GetLength(0)-4;
		
		float x2 = monumentX*splatRatio;
		float y2 = monumentY*splatRatio;
		float x1 = x*splatRatio;
		float y1 = y*splatRatio;
		//x=(int)(x/ratio);
		//y=(int)(y/ratio);
		
		float sum = 0;
		float sum1= 0;
		int count = 0;
		int xCheck = 0;
		int yCheck = 0;
		
		float maxZ = 0;
		float minZ = 1;
		float zDiff = 0;

		int heightMapsX = 0;
		int heightMapsY = 0;
		int heightMapsX1 = 0;
		int heightMapsY1 = 0;
		//int biomeMaskX = 0;
		//int biomeMaskY = 0;

		
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				if (pBiome[i,j,1] > 0f)
				{
				heightMapsX = (int)(1f*(i+x) * ratioMaps);
				heightMapsY = (int)(1f*(j+y) * ratioMaps);
				heightMapsX1 = (int)(1f*(i+monumentX) * ratioMaps);
				heightMapsY1 = (int)(1f*(j+monumentY) * ratioMaps);

				count++;
				maxZ = Math.Max(maxZ, baseMap[heightMapsX,heightMapsY]);
				minZ = Math.Min(minZ, baseMap[heightMapsX,heightMapsY]);
				sum += pasteMap[heightMapsX1,heightMapsY1];
				sum1 += baseMap[heightMapsX,heightMapsY];
				}
			}
		}
		
		zDiff = maxZ-minZ;
		zOffset = (sum1/count)-(sum/count)+(.5f*zDiff) + zOff;

		if (zDiff<steepness)
		{
			for (int i = 0; i < width; i++)
					{
						
						
						//EditorUtility.DisplayProgressBar("Loading", "Heightmap", (i*1f/heightmapDim));
						
						for (int j = 0; j < height; j++)
						{
							/*
							heightMapsX = (int)(1f*i+(x * ratioMaps));
							heightMapsY = (int)(1f*j+(y * ratioMaps));
							
							heightMapsX1 = (int)(1f*i+(monumentX * ratioMaps));
							heightMapsY1 = (int)(1f*j+(monumentY * ratioMaps));
							biomeMaskX = (int)(1f*(i/ ratioMaps)+monumentX);
							biomeMaskY = (int)(1f*(j/ ratioMaps)+monumentY);
							*/
							baseMap[i+x, j+y] = Mathf.Lerp(baseMap[i+x, j+y], pasteMap[i+monumentX,j+monumentY]+zOffset, pBiome[i+monumentX,j+monumentY,0]);
						}
					}
					
					//width = (int)(width / ratioMaps);
					//height = (int)(height / ratioMaps);
					
					//EditorUtility.ClearProgressBar();
					for (int i = 0; i < (int)(width/mapSplatRatio); i++)
					{
						//EditorUtility.DisplayProgressBar("Loading", "Monument Layers", (i*1f/dim));
						for (int j = 0; j < (int)(height/mapSplatRatio); j++)
						{
							
							
							for (int k = 0; k < 8; k++)
							{
								newGround[i + (int)(x/mapSplatRatio), j + (int)(y/mapSplatRatio), k] = Mathf.Lerp(newGround[i + (int)(x/mapSplatRatio), j + (int)(y/mapSplatRatio), k], pSplat[(int)(i*mapSplatRatio)+monumentX,(int)(j*mapSplatRatio)+monumentY,k], pBiome[(int)(i*mapSplatRatio)+monumentX,(int)(j*mapSplatRatio)+monumentY,0]);
							}
							
							if (pBiome[i,j,0] > 0f)
							{
								for(int k = 0; k < TerrainTopology.COUNT; k++)
								{
									topologyArray[k][i + (int)(x/mapSplatRatio), j + (int)(y/mapSplatRatio),0] = pTopologyArray[k][(int)(i*mapSplatRatio)+monumentX, (int)(j*mapSplatRatio)+monumentY,0];
									topologyArray[k][i + (int)(x/mapSplatRatio), j + (int)(y/mapSplatRatio),1] = pTopologyArray[k][(int)(i*mapSplatRatio)+monumentX, (int)(j*mapSplatRatio)+monumentY,1];
								}
								
								//newAlpha[i + x, j + y] = pAlpha[i+monumentX, j+monumentY];
							}
							
						}
						
						
					}
				
						
				land.terrainData.SetHeights(0,0,baseMap);

				Debug.LogError(topologyArray[0].GetLength(0)+" topology length");
				SetSplatMap(newGround, LayerType.Ground, 0);
				
				//TerrainManager.SetSplatMap(newBiome, LayerType.Biome, 0);
				//TerrainManager.SetSplatMap(newAlpha, LayerType.Alpha);
				
				for (int i = 0; i < TerrainTopology.COUNT; i++)
				{
					TerrainManager.SetSplatMap(topologyArray[i], LayerType.Topology, i);
				}
				//SetLayer(TerrainManager.CurrentLayerType, 0);
				
				Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
				GameObject defaultObj = Resources.Load<GameObject>("Prefabs/DefaultPrefab");
				
				int prefabcounter=0;
				uint id = 0;

				uint industrialglassnt = 1048750230;

				
				Vector3 holderPosition = new Vector3(0,0,0);
				
				int palette = UnityEngine.Random.Range(0,9);
				
				for (int i = 0; i < terrains.prefabData.Length; i++)
				{
					xCheck = (int)((terrains.prefabData[i].position.z/splatRatio)+res/2f);
					yCheck = (int)((terrains.prefabData[i].position.x/splatRatio)+res/2f);
					
					
					if( xCheck > monumentX && xCheck < monumentX+width && yCheck > monumentY && yCheck < monumentY+height )
					{
							id = terrains.prefabData[i].id;
							
							holderPosition.x = terrains.prefabData[i].position.x+y1-y2;
							holderPosition.z = terrains.prefabData[i].position.z+x1-x2;
							holderPosition.y = terrains.prefabData[i].position.y + zOffset*1000f;
							id = PrefabManager.ScrambleContainer(terrains.prefabData[i].id, palette);
							
							if(terrains.prefabData[i].id == industrialglassnt)
							{	
								PrefabManager.FoliageCube(holderPosition, terrains.prefabData[i].scale);
							}

							PrefabManager.createPrefab(terrains.prefabData[i].category, id, holderPosition, terrains.prefabData[i].rotation, terrains.prefabData[i].scale);
							prefabcounter++;
					}
					
					
				}
				/*
				Transform pathsParent = GameObject.FindGameObjectWithTag("Paths").transform;
				GameObject pathObj = Resources.Load<GameObject>("Paths/Path");
				for (int i = 0; i < terrains.pathData.Length; i++)
				{
					Vector3 averageLocation = Vector3.zero;
					for (int j = 0; j < terrains.pathData[i].nodes.Length; j++)
					{
						averageLocation += terrains.pathData[i].nodes[j];
						terrains.pathData[i].nodes[j].x = terrains.pathData[i].nodes[j].x + y1*2f;
						terrains.pathData[i].nodes[j].z = terrains.pathData[i].nodes[j].z + x1*2f;
						terrains.pathData[i].nodes[j].y = terrains.pathData[i].nodes[j].y + zOffset*1000f;
						
					}
					
					averageLocation /= terrains.pathData[i].nodes.Length;
					
					GameObject newObject = GameObject.Instantiate(pathObj, averageLocation + terrainPosition, Quaternion.identity, pathsParent);
					newObject.GetComponent<PathDataHolder>().pathData = terrains.pathData[i];
				}
				*/
		}
	}
	
	public static float[,] Dithering(float[,] array)
	{
		float[,] cliffMap = array;
		float oldPixel, newPixel, slopeDiff, randomizer, quotient;

			for (int i = 2; i < array.GetLength(0)-2; i++)
			{
				//EditorUtility.DisplayProgressBar("Dithering", "Cliff Map",(i*1f/TerrainManager.HeightMapRes));
				for (int j = 2; j < array.GetLength(1)-2; j++)
				{
					
					oldPixel = cliffMap[i,j];
					
					if (cliffMap[i,j] > 0f)
						{
							newPixel = 1f;
						}
					else
						{
							newPixel = 0f;
						}
						
					cliffMap[i,j] = newPixel;
					slopeDiff = (oldPixel-newPixel);					
					
					randomizer = UnityEngine.Random.Range(0f,1f);
					quotient = 42f;
					
					cliffMap[i+1,j] = cliffMap[i+1,j] + (slopeDiff * 8f * randomizer / quotient);
					cliffMap[i-1,j+1] = cliffMap[i-1,j+1] + (slopeDiff * 4f * randomizer /quotient);
					cliffMap[i,j+1] = cliffMap[i,j+1] + (slopeDiff * 8f * randomizer / quotient);
					cliffMap[i+1,j+1] = cliffMap[i+1,j+1] + (slopeDiff * 4f * randomizer / quotient);

					randomizer = UnityEngine.Random.Range(1f,1.5f);
					
					cliffMap[i+2,j] = cliffMap[i+2,j] + (slopeDiff * 4f * randomizer / quotient);
					cliffMap[i-2,j+1] = cliffMap[i-2,j+1] + (slopeDiff * 2f * randomizer / quotient);
					cliffMap[i,j+2] = cliffMap[i,j+2] + (slopeDiff * 4f * randomizer / quotient);
					cliffMap[i+2,j+1] = cliffMap[i+2,j+1] + (slopeDiff * 2f * randomizer / quotient);
					
					randomizer = UnityEngine.Random.Range(1f,2f);
					
					cliffMap[i+1,j+2] = cliffMap[i+1,j+2] + (slopeDiff * 2f * randomizer / quotient);
					cliffMap[i-2,j+2] = cliffMap[i-2,j+2] + (slopeDiff * 1f * randomizer /quotient);
					cliffMap[i-1,j+2] = cliffMap[i-1,j+2] + (slopeDiff * 2f * randomizer / quotient);
					cliffMap[i+2,j+2] = cliffMap[i+2,j+2] + (slopeDiff * 1f * randomizer / quotient);
					
				}
			}
		
		//EditorUtility.ClearProgressBar();
		return cliffMap;
		
	}
	/*
	public static bool spawnGeology(int i, int j, float balance, int density)
	{
		int odds = (int)(GaussCurve(TerrainManager.CliffMap[i,j],balance,.6f)*density);
		return (odds<=(.332f*density-2f) && (UnityEngine.Random.Range(0,odds))==0);
		
	}
	*/
	public static List<GeologyItem> OddsList(List<GeologyItem> geologyItems)
	{
		List<GeologyItem> oddsList = new List<GeologyItem>();
									foreach (GeologyItem geoItem in geologyItems)
									{
										for(int p = 0; p < geoItem.emphasis; p++)
										{
											oddsList.Add(geoItem);
										}
									}
									
		return oddsList;
	}
	
	public static float GaussCurve(float x, float mean, float deviance)
	{
		float coefficient = 1f / (float)(deviance * Math.Sqrt(2f * Math.PI));
        float exponent = -0.5f * (float)Math.Pow((x - mean) / deviance, 2f);
        float result = coefficient * (float)Math.Exp(exponent);
        return result;
		//the journey ends here
	}
	
	

	public static class RandomTS
	{
		private static System.Random _global = new System.Random();
		[ThreadStatic]
		private static System.Random _local;

		private static System.Random GetLocalRandom()
		{
			if (_local == null)
			{
				int seed;
				lock (_global)
				{
					seed = _global.Next();
				}
				_local = new System.Random(seed);
			}
			return _local;
		}

		public static int Next(int max)
		{
			if (max <= 0) return 0;
			return GetLocalRandom().Next() % max;
		}

		public static double NextDouble()
		{
			return GetLocalRandom().NextDouble();
		}

		public static int GetGlobalSeed()
		{
			lock (_global)
			{
				return _global.Next();
			}
		}

		public static void SetGlobalSeed(int seed)
		{
			lock (_global)
			{
				_global = new System.Random(seed); // Reassign _global with new seed
			}
		}
	}


	
	
	public static void spawnCustom(GeologyItem geoItem, Vector3 position, Vector3 rotation, Vector3 scale, Transform parent)
	{
		// Check if geoItem or its customPrefab is null
		if (geoItem == null || string.IsNullOrEmpty(geoItem.customPrefab))
		{
			Debug.LogError("GeologyItem or its customPrefab is null or empty.");
			return;
		}
		string prefabPath = Path.Combine(SettingsManager.AppDataPath(), geoItem.customPrefab);;
		
		if(Path.GetExtension(prefabPath).ToLower() != ".monument"){
			prefabPath = Path.Combine(SettingsManager.AppDataPath(), geoItem.customPrefab + ".prefab");
		}
		else{
			if (System.IO.File.Exists(prefabPath)){
				PrefabManager.placeCustomMonument(prefabPath, position, rotation, scale, parent);
				return;
			}
			Debug.LogError("monument not found");
			return;
		}
		
		// Check if the file exists at the given path
		if (!System.IO.File.Exists(prefabPath))
		{
			Debug.LogError($"Prefab file not found at path: {prefabPath}");
			return;
		}

		// Attempt to place the prefab and log the result
		try
		{
			PrefabManager.placeCustomPrefab(prefabPath, position, rotation, scale, parent);
		}
		catch (System.Exception e)
		{
			Debug.LogError($"Failed to place prefab at {prefabPath}. Error: {e.Message}");
		}
	}
	
	public static void SpawnFeature(GeologyItem item, Vector3 position, Vector3 rotation, Vector3 scale, Transform parent = null)
	{
		if (!item.custom)
		{
			spawnGeoItem(item, position, rotation, scale, parent);
			return;
		}
		else
			spawnCustom(item, position, rotation, scale, parent);
	}
	
	
	public static void spawnItem(GeologyItem geoItem, Transform transItem)
	{
			PrefabManager.createPrefab("Decor", geoItem.prefabID, transItem);
	}
	
	public static void spawnGeoItem(GeologyItem geoItem, Vector3 position, Vector3 rotation, Vector3 scale, Transform parent = null)
	{
			PrefabManager.createPrefab("Decor", geoItem.prefabID, position, rotation, scale, parent);
	}
	
	#if UNITY_EDITOR
	
	public static void insertPrefabCliffs(GeologyPreset geo)
	{
		
	}
	

	public static void MakeCliffMap(GeologyPreset geo, Action onComplete = null)
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.MakeCliffMap(geo, onComplete));
	}
	
	
	public static void MakeCliffs(GeologyPreset geo)
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.MakeCliffs(geo));
	}
	
	public static void ApplyGeologyTemplate()
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.ApplyGeologyTemplate());
	}
	
	public static void ApplyGeologyPreset(GeologyPreset geo)
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.ApplyGeologyPreset(geo));
	}
	
	
	public static void PreviewDither()
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.PreviewDither());
	}
	
	public static void StopCliffs(){
		Debug.LogError("only works in compiled mode");
	}
	
	#else
		//runtime coroutines
		public static void insertPrefabCliffs(GeologyPreset geo)
		{
			//CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.insertPrefabCliffs(geo));
		}
		

		public static void MakeCliffMap(GeologyPreset geo, Action onComplete = null)
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.MakeCliffMap(geo, onComplete));
		}
		
		
		public static void MakeCliffs(GeologyPreset geo)
		{
			Debug.Log("Starting MakeCliffs coroutine...");
			cliffCoroutine = CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.MakeCliffs(geo));
			Debug.Log("MakeCliffs coroutine reference: " + (cliffCoroutine != null ? "Valid" : "Null"));
		}

		public static void StopCliffs()
		{
			Debug.LogError("stop cliffs GM");
			if (Coroutines.makeCliffsRunning){
				if (cliffCoroutine != null)
				{
					CoroutineManager.Instance.StopRuntimeCoroutine(cliffCoroutine);
					cliffCoroutine = null;
					Debug.LogError("stop cliffs GM 2");
					Coroutines.makeCliffsRunning = false;
					GeologyWindow.Instance.Progress(0f);
					Debug.LogError("stop cliffs GM 3");
				}
				else
				{
					Debug.LogWarning("Attempted to stop a null coroutine.");
				}
			}
		}
		
		public static void ApplyGeologyTemplate()
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.ApplyGeologyTemplate());
		}
		
		public static void ApplyGeologyPreset(GeologyPreset geo)
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.ApplyGeologyPreset(geo));
		}
		
		public static void PreviewDither()
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.PreviewDither());
		}
	#endif
		
		
	private static class Coroutines
    {

		private static bool makeCliffMapRunning = false;
		public static bool makeCliffsRunning = false;

		public static IEnumerator ApplyGeologyTemplate()
		{
			List<GeologyPreset> geologyPresets = new List<GeologyPreset>();

		foreach (string filename in SettingsManager.macro)
        {
			GeologyPreset preset = LoadGeologyPreset(filename); 
			if (!preset.Equals(default(GeologyPreset)))
			{
				geologyPresets.Add(preset);
			}
			else
			{
				Debug.LogError($"Failed to load geology preset from file: {filename}");
			}
        }



			foreach (GeologyPreset pre in geologyPresets)
			{
					
					makeCliffMapRunning = true;
					GenerativeManager.MakeCliffMap(pre);
					yield return new WaitUntil(() => !makeCliffMapRunning && !makeCliffsRunning);
					

					makeCliffsRunning = true;
					GenerativeManager.MakeCliffs(pre);
					yield return new WaitUntil(() => !makeCliffsRunning && !makeCliffMapRunning);
					
					
			}
			
			if( GeologyWindow.Instance!= null){
				
				GeologyWindow.Instance.StopGeneration.interactable = true;
			}
		}
		
		public static IEnumerator ApplyGeologyPreset(GeologyPreset geo)
		{
			
				makeCliffMapRunning = true;
				GenerativeManager.MakeCliffMap(geo);
				yield return new WaitUntil(() => !makeCliffMapRunning && !makeCliffsRunning);
				
				makeCliffsRunning = true;
				GenerativeManager.MakeCliffs(geo);
				yield return new WaitUntil(() => !makeCliffsRunning && !makeCliffMapRunning);
			
		}
		
		public static IEnumerator PreviewDither()
		{
			float[,] cliffMap = TerrainManager.CliffMap;
			cliffMap = Dithering(cliffMap);
			yield return null;
			
			TerrainManager.SetLandMask(cliffMap);
			yield return null;
		}
		
	public static IEnumerator MakeCliffs(GeologyPreset geo)
	{
		makeCliffsRunning = true;

		Transform parentContainer = GameObject.FindGameObjectWithTag("Prefabs").transform;
		string uniqueName = $"{geo.title}_{UnityEngine.Random.Range(10000, 99999)}";
		Transform parentObject = new GameObject(uniqueName).transform;
		parentObject.tag = "Collection";
		parentObject.SetParent(parentContainer);
		parentObject.position = parentContainer.position;

		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		List<GeologyItem> oddsList = OddsList(geo.geologyItems);

		Vector3 rotationRange1 = geo.rotationsLow;
		Vector3 rotationRange2 = geo.rotationsHigh;
		Vector3 scaleRange1 = geo.scalesLow;
		Vector3 scaleRange2 = geo.scalesHigh;

		int res = TerrainManager.HeightMapRes;
		int splatRes = TerrainManager.SpawnMap.GetLength(0);
		float resRatio = 1f * res / splatRes;

		int count = 0;
		int cullcount = 0;
		bool testing = geo.cliffTest;

		float size = land.terrainData.size.x;
		float sizeZ = land.terrainData.size.y;
		float ratio = size / splatRes;

		bool prefabCollisions = geo.geologyCollisions.Exists(collision => collision.layer == ColliderLayer.Prefabs);
		if (prefabCollisions) Debug.Log("Testing Prefab Collision, Generating may take longer than normal");

		float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);		
		
		GeologyWindow.Instance.Progress(0f);

    for (int i = 2; i < splatRes - 2; i++)
    {
        for (int j = 2; j < splatRes - 2; j++)
        {
            if (!TerrainManager.SpawnMap[i, j]) continue;

            int flipX = geo.flipping ? UnityEngine.Random.Range(0, 2) * 180 : 0;
            int flipZ = geo.flipping ? UnityEngine.Random.Range(0, 2) * 180 : 0;
            float geology = geo.tilting ? Mathf.PerlinNoise(i * 0.0125f, j * 0.0125f) * 20 : 0f;

            int heightMapsX = Mathf.Clamp((int)(i * resRatio), 0, res - 1);
            int heightMapsY = Mathf.Clamp((int)(j * resRatio), 0, res - 1);
            float height = baseMap[heightMapsX, heightMapsY];

            Vector3 normal = land.terrainData.GetInterpolatedNormal((float)j / splatRes, (float)i / splatRes).normalized;
            Vector3 prePosition = new Vector3((j * ratio) - (size / 2f), (height * sizeZ) - (sizeZ * 0.5f), (i * ratio) - (size / 2f));

            Vector3 offsetPerpendicular = Vector3.Cross(normal, Vector3.up);
            Vector3 antiNormal = Vector3.Cross(normal, offsetPerpendicular);

            Vector3 position = prePosition
                               + antiNormal * UnityEngine.Random.Range(geo.jitterLow.z, geo.jitterHigh.z)
                               + normal * UnityEngine.Random.Range(geo.jitterLow.x, geo.jitterHigh.x)
                               + offsetPerpendicular * UnityEngine.Random.Range(geo.jitterLow.y, geo.jitterHigh.y);

            Vector3 rRotate = new Vector3(
                UnityEngine.Random.Range(rotationRange1.x, rotationRange2.x) + geology + flipX,
                UnityEngine.Random.Range(rotationRange1.y, rotationRange2.y),
                UnityEngine.Random.Range(rotationRange1.z, rotationRange2.z) + flipZ
            );

            Vector3 rScale = new Vector3(
                UnityEngine.Random.Range(scaleRange1.x, scaleRange2.x),
                UnityEngine.Random.Range(scaleRange1.y, scaleRange2.y),
                UnityEngine.Random.Range(scaleRange1.z, scaleRange2.z)
            );

            AdjustRotationForNormalization(ref rRotate, geo, normal, land, i, j, splatRes);

            // Select the feature (GeologyItem) before collision testing
            int selection = UnityEngine.Random.Range(0, oddsList.Count);
            GeologyItem selectedItem = oddsList[selection];

            bool placeFeature = true;

            // Handle cliffTest (ray testing) with dynamic ray data loading
            if (testing)
            {
                // Determine the prefab path based on GeologyItem
                string prefabPath;
                if (selectedItem.custom && !string.IsNullOrEmpty(selectedItem.customPrefab))
                {
                    // Use custom prefab path if available
                    prefabPath = selectedItem.customPrefab;
                }
                else
                {
                    // Use prefabID as filename (e.g., "123456789.prefab")
                    prefabPath = $"{selectedItem.prefabID}.prefab";
                }

                // Load ray data for the selected prefab
                List<PrefabData> rayList = GetRayDataFromPrefab(prefabPath);

                // Perform terrain collision test
                if (!PrefabManager.inTerrain(new PrefabData("f", 261440689, position, Quaternion.Euler(rRotate), rScale), rayList))
                {
                    placeFeature = false;
                }
            }

            // Check other collision conditions
            foreach (GeologyCollisions collision in geo.geologyCollisions)
            {
                bool sphere = PrefabManager.sphereCollision(position, collision.radius, (int)collision.layer);
                if (collision.minMax == sphere)
                {
                    placeFeature = false;
                    break;
                }
            }

            // Place the feature if all conditions are met
            if (placeFeature)
            {
                SpawnFeature(selectedItem, position, rRotate, rScale, parentObject);
                count++;
                if (prefabCollisions) yield return null;
            }
            else
            {
                cullcount++;
            }

            if (GeologyWindow.Instance != null)
            {
                GeologyWindow.Instance.footer.text = geo.title + ": " + GeologySpawns + " spawns, " + cullcount + " excluded, " + count + " items placed";
                GeologyWindow.Instance.Progress((1f * count + cullcount) / (1f * GeologySpawns));
            }

            yield return null;
        }
    }

		Debug.LogError($"Geology Complete: {count} Features Placed.");
		if (cullcount > 0) Debug.LogError($"{cullcount} prefabs culled");

		makeCliffsRunning = false;
		
		PrefabManager.NotifyItemsChanged();

		
	}

	public static List<PrefabData> GetRayDataFromPrefab(string prefabPath)
	{
		// Initialize cache if null (defensive programming)
		if (rayDataCache == null)
		{
			rayDataCache = new Dictionary<string, List<PrefabData>>();
			Debug.LogWarning("rayDataCache was null, initialized new instance.");
		}

		// Construct the full path to the prefab file
		string fullPath = Path.Combine(SettingsManager.AppDataPath(), "Presets/Geology/", prefabPath);
		Debug.Log($"Attempting to load REPrefab file: {fullPath}");

		// Check if data is already cached
		if (rayDataCache.ContainsKey(fullPath))
		{
			Debug.Log($"Returning cached ray data for: {fullPath} with {rayDataCache[fullPath].Count} entries");
			return rayDataCache[fullPath];
		}

		// Initialize an empty list for this path
		List<PrefabData> rayDataList = new List<PrefabData>();

		// Check if the file exists
		if (!File.Exists(fullPath))
		{
			Debug.LogError($"Raycasting prefab not found at: {fullPath}");
			rayDataCache[fullPath] = rayDataList;
			return rayDataList;
		}

		try
		{
			// Load the prefab using WorldSerialization
			Debug.Log("Initializing WorldSerialization...");
			var world = new WorldSerialization();
			if (world == null)
			{
				Debug.LogError("Failed to create WorldSerialization instance.");
				rayDataCache[fullPath] = rayDataList;
				return rayDataList;
			}

			Debug.Log($"Loading REPrefab file: {fullPath}");
			world.LoadREPrefab(fullPath);

			// Convert to MapInfo to extract PrefabData using WorldToREPrefab
			Debug.Log("Converting WorldSerialization to REPrefab MapInfo...");
			WorldConverter.MapInfo mapInfo = WorldConverter.WorldToREPrefab(world);
			if (mapInfo.prefabData == null || mapInfo.prefabData.Length == 0)
			{
				Debug.LogError($"Invalid REPrefab file (no prefabData): {fullPath}");
				rayDataCache[fullPath] = rayDataList;
				return rayDataList;
			}

			// Process each PrefabData entry
			Debug.Log($"Processing {mapInfo.prefabData.Length} prefabData entries...");
			foreach (var prefabData in mapInfo.prefabData)
			{
				if (prefabData == null)
				{
					Debug.LogWarning("Encountered null prefabData entry, skipping...");
					continue;
				}

				if (prefabData.id == 3244004659) // Include only specific ID
				{
					rayDataList.Add(prefabData);
				}
			}

			if (rayDataList.Count == 0)
			{
				Debug.LogWarning($"No ray test prefabs (ID 3244004659) in: {fullPath}");
			}
			else
			{
				Debug.Log($"Loaded {rayDataList.Count} ray test prefabs from: {fullPath}");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Error processing REPrefab file {fullPath}: {e.Message}\nStackTrace: {e.StackTrace}");
			rayDataList = new List<PrefabData>();
		}

		// Cache the result (even if empty) to avoid reloading
		Debug.Log($"Caching ray data for: {fullPath} with {rayDataList.Count} entries");
		rayDataCache[fullPath] = rayDataList;
		return rayDataList;
	}

	public static List<PrefabData> GetRayDataFromTemplate()
	{
		GameObject raytestTemplate = GameObject.FindGameObjectWithTag("raytestTemplate");
		List<PrefabData> rayDataList = new List<PrefabData>();
		Vector3 offset = Vector3.zero;
		bool offsetCaptured = false;


		if (raytestTemplate != null)
		{
			foreach (Transform child in raytestTemplate.transform)
			{
				PrefabDataHolder prefabDataHolder = child.GetComponent<PrefabDataHolder>();

				if (prefabDataHolder != null)
				{
					prefabDataHolder.UpdatePrefabDataWrong();
					PrefabData prefabDataComponent = prefabDataHolder.prefabData;

					if (prefabDataComponent != null)
					{
						if (prefabDataComponent.id != 3244004659)
						{
							if (!offsetCaptured)
							{
								offset = prefabDataComponent.position - new Vector3(26f,16f,0f); //absolute fudge factor
								offsetCaptured = true;
							}
							continue;
						}

						prefabDataComponent.position = prefabDataComponent.position - offset;

						rayDataList.Add(prefabDataComponent);
					}
				}
			}
		}

		return rayDataList;
	}

    public static GeologyPreset LoadGeologyPreset(string filename)
    {
        //string path = SettingsManager.AppDataPath() + $"Presets/Geology/{filename}.preset";
        string path = filename; // Using the provided filename directly
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<GeologyPreset>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading GeologyPreset from {path}: {e.Message}");
                return new GeologyPreset(); // Return default preset on error
            }
        }
        else
        {
            Debug.LogWarning($"Geology preset file not found: {path}");
            return new GeologyPreset(); // Return default preset if file doesn’t exist
        }
    }

private static void AdjustRotationForNormalization(ref Vector3 rRotate, GeologyPreset geo, Vector3 normal, Terrain land, int i, int j, int splatRes)
{
    Quaternion qRotate;
    Vector3 preRotate;

    if (geo.normalizeY)
    {
        normal = land.terrainData.GetInterpolatedNormal((float)j / splatRes, (float)i / splatRes);
        qRotate = Quaternion.LookRotation(normal);
        preRotate = qRotate.eulerAngles;
        rRotate.y += preRotate.y;
    }

    if (geo.normalizeX)
    {
        normal = land.terrainData.GetInterpolatedNormal((float)j / splatRes, (float)i / splatRes);
        qRotate = Quaternion.LookRotation(normal);
        preRotate = qRotate.eulerAngles;
        rRotate.x += preRotate.x;
    }

    if (geo.normalizeZ)
    {
        normal = land.terrainData.GetInterpolatedNormal((float)j / splatRes, (float)i / splatRes);
        qRotate = Quaternion.LookRotation(normal);
        preRotate = qRotate.eulerAngles;
        rRotate.z += preRotate.z;
    }
}


		
	public static IEnumerator MakeCliffMap(GeologyPreset geo, Action onComplete = null)
	{
		makeCliffMapRunning = true;
		TerrainManager.UpdateHeightCache();

		bool avoid = geo.avoidTopo;
		int spawnCount = 0;
		float[,,] biomeMap = TerrainManager.Biome;
		int resRatio = TerrainManager.HeightMapRes / TerrainManager.SplatMapRes;
		float heightMin = geo.heights.heightMin * 1000f;
		float heightMax = geo.heights.heightMax * 1000f;

		float slopeLow = geo.heights.slopeLow;
		float slopeHigh = geo.heights.slopeHigh;
		float curveMin = geo.heights.curveMin;
		float curveMax = geo.heights.curveMax;
		float curveWeight = geo.heights.curveWeight;
		float frequencyInv = 1.0f / geo.frequency;
		float[][,,] topologies = TerrainManager.Topology;

		//float[,] heightOut = new float[TerrainManager.HeightMapRes, TerrainManager.HeightMapRes];
		bool[,] spawnMap = new bool[TerrainManager.HeightMapRes, TerrainManager.HeightMapRes];

		Parallel.For(0, TerrainManager.HeightMapRes - 1, i =>
		{
			int iResRatio = i / resRatio;
			for (int j = 0; j < TerrainManager.HeightMapRes - 1; j++)
			{
				int jResRatio = j / resRatio;
				//heightOut[i, j] = (TerrainManager.Height[i, j] - 2f) * 0.001f;

				if (RandomTS.NextDouble() < frequencyInv)
				{
					float height = TerrainManager.Height[i, j];
					
					if (geo.heightRange && height < heightMin)		{	continue;	}

					if (geo.heightRange && height > heightMax)		{	continue;	}

					float slope = TerrainManager.Slope[i, j] * (geo.density * 0.01f);
					
					if (geo.slopeRange && slope < slopeLow)	{	continue;	}

					if (geo.slopeRange && slope > slopeHigh)	{	continue;	}

					float curve = TerrainManager.Curvature[i, j] * curveWeight;
					
					if (geo.curveRange && curve < curveMin)		{	continue;	}

					if (geo.curveRange && curve > curveMax)
					{
						continue;
					}
					
					if (!geo.avoidTopo)
					{
						bool topologyValid = false;
						// Invert geo.topologies
						Topologies invertedTopologies = (Topologies)~geo.topologies;

						for (int k = 0; k < topologies.Length; k++)
						{
							if (topologies[k][iResRatio, jResRatio, 0] > 0 && 
								(invertedTopologies & (Topologies)(1 << k)) == 0)
							{
								topologyValid = true;
								break;
							}
						}
						if (!topologyValid)
						{ 	
							continue;
						}
					}
					else
					{
						    bool topologyValid = true;
							Topologies invertedTopologies = (Topologies)~geo.topologies;
							
							for (int k = 0; k < topologies.Length; k++)
							{
								if (topologies[k][iResRatio, jResRatio, 0] >= 0.1f && 
									(invertedTopologies & (Topologies)(1 << k)) == 0)
								{
									topologyValid = false;
									break;
								}
							}
							if (!topologyValid) continue;
					}
						
					
					if (biomeMap[iResRatio, jResRatio, 0] >= 0.1f && !geo.arid)		{	continue;	}

					if (biomeMap[iResRatio, jResRatio, 1] >= 0.1f && !geo.temperate)		{	continue;	}
					
					if (biomeMap[iResRatio, jResRatio, 2] >= 0.1f && !geo.tundra)		{	continue;	}

					if (biomeMap[iResRatio, jResRatio, 3] >= 0.1f && !geo.arctic)		{	continue;	}

					//heightOut[i, j] = (height + 4f) * 0.001f;
					spawnMap[i, j] = true;
					Interlocked.Increment(ref spawnCount);
				}
				else
				{
					spawnMap[i, j] = false;
				}
			}
		});

		GeologySpawns = spawnCount;

		if (spawnMap != null)
		{
			TerrainManager.SetCliffMap(spawnMap);
			//TerrainManager.SetLandMask(heightOut);
			TerrainManager.SetLandMask(spawnMap);
		}
		makeCliffMapRunning = false;
		onComplete?.Invoke();
		yield break;
	}

	}
	
}
