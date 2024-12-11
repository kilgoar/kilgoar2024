using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public class MeshRendererBatch : MeshBatch
{
	/*
    [SerializeField] private List<Mesh> meshes = new List<Mesh>();
    [SerializeField] private List<Material> materials = new List<Material>();
    private Vector3 setupPosition;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Mesh combinedMesh;
    private MaterialPropertyBlock propertyBlock;
    private bool isSetup = false;

    protected void Awake()
    {
        base.Awake();
        InitializeComponents();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void InitializeComponents()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
    }

    protected override void OnEnable() 
    {
        if (!isSetup)
        {
            Debug.LogWarning("MeshRendererBatch was enabled without being setup. Please call Setup first.");
        }
    }

    protected override void OnDisable() 
    {
        Free();
    }

    public override void Apply()
    {
        if (!isSetup) return;
        // Apply any last-minute changes before rendering
        UpdateMaterialProperties();
    }

    public override void Refresh()
    {
        // Recombine meshes if any have changed
        CombineMeshes();
    }

    public override void Alloc()
    {
        // Allocate resources, like creating a new combined mesh if it doesn't exist
        if (combinedMesh == null)
        {
            combinedMesh = new Mesh();
            combinedMesh.name = "CombinedMesh";
        }
    }

    public override void Display()
    {
        if (!isSetup) return;
        if (meshes.Count > 0)
        {
            Alloc(); // Ensure we have a combined mesh
            CombineMeshes();
            if (combinedMesh != null)
            {
                meshFilter.mesh = combinedMesh;
                UpdateMaterialProperties();
            }
        }
    }

    private void CombineMeshes()
    {
        CombineInstance[] combineInstances = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combineInstances[i].mesh = meshes[i];
            combineInstances[i].transform = Matrix4x4.TRS(setupPosition, Quaternion.identity, Vector3.one);
        }

        if (combinedMesh == null)
        {
            Alloc(); // Make sure we have a combined mesh before combining
        }

        combinedMesh.Clear();
        combinedMesh.CombineMeshes(combineInstances, false, false);
    }

    public override void Free()
    {
        if (combinedMesh != null)
        {
            DestroyImmediate(combinedMesh);
            combinedMesh = null;
        }
        meshes.Clear();
        materials.Clear();
        isSetup = false;
    }

    public override void Invalidate()
    {
        isSetup = false;
    }

    public void Setup(Vector3 position, Material material, 
                      ShadowCastingMode shadowMode, int layer, Color color)
    {
        setupPosition = position;
        meshRenderer.material = material;
        meshRenderer.shadowCastingMode = shadowMode;
        gameObject.layer = layer;
        
        propertyBlock.SetColor("_Color", color);
        UpdateMaterialProperties();
        isSetup = true;
    }

    private void UpdateMaterialProperties()
    {
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void AddMesh(Mesh mesh)
    {
        if (!meshes.Contains(mesh))
        {
            meshes.Add(mesh);
            Invalidate(); // Mark for re-combination
        }
    }

    public void RemoveMesh(Mesh mesh)
    {
        if (meshes.Remove(mesh))
        {
            Invalidate(); // Mark for re-combination
        }
    }

    public void ClearMeshes()
    {
        meshes.Clear();
        Invalidate(); // Mark for re-combination
    }

    public void AddMaterial(Material material)
    {
        if (!materials.Contains(material))
        {
            materials.Add(material);
        }
    }

    public int MeshCount => meshes.Count;
    public int MaterialCount => materials.Count;
	*/
}