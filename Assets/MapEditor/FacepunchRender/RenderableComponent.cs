using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public abstract class RenderableComponent : MonoBehaviour
{
    protected Material material;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;

    public virtual void Toggle(bool enable)
    {
        gameObject.SetActive(enable);
    }

    public virtual void SetMaterial(Material newMaterial) 
    {
        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial = newMaterial;
        }
    }

    protected virtual void UpdateColor(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.sharedMaterial.color = color;
        }
    }
}