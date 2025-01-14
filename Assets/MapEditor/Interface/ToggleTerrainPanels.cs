using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;
using RustMapEditor.Variables;
using RustMapEditor;
using static TerrainManager;


public class ToggleTerrainPanels : MonoBehaviour
{
	public List<Toggle> tabToggles;
	public List<GameObject> tabPanels;
	public List<UIRecycleTree> recycleTrees;
	Layers layers = new Layers() { Ground = TerrainSplat.Enum.Grass, Biome = TerrainBiome.Enum.Temperate, Topologies = TerrainTopology.Enum.Field};
	
	
    public void Awake(){
	}
	
	public void Start(){
		Setup();
		OnToggleChanged(0);
	}
	
	public void Setup()
	{
		// Check if either list is null before comparing counts
		if (tabToggles == null || tabPanels == null) {
			return;
		}

		if (tabToggles.Count != tabPanels.Count) {
			return;
		}

		for (int i = 0; i < tabToggles.Count; i++) {
			// Check if tabPanels[i] is not null before setting its active state
			if (i < tabPanels.Count && tabPanels[i] != null) {
				tabPanels[i].SetActive(false);
			}

			// Check if recycleTrees is not null before accessing it
			if (recycleTrees != null && i >= 0 && i < recycleTrees.Count && recycleTrees[i] != null) {
				recycleTrees[i].gameObject.SetActive(false);
			}

			int index = i; 
			// Check if tabToggles[i] is not null before adding the listener
			if (i < tabToggles.Count && tabToggles[i] != null) {
				tabToggles[i].onValueChanged.AddListener((isOn) => OnToggleChanged(index));
			}
		}

	}
	
    public void OnToggleChanged(int index)
    {
		TerrainManager.SetPreviewHoles(false);
		
		int layerIndex = 0;
		if (index!= 4){
			layerIndex=index;
		}
		TerrainManager.ChangeLayer((LayerType)layerIndex, TerrainTopology.TypeToIndex((int)layers.Topologies));
		MainScript.Instance.brushType = index;
		Debug.LogError("setting brush to index " + index);
		
        for (int i = 0; i < tabPanels.Count; i++)
        {
            bool isActive = i == index;

            tabPanels[i].SetActive(isActive);

            tabToggles[i].SetIsOnWithoutNotify(isActive);
            tabToggles[i].interactable = !isActive;

            if (recycleTrees.Count > i && recycleTrees[i] != null)
            {
                recycleTrees[i].gameObject.SetActive(isActive);
            }
        }
    }
}