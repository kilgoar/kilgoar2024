using System;
using UnityEngine;
using UnityEngine.Rendering;

public class RendererLOD : LODComponent
{
    public float LODBias = 0.7f;
    public State[] States;

    private MaterialPropertyBlock materialPropertyBlock;
    private int currentLODLevel = -1;
	private int newLevel = -1;
    private bool isVisible = true;
    private bool isCollapsed = false;
    private bool hasInitialized = false;

    private Renderer collapsedRenderer;
    private MeshFilter collapsedFilter;
    private Material sharedMaterial; // Shared material for batching
	
    private void Awake()
    {
        InitializeSharedComponents();
    }

    private void Start() 
    {	
		base.Awake();
        if (States == null || States.Length == 0)
        {
            Debug.LogError($"{gameObject.name} does not have LOD States set up!");
            return;
        }

        // Assign shared components to all States that lack their own
        for (int i = 0; i < States.Length; i++)
        {
            if (States[i].renderer == null) States[i].renderer = collapsedRenderer;
            if (States[i].meshFilter == null) States[i].meshFilter = collapsedFilter;
        }

        hasInitialized = true;
		ClearLODs(); //hide all LODs
        RefreshLOD(); // Start with the correct LOD
    }
	
	protected override void CheckLOD(float distance)
		{
			newLevel = CalculateLODLevel(distance);
			if (newLevel != currentLODLevel){
				UpdateLOD(newLevel);
			}
		}
	
	private void InitializeSharedComponents()
    {
        if (collapsedRenderer == null)      // remove these get components
        {
            collapsedRenderer = BatchManager.collapsedRenderer;
            collapsedFilter = BatchManager.collapsedFilter;
        }
    }
	
	private void ClearLODs()
    {
        if (States == null) return; // Ensure States is not null

        for (int i = 0; i < States.Length; i++)
        {
            States[i].Hide(); // Hide each state with immediate rendering off
        }
        currentLODLevel = -1; // Reset the current LOD level to none
    }

    protected override void UpdateLOD(int level)
    {
        if (level == currentLODLevel || level < 0 || level >= States.Length) return;
        if (currentLODLevel != -1)
        {
            States[currentLODLevel].Hide();
        }
        States[level].Show();
        currentLODLevel = level;
    }
	
    public RendererLOD() 
    {
        // Empty constructor, no initialization here
    }

    public void SetMaterials(Material[] materials) 
    {
        if (materials.Length > 0)
        {
            sharedMaterial = materials[0]; // Assume all materials are the same for simplicity
            for (int i = 0; i < States.Length; i++)
            {
                States[i].SetMaterial(sharedMaterial, 0);
            }
        }
    }

    public void ReplaceMaterials(Material[] oldMaterials, Material[] newMaterials) 
    {
        if (newMaterials.Length > 0)
        {
            sharedMaterial = newMaterials[0];
            for (int i = 0; i < States.Length; i++)
            {
                States[i].SetMaterial(sharedMaterial, 0);
            }
        }
    }

    public Mesh GetMesh() 
    {
        if (currentLODLevel >= 0 && currentLODLevel < States.Length)
        {
            return States[currentLODLevel].GetMesh();
        }
        return null;
    }

    protected override int CalculateLODLevel(float distance)
    {
		if(States!= null){
			if (States.Length == 0) return 0;
			
			if (distance >= States[States.Length - 1].distance)
			{
				return States.Length - 1;  
			}
			for (int i = 0; i < States.Length - 1; i++)
			{
				if (distance >= States[i].distance && distance < States[i + 1].distance)
				{
					return i;  
				}
			}
		}
        return -1; // This should not occur due to the previous checks
    }

    [Serializable]
    public class State
    {
        public float distance;
        public Renderer renderer;
        public MeshFilter meshFilter;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
        public bool hasCached;
        public Material[] stateMaterials;
        public ShadowCastingMode cachedShadowMode;

        public void Show() 
        {
			if(renderer != null){
				
            renderer.enabled = true;
			}
        }

        public void Hide() 
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        public void SetMaterial(Material material, int index) 
        {
            if (renderer != null)
            {
                renderer.sharedMaterial = material; // Ensure batching
                if (stateMaterials == null || stateMaterials.Length <= index)
                {
                    Array.Resize(ref stateMaterials, index + 1);
                }
                stateMaterials[index] = material;
            }
        }

        public void CacheMaterials() 
        {
            if (renderer != null)
            {
                stateMaterials = renderer.sharedMaterials;
                cachedShadowMode = renderer.shadowCastingMode;
                hasCached = true;
            }
        }

        public Mesh GetMesh() 
        {
            return meshFilter.mesh;
        }
    }
}