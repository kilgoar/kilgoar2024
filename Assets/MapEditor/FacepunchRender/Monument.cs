using System;
using UnityEngine;
using System.Collections.Generic;

public class Monument : TerrainPlacement
{
    // Public fields specific to Monument
    public float Radius = 0f;               // Radius of the monument's influence area
    public float Fade = 0f;                 // Fade distance for blending effects
    public bool AutoCliffSplat = false;     // Automatically apply cliff splat maps
    public bool AutoCliffTopology = false;  // Automatically apply cliff topology
    public bool RemoveExistingTopology = false; // Remove existing topology when applying
	

private void Start()
{
    PrefabDataHolder holder = GetComponent<PrefabDataHolder>();
    if (holder == null)
    {
        Debug.LogError("PrefabDataHolder component not found on this GameObject.");
        return;
    }

    if (!AssetManager.IDLookup.TryGetValue(holder.prefabData.id, out string name))
    {
        Debug.LogError($"No IDLookup entry found for prefab ID {holder.prefabData.id}");
        return;
    }

    string[] parse = name.Split('/');
    name = parse[parse.Length - 1].Replace(".prefab", "");

    if (!AssetManager.MonumentLayers.TryGetValue(name, out uint[] idArray))
    {
        Debug.LogError($"No MonumentLayers entry found for {name}");
        return;
    }

    List<Texture2D> monumentTextures = new List<Texture2D>(8);
    for (int i = 0; i < idArray.Length; i++)
    {
        if (idArray[i] == 0)
        {
            monumentTextures.Add(null);
            continue;
        }

        if (!AssetManager.IDLookup.TryGetValue(idArray[i], out string filePath))
        {
            Debug.LogWarning($"No file path found for hash {idArray[i]} at index {i} for monument {name}");
            monumentTextures.Add(null);
            continue;
        }

        Texture2D texture = AssetManager.LoadAsset<Texture2D>(filePath);
        monumentTextures.Add(texture);

        if (texture == null)
        {
            Debug.LogWarning($"Failed to load texture for '{filePath}' at index {i} for monument {name}");
        }
    }
}
	

    // Implement the inherited abstract method from PrefabAttribute
    protected override Type GetPrefabAttributeType()
    {
        return typeof(Monument);
    }

    // Draw gizmos when the object is selected in the editor
    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Radius); // Visualize the monument's radius
    }

    // Override abstract methods from TerrainPlacement
    protected override void ApplyHeight(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        if (!ShouldApplyHeight() || !HasHeightMap()) return;

        Texture2D heightTexture = heightmap.GetResource();
        if (heightTexture == null) return;

        // Placeholder: Apply heightmap to terrain
        Vector3 position = transform.position;
        float[,] heights = new float[heightTexture.width, heightTexture.height];
        for (int x = 0; x < heightTexture.width; x++)
        {
            for (int y = 0; y < heightTexture.height; y++)
            {
                heights[x, y] = heightTexture.GetPixel(x, y).r; // Assuming height is in red channel
            }
        }
        // TODO: Apply heights to Unity Terrain (requires Terrain component reference)
    }

    protected override void ApplySplat(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        if (!ShouldApplySplat() || (!HasSplatMap0() && !HasSplatMap1())) return;

        Texture2D splat0 = splatmap0?.GetResource();
        Texture2D splat1 = splatmap1?.GetResource();

        // Placeholder: Apply splatmaps (e.g., cliff textures if AutoCliffSplat is true)
    }

    protected override void ApplyAlpha(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        if (!ShouldApplyAlpha() || !HasAlphaMap()) return;

        Texture2D alphaTexture = alphamap.GetResource();
        if (alphaTexture == null) return;

        // Placeholder: Apply alphamap for transparency
    }

    protected override void ApplyBiome(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        if (!ShouldApplyBiome() || !HasBiomeMap()) return;

        Texture2D biomeTexture = biomemap.GetResource();
        if (biomeTexture == null) return;

        // Placeholder: Apply biome data
    }

    protected override void ApplyTopology(Matrix4x4 localToWorld, Matrix4x4 worldToLocal)
    {
        if (!ShouldApplyTopology() || !HasTopologyMap()) return;

        Texture2D topologyTexture = topologymap.GetResource();
        if (topologyTexture == null) return;

        // Placeholder: Apply topology (e.g., cliff topology if AutoCliffTopology is true)
        if (RemoveExistingTopology)
        {
            // Logic to clear existing topology
        }
    }

    // Nested class for height application
    private sealed class HeightApplicator
    {
        public Matrix4x4 worldToLocal;
        public Monument parentMonument;
        public bool useBlendMap;
        public Texture2D blendData;    // Loaded from blendmap
        public Vector3 position;
        public Texture2D heightData;   // Loaded from heightmap

        public void ApplyHeightAtPoint(int x, int y)
        {
            if (heightData == null) return;

            float height = heightData.GetPixel(x, y).r;
            if (useBlendMap && blendData != null)
            {
                float blend = blendData.GetPixel(x, y).r;
                height *= blend; // Blend height based on blendmap
            }

            // Transform coordinates using worldToLocal and apply height
            Vector3 localPos = new Vector3(x, 0, y);
            Vector3 worldPos = parentMonument.transform.TransformPoint(localPos);
            // TODO: Apply to Terrain component
        }

        public HeightApplicator(Monument monument)
        {
            parentMonument = monument;
            worldToLocal = monument.transform.worldToLocalMatrix;
            position = monument.transform.position;
            useBlendMap = monument.HasBlendMap();
            blendData = monument.blendmap?.GetResource();
            heightData = monument.heightmap?.GetResource();
        }
    }

    // Nested class for splat application
    private sealed class SplatApplicator
    {
        public Monument parentMonument;
        public Matrix4x4 worldToLocal;
        public Texture2D splat0Data;   // Loaded from splatmap0
        public Texture2D splat1Data;   // Loaded from splatmap1
        public bool should0, should1, should2, should3, should4, should5, should6, should7;

        public void ApplySplatAtPoint(int x, int y)
        {
            // Placeholder: Apply splat layers based on conditions
            if (splat0Data != null && should0)
            {
                Color splatColor = splat0Data.GetPixel(x, y);
                // Apply splat layer 0 (e.g., ground texture)
            }
            if (splat1Data != null && should1)
            {
                Color splatColor = splat1Data.GetPixel(x, y);
                // Apply splat layer 1 (e.g., cliff texture)
            }
            // Extend for other layers as needed
        }

        public SplatApplicator(Monument monument)
        {
            parentMonument = monument;
            worldToLocal = monument.transform.worldToLocalMatrix;
            splat0Data = monument.splatmap0?.GetResource();
            splat1Data = monument.splatmap1?.GetResource();
            should0 = monument.ShouldApplySplatLayer(0);
            should1 = monument.ShouldApplySplatLayer(1);
            should2 = monument.ShouldApplySplatLayer(2);
            should3 = monument.ShouldApplySplatLayer(3);
            should4 = monument.ShouldApplySplatLayer(4);
            should5 = monument.ShouldApplySplatLayer(5);
            should6 = monument.ShouldApplySplatLayer(6);
            should7 = monument.ShouldApplySplatLayer(7);
        }
    }
}