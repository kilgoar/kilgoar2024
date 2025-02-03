using UnityEngine;
using System;
using System.Collections.Generic;

public class LODMasterMesh : LODComponent
{
    public MeshRenderer ReplacementMesh;
    public float Distance;
    public LODComponent[] ChildComponents;    //this gives us convenient access to all the LOD components in the monument.
    public bool Block;
    public Bounds MeshBounds;

    private Material _originalMaterial;
    private int _currentLODLevel = -1;
    private bool _isInitialized;
    private bool _isDirty;

    // Constructor
    public LODMasterMesh()
    {
        // Initialization could be done here if needed
    }

    protected override void Awake()
    {
        base.Awake();
        _isInitialized = false;
        _isDirty = true;
    }

    protected override void Start()
    {
        base.Start();
        if (!_isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (ReplacementMesh != null)
        {
            _originalMaterial = ReplacementMesh.sharedMaterial;
        }
        _isInitialized = true;
        RefreshLOD();
    }

    protected override int CalculateLODLevel(float distance)
    {

        if (distance < Distance)
        {
            return 0;
        }

        return 1;
    }
	
	protected override void CheckLOD(float distance)
		{
			int newLevel = CalculateLODLevel(distance);
			
			if (newLevel != currentLODLevel){
				UpdateLOD(newLevel);
			}
		}

    protected override void UpdateLOD(int newLevel)
    {
		ReplacementMesh.enabled = newLevel > 0;
    }
	
	public List<Renderer> FetchRenderers(){
		List<Renderer> renderers = new List<Renderer>();
		
		MeshRenderer siblingMeshRenderer = GetComponent<MeshRenderer>();
		if (siblingMeshRenderer != null)
		{
			renderers.Add(siblingMeshRenderer);
		}
		
		foreach (LODComponent lod in ChildComponents)
		{
			if(lod is RendererLOD){
				renderers.AddRange(lod.RendererList());
			}
			if(lod is TreeLOD){
				renderers.AddRange(lod.RendererList());
			}
			if(lod is MeshLOD){
				renderers.AddRange(lod.RendererList());
			}
			if(lod is MeshCull){
				renderers.Add(lod.GetRenderer());
			}
		}
		return renderers;
	}

    // Inner class for representing LOD states
    public class LODState
    {
        public float distance;
        public MeshFilter meshFilter;
        public Renderer renderer;
        public Mesh mesh;
        public bool isVisible;

        public void Show()
        {
            if (!isVisible)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
                if (meshFilter != null && mesh != null)
                {
                    meshFilter.mesh = mesh;
                }
                isVisible = true;
            }
        }

        public void Hide()
        {
            if (isVisible)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
                isVisible = false;
            }
        }
    }
}