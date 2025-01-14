using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;

public class ToggleHeightTools : MonoBehaviour
{
	public List<Toggle> tabToggles;
	public List<UIRecycleTree> recycleTrees;
	
    public void Awake(){
		Init();
	}
	
	public void Init(){
		Setup();
	}
	
	public void Setup()
	{
		for (int i = 0; i < tabToggles.Count; i++)
		{
			int capturedIndex = i; // Capture the loop variable to avoid closure issues
			tabToggles[i].onValueChanged.AddListener((isOn) => OnToggleChanged(capturedIndex, isOn));
		}

		// Ensure there's at least one toggle before setting it to true
		if (tabToggles.Count > 0)
		{
			tabToggles[0].isOn = true; // Changed from index 5 to 0 for safety, assuming 5 might not exist
		}
	}

	public void OnToggleChanged(int index, bool isOn)
	{
		if (isOn)
		{
			for (int i = 0; i < tabToggles.Count; i++)
			{
				tabToggles[i].SetIsOnWithoutNotify(i == index);
			}
			
			MainScript.Instance.paintMode = index;
		}
	}
}