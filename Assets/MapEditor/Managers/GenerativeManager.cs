using UnityEngine;

using RustMapEditor.Variables;
using static WorldSerialization;
using static TerrainManager;

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
					
										
					
					if (o > 0f)
					{
					newGround[i,j,t] = Math.Max(o, newGround[i,j,t]);
					
					for (int m = 0; m <=7; m++)
									{
										if (m!=t)
											newGround[i,j,m] *= 1f-o;
										
									}
					
					
						
					}						
				
            }
        }
		//EditorUtility.ClearProgressBar();
		//dont forget this shit again
		TerrainManager.SetSplatMap(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
	
	
	
	public static void terrainToTopology(Layers layer, Layers sourceLayer, float threshhold)
	{
		float[,,] splatMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		float[,,] targetGround = TerrainManager.Ground;
		int t = TerrainSplat.TypeToIndex((int)sourceLayer.Ground);
		
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
		TerrainManager.SetSplatMap(splatMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
	}	
	
	public static void copyTopologyLayer(Layers layer, Layers sourceLayer)
	{
		float[,,] splatMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)sourceLayer.Topologies));
		TerrainManager.SetSplatMap(splatMap, LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));

	}
	
	public static void lakeTopologyFill(Layers layer)
	{
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		float[,] heightMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
		float[,,] topoMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
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
		TerrainManager.SetSplatMap(lakeMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
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
	
	
	public static void paintTopologyOutline(Layers layer, Layers sourceLayer, int w)
	{
		
		float[,,] sourceMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)sourceLayer.Topologies));	
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
		
        TerrainManager.SetSplatMap(splatMap, LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology,  TerrainTopology.TypeToIndex((int)layer.Topologies));
	}

	public static void fillTopology(int topology)
	{
		float[,,] sourceMap = TerrainManager.GetSplatMap(LayerType.Topology, topology);	
		int res = sourceMap.GetLength(0);
		for (int i = 0; i < sourceMap.GetLength(0); i++)
			{
				for (int j = 0; j < sourceMap.GetLength(1); j++)
				{
					sourceMap[i,j,0] = float.MaxValue;
					sourceMap[i,j,1] = float.MinValue;
				}
			}
		TerrainManager.SetSplatMap(sourceMap, LayerType.Topology,  topology);
        //TerrainManager.SetLayer(LayerType.Topology,  topology);
	}

	public static void notTopologyLayer(Layers layer, Layers sourceLayer)
	{
		float[,,] sourceMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)sourceLayer.Topologies));
		float[,,] splatMap = TerrainManager.GetSplatMap(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
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
		
        TerrainManager.SetSplatMap(splatMap, LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
        //TerrainManager.SetLayer(LayerType.Topology, TerrainTopology.TypeToIndex((int)layer.Topologies));
		
	}

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
		TerrainManager.SetSplatMap(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
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
				
				if(Mathf.PerlinNoise(x*1f/5f,y*1f/5f) > .666f)
				{
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 1;
					newGround[x, y, 2] = 0;
					newGround[x, y, 3] = 0;
				}
				else if(Mathf.PerlinNoise(x*1f/5f,y*1f/5f) > .444f)
				{
					newGround[x, y, 0] = 0;
					newGround[x, y, 1] = 0;
					newGround[x, y, 2] = 1;
					newGround[x, y, 3] = 0;
				}
				
			}
		}
		
		TerrainManager.SetSplatMap(newGround, TerrainManager.CurrentLayerType, 0);
		//TerrainManager.SetLayer(TerrainManager.CurrentLayerType, 0);
	}
	
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
		
		TerrainManager.SetSplatMap(newGround, TerrainManager.CurrentLayerType, 0);
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
        
		for (int i = 0; i < terrains.prefabData.Length; i++)
        {
			terrains.prefabData[i].position.x = terrains.prefabData[i].position.x+y1*2f;
			terrains.prefabData[i].position.z = terrains.prefabData[i].position.z+x1*2f;
			terrains.prefabData[i].position.y = terrains.prefabData[i].position.y + zOffset*1000f;
			GameObject newObj = PrefabManager.SpawnPrefab(defaultObj, terrains.prefabData[i], prefabsParent);
            newObj.GetComponent<PrefabDataHolder>().prefabData = terrains.prefabData[i];
        }
		
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
		uint industrialglassnt = 1048750230;
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

        public static int Next(int max)
        {
            if(max <=0)
				{  return 0; }
			
			System.Random inst = _local;
            if (inst == null)
            {
                int seed;
                lock (_global) seed = _global.Next();
                _local = inst = new System.Random(seed);
            }
			
		
			return inst.Next() % max;
        }
		public static int GetGlobalSeed()
		{
			lock (_global)
			{
				return ((System.Random)_global).Next();
			}
		}

		public static void SetGlobalSeed(int seed)
		{
			lock (_global)
			{
				_global = new System.Random(seed);
			}
		}
    }

	
	
	public static void spawnCustom(GeologyItem geoItem, Vector3 position, Vector3 rotation, Vector3 scale, Transform parent)
	{
		PrefabManager.placeCustomPrefab(SettingsManager.AppDataPath() + geoItem.customPrefab, position, rotation, scale, parent);
	}
	
	public static void spawnItem(GeologyItem geoItem, Transform transItem)
	{

			PrefabManager.createPrefab("Decor", geoItem.prefabID, transItem);
	}
	
	public static void spawnGeoItem(GeologyItem geoItem, Vector3 position, Vector3 rotation, Vector3 scale)
	{

			PrefabManager.createPrefab("Decor", geoItem.prefabID, position, rotation, scale);

	}
	
	#if UNITY_EDITOR
	
	public static void insertPrefabCliffs(GeologyPreset geo)
	{
		EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.insertPrefabCliffs(geo));
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
	
	#else
		//runtime coroutines
		public static void insertPrefabCliffs(GeologyPreset geo)
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.insertPrefabCliffs(geo));
		}
		

		public static void MakeCliffMap(GeologyPreset geo, Action onComplete = null)
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.MakeCliffMap(geo, onComplete));
		}
		
		
		public static void MakeCliffs(GeologyPreset geo)
		{
			CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.MakeCliffs(geo));
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
		private static bool makeCliffsRunning = false;

		public static IEnumerator ApplyGeologyTemplate()
		{
			foreach (GeologyPreset pre in SettingsManager.macro)
			{
				makeCliffMapRunning = true;
				GenerativeManager.MakeCliffMap(pre);
				yield return new WaitUntil(() => !makeCliffMapRunning && !makeCliffsRunning);

				makeCliffsRunning = true;
				GenerativeManager.MakeCliffs(pre);
				yield return new WaitUntil(() => !makeCliffsRunning && !makeCliffMapRunning);
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
			/*string message = "Do you want to proceed with " + GeologySpawns + " prefab spawns?";
			bool response = EditorUtility.DisplayDialog("Confirmation", message, "Yes", "No");
			if(!response)
			{
				yield break;
			}*/
			makeCliffsRunning = true;
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			List<GeologyItem> oddsList = OddsList(geo.geologyItems);
			int selection;
			
			Vector3 rotationRange1 = geo.rotationsLow;
			Vector3 rotationRange2 = geo.rotationsHigh;
			Vector3 scaleRange1 = geo.scalesLow;
			Vector3 scaleRange2 = geo.scalesHigh;
			
			int res = TerrainManager.HeightMapRes;
			int splatRes = TerrainManager.SpawnMap.GetLength(0);
			float resRatio = 1f * res/splatRes;
			
			int heightMapsX, heightMapsY;
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			
			int count = 0;
			int cullcount=0;
			bool testing = geo.cliffTest;
			
			float geology = new float();
			int flipX=0;
			int flipZ=0;
			//float slope=0;
			int size = (int)land.terrainData.size.x;
			int sizeZ = (int)land.terrainData.size.y;
			
			float zOffset = 0f;
			float slopeOffset = 0f;
			float sideOffset = 0f;
			
			Vector3 position, prePosition;
			Vector3 rRotate, preRotate;
			Vector3 rScale;
			Vector3 normal = new Vector3(0,0,0);
			Quaternion qRotate;
			
			Vector3 upWard = new Vector3(0, 1, 0);
			Vector3 offsetPerpendicular = new Vector3(0, 0, 1);
			Vector3 antiNormal = new Vector3(1, 0, 0);
			
			float[] heights = new float[9];
			
			float height=0f;
			//float tempHeight=0f;
			int odds=0;
			
			float ratio = 1f* land.terrainData.size.x / splatRes;
			
			bool prefabCollisions = false;
			foreach (GeologyCollisions collision in geo.geologyCollisions)
			{ prefabCollisions |= (collision.layer ==  ColliderLayer.Prefabs);}
			
			if (prefabCollisions)
			{Debug.Log("Testing Prefab Collision, Generating may take longer than normal");}
			
			for (int i = 2; i < splatRes-2; i++)
			{
				//EditorUtility.DisplayProgressBar("Spawning", "Geology features",(i*1f/splatRes-1));
				for (int j = 2; j < splatRes-2; j++)
				{
					
					if(TerrainManager.SpawnMap[i,j])
					{
													//if(prefabCollisions){yield return null;}
													yield return null;

												
													if(geo.flipping)
													{
													flipX = UnityEngine.Random.Range(0,2) * 180;
													flipZ = UnityEngine.Random.Range(0,2) * 180;
													}
													
													if(geo.tilting)
													{
													geology = (Mathf.PerlinNoise(i*1f/80,j*1f/80))*20;
													}
													
													heightMapsX = (int)(i * resRatio);
													heightMapsY = (int)(j * resRatio);
													heightMapsX = (int)Mathf.Clamp(heightMapsX, 0f, res *1f);
													heightMapsY = (int)Mathf.Clamp(heightMapsY, 0f, res *1f);
													height = baseMap[heightMapsX, heightMapsY];
													
													
													prePosition = new Vector3(((j) *ratio)-(size/2f), (height * sizeZ) - (sizeZ*.5f),((i) *ratio)-(size/2f));
													normal = land.terrainData.GetInterpolatedNormal(1f * j / splatRes, 1f * i / splatRes).normalized;
													
													offsetPerpendicular = Vector3.Cross(normal, upWard);													
													antiNormal = Vector3.Cross(normal, offsetPerpendicular);
													
													zOffset = UnityEngine.Random.Range(geo.jitterLow.z,geo.jitterHigh.z);
													slopeOffset = UnityEngine.Random.Range(geo.jitterLow.x,geo.jitterHigh.x);
													sideOffset = UnityEngine.Random.Range(geo.jitterLow.y,geo.jitterHigh.y);
													
													position = new Vector3(
													
													prePosition.x + (antiNormal.x * zOffset) + (normal.x * slopeOffset) + (offsetPerpendicular.x * sideOffset), 
													prePosition.y + (antiNormal.y * zOffset) + (normal.y * slopeOffset) + (offsetPerpendicular.y * sideOffset),
													prePosition.z + (antiNormal.z * zOffset) + (normal.z * slopeOffset) + (offsetPerpendicular.z * sideOffset)
													
													);

													
													
													//position = new Vector3((j *ratio)-(size/2f), (height * sizeZ) - (sizeZ*.5f),(i *ratio)-(size/2f));
													

													
													
													rRotate = new Vector3(
													UnityEngine.Random.Range(rotationRange1.x, rotationRange2.x) + geology + flipX, 
													UnityEngine.Random.Range(rotationRange1.y, rotationRange2.y), 
													UnityEngine.Random.Range(rotationRange1.z,rotationRange2.z) + flipZ);
													
													rScale = new Vector3(
													UnityEngine.Random.Range(scaleRange1.x, scaleRange2.x), 
													UnityEngine.Random.Range(scaleRange1.y, scaleRange2.y), 
													UnityEngine.Random.Range(scaleRange1.z,scaleRange2.z));
													
													if(geo.normalizeY)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.y += preRotate.y;
													}
													
													if(geo.normalizeX)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.x += preRotate.x;
													}
													
													if(geo.normalizeZ)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.z += preRotate.z;
													}
													GameObject raycaster = GameObject.Find("Raycaster");
													raycaster.transform.position = position;
													raycaster.transform.localScale = rScale;
													raycaster.transform.eulerAngles = rRotate;
													
													selection = UnityEngine.Random.Range(0, oddsList.Count);
													
													bool collisions = true;
													bool skirts = true;
													
													if(testing)
													{
														skirts = PrefabManager.inTerrain(new PrefabData("f", 261440689, position, Quaternion.Euler(rRotate), rScale));
													}
												
													foreach (GeologyCollisions collision in geo.geologyCollisions)
													{
														
														bool sphere = PrefabManager.sphereCollision(position, collision.radius,(int)collision.layer);
														collisions &= collision.minMax ? !sphere : sphere;
													}
												
													if (collisions && skirts)
														{
															
															if(!oddsList[selection].custom)
																{ spawnGeoItem(oddsList[selection], position, rRotate, rScale); count++; }
															else
																{ spawnCustom(oddsList[selection], position, rRotate, rScale, GameObject.FindGameObjectWithTag("Prefabs").transform); count++; }
															
															if(prefabCollisions){yield return null;}
														}
													else
														{
															cullcount++;
														}
											
										
										
										
										
					}
				}
			}
			
			//EditorUtility.ClearProgressBar();
			Debug.LogError("Geology Complete: " + count + " Features Placed.");
			if (cullcount >0)
			{
				Debug.LogError(cullcount + " prefabs culled");
			}
			makeCliffsRunning = false;
		}
		
		public static IEnumerator MakeCliffMap(GeologyPreset geo, Action onComplete = null)
		{
			makeCliffMapRunning = true;
			TerrainManager.UpdateHeightCache();
		
			int odds = 0;
			int spawnCount = 0;
			float[,,] field = TerrainManager.Topology[0];
			float[,,] cliff = TerrainManager.Topology[1];
			float[,,] summit = TerrainManager.Topology[2];
			float[,,] beachside = TerrainManager.Topology[3];
			float[,,] beach = TerrainManager.Topology[4];
			float[,,] forest = TerrainManager.Topology[5];
			float[,,] forestside = TerrainManager.Topology[6];
			float[,,] ocean = TerrainManager.Topology[7];
			float[,,] oceanside = TerrainManager.Topology[8];
			float[,,] decor = TerrainManager.Topology[9];
			float[,,] monument = TerrainManager.Topology[10];
			float[,,] road = TerrainManager.Topology[11];			
			float[,,] roadside = TerrainManager.Topology[12];
			float[,,] swamp = TerrainManager.Topology[13];
			float[,,] river = TerrainManager.Topology[14];
			float[,,] riverside = TerrainManager.Topology[15];
			float[,,] lake = TerrainManager.Topology[16];
			float[,,] lakeside = TerrainManager.Topology[17];
			float[,,] offshore = TerrainManager.Topology[18];
			float[,,] powerline = TerrainManager.Topology[19];
			float[,,] runway = TerrainManager.Topology[20];
			float[,,] building = TerrainManager.Topology[21];
			float[,,] cliffside = TerrainManager.Topology[22];
			float[,,] mountain = TerrainManager.Topology[23];
			float[,,] clutter = TerrainManager.Topology[24];
			float[,,] alt = TerrainManager.Topology[25];
			float[,,] tier0 = TerrainManager.Topology[26];
			float[,,] tier1 = TerrainManager.Topology[27];
			float[,,] tier2 = TerrainManager.Topology[28];
			float[,,] mainland = TerrainManager.Topology[29];
			float[,,] hilltop = TerrainManager.Topology[30];			
			float[,,] biomeMap = TerrainManager.Biome;

			int resRatio = (int)( TerrainManager.HeightMapRes / TerrainManager.SplatMapRes);
			
			//float[,] cliffMap = new float[TerrainManager.HeightMapRes,TerrainManager.HeightMapRes];
			float[,] heightOut = new float[TerrainManager.HeightMapRes,TerrainManager.HeightMapRes];
			bool[,] spawnMap = new bool[TerrainManager.HeightMapRes,TerrainManager.HeightMapRes];
			
			
				Parallel.For( 0, TerrainManager.HeightMapRes-1, i=>
				{
					for (int j = 0; j < TerrainManager.HeightMapRes-1; j++)
					{
						heightOut[i,j] = (TerrainManager.Height[i,j] - 2f) / 1000f;
						if(RandomTS.Next(geo.frequency) == 1)
							{
							
							if (
									(geo.heightRange && (TerrainManager.Height[i,j] >= geo.heights.heightMin * 1000f && TerrainManager.Height[i,j]-1 <= geo.heights.heightMax * 1000f))						
									&&((geo.slopeRange && (TerrainManager.Slope[i,j]*(geo.density / 100f) >= geo.heights.slopeLow && TerrainManager.Slope[i,j]*(geo.density / 100f) <= geo.heights.slopeHigh))
									||(geo.curveRange && (TerrainManager.Curvature[i,j] * geo.heights.curveWeight >= geo.heights.curveMin && TerrainManager.Curvature[i,j] * geo.heights.curveWeight <= geo.heights.curveMax)))		
									&&(field[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Field)) 
									&&(cliff[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Cliff))
									&&(summit[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Summit)) 
									&&(beachside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Beachside))
									&&(beach[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Beach)) 
									&&(forest[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Forest))
									&&(forestside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Forestside)) 
									&&(ocean[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Ocean))
									&&(oceanside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Oceanside))
									&&(decor[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Decor))
									&&(monument[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Monument)) 
									&&(road[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Road))
									&&(roadside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Roadside)) 
									&&(swamp[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Swamp))
									&&(river[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.River))
									&&(riverside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Riverside))
									&&(lake[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Lake))
									&&(lakeside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Lakeside))
									&&(offshore[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Offshore))
									&&(powerline[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Powerline))
									&&(runway[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Runway))
									&&(building[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Building))
									&&(cliffside[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Cliffside))
									&&(mountain[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Mountain))
									&&(clutter[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Clutter))
									&&(alt[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Alt))
									&&(tier0[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Tier0))
									&&(tier1[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Tier1))
									&&(tier2[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Tier2))
									&&(mainland[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Mainland))
									&&(hilltop[i/resRatio,j/resRatio,0] < .1f || geo.topologies.HasFlag(Topologies.Hilltop))

									&&(biomeMap[i/resRatio,j/resRatio,0] < .1f || geo.arid) &&(biomeMap[i/resRatio,j/resRatio,1] < .1f || geo.temperate)									
									&&(biomeMap[i/resRatio,j/resRatio,2] < .1f || geo.tundra) &&(biomeMap[i/resRatio,j/resRatio,3] < .1f || geo.arctic)
								)
							{
								
								//cliffMap[i,j] =  TerrainManager.Slope[i,j] * geo.heights.slopeWeight + Math.Abs(TerrainManager.Curvature[i,j]) * geo.heights.curveWeight;
								heightOut[i,j] = (TerrainManager.Height[i,j] + 4f) / 1000f;
								spawnMap[i,j] = true;
								spawnCount++;
								//if(TerrainManager.CliffMap != null) // can't make spawnMap first time
								//{
									//odds = (int)(GaussCurve(TerrainManager.CliffMap[i,j],geo.balance,1f)*geo.density);
									//if (odds<=(.4f*geo.density-2f) && (RandomTS.Next(odds))==1)
									//{
									


										
										
									//}
									//else
									//{
									//	spawnMap[i,j] = false;
									//}
								//}
							}
							}
							else
							{
								
								//cliffMap[i,j] = 0f;
								spawnMap[i,j] = false;
							}
					}
				});
				
				GeologySpawns = spawnCount;
				
				if(spawnMap != null && heightOut != null)
				{
					TerrainManager.SetCliffMap(spawnMap);
					TerrainManager.SetLandMask(heightOut);
					makeCliffMapRunning = false;
				}
				else
				{ makeCliffMapRunning = false; yield return null; }
			
			onComplete?.Invoke();
		}
		

		public static IEnumerator insertPrefabCliffs(GeologyPreset geo)
		{
			//var sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
			

			//uint featPrefabID = 0;
			//int roll = 0;

			
			List<GeologyItem> oddsList = OddsList(geo.geologyItems);
			int selection;
			
			Vector3 rotationRange1 = geo.rotationsLow;
			Vector3 rotationRange2 = geo.rotationsHigh;
			Vector3 scaleRange1 = geo.scalesLow;
			Vector3 scaleRange2 = geo.scalesHigh;
			
			float zOffset = geo.zOffset;
			float floor = geo.floor/1000f;
			float ceiling = geo.ceiling/1000f;
			float cliffFade = 0f;

			float s1 = geo.heights.slopeLow;
			float s2 = geo.heights.slopeHigh;

			int density = geo.density;
			int thinnitude = geo.frequency;
			int colliderLayer = (int)geo.colliderLayer;
			int closeColliderLayer = (int)geo.closeColliderLayer;
			float colliderDistance = geo.colliderDistance;
			float closeColliderDistance = geo.closeColliderDistance;
			
			
			bool avoid = geo.avoidTopo;
			bool tilting = geo.tilting;
			bool flipping = geo.flipping;
			bool testing = geo.cliffTest;
			bool overlap = geo.overlap;
			bool closeOverlap = geo.closeOverlap;
			
			
			Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
			float[,] baseMap = land.terrainData.GetHeights(0, 0, land.terrainData.heightmapResolution, land.terrainData.heightmapResolution);
			float[,,] avoidMap = TerrainManager.Topology[TerrainTopology.TypeToIndex(1024)];
			float[,,] avoidMap1 = TerrainManager.Topology[TerrainTopology.TypeToIndex(2048)];
			float[,,] biomeMap = TerrainManager.Biome;
			
			Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
			GameObject defaultObj = Resources.Load<GameObject>("Prefabs/DefaultPrefab");
			
			int count = 0;
			int cullcount=0;
			
			int res = baseMap.GetLength(0);
			int splatRes = avoidMap.GetLength(0);
			float resRatio = 1f * res/splatRes;
			int heightMapsX, heightMapsY;
			
			int size = (int)land.terrainData.size.x;
			int sizeZ = (int)land.terrainData.size.y;
			
			Vector3 position;
			Vector3 rRotate, preRotate;
			Vector3 rScale;
			Vector3 normal = new Vector3(0,0,0);
			Quaternion qRotate;
			
			float[] heights = new float[9];
			
			float geology = new float();
			int flipX=0;
			int flipZ=0;
			float slope=0;
			float avoider = 1f;
			float[,] cliffMap = new float[splatRes,splatRes];
			
			float slopeDiff = 0f;
			float oldPixel = 0f;
			float newPixel = 0f;
			float randomizer = 0f;
			
			//float xNormalizer = 0f;
			//float yNormalizer = 0f;
			//float zNormalizer = 0f;

			
			float quotient = 0f;		
			
			float height=0f;
			float tempHeight=0f;
			int xOff=0;
			int yOff=0;
			
			float ratio = 1f* land.terrainData.size.x / splatRes;
			
			
			if (avoid)
			{
				avoider=.01f;
			}		
			
			for (int i = 0; i < splatRes; i++)
			{
				//EditorUtility.DisplayProgressBar("Generating", "Slope Map",(i*1f/splatRes));
				for (int j = 0; j < splatRes; j++)
				{
					
						cliffMap[i,j] = (land.terrainData.GetSteepness(1f*j/splatRes, 1f*i/splatRes))/90 * (density / 100f);
				
				}
			}
			//EditorUtility.ClearProgressBar();

			for (int i = 2; i < splatRes-2; i++)
			{
				//EditorUtility.DisplayProgressBar("Dithering", "Cliff Map",(i*1f/splatRes));
				for (int j = 2; j < splatRes-2; j++)
				{
					
					oldPixel = cliffMap[i,j];
					
					if (cliffMap[i,j] >= .5f)
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
			
			bool prefabCollisions = false;

			
			foreach (GeologyCollisions collision in geo.geologyCollisions)
			{ prefabCollisions |= (collision.layer ==  ColliderLayer.Prefabs);}
			
			if (prefabCollisions)
			{Debug.LogError("Testing Prefab Collision, Generating may take longer than normal");}

			
			for (int i = 1; i < splatRes-1; i++)
			{
				//EditorUtility.DisplayProgressBar("Spawning", "Geology features",(i*1f/splatRes));
				for (int j = 1; j < splatRes-1; j++)
				{
					slope = land.terrainData.GetSteepness(1f*j/splatRes, 1f*i/splatRes);
					
					if (geo.biomeExclusive)
					{
						cliffFade = cliffMap[i,j] * biomeMap[i,j,geo.biomeIndex];		
					}
					else
					{
						cliffFade = cliffMap[i,j];
					}
					
					heightMapsX = (int)(1f* i * resRatio);
					heightMapsY = (int)(1f* j * resRatio);
					heightMapsX = (int)Mathf.Clamp(heightMapsX, 0f, res *1f);
					heightMapsY = (int)Mathf.Clamp(heightMapsY, 0f, res *1f);
					
					if(baseMap[heightMapsX,heightMapsY] > floor && baseMap[heightMapsX,heightMapsY] < ceiling && avoidMap[i,j,0] < avoider && avoidMap1[i,j,0] < avoider && cliffFade > .5f && slope > s1 && slope < s2)
					{

										
										
										
										

											if(UnityEngine.Random.Range(0,thinnitude) == 0)
											{
													if(prefabCollisions){yield return null;}
													
													height = 0f;
													tempHeight= 0f;
													for (int n = -1; n < 2; n++)
													{
														for (int o = -1; o < 2; o++)
														{
															tempHeight = baseMap[heightMapsX+n, heightMapsY+o];
															
															if (height < tempHeight)
															{
																height = tempHeight;
																xOff = n;
																yOff = o;
															}
															
															
															
														}
													}
												
													if(flipping)
													{
													flipX = UnityEngine.Random.Range(0,2) * 180;
													flipZ = UnityEngine.Random.Range(0,2) * 180;
													}
													
													if(tilting)
													{
													geology = (Mathf.PerlinNoise(i*1f/80,j*1f/80))*20;
													}
													
													position = new Vector3(j *ratio-(size/2f)+yOff*ratio, height * sizeZ - (sizeZ*.5f) + zOffset,i *ratio-(size/2f)+xOff*ratio);
													

													
													
													rRotate = new Vector3(UnityEngine.Random.Range(rotationRange1.x, rotationRange2.x) + geology + flipX, UnityEngine.Random.Range(rotationRange1.y, rotationRange2.y), UnityEngine.Random.Range(rotationRange1.z,rotationRange2.z) + flipZ);
													rScale = new Vector3(UnityEngine.Random.Range(scaleRange1.x, scaleRange2.x), UnityEngine.Random.Range(scaleRange1.y, scaleRange2.y), UnityEngine.Random.Range(scaleRange1.z,scaleRange2.z));
													
													if(geo.normalizeY)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.y += preRotate.y;
													}
													
													if(geo.normalizeX)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.x += preRotate.x;
													}
													
													if(geo.normalizeZ)
													{
													normal = land.terrainData.GetInterpolatedNormal(1f*j/splatRes, 1f*i/splatRes);
													qRotate = Quaternion.LookRotation(normal);
													preRotate = qRotate.eulerAngles;
													rRotate.z += preRotate.z;
													}
													GameObject raycaster = GameObject.Find("Raycaster");
													raycaster.transform.position = position;
													raycaster.transform.localScale = rScale;
													raycaster.transform.eulerAngles = rRotate;
													
													selection = UnityEngine.Random.Range(0, oddsList.Count);
													
													bool collisions = true;
													bool skirts = true;
													
													if(testing)
													{
														skirts = PrefabManager.inTerrain(new PrefabData("f", 261440689, position, Quaternion.Euler(rRotate), rScale));
													}
												
													foreach (GeologyCollisions collision in geo.geologyCollisions)
													{

														
														bool sphere = PrefabManager.sphereCollision(position, collision.radius,(int)collision.layer);
														collisions &= collision.minMax ? !sphere : sphere;
														

													}
												
												
													if (collisions && skirts)
														{
															
															if(!oddsList[selection].custom)

																{ spawnGeoItem(oddsList[selection], position, rRotate, rScale); count++; }
															else
																{ spawnCustom(oddsList[selection], position, rRotate, rScale, GameObject.FindGameObjectWithTag("Prefabs").transform); count++; }

															
															if(prefabCollisions){yield return null;}
														}
													else
														{
															cullcount++;
														}
											}
										
										
										
										
					}
					
					
				}
				
			}
			
			

			//EditorUtility.ClearProgressBar();
			Debug.LogError("Geology Complete: " + count + " Features Placed.");
			if (cullcount >0)
			{
				Debug.LogError(cullcount + " prefabs culled");
			}
		}
	}
	
}
