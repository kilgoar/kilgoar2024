using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public abstract class MeshBatch : MonoBehaviour
{
	/*
    protected Mesh combinedMesh;
    protected List<Mesh> meshes = new List<Mesh>();
    protected List<Material> materials = new List<Material>();
    protected bool isDirty = true;
    protected MeshFilter meshFilter;
    protected Renderer renderer; // Can be MeshRenderer or SkinnedMeshRenderer

    protected virtual void Awake()
    {
        combinedMesh = new Mesh();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        renderer = gameObject.AddComponent<MeshRenderer>();
    }

    public virtual void Invalidate()
    {
        isDirty = true;
    }

    public virtual void Apply()
    {
        if (isDirty)
        {
            CombineMeshes();
            Display();
            isDirty = false;
        }
    }

    public virtual void Alloc()
    {
        // Ensure the combined mesh exists and is ready for writing
        if (combinedMesh == null)
        {
            combinedMesh = new Mesh();
        }
        combinedMesh.Clear();
    }

    public virtual void Free()
    {
        DestroyImmediate(combinedMesh);
        combinedMesh = null;
        meshes.Clear();
        materials.Clear();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    public virtual void Refresh()
    {
        if (isDirty)
        {
            CombineMeshes();
            Display();
        }
    }

    protected virtual void CombineMeshes()
    {
        Alloc(); // Ensure the combined mesh is ready

        CombineInstance[] combineInstances = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combineInstances[i].mesh = meshes[i];
            combineInstances[i].transform = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        }

        combinedMesh.CombineMeshes(combineInstances, false, false);
    }

    public virtual void Display()
    {
        // Update the mesh filter with the combined mesh
        if (meshFilter != null)
        {
            meshFilter.mesh = combinedMesh;
        }

        // Ensure the renderer has the correct materials
        if (renderer != null)
        {
            renderer.materials = materials.ToArray();
            renderer.enabled = true;
        }
    }

    protected virtual void OnEnable()
    {
        Display(); // Ensure the batched mesh is displayed when the object is enabled
    }

    protected virtual void OnDisable()
    {
        Free(); // Clean up when the object is disabled
    }

    // ... other methods remain the same ...

    public virtual void Setup(Vector3 position, Material material, 
                              ShadowCastingMode shadowMode, int layer, Color color)
    {
        transform.position = position;
        if (renderer != null)
        {
            renderer.shadowCastingMode = shadowMode;
            gameObject.layer = layer;
            
            // If we have a material, set it
            if (material != null)
            {
                renderer.material = material;
            }
        }
        // Color can be set using a MaterialPropertyBlock if needed for batching
    }
	*/
}