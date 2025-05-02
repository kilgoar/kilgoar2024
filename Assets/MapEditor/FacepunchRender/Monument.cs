using UnityEngine;

public class Monument : TerrainPlacement
{
    [SerializeField] public float Radius; // Radius of influence in world units
    [SerializeField] public float Fade;   // Fade distance for blending in world units
	
protected override void ApplyHeightMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    if (!ShouldHeight() || (heightmap == null || !heightmap.IsValid)) return;
    Texture2D heightTexture = heightmap?.cachedInstance ?? heightmap?.GetResource();
    Texture2D blendTexture = blendmap?.GetResource();

    float radius = Radius == 0f ? extents.x : Radius;
    bool useBlendMap = blendTexture != null && blendmap.IsValid;
    float radiusX = useBlendMap ? extents.x : radius;
    float radiusZ = useBlendMap ? extents.z : radius;
    Vector3 position = localToWorld.MultiplyPoint3x4(Vector3.zero);

    // Extract the rotation from localToWorld
    Quaternion rotation = localToWorld.rotation;

    // Compute the world-space X and Z axes of the rotated rectangle
    Vector3 localXAxis = rotation * Vector3.right;
    Vector3 localZAxis = rotation * Vector3.forward;

    // Project the local extents onto the world axes to find the maximum extent
    float extentX = Mathf.Abs(Vector3.Dot(new Vector3(radiusX, 0f, 0f), localXAxis)) +
                    Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radiusZ), localXAxis));
    float extentZ = Mathf.Abs(Vector3.Dot(new Vector3(radiusX, 0f, 0f), localZAxis)) +
                    Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radiusZ), localZAxis));

    // Define the world-space AABB using the projected extents
    Vector3[] corners = new Vector3[]
    {
        position + new Vector3(-extentX, 0f, -extentZ),
        position + new Vector3(extentX, 0f, -extentZ),
        position + new Vector3(-extentX, 0f, extentZ),
        position + new Vector3(extentX, 0f, extentZ)
    };

    // Convert the corners to grid bounds
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    // Log the bounds for debugging
    //Debug.Log($"Grid Bounds: minX={minX}, minZ={minZ}, maxX={maxX}, maxZ={maxZ}, width={width}, height={height}");

    if (width <= 0 || height <= 0) return;

    float[,] heightMap = TerrainManager.GetHeightMap();
    float[,] regionHeights = new float[height, width];

    Vector3 terrainPosition = TerrainManager.Land.transform.position;
    Vector3 terrainSize = TerrainManager.TerrainSize;
    Vector3 rcpSize = new Vector3(1f / terrainSize.x, 1f / terrainSize.y, 1f / terrainSize.z);

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            float normX = ((float)x + 0.5f) / TerrainManager.HeightMapRes;
            float normZ = ((float)z + 0.5f) / TerrainManager.HeightMapRes;

            Vector3 worldPos = new Vector3(
                terrainPosition.x + normX * terrainSize.x,
                0f,
                terrainPosition.z + normZ * terrainSize.z
            );
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = Mathf.Clamp01((localPos.x + extents.x) / size.x);
            float v = Mathf.Clamp01((localPos.z + extents.z) / size.z);
            float fade = useBlendMap
                ? (blendTexture?.GetPixelBilinear(u, v).a ?? 0f)
                : Mathf.InverseLerp(radius, radius - Fade, localPos.Magnitude2D());

            if (fade <= 0f)
            {
                regionHeights[z - minZ, x - minX] = heightMap[z, x];
                continue;
            }

            float combinedHeight = BitUtility.SampleHeightBilinear(heightTexture, u, v);

            float worldHeight = position.y + offset.y + combinedHeight * size.y;
            float normalizedHeight = (worldHeight - terrainPosition.y) * rcpSize.y;

            regionHeights[z - minZ, x - minX] = Mathf.SmoothStep(heightMap[z, x], normalizedHeight, fade);
            dimensions.IncludeRect(new RectInt(x, z, 1, 1));
        }
    }

    TerrainManager.SetHeightMapRegion(regionHeights, minX, minZ, width, height);
}

