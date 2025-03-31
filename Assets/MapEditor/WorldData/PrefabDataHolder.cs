using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using static TerrainManager;
using static AreaManager;

[SelectionBase, DisallowMultipleComponent]
public class PrefabDataHolder : MonoBehaviour
{
    public WorldSerialization.PrefabData prefabData;
    public Area currentSector; // To keep track of which sector this prefab belongs to
    public List<LODComponent> lodComponents = new List<LODComponent>(); //  LOD component list
	

	public void AddLODs(List<LODComponent> newLODs)
	{
		lodComponents.AddRange(newLODs);
	}

    // Use this for initialization
    void Awake()
    {

    }
	
	void Start()
	{
		UpdatePrefabData();
	}

    public void Setup()
    {
		/*
        #if UNITY_EDITOR
        for (int i = 0; i < GetComponents<Component>().Length; i++)
            ComponentUtility.MoveComponentUp(this);
        #endif
		*/
    }

    // Update prefab data including position, rotation, and scale
    public void UpdatePrefabData()
    {
		
        prefabData.position = gameObject.transform.localPosition;
        prefabData.rotation = transform.rotation;
        prefabData.scale = transform.localScale;
        UpdateSectorMembership(); // Also update which sector this prefab belongs to
		
    }

    // This method updates the sector membership of the prefab
    private void UpdateSectorMembership(Area newSector = null)
    {
        Area sector = newSector ?? AreaManager.FindSector(transform.position);
        
        if (currentSector != null)
        {
            // Remove from old sector if it was in one
            List<PrefabDataHolder> oldList = AreaManager.GetPrefabsInSector(currentSector);
            if (oldList.Contains(this))
            {
                oldList.Remove(this);
            }
        }

        // Add to new sector
        AreaManager.AddPrefabToSector(sector, this);
        currentSector = sector;
    }
	
	public void Refresh()
	{
		foreach (LODComponent lodComponent in lodComponents)	{
		lodComponent.RefreshLOD();
			
		}				
	}

    public void UpdatePrefabDataWrong()
    {
        prefabData.position = gameObject.transform.position;
        prefabData.rotation = transform.rotation;
        prefabData.scale = transform.localScale;
    }

/*
    public void EnableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
            collider.enabled = true;
    }

    public void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
            collider.enabled = false;
    }
*/
    public void AlwaysBreakPrefabs()
    {
		if (prefabData.id == 0){
			Destroy(this);
			return;
		}
		prefabData.position = transform.position-TerrainManager.MapOffset;
        //prefabData.position = gameObject.transform.localPosition;
        prefabData.rotation = gameObject.transform.rotation.eulerAngles;
        prefabData.scale = gameObject.transform.lossyScale;        
        prefabData.category = prefabData.category.Contains(":") ? "decor" : prefabData.category;
    }
    
    public void CastPrefabData()
    {
        gameObject.transform.localPosition = prefabData.position;
        transform.rotation = prefabData.rotation;
        transform.localScale = prefabData.scale;
    }
     
    public void SnapToGround()
    {
        Vector3 newPos = transform.position;
        #if UNITY_EDITOR
        Undo.RecordObject(transform, "Snap to Ground");
        #endif
        newPos.y = Land.SampleHeight(transform.position);
        transform.position = newPos;
    }

    public void ToggleLights()
    {
        foreach (var item in gameObject.GetComponentsInChildren<Light>(true))
            item.enabled = !item.enabled;
    }
}