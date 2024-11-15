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
	private static ItemsWindow _instance;
	
    void Start()   {		
		tree.onNodeCheckedChanged.AddListener(OnChecked);		
        query.onEndEdit.AddListener(OnQueryEntered);
		query.onValueChanged.AddListener(OnQuery);
		tree.onSelectionChanged.AddListener(OnSelect);
		deleteChecked.onClick.AddListener(DeleteCheckedNodes);
		checkAll.onClick.AddListener(CheckNodes);
		uncheckAll.onClick.AddListener(UncheckNodes);
		tree.onNodeDblClick.AddListener(FocusItem);
    }
	
    private void Awake()    {        
		if (_instance != null && _instance != this)        {
            Destroy(gameObject);
        }
        else    {
            _instance = this;
            DontDestroyOnLoad(gameObject); 
        }
    }
	
	void OnEnable()	{
		PopulateList();
	}
	
	public void OnChecked(Node parent){
		parent.ChangeIsCheckedStateForAllChildren(parent.isChecked);
	}
	
	public void FocusItem(Node node)
	{
		if (node?.data == null) return; 

		Vector3 targetPosition = Vector3.zero;
		bool positionFound = false; 
		
		if (node.TryCastData<PrefabDataHolder>(out PrefabDataHolder prefab))
		{
			targetPosition = prefab.transform.position;
			positionFound = true;
		}
		else if (node.TryCastData<Transform>(out Transform collection))
		{
			targetPosition = collection.position;
			positionFound = true;
		}

		if (positionFound)
		{
			CameraManager.Instance.SetCamera(targetPosition);
		}
	}



    public static ItemsWindow Instance
    {
        get
        {
            // If the instance doesn't exist, find it in the scene
            if (_instance == null)
            {
                _instance = FindObjectOfType<ItemsWindow>();
                
                // If no instance exists in the scene, create a new GameObject and attach the ItemsWindow
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("ItemsWindow");
                    _instance = singletonObject.AddComponent<ItemsWindow>();
                }
            }
            return _instance;
        }
    }
	
	public void PopulateList()
	{
		tree.nodes.Clear(); // Clear any existing nodes in the tree
		Dictionary<Transform, Node> transformToNodeMap = new Dictionary<Transform, Node>();

		Transform prefabParentTransform = PrefabManager.PrefabParent.transform;
		foreach (Transform child in prefabParentTransform)
		{
			BuildTreeRecursive(child, null, transformToNodeMap); // Build the tree recursively from each top-level child
		}

		tree.Rebuild(); // Update the tree view after all nodes are added
	}

	private void BuildTreeRecursive(Transform current, Node parentNode, Dictionary<Transform, Node> transformToNodeMap)
	{
		Node currentNode = null;

		switch (current.tag)
		{
			case "Prefab":
				if (current.GetComponent<PrefabDataHolder>() is PrefabDataHolder prefabData)
				{
					string prefabName = AssetManager.ToName(prefabData.prefabData.id);
					currentNode = new Node(prefabName) { data = prefabData };
				}
				break;

			case "Collection":
				currentNode = new Node(current.name) { data = current };
				break;

			default:
				return; // Skip if tag does not match
		}

		if (currentNode != null)
		{
			if (parentNode != null)
			{
				parentNode.nodes.AddWithoutNotify(currentNode);
			}
			else
			{
				tree.nodes.AddWithoutNotify(currentNode); // Add root-level nodes directly to the tree
			}

			transformToNodeMap[current] = currentNode; // Map the current transform to its node
		}

		for (int i = 0; i < current.childCount; i++)
		{
			BuildTreeRecursive(current.GetChild(i), currentNode, transformToNodeMap); // Recur on children
		}
	}
	
	private void OnSelect(Node selection)
	{
				if (selection.TryCastData<PrefabDataHolder>(out PrefabDataHolder prefab))				{
					PrefabManager.SetSelection(prefab);
					return;
				}
				
				if (selection.TryCastData<Transform>(out Transform collection))				{
					PrefabManager.SetSelection(collection);
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
		OnQuery(query.text);
		ToggleNodes(false);
	}
	
	private void CheckNodes(){
		OnQuery(query.text);
		ToggleNodes(true);
	}
	
	private void ToggleNodes(bool isChecked)	{
		
		foreach (Node node in matchingNodes)		{
			node.SetCheckedWithoutNotify(isChecked);
		}
		tree.Rebuild();
	}

	private void DeleteCheckedNodes()
	{
		DeleteCheckedNodesStack(tree.rootNode);
		tree.Rebuild();
	}

	private void DeleteCheckedNodesStack(Node rootNode)
	{
		Stack<Node> stack = new Stack<Node>();
		stack.Push(rootNode);

		while (stack.Count > 0)
		{
			Node currentNode = stack.Pop();

			for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
			{
				Node childNode = currentNode.nodes[i];

				if (!childNode.isChecked)
				{
					stack.Push(childNode);
					continue;
				}

				if (childNode.data is PrefabDataHolder childPrefabData && childPrefabData.gameObject != null)
				{
					Destroy(childPrefabData.gameObject);
					currentNode.nodes.RemoveAtWithoutNotify(i);
				}
				else if (childNode.data is Transform childTransform)
				{
					Destroy(childTransform.gameObject);
					currentNode.nodes.RemoveAtWithoutNotify(i);
				}
				else
				{
					stack.Push(childNode);
				}
			}

			if (!currentNode.isChecked)
				continue;

			if (currentNode.data is PrefabDataHolder prefabDataHolder && prefabDataHolder.gameObject != null)
			{
				Destroy(prefabDataHolder.gameObject);
				tree.nodes.RemoveWithoutNotify(currentNode);
				continue;
			}
			
			if (currentNode.data is Transform collection)
			{
				Destroy(collection.gameObject);
				tree.nodes.RemoveWithoutNotify(currentNode);
				
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
