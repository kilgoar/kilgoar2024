using UnityEngine;

public class Monument : TerrainPlacement
{
    [SerializeField] public float Radius; // Radius of influence in world units
    [SerializeField] public float Fade;   // Fade distance for blending in world units
	
	
protected override void ApplyHeightMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
{
    if (!ShouldHeight() || (heightmap == null || !heightmap.IsValid)) return;

    Texture2D heightTexture = heightmap?.cachedInstance ?? heightmap?.GetResource();
    Texture2D blendTexture = blendmap.GetResource();

    float radius = Radius == 0f ? extents.x : Radius;
    bool useBlendMap = blendTexture != null && blendmap.IsValid;
    float radiusX = useBlendMap ? extents.x : radius;
    float radiusZ = useBlendMap ? extents.z : radius;
    Vector3 position = localToWorld.MultiplyPoint3x4(Vector3.zero);

    Vector3[] corners = new Vector3[]
    {
        localToWorld.MultiplyPoint3x4(offset + new Vector3(-radiusX, 0f, -radiusZ)),
        localToWorld.MultiplyPoint3x4(offset + new Vector3(radiusX, 0f, -radiusZ)),
        localToWorld.MultiplyPoint3x4(offset + new Vector3(-radiusX, 0f, radiusZ)),
        localToWorld.MultiplyPoint3x4(offset + new Vector3(radiusX, 0f, radiusZ))
    };
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.HeightMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    float[,] heightMap = TerrainManager.GetHeightMap();
    float[,] regionHeights = new float[height, width];

    Vector3 terrainPosition = TerrainManager.Land.transform.position;
    Vector3 terrainSize = TerrainManager.TerrainSize;
    Vector3 rcpSize = new Vector3(1f / terrainSize.x, 1f / terrainSize.y, 1f / terrainSize.z);


    int logCount = 0;
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

            float u = (localPos.x + extents.x) / size.x;
            float v = (localPos.z + extents.z) / size.z;
            float fade = useBlendMap
                ? (blendTexture?.GetPixelBilinear(u, v).a ?? 0f)
                : Mathf.InverseLerp(radius, radius - Fade, localPos.Magnitude2D());

            float currentHeight = heightMap[z, x];
            if (fade > 0f)
            {
                float heightValue = heightTexture != null ? heightTexture.GetPixelBilinear(u, v).r : 0f;
                float worldHeight = position.y + offset.y + heightValue * (size.y/100f);
                float normalizedHeight = (worldHeight - terrainPosition.y) * rcpSize.y;

                regionHeights[z - minZ, x - minX] = Mathf.SmoothStep(currentHeight, normalizedHeight, fade);
            }
            else
            {
                regionHeights[z - minZ, x - minX] = currentHeight;
            }

            dimensions.IncludeRect(new RectInt(x, z, 1, 1));
        }
    }

    TerrainManager.SetHeightMapRegion(regionHeights, minX, minZ, width, height);
}

    protected override void ApplyAlphaMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
    {
        if (!ShouldAlpha() || alphamap == null || !alphamap.IsValid) return;

        Texture2D alphaTexture = alphamap.cachedInstance ?? alphamap.GetResource();
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

                float dist = localPos.Magnitude2D();
                float fade = Mathf.InverseLerp(Radius, Radius - Fade, dist);
                if (fade > 0f)
                {
                    float u = (localPos.x + extents.x) / (2f * extents.x);
                    float v = (localPos.z + extents.z) / (2f * extents.z);
                    float alphaValue = alphaTexture.GetPixelBilinear(u, v).a;
                    regionAlpha[z - minZ, x - minX] = Mathf.Lerp(alphaMap[z, x], alphaValue, fade);
                }
                else
                {
                    regionAlpha[z - minZ, x - minX] = alphaMap[z, x];
                }
            }
        }


        TerrainManager.SetAlphaMapRegion(regionAlpha, minX, minZ, width, height);
        dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
    }

    protected override void ApplyBiomeMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
    {
        if (!ShouldBiome() || biomemap == null || !biomemap.IsValid) return;

        Texture2D biomeTexture = biomemap.cachedInstance ?? biomemap.GetResource();
        if (biomeTexture == null)
        {
            Debug.LogWarning($"No biome texture available for {this}.");
            return;
        }

        Vector3[] corners = GetWorldCorners(localToWorld);
        int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
        int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
        int maxX = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[3]);
        int width = maxX - minX + 1, height = maxZ - minZ + 1;

        if (width <= 0 || height <= 0) return;

        Vector4[,] biomeMap = TerrainManager.GetBiomeMap();
        Vector4[,] regionBiomes = new Vector4[height, width];

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
                Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

                float dist = localPos.Magnitude2D();
                float fade = Mathf.InverseLerp(Radius, Radius - Fade, dist);
                if (fade > 0f)
                {
                    float u = (localPos.x + extents.x) / (2f * extents.x);
                    float v = (localPos.z + extents.z) / (2f * extents.z);
                    Vector4 biomeValue = biomeTexture.GetPixelBilinear(u, v);
                    biomeValue.x = ShouldBiome(1) ? biomeValue.x : 0f; // Arid
                    biomeValue.y = ShouldBiome(2) ? biomeValue.y : 0f; // Temperate
                    biomeValue.z = ShouldBiome(4) ? biomeValue.z : 0f; // Tundra
                    biomeValue.w = ShouldBiome(8) ? biomeValue.w : 0f; // Arctic
                    regionBiomes[z - minZ, x - minX] = Vector4.Lerp(biomeMap[z, x], biomeValue, fade);
                }
                else
                {
                    regionBiomes[z - minZ, x - minX] = biomeMap[z, x];
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

    if (splat0Texture == null && splat1Texture == null)
    {
        Debug.LogWarning($"No splat textures available for {this}.");
        return;
    }

    Vector3[] corners = GetWorldCorners(localToWorld);
    int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
    int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
    int maxX = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[3]);
    int width = maxX - minX + 1, height = maxZ - minZ + 1;

    if (width <= 0 || height <= 0) return;

    float[,,] splatMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Ground);
    float[,,] regionSplats = new float[height, width, TerrainManager.LayerCount(TerrainManager.LayerType.Ground)];
    int layerCount = TerrainManager.LayerCount(TerrainManager.LayerType.Ground);
    Debug.Log($"Splat Layer Count: {layerCount}"); // Verify this

    for (int x = minX; x <= maxX; x++)
    {
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
            Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

            float dist = localPos.Magnitude2D();
            float fade = Mathf.InverseLerp(Radius, Radius - Fade, dist);
            if (fade > 0f)
            {
                float u = (localPos.x + extents.x) / (2f * extents.x);
                float v = (localPos.z + extents.z) / (2f * extents.z);
                Vector4 splat0 = splat0Texture != null ? splat0Texture.GetPixelBilinear(u, v) : Vector4.zero;
                Vector4 splat1 = splat1Texture != null ? splat1Texture.GetPixelBilinear(u, v) : Vector4.zero;

                // Map all 8 layers explicitly
                float[] splatValues = new float[8];
                splatValues[0] = ShouldSplat(1) ? splat0.x : 0f;   // Dirt
                splatValues[1] = ShouldSplat(2) ? splat0.y : 0f;   // Snow
                splatValues[2] = ShouldSplat(4) ? splat0.z : 0f;   // Sand
                splatValues[3] = ShouldSplat(8) ? splat0.w : 0f;   // Rock
                splatValues[4] = ShouldSplat(16) ? splat1.x : 0f;  // Grass
                splatValues[5] = ShouldSplat(32) ? splat1.y : 0f;  // Forest
                splatValues[6] = ShouldSplat(64) ? splat1.z : 0f;  // Stones
                splatValues[7] = ShouldSplat(128) ? splat1.w : 0f; // Gravel

                // Normalize if needed (optional, depending on TerrainManager requirements)
                float sum = 0f;
                for (int i = 0; i < 8; i++) sum += splatValues[i];
                if (sum > 0f) for (int i = 0; i < 8; i++) splatValues[i] /= sum;

                // Ensure k doesn’t exceed layerCount or splatValues length
                for (int k = 0; k < Mathf.Min(layerCount, 8); k++)
                {
                    regionSplats[z - minZ, x - minX, k] = Mathf.Lerp(splatMap[z, x, k], splatValues[k], fade);
                }
            }
            else
            {
                for (int k = 0; k < layerCount; k++)
                {
                    regionSplats[z - minZ, x - minX, k] = splatMap[z, x, k];
                }
            }
        }
    }

    TerrainManager.SetSplatMapRegion(regionSplats, TerrainManager.LayerType.Ground, minX, minZ, width, height);
    dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
}

    protected override void ApplyTopologyMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
    {
        if (!ShouldTopology() || topologymap == null || !topologymap.IsValid) return;

        Texture2D topologyTexture = topologymap.cachedInstance ?? topologymap.GetResource();
        if (topologyTexture == null)
        {
            Debug.LogWarning($"No topology texture available for {this}.");
            return;
        }

        Vector3[] corners = GetWorldCorners(localToWorld);
        int[] gridBounds = TerrainManager.WorldCornersToGrid(corners[0], corners[1], corners[2], corners[3]);
        int minX = Mathf.Max(0, gridBounds[0]), minZ = Mathf.Max(0, gridBounds[1]);
        int maxX = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[2]), maxZ = Mathf.Min(TerrainManager.SplatMapRes - 1, gridBounds[3]);
        int width = maxX - minX + 1, height = maxZ - minZ + 1;

        if (width <= 0 || height <= 0) return;

        int[,] topologyMap = TerrainManager.GetTopologyMap();
        int[,] regionTopology = new int[height, width];

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 worldPos = new Vector3(TerrainManager.ToWorldX(x), 0, TerrainManager.ToWorldZ(z));
                Vector3 localPos = worldToLocal.MultiplyPoint3x4(worldPos) - offset;

                float dist = localPos.Magnitude2D();
                if (dist <= Radius)
                {
                    float u = (localPos.x + extents.x) / (2f * extents.x);
                    float v = (localPos.z + extents.z) / (2f * extents.z);
                    int topologyValue = (int)(topologyTexture.GetPixelBilinear(u, v).r * 255f);
                    if (ShouldTopology(topologyValue))
                    {
                        regionTopology[z - minZ, x - minX] = topologyValue & (int)TopologyMask;
                    }
                    else
                    {
                        regionTopology[z - minZ, x - minX] = topologyMap[z, x];
                    }
                }
                else
                {
                    regionTopology[z - minZ, x - minX] = topologyMap[z, x];
                }
            }
        }

        TerrainManager.SetTopologyMapRegion(regionTopology, minX, minZ, width, height);
        dimensions.IncludeRect(new RectInt(minX, minZ, width, height));
    }

    protected override void ApplyWaterMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions)
    {
    }

    private Vector3[] GetWorldCorners(Matrix4x4 localToWorld)
    {
        Vector3 bottomLeft = localToWorld.MultiplyPoint3x4(new Vector3(-Radius, 0, -Radius) + offset);
        Vector3 bottomRight = localToWorld.MultiplyPoint3x4(new Vector3(Radius, 0, -Radius) + offset);
        Vector3 topLeft = localToWorld.MultiplyPoint3x4(new Vector3(-Radius, 0, Radius) + offset);
        Vector3 topRight = localToWorld.MultiplyPoint3x4(new Vector3(Radius, 0, Radius) + offset);
        return new[] { bottomLeft, bottomRight, topLeft, topRight };
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