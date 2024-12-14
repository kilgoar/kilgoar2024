using UnityEngine;
using UnityEngine.Rendering;

public class MeshLOD : LODComponent
{
    // LOD Levels
    public int CurrentLODLevel { get; private set; }
    public int MaxLODLevel { get; private set; }
    public int MinLODLevel { get; private set; }

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private Material sharedMaterial; // For batching purposes

    public State[] States;

    protected override void Awake()
    {
        base.Awake();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        materialPropertyBlock = new MaterialPropertyBlock();
        
        if (States != null && States.Length > 0)
        {
            MinLODLevel = 0;
            MaxLODLevel = States.Length - 1;
        }
    }

    protected override void Start()
    {
        // Initialize shared material for batching if needed
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            sharedMaterial = meshRenderer.sharedMaterial;
        }

        //RefreshLOD(); // Ensure initial LOD is set
    }

    protected override void UpdateLOD(int newLevel)
    {
        if (newLevel < 0 || newLevel >= States.Length) return;

        ShowLOD(newLevel);
        CurrentLODLevel = newLevel;
    }
	
	protected override void CheckLOD(float distance)
		{
			if (States == null){
				return;
			}
			if (meshRenderer == null){
				return;
			}
			int newLevel = CalculateLODLevel(distance);
			if (newLevel != currentLODLevel){
				UpdateLOD(newLevel);
			}
		}

    private void ShowLOD(int level)
    {
            meshRenderer.enabled = true;
            meshRenderer.GetPropertyBlock(materialPropertyBlock);
            
            // Apply shared material for batching
            if (sharedMaterial != null)
                meshRenderer.sharedMaterial = sharedMaterial;

            // Setup shadow properties if needed
            meshRenderer.shadowCastingMode = States[level].shadowCastingMode;
            meshRenderer.receiveShadows = States[level].receiveShadows;

            // Update mesh for the correct LOD level
            if (meshFilter != null)
            {
                meshFilter.mesh = States[level].mesh; // Use sharedMesh for batching
            }
            
            // Apply any dynamic properties through the MaterialPropertyBlock
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        
    }
	
    public void SetMaterials(Material[] materials) 
    {
        if (materials.Length > 0)
        {
            sharedMaterial = materials[0];
        }
    }

    protected override int CalculateLODLevel(float distance)
    {
		if(States == null){
			return 0;
		}
		
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

        return 0; // This should not occur due to the previous checks
    }

    public Mesh GetMesh(out Matrix4x4 matrix) 
    {
        matrix = Matrix4x4.identity;
        if (CurrentLODLevel >= 0 && CurrentLODLevel < States.Length)
        {
            return States[CurrentLODLevel].mesh;
        }
        return null;
    }

    [System.Serializable]
    public class State
    {
        public float distance;
        public Mesh mesh;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;

    }
}