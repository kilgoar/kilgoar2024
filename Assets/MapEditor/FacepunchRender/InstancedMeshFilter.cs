using UnityEngine;
using System;

public class InstancedMeshFilter : PrefabAttribute
{
    // Reference to the MeshRenderer component which will use the mesh from the prefab
    public MeshRenderer meshRenderer;
    
    // For managing Level of Detail of the renderer
    public RendererLOD rendererLOD;
    
    // For managing Level of Detail of the mesh itself
    public MeshLOD meshLOD;

    // Configuration for how this mesh should be instanced
    [NonSerialized]
    public Instancing.InstancedMeshConfig instancedMeshConfig;

    public InstancedMeshFilter()
    {
        // Constructor for initialization, if needed
    }

    // Override to specify that this is a MeshFilter type in the context of prefab attributes
    protected override Type GetPrefabAttributeType()
    {
        return typeof(MeshFilter);
    }

    // Method to get the type of this component for reflection or type checking
    protected virtual Type GetComponentType()
    {
        return typeof(InstancedMeshFilter);
    }

    // If there are specific types for shader, mesh, material, or texture related to this instanced mesh
    protected virtual Type GetShaderType() => null; // Placeholder for shader type if needed
    protected virtual Type GetMeshType() => typeof(Mesh); // Assuming it's always Mesh for mesh filters
    protected virtual Type GetMaterialType() => null; // Placeholder for material type if needed
    protected virtual Type GetTextureType() => null; // Placeholder for texture type if needed
}