using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class MeshCull : LODComponent
{
    public class CullState : LODState
    {
        public float cullDistance;
        public bool isCulled;
    }

    // Public Fields
    public float CullDistance = 100f; // Distance at which to cull the mesh

    // Private Fields
    private List<CullState> cullStates = new List<CullState>();

    protected override void Awake()
    {
        base.Awake();
        InitializeCullStates();
		
    }

    protected void InitializeCullStates()
    {
        // Assuming there's only one LOD state for the entire object
        var state = new CullState
        {
            meshFilter = GetComponent<MeshFilter>(),
            renderer = GetComponent<Renderer>(),
            cullDistance = CullDistance,
            isCulled = false
        };
        cullStates.Add(state);
    }


    protected override void CheckLOD(float distanceToCamera)
    {
		if (cullStates!=null){
			
        if (cullStates.Count == 0) return; // No states defined
        bool shouldBeCulled = distanceToCamera > cullStates[0].cullDistance;

			if (shouldBeCulled != cullStates[0].isCulled)        {
				if (shouldBeCulled)
				{
					CullObject();
				}
				else
				{
					UnCullObject();
				}
			}
		}
    }

    private void CullObject()
    {
        cullStates[0].Hide();
        cullStates[0].isCulled = true;
    }

    private void UnCullObject()
    {
        cullStates[0].Show(); 
        cullStates[0].isCulled = false;
    }

    // Override this if you need to implement custom LOD behavior alongside culling
    protected override void UpdateLOD(int newLevel) { }

}