using System;
using UnityEngine;

public abstract class TerrainPlacement : PrefabAttribute
{
    public void ApplyHeight(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldHeight()) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplyHeightMap(localToWorld, worldToLocal, dimensions);
    }

    public void ApplySplat(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldSplat(-1)) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplySplatMap(localToWorld, worldToLocal, dimensions);
    }

    public void ApplyAlpha(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldAlpha()) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplyAlphaMap(localToWorld, worldToLocal, dimensions);
    }

    public void ApplyBiome(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldBiome(-1)) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplyBiomeMap(localToWorld, worldToLocal, dimensions);
    }

    public void ApplyTopology(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldTopology(-1)) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplyTopologyMap(localToWorld, worldToLocal, dimensions);
    }
	
	public void ApplyWater(Vector3 position, Quaternion rotation, Vector3 scale, TerrainBounds dimensions)
    {
        //if (!ShouldTopology(-1)) return;
        Matrix4x4 localToWorld = Matrix4x4.TRS(position, rotation, scale);
        Matrix4x4 worldToLocal = localToWorld.inverse;
        ApplyWaterMap(localToWorld, worldToLocal, dimensions);
    }

    public void Apply(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        TerrainBounds dimensions = new TerrainBounds();
        if (ShouldHeight()) ApplyHeightMap(localToWorld, worldToLocal, dimensions);
        if (ShouldSplat(-1)) ApplySplatMap(localToWorld, worldToLocal, dimensions);
        if (ShouldAlpha()) ApplyAlphaMap(localToWorld, worldToLocal, dimensions);
        if (ShouldBiome(-1)) ApplyBiomeMap(localToWorld, worldToLocal, dimensions);
        if (ShouldTopology(-1)) ApplyTopologyMap(localToWorld, worldToLocal, dimensions);
        if (ShouldWater()) ApplyWaterMap(localToWorld, worldToLocal, dimensions);
    }

    protected abstract void ApplyAlphaMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);
    protected abstract void ApplyBiomeMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);
    protected abstract void ApplyHeightMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);
    protected abstract void ApplySplatMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);
    protected abstract void ApplyTopologyMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);
    protected abstract void ApplyWaterMap(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, TerrainBounds dimensions);

    protected override Type GetPrefabAttributeType()
    {
        return typeof(TerrainPlacement);
    }

    public virtual bool ShouldAlpha() => alphamap != null && alphamap.IsValid && AlphaMap;
    public virtual bool ShouldBiome(int mask = -1) => biomemap != null && biomemap.IsValid && (BiomeMask & (TerrainBiome.Enum)mask) > (TerrainBiome.Enum)0;
    public virtual bool ShouldHeight() => heightmap != null && heightmap.IsValid && HeightMap;
    public virtual bool ShouldSplat(int mask = -1) => splatmap0 != null && splatmap0.IsValid && splatmap1 != null && splatmap1.IsValid && (SplatMask & (TerrainSplat.Enum)mask) > (TerrainSplat.Enum)0;
    public virtual bool ShouldTopology(int mask = -1) => topologymap != null && topologymap.IsValid && (TopologyMask & (TerrainTopology.Enum)mask) > (TerrainTopology.Enum)0;
    public virtual bool ShouldWater() => watermap != null && watermap.IsValid && WaterMap;

    public Vector3 size = Vector3.zero;
    public Vector3 extents = Vector3.zero;
    public Vector3 offset = Vector3.zero;
    public bool HeightMap = true;
    public bool AlphaMap = true;
    public bool WaterMap;
    public TerrainSplat.Enum SplatMask;
    public TerrainBiome.Enum BiomeMask;
    public TerrainTopology.Enum TopologyMask;
    public Texture2DRef heightmap;
    public Texture2DRef splatmap0;
    public Texture2DRef splatmap1;
    public Texture2DRef alphamap;
    public Texture2DRef biomemap;
    public Texture2DRef topologymap;
    public Texture2DRef watermap;
    public Texture2DRef blendmap;
}