using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;

public class TogglePanels : MonoBehaviour
{
	public List<Toggle> tabToggles;
	public List<GameObject> tabPanels;
	public List<UIRecycleTree> recycleTrees;
	
	
    public void Awake(){
		Init();
	}
	
	public void Init(){
		Setup();
	}
	
	public void Setup()
	{
		if (tabToggles.Count != tabPanels.Count)    {
			return;
		}

		for (int i = 0; i < tabToggles.Count; i++)    {
			tabPanels[i].SetActive(false);
			
			if (i >= 0 && i < recycleTrees.Count && recycleTrees[i] != null)			{
					recycleTrees[i].gameObject.SetActive(false);
				}
			
			int index = i; 
			tabToggles[i].onValueChanged.AddListener((isOn) => OnToggleChanged(index));
		}

		if (tabToggles.Count > 0)    {
			tabToggles[5].SetIsOnWithoutNotify(true);
			tabPanels[5].SetActive(true);
		}
	}

	private void OnEnable()
    {
		OnToggleChanged(5);
	}
	
    public void OnToggleChanged(int index)
    {
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