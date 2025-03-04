using System;
using UnityEngine;

public abstract class TerrainPlacement : PrefabAttribute
{
    // Public fields for configuring terrain placement
    public Vector3 size = Vector3.zero;            // Size of the placement area
    public Vector3 extents = Vector3.zero;         // Extents (half-size) of the placement area
    public Vector3 offset = Vector3.zero;          // Offset from the origin
    public bool HeightMap = false;                 // Enable height map modifications
    public bool AlphaMap = false;                  // Enable alpha map modifications
    public bool WaterMap = false;                  // Enable water map modifications
    public TerrainSplat.Enum SplatMask = 0;   // Mask for splat (texture) layers
    public TerrainBiome.Enum BiomeMask = 0;   // Mask for biome layers
    public TerrainTopology.Enum TopologyMask = 0; // Mask for topology layers

    // Hidden texture references for terrain data
    public Texture2DRef heightmap;   // Heightmap texture
    public Texture2DRef splatmap0;   // First splatmap texture
    public Texture2DRef splatmap1;   // Second splatmap texture
    public Texture2DRef alphamap;    // Alphamap texture
    public Texture2DRef biomemap;    // Biomemap texture
    public Texture2DRef topologymap; // Topologymap texture
    public Texture2DRef watermap;    // Watermap texture
    public Texture2DRef blendmap;    // Blendmap texture

    // Constructor
    protected TerrainPlacement()
    {
        // Initialization can go here if needed
    }

    // Public method to apply terrain modifications
    public void Apply(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        // Call abstract/virtual methods to apply specific modifications
        ApplyHeight(localToWorld, worldToLocal);
        ApplySplat(localToWorld, worldToLocal);
        ApplyAlpha(localToWorld, worldToLocal);
        ApplyBiome(localToWorld, worldToLocal);
        ApplyTopology(localToWorld, worldToLocal);
    }

    // Abstract methods for terrain modification (must be implemented by derived classes)
    protected abstract void ApplyHeight(Matrix4x4 localToWorld, Matrix4x4 worldToLocal);
    protected abstract void ApplySplat(Matrix4x4 localToWorld, Matrix4x4 worldToLocal);
    protected abstract void ApplyAlpha(Matrix4x4 localToWorld, Matrix4x4 worldToLocal);
    protected abstract void ApplyBiome(Matrix4x4 localToWorld, Matrix4x4 worldToLocal);
    protected abstract void ApplyTopology(Matrix4x4 localToWorld, Matrix4x4 worldToLocal);

    // Virtual methods for type determination (can be overridden)
    protected virtual Type GetHeightType() => null;
    protected virtual Type GetSplatType() => null;
    protected virtual Type GetAlphaType() => null;
    protected virtual Type GetBiomeType() => null;
    protected virtual Type GetTopologyType() => null;

    // Protected methods to check if specific modifications should be applied
    protected bool ShouldApplyHeight() => HeightMap;
    protected bool ShouldApplyAlpha() => AlphaMap;
    protected bool ShouldApplyWater() => WaterMap;
    protected bool ShouldApplySplat() => SplatMask != 0;
    protected bool ShouldApplyBiome() => BiomeMask != 0;
    protected bool ShouldApplyTopology() => TopologyMask != 0;

    // Methods to check specific splat layers (optional layer index)
    protected bool ShouldApplySplatLayer(int layer = -1)
    {
        if (layer == -1) return SplatMask != 0;
        return ((int)SplatMask & (1 << layer)) != 0;
    }

    // Methods to check specific biome layers
    protected bool ShouldApplyBiomeLayer(int layer = -1)
    {
        if (layer == -1) return BiomeMask != 0;
        return ((int)BiomeMask & (1 << layer)) != 0;
    }

    // Methods to check specific topology layers
    protected bool ShouldApplyTopologyLayer(int layer = -1)
    {
        if (layer == -1) return TopologyMask != 0;
        return ((int)TopologyMask & (1 << layer)) != 0;
    }

    // Additional utility methods for checking modification conditions
    protected bool HasHeightMap() => heightmap != null && heightmap.IsValid;
    protected bool HasSplatMap0() => splatmap0 != null && splatmap0.IsValid;
    protected bool HasSplatMap1() => splatmap1 != null && splatmap1.IsValid;
    protected bool HasAlphaMap() => alphamap != null && alphamap.IsValid;
    protected bool HasBiomeMap() => biomemap != null && biomemap.IsValid;
    protected bool HasTopologyMap() => topologymap != null && topologymap.IsValid;
    protected bool HasWaterMap() => watermap != null && watermap.IsValid;
    protected bool HasBlendMap() => blendmap != null && blendmap.IsValid;
}