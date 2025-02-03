using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RendererBatch : MonoBehaviour
{
    private bool UseBatchRendering = true;

    private bool EnableGPUInstancing = true;

    private int MaxVerticesPerSubmesh = 300;

    private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
	private Dictionary<Material, bool> processedMaterials = new Dictionary<Material, bool>();

    private List<MeshFilter> meshFilters = new List<MeshFilter>();
    private Dictionary<Material, bool> materialCache = new Dictionary<Material, bool>();


    void Awake()
    {
        //CollectRenderersAndFilters();
		//SetupForBatching();
    }
	
    private void CollectRenderersAndFilters()
    {
        var parent = transform.parent;
        if (parent != null)
        {
            var components = parent.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component is MeshRenderer mr && component.gameObject != gameObject)
                {
                    meshRenderers.Add(mr);
                }
                else if (component is MeshFilter mf && component.gameObject != gameObject)
                {
                    meshFilters.Add(mf);
                }
            }
        }
    }

    void Start()
    {

    }

    private void SetupForBatching()
    {
        Dictionary<Material, bool> processedMaterials = new Dictionary<Material, bool>();
        for (int i = 0; i < meshRenderers.Count; i++)
        {
            var mr = meshRenderers[i];
            if (mr != null && mr.sharedMaterial != null)
            {
                Material mat = mr.sharedMaterial;
                if (!processedMaterials.ContainsKey(mat))
                {
                    SetInstancingForMaterial(mat);
                    processedMaterials[mat] = true;
                }
                mr.enabled = !UseBatchRendering;
            }
        }
    }

    private void SetInstancingForMaterial(Material material)
    {
        if (material == null) return;
        if (materialCache.TryGetValue(material, out bool instanced))
        {
            if (instanced != EnableGPUInstancing)
            {
                material.enableInstancing = EnableGPUInstancing;
                materialCache[material] = EnableGPUInstancing;
            }
        }
        else
        {
            material.enableInstancing = EnableGPUInstancing;
            materialCache[material] = EnableGPUInstancing;
        }
    }


    public bool CanBeBatched()
    {
        return meshRenderers.All(mr => mr == null || (mr.sharedMaterial != null && (materialCache.ContainsKey(mr.sharedMaterial) || mr.sharedMaterial.enableInstancing))) &&
               meshFilters.All(mf => mf == null || (mf.sharedMesh != null && mf.sharedMesh.vertexCount <= MaxVerticesPerSubmesh));
    }

}