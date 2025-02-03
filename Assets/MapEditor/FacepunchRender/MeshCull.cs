using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class MeshCull : LODComponent
{
    // Public Fields
	private float cullDistance=25f; // Distance at which to cull the mesh
    
	private Renderer meshCullRenderer;

	public override Renderer GetRenderer(){ return meshCullRenderer; }
 
	protected override void Awake()
	{
		if (!gameObject.TryGetComponent(out meshCullRenderer))
		{
			Debug.LogWarning($"No Renderer component found on {gameObject.name}. Disabling MeshCull component.");
			this.enabled = false; // Disable this script if no renderer is found
		}
	}
		
	protected override void Start() {
		HideObject();
	}


    protected override void CheckLOD(float distanceToCamera)
    {
		if (meshCullRenderer!=null){
			if (distanceToCamera > cullDistance)	{
				HideObject();
				return;
			}
				ShowObject();				
		}
    }

    private void HideObject()
    {
        meshCullRenderer.enabled = false;
    }

    private void ShowObject()
    {
		meshCullRenderer.enabled = true;
    }

    // Override this if you need to implement custom LOD behavior alongside culling
    protected override void UpdateLOD(int newLevel) { }

}