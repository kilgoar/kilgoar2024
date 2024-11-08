using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;
using static WorldSerialization;

public class ItemsWindow : MonoBehaviour
{
    public UIRecycleTree tree;
	public InputField query;
	public Button deleteChecked, checkAll, uncheckAll;
	private int currentMatchIndex = 0; 
	private List<Node> matchingNodes = new List<Node>();
	
	
    void Start()   {
        query.onEndEdit.AddListener(OnQueryEntered);
		query.onValueChanged.AddListener(OnQuery);
		tree.onSelectionChanged.AddListener(OnSelect);
		deleteChecked.onClick.AddListener(DeleteCheckedNodes);
		checkAll.onClick.AddListener(CheckNodes);
		uncheckAll.onClick.AddListener(UncheckNodes);
    }
	
	

	void OnEnable()
	{
		PopulateList();
	}
	
	
	
	private void PopulateList()	{
		tree.Clear();

		if( PrefabManager.CurrentMapPrefabs!=null){
			foreach (PrefabDataHolder prefab in PrefabManager.CurrentMapPrefabs)
			{
				string prefabName = AssetManager.ToName(prefab.prefabData.id);
				Node newNode = new Node(prefabName);
				newNode.data = prefab;
				tree.nodes.Add(newNode);
			}
		}
	}
	
	private void OnSelect(Node selection)
	{
				if (selection.TryCastData<PrefabDataHolder>(out PrefabDataHolder prefab))
				{
					PrefabManager.SetSelection(prefab);
				}
				else
				{
					Debug.LogWarning("Prefab Data Holder mismatch (transform tool)");
				}
	}
	
	private void OnQuery(string query)
	{
		if (!string.IsNullOrEmpty(query))
		{
			matchingNodes = FindNodesByPartRecursive(tree.rootNode, query);
			
			if (matchingNodes.Count > 0)        {
				Node firstMatch = matchingNodes[0];
				firstMatch.isSelected = true;
				tree.FocusOn(firstMatch);
			}
			return;
		}
		matchingNodes = FindNodesByPartRecursive(tree.rootNode, "");
	}
	
	private void UncheckNodes(){
		ToggleNodes(false);
	}
	
	private void CheckNodes(){
		OnQuery(query.text);
		ToggleNodes(true);
	}
	
	private void ToggleNodes(bool isChecked)	{
		
		foreach (Node node in matchingNodes)		{
			node.isChecked = isChecked;
		}
	}

	
	private void DeleteCheckedNodes()
	{
		DeleteCheckedNodesRecursive(tree.rootNode);
	}

	private void DeleteCheckedNodesRecursive(Node currentNode)
	{
		for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
		{
			Node childNode = currentNode.nodes[i];
			DeleteCheckedNodesRecursive(childNode);

			if (childNode.isChecked)
			{
				// Assuming 'data' is a PrefabDataHolder and has a reference to the associated GameObject
				if (childNode.data is PrefabDataHolder prefabDataHolder && prefabDataHolder.gameObject != null)				{
					
					Destroy(prefabDataHolder.gameObject);
					childNode.RemoveYourself();
				}
				else
				{
					Debug.LogWarning($"Node data is not a PrefabDataHolder or missing GameObject reference. Could not destroy: {childNode.name}");
				}

				currentNode.RemoveYourself();
			}
		}
	}
		
	private List<Node> FindNodesByPartRecursive(Node currentNode, string query){
	
    List<Node> matches = new List<Node>();

		if (currentNode.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)    {
			matches.Add(currentNode);
		}

		foreach (Node child in currentNode.nodes)    {
			matches.AddRange(FindNodesByPartRecursive(child, query));
		}

		return matches;
	}
	


	private void FocusNextMatch()
	{
		if (matchingNodes.Count == 0) return;

		currentMatchIndex = (currentMatchIndex + 1) % matchingNodes.Count;

		Node nextMatch = matchingNodes[currentMatchIndex];
		nextMatch.isSelected = true;
		tree.FocusOn(nextMatch);
	}
	
	
	private void OnQueryEntered(string query)
	{
		if (!string.IsNullOrEmpty(query))
		{
			FocusNextMatch();
		}
	}

}