protected override void ApplyAlphaMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    Texture2D alphaTexture = alphamap.GetResource();
    if (alphaTexture == null)
    {
        Debug.LogWarning($"No alpha texture available for {this}.");
        return;
    }

    Vector3[] corners = GetWorldCorners(localToWorld);
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.AlphaMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.AlphaMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    float[,] alphaMap = TerrainManager.GetAlphaMapFloat();
    float[,] regionAlpha = new float[height, width];

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = (localPos.x + extents.x) / (2f * extents.x);
            float v = (localPos.z + extents.z) / (2f * extents.z);
            float alphaValue = alphaTexture.GetPixelBilinear(u, v).a;
            regionAlpha[z - minZ, x - minX] = alphaValue;
        }
    }

    TerrainManager.SetAlphaMapRegion(regionAlpha, minX, minZ, width, height);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
}
	
	
protected override void ApplyBiomeMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    Texture2D biomeTexture1 = biomemap.GetResource(); // First biome texture
    //Texture2D biomeTexture2 = biomemap2?.GetResource(); // Second biome texture (assuming biomemap2 is a new field)
    Texture2D blendTexture = blendmap?.cachedInstance ?? blendmap?.GetResource();

	/*
    if (biomeTexture1 == null || biomeTexture2 == null)
    {
        Debug.LogWarning($"Missing biome texture(s) for {this}. BiomeTexture1: {biomeTexture1}, BiomeTexture2: {biomeTexture2}");
        return;
    }
	*/
    bool useBlendMap = blendTexture != null && blendmap.IsValid;

    Vector3[] corners = GetWorldCorners(localToWorld);
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    float[,,] biomeMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Ground);
    float[,,] regionBiomes = new float[height, width, TerrainManager.LayerCount(TerrainManager.LayerType.Biome)];
    int biomeCount = 5; // Arid, Temperate, Tundra, Arctic, Jungle

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = Mathf.Clamp01((localPos.x + extents.x) / size.x);
            float v = Mathf.Clamp01((localPos.z + extents.z) / size.z);
            float fade = useBlendMap
                ? BitUtility.SampleAlphaBilinear(blendTexture, u, v)
                : Mathf.InverseLerp(Radius, Radius - Fade, localPos.Magnitude2D());

            // Sample both biome textures
            Color32 biomeValue1 = biomeTexture1.GetPixelBilinear(u, v);
            //Color32 biomeValue2 = biomeTexture2.GetPixelBilinear(u, v);

            // Blend biome values from both textures (e.g., weighted average)
            float[] biomeValues = new float[biomeCount];
            biomeValues[0] = ShouldBiome(1) ? (biomeValue1.r) : 0f; // Arid
            biomeValues[1] = ShouldBiome(2) ? (biomeValue1.g ) : 0f; // Temperate
            biomeValues[2] = ShouldBiome(4) ? (biomeValue1.b ) : 0f; // Tundra
            biomeValues[3] = ShouldBiome(8) ? (biomeValue1.a ) : 0f; // Arctic
            biomeValues[4] = 0f; // Jungle (example)

            // Compute total biome contribution
            float totalBiomeContribution = 0f;
            for (int i = 0; i < biomeCount; i++)
            {
                totalBiomeContribution += biomeValues[i];
            }

            // Normalize biome values (if non-zero contribution)
            if (totalBiomeContribution > 0f)
            {
                for (int i = 0; i < biomeCount; i++)
                {
                    biomeValues[i] /= totalBiomeContribution;
                }
            }

            // Adjust fade factor based on total biome contribution
            float effectiveFade = fade * totalBiomeContribution;

            // Blend biome values into the regionBiomes array
            regionBiomes[z - minZ, x - minX, 0] = Mathf.Lerp(biomeMap[z, x, 0], biomeValues[0], effectiveFade); // Arid
            regionBiomes[z - minZ, x - minX, 1] = Mathf.Lerp(biomeMap[z, x, 1], biomeValues[1], effectiveFade); // Temperate
            regionBiomes[z - minZ, x - minX, 2] = Mathf.Lerp(biomeMap[z, x, 2], biomeValues[2], effectiveFade); // Tundra
            regionBiomes[z - minZ, x - minX, 3] = Mathf.Lerp(biomeMap[z, x, 3], biomeValues[3], effectiveFade); // Arctic
            regionBiomes[z - minZ, x - minX, 4] = Mathf.Lerp(biomeMap[z, x, 4], biomeValues[4], effectiveFade); // Jungle
        }
    }

    // Final normalization pass to ensure biome values sum to 1
    for (int x = 0; x < width; x++)
    {
        for (int z = 0; z < height; z++)
        {
            float sum = 0f;
            for (int i = 0; i < biomeCount; i++)
            {
                sum += regionBiomes[z, x, i];
            }
            if (sum > 0f)
            {
                for (int i = 0; i < biomeCount; i++)
                {
                    regionBiomes[z, x, i] /= sum;
                }
            }
        }
    }

    TerrainManager.SetBiomeMapRegion(regionBiomes, minX, minZ, width, height);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
}


