using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TogglePanels : MonoBehaviour
{
	public List<Toggle> tabToggles;
	public List<GameObject> tabPanels;
	
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
        int index = i; 
        tabToggles[i].onValueChanged.AddListener((isOn) => OnToggleChanged(index));
    }

    if (tabToggles.Count > 0)    {
        tabToggles[0].isOn = true; 
		tabPanels[0].SetActive(true);
    }
}

public void OnToggleChanged(int index)	{
			for (int i = 0; i < tabPanels.Count; i++) {
				tabPanels[i].SetActive(i == index);
				tabToggles[i].SetIsOnWithoutNotify(i == index);
				tabToggles[i].interactable = (i != index);
			}
	
	}
}