protected override void ApplySplatMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    if (!ShouldSplat() || (splatmap0 == null || !splatmap0.IsValid) && (splatmap1 == null || !splatmap1.IsValid)) return;

    Texture2D splat0Texture = splatmap0?.cachedInstance ?? splatmap0?.GetResource();
    Texture2D splat1Texture = splatmap1?.cachedInstance ?? splatmap1?.GetResource();
    Texture2D blendTexture = blendmap?.cachedInstance ?? blendmap?.GetResource();

    if (splat0Texture == null && splat1Texture == null)
    {
        Debug.LogWarning($"No splat textures available for {this}.");
        return;
    }

    bool useBlendMap = blendTexture != null && blendmap.IsValid;

    Vector3[] corners = GetWorldCorners(localToWorld);
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    float[,,] splatMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Ground);
    float[,,] regionSplats = new float[height, width, TerrainManager.LayerCount(TerrainManager.LayerType.Ground)];
    int layerCount = TerrainManager.LayerCount(TerrainManager.LayerType.Ground);
    Debug.Log($"Splat Layer Count: {layerCount}");

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = Mathf.Clamp01((localPos.x + extents.x) / size.x);
            float v = Mathf.Clamp01((localPos.z + extents.z) / size.z);
            float fade = useBlendMap
                ? BitUtility.SampleAlphaBilinear(blendTexture, u, v)
                : Mathf.InverseLerp(Radius, Radius - Fade, localPos.Magnitude2D());

            Vector4 splat0 = splat0Texture != null ? splat0Texture.GetPixelBilinear(u, v) : Vector4.zero;
            Vector4 splat1 = splat1Texture != null ? splat1Texture.GetPixelBilinear(u, v) : Vector4.zero;

            float[] splatValues = new float[8];
            splatValues[0] = ShouldSplat(1) ? splat0.x : 0f;   // Dirt
            splatValues[1] = ShouldSplat(2) ? splat0.y : 0f;   // Snow
            splatValues[2] = ShouldSplat(4) ? splat0.z : 0f;   // Sand
            splatValues[3] = ShouldSplat(8) ? splat0.w : 0f;   // Rock
            splatValues[4] = ShouldSplat(16) ? splat1.x : 0f;  // Grass
            splatValues[5] = ShouldSplat(32) ? splat1.y : 0f;  // Forest
            splatValues[6] = ShouldSplat(64) ? splat1.z : 0f;  // Stones
            splatValues[7] = ShouldSplat(128) ? splat1.w : 0f; // Gravel

            // Compute the total splat contribution
            float totalSplatContribution = 0f;
            for (int i = 0; i < 8; i++) totalSplatContribution += splatValues[i];

            // Normalize splat values (if non-zero contribution)
            if (totalSplatContribution > 0f)
            {
                for (int i = 0; i < 8; i++) splatValues[i] /= totalSplatContribution;
            }

            // Adjust the fade factor based on the total splat contribution
            // When totalSplatContribution approaches 0, the effective fade should also approach 0
            float effectiveFade = fade * totalSplatContribution;

            // Blend all layers using the effective fade factor
            for (int k = 0; k < Mathf.Min(layerCount, 8); k++)
            {
                regionSplats[z - minZ, x - minX, k] = Mathf.Lerp(splatMap[z, x, k], splatValues[k], effectiveFade);
            }
        }
    }

    // Final normalization pass to ensure splat values sum to 1
    for (int x = 0; x < width; x++)
    {
        for (int z = 0; z < height; z++)
        {
            float sum = 0f;
            for (int k = 0; k < layerCount; k++)
            {
                sum += regionSplats[z, x, k];
            }
            if (sum > 0f)
            {
                for (int k = 0; k < layerCount; k++)
                {
                    regionSplats[z, x, k] /= sum;
                }
            }
        }
    }

    TerrainManager.SetSplatMapRegion(regionSplats, TerrainManager.LayerType.Ground, minX, minZ, width, height);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
}

protected override void ApplyTopologyMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    Texture2D topologyTexture = topologymap.GetResource();

    if (topologyTexture == null)
    {
        Debug.LogWarning($"No topology texture available for {this}.");
        return;
    }

    Vector3[] corners = GetWorldCorners(localToWorld);
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.AlphaMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.AlphaMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    int[,] topologyMap = TerrainManager.GetTopologyMap();
    int[,] regionTopology = new int[height, width];

    // Debug extents, size, and topology mask

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = Mathf.Clamp01((localPos.x + extents.x) / size.x);
            float v = Mathf.Clamp01((localPos.z + extents.z) / size.z);

            float pixelX = u * (float)(topologyTexture.width - 1);
            float pixelY = v * (float)(topologyTexture.height - 1);
            int px = Mathf.Clamp(Mathf.RoundToInt(pixelX), 1, topologyTexture.width - 2);
            int py = Mathf.Clamp(Mathf.RoundToInt(pixelY), 1, topologyTexture.height - 2);

            Color32 topologySample = topologyTexture.GetPixelData<Color32>(0)[py * topologyTexture.width + px];

            // Use BitUtility.DecodeInt to construct topology value
            int topologyValue = BitUtility.DecodeInt(topologySample);

            bool shouldApply = ShouldTopology(topologyValue);

            if (!shouldApply)
            {
                regionTopology[z - minZ, x - minX] = topologyMap[z, x];
                continue;
            }

            // Apply topology mask
            topologyValue &= (int)TopologyMask;

            // Apply topology additively
            int existingTopology = topologyMap[z, x];
            int newTopology = BitUtility.ApplyTopologyAdditive(existingTopology, topologyValue);

            regionTopology[z - minZ, x - minX] = newTopology;
        }
    }

    TopologyData.SetTopologyRegion(regionTopology, minX, minZ, width, height);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
}

protected override void ApplyWaterMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    Texture2D waterTexture = watermap.GetResource();
    if (waterTexture == null)
    {
        Debug.LogWarning($"No water heightmap texture available for {this}.");
        return;
    }

    Texture2D blendTexture =blendmap.GetResource();
    bool useBlendMap = blendTexture != null && blendmap.IsValid;

    float radius = Radius == 0f ? extents.x : Radius;
    Vector3 position = localToWorld.MultiplyPoint3x4(Vector3.zero);

    Quaternion rotation = localToWorld.rotation;
    Vector3 localXAxis = rotation * Vector3.right;
    Vector3 localZAxis = rotation * Vector3.forward;

    float extentX = Mathf.Abs(Vector3.Dot(new Vector3(radius, 0f, 0f), localXAxis)) +
                    Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radius), localXAxis));
    float extentZ = Mathf.Abs(Vector3.Dot(new Vector3(radius, 0f, 0f), localZAxis)) +
                    Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radius), localZAxis));

    Vector3[] corners = new Vector3[]
    {
        position + new Vector3(-extentX, 0f, -extentZ),
        position + new Vector3(extentX, 0f, -extentZ),
        position + new Vector3(-extentX, 0f, extentZ),
        position + new Vector3(extentX, 0f, extentZ)
    };

    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0)
    {
        Debug.LogWarning($"Invalid region size for water map: width={width}, height={height}. Skipping application.");
        return;
    }

    float[,] waterMapData = TerrainManager.GetWaterHeightMap();
    float[,] regionWater = new float[height, width];
    int modifiedPixels = 0;

    Vector3 terrainPosition = TerrainManager.Water.transform.position;
    Vector3 terrainSize = TerrainManager.TerrainSize;
    Vector3 rcpSize = new Vector3(1f / terrainSize.x, 1f / terrainSize.y, 1f / terrainSize.z);

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            float normX = ((float)x + 0.5f) / TerrainManager.HeightMapRes;
            float normZ = ((float)z + 0.5f) / TerrainManager.HeightMapRes;

            Vector3 worldPos = new Vector3(
                terrainPosition.x + normX * terrainSize.x,
                0f,
                terrainPosition.z + normZ * terrainSize.z
            );
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float u = Mathf.Clamp01((localPos.x + extents.x) / size.x);
            float v = Mathf.Clamp01((localPos.z + extents.z) / size.z);

            float combinedHeight = BitUtility.SampleHeightBilinear(waterTexture, u, v);
            float worldHeight = position.y + offset.y + combinedHeight * size.y;
            float normalizedHeight = (worldHeight - terrainPosition.y) * rcpSize.y;

            float fade = useBlendMap ? (blendTexture?.GetPixelBilinear(u, v).a ?? 0f) : 1f;
            if (fade <= 0f)
            {
                regionWater[z - minZ, x - minX] = waterMapData[z, x];
                continue;
            }

            regionWater[z - minZ, x - minX] = Mathf.SmoothStep(waterMapData[z, x], normalizedHeight, fade);
            if (normalizedHeight > 0f) modifiedPixels++;
            dimensions.IncludeRect(new RectInt(x, z, 1, 1));
        }
    }

    TerrainManager.SetHeightMapRegion(regionWater, minX, minZ, width, height, TerrainManager.TerrainType.Water);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));

    Debug.Log($"ApplyWaterMap: Applied {modifiedPixels} non-zero height pixels for water map in region ({minX}, {minZ}, {width}, {height}).");
}

	private Vector3[] GetWorldCorners(Matrix4x4 localToWorld)
	{
		float radius = Radius == 0f ? extents.x : Radius;
		bool useBlendMap = blendmap != null && blendmap.IsValid;
		float radiusX = useBlendMap ? extents.x : radius;
		float radiusZ = useBlendMap ? extents.z : radius;
		Vector3 position = localToWorld.MultiplyPoint3x4(Vector3.zero);

		Quaternion rotation = localToWorld.rotation;
		Vector3 localXAxis = rotation * Vector3.right;
		Vector3 localZAxis = rotation * Vector3.forward;

		float extentX = Mathf.Abs(Vector3.Dot(new Vector3(radiusX, 0f, 0f), localXAxis)) +
						Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radiusZ), localXAxis));
		float extentZ = Mathf.Abs(Vector3.Dot(new Vector3(radiusX, 0f, 0f), localZAxis)) +
						Mathf.Abs(Vector3.Dot(new Vector3(0f, 0f, radiusZ), localZAxis));

		return new Vector3[]
		{
			position + new Vector3(-extentX, 0f, -extentZ),
			position + new Vector3(extentX, 0f, -extentZ),
			position + new Vector3(-extentX, 0f, extentZ),
			position + new Vector3(extentX, 0f, extentZ)
		};
	}

    private void GenerateCliffSplat(Vector3 worldPos, float[,,] splatMap, int z, int x)
    {
    }

    private void GenerateCliffTopology(Vector3 worldPos, float[,,] topologyMap, int z, int x, int layer)
    {
    }
}

public static class Vector3Extensions
{
    public static float Magnitude2D(this Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.z * v.z);
    }
}