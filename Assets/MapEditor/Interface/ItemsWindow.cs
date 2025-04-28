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
	public Text footer;
    public UIRecycleTree tree;
	public InputField query;
	public Button deleteChecked, checkAll, uncheckAll, findInBreaker, saveCustom, applyTerrain;
	private int currentMatchIndex = 0; 
	private List<Node> matchingNodes = new List<Node>();
	public List<InputField> prefabDataFields = new List<InputField>();
	public List<InputField> snapFields = new List<InputField>();
	
	public static ItemsWindow Instance { get; private set; }
	

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;            
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
	
	private void Start(){
		InitializeComponents();
	}

    public void SetSelection(GameObject go)
    {
        if (go == null)
        {
            DefaultPrefabData(); // Clear UI if no object
            return;
        }

        RetrievePrefabData(go); // Just fetch and display data directly
    }

    public void UpdateData()
    {
        if (CameraManager.Instance._selectedObjects.Count > 0)
        {
            GameObject lastSelected = CameraManager.Instance._selectedObjects[^1]; // Last item via index accessor
            SetSelection(lastSelected);
        }
        else
        {
            DefaultPrefabData();
        }
    }

    public void RetrievePrefabData(GameObject go)
    {
        DestroyListeners();

        PrefabDataHolder prefabHolder = go.GetComponent<PrefabDataHolder>();
        bool isCollection = go.CompareTag("Collection");
		saveCustom.interactable = false;
		
		TerrainPlacement terrainPlacement = go.GetComponent<TerrainPlacement>();
        applyTerrain.interactable = (terrainPlacement != null);
        
		
        if (prefabHolder != null && !isCollection) // Prefab with data
        {
            PrefabData data = prefabHolder.prefabData;
            prefabHolder.UpdatePrefabData(); // Ensure data is fresh

            prefabDataFields[0].text = data.position.x.ToString("F3");
            prefabDataFields[1].text = data.position.y.ToString("F3");
            prefabDataFields[2].text = data.position.z.ToString("F3");

            prefabDataFields[3].text = data.rotation.x.ToString("F3");
            prefabDataFields[4].text = data.rotation.y.ToString("F3");
            prefabDataFields[5].text = data.rotation.z.ToString("F3");

            prefabDataFields[6].text = data.scale.x.ToString("F3");
            prefabDataFields[7].text = data.scale.y.ToString("F3");
            prefabDataFields[8].text = data.scale.z.ToString("F3");

            prefabDataFields[9].text = data.category;
            prefabDataFields[10].text = data.id.ToString();
        }
        else // Collection or no prefab data, use transform
        {
            Transform transform = go.transform;
            prefabDataFields[0].text = transform.position.x.ToString("F3");
            prefabDataFields[1].text = transform.position.y.ToString("F3");
            prefabDataFields[2].text = transform.position.z.ToString("F3");

			Vector3 rotation = transform.eulerAngles;
			prefabDataFields[3].text = rotation.x.ToString("F3");
			prefabDataFields[4].text = rotation.y.ToString("F3");
			prefabDataFields[5].text = rotation.z.ToString("F3");
			
            prefabDataFields[6].text = transform.localScale.x.ToString("F3");
            prefabDataFields[7].text = transform.localScale.y.ToString("F3");
            prefabDataFields[8].text = transform.localScale.z.ToString("F3");

            prefabDataFields[9].text = isCollection ? go.name : string.Empty;
            prefabDataFields[10].text = isCollection ? string.Empty : go.name;
			saveCustom.interactable = true;
        }

        CreateListeners(go); // Pass the GameObject to listeners
    }

	public void SendPrefabData(GameObject go)
	{
		// Parse transform data from fields
		Vector3 position = new Vector3(
			float.Parse(prefabDataFields[0].text),
			float.Parse(prefabDataFields[1].text),
			float.Parse(prefabDataFields[2].text));

		Vector3 rotation = new Vector3(
			float.Parse(prefabDataFields[3].text),
			float.Parse(prefabDataFields[4].text),
			float.Parse(prefabDataFields[5].text));

		Vector3 scale = new Vector3(
			float.Parse(prefabDataFields[6].text),
			float.Parse(prefabDataFields[7].text),
			float.Parse(prefabDataFields[8].text));


		// Handle PrefabData if it exists (prefabs)
		PrefabDataHolder holder = go.GetComponent<PrefabDataHolder>();
		if (holder != null && !go.CompareTag("Collection"))
		{
			PrefabData data = holder.prefabData;
			data.position = new VectorData(position.x, position.y, position.z);
			data.rotation = new VectorData(rotation.x, rotation.y, rotation.z);
			data.scale = new VectorData(scale.x, scale.y, scale.z);
			data.category = go.name; // Sync category with name
			data.id = uint.Parse(prefabDataFields[10].text);
			holder.CastPrefabData(); // Sets transform for prefabs
		}
		else if (go.CompareTag("Collection"))
		{
			// For collections, set transform directly since no PrefabData
			go.transform.position = position;
			go.transform.rotation = Quaternion.Euler(rotation);
			go.transform.localScale = scale;
			
			// Update name from category field
			go.name = prefabDataFields[9].text;
			    Node node = tree.FindFirstNodeByDataRecursive(go);
			if (node != null)
			{
				node.name = go.name;
			}
		}



	}

    public void DefaultPrefabData()
    {
        DestroyListeners();
        for (int i = 0; i < prefabDataFields.Count; i++)
        {
            prefabDataFields[i].text = string.Empty;
        }
    }

    public void CreateListeners(GameObject go)
    {
        for (int i = 0; i < 9; i++)
        {
            int index = i;
            prefabDataFields[i].onEndEdit.AddListener(text =>
            {
                if (float.TryParse(text, out float value))
                {
                    UpdatePrefabData(go, index, value);
                }
                SendPrefabData(go);
            });
        }

        prefabDataFields[9].onEndEdit.AddListener(text => 
        {
                SendPrefabData(go);
        });

        prefabDataFields[10].onEndEdit.AddListener(text =>
        {
            if (uint.TryParse(text, out uint id) && go.GetComponent<PrefabDataHolder>() is PrefabDataHolder holder)
            {
                holder.prefabData.id = id;
                SendPrefabData(go);
            }
        });
    }

    private void UpdatePrefabData(GameObject go, int index, float value)
    {
        PrefabDataHolder holder = go.GetComponent<PrefabDataHolder>();
        if (holder == null) return;

        PrefabData data = holder.prefabData;
        int vectorIndex = index % 3;
        switch (index / 3)
        {
            case 0: // Position
                if (vectorIndex == 0) data.position.x = value;
                else if (vectorIndex == 1) data.position.y = value;
                else data.position.z = value;
                break;
            case 1: // Rotation
                if (vectorIndex == 0) data.rotation.x = value;
                else if (vectorIndex == 1) data.rotation.y = value;
                else data.rotation.z = value;
                break;
            case 2: // Scale
                if (vectorIndex == 0) data.scale.x = value;
                else if (vectorIndex == 1) data.scale.y = value;
                else data.scale.z = value;
                break;
        }
    }
	
	private void InitializeComponents()
	{	

		if (tree != null)
		{
			tree.onNodeCheckedChanged.AddListener(OnChecked);
			tree.onSelectionChanged.AddListener(OnSelect);
			tree.onNodeDblClick.AddListener(FocusItem);
		}

		if (query != null)
		{
			query.onEndEdit.AddListener(OnQueryEntered);
			query.onValueChanged.AddListener(OnQuery);
		}

		deleteChecked.onClick.AddListener(DeleteCheckedNodes);
		checkAll.onClick.AddListener(CheckNodes);
		uncheckAll.onClick.AddListener(UncheckNodes);
		findInBreaker.onClick.AddListener(FindBreakerWithPath);
		applyTerrain.onClick.AddListener(ApplyTerrain);
        applyTerrain.interactable = false;
	

		saveCustom.onClick.AddListener(() =>
		{
			if (CameraManager.Instance._selectedObjects.Count > 0)
			{
				SaveCollection(CameraManager.Instance._selectedObjects[^1]);
			}
			else
			{
				Debug.LogWarning("No object selected to save as a collection.");
			}
		});
		
	}
	
	private void ApplyTerrain()
    {
        if (CameraManager.Instance._selectedObjects.Count == 0) return;
        
        GameObject selected = CameraManager.Instance._selectedObjects[^1];
        TerrainPlacement terrainPlacement = selected.GetComponent<TerrainPlacement>();
        
        if (terrainPlacement != null && terrainPlacement.ShouldHeight())
        {
            TerrainBounds dimensions = new TerrainBounds();
            Vector3 position = selected.transform.position;
            Quaternion rotation = selected.transform.rotation;
            Vector3 scale = selected.transform.localScale;
            
            terrainPlacement.ApplyHeight(position, rotation, scale, dimensions);
			terrainPlacement.ApplySplat(position, rotation, scale, dimensions);
            Debug.Log($"Applied height map to terrain at {position}");
        }
        else
        {
            Debug.LogWarning("No valid TerrainPlacement component with height map found on selected object");
        }
    }
	
	public void SaveCollection(GameObject go)
	{
		// Check if the GameObject is valid
		if (go == null)
		{
			Debug.LogWarning("No collection provided to save.");
			return;
		}

		string savePath = System.IO.Path.Combine(SettingsManager.AppDataPath(), "custom", $"{go.name}.prefab");

		// Ensure the 'custom' directory exists
		string customDir = System.IO.Path.Combine(SettingsManager.AppDataPath(), "custom");
		if (!System.IO.Directory.Exists(customDir))
		{
			System.IO.Directory.CreateDirectory(customDir);
		}

		// Save the collection using MapManager
		MapManager.SaveCollectionPrefab(savePath, go.transform);

		Debug.Log($"Collection saved to: {savePath}");
	}
	
	public void FindBreakerWithPath(){
		AppManager.Instance.ActivateWindow(5);
		if(BreakerWindow.Instance!=null){
			BreakerWindow.Instance.FocusByPath(prefabDataFields[9].text);
		}
	}
	
	public void DestroyListeners()
	{
		for (int i = 0; i < prefabDataFields.Count; i++)
		{
			prefabDataFields[i].onEndEdit.RemoveAllListeners();
		}
	}
	


	
	void OnEnable()	{
		PopulateList();
		FocusItem(CameraManager.Instance._selectedObjects);
	}
	
	public void OnChecked(Node node){
		//node.SetSelectedWithoutNotify(true);
		OnSelect(node);
	}
	
	public void FocusItem(Node node)
	{
		if (node?.data == null) return; 

		Vector3 targetPosition = Vector3.zero;
		GameObject go = (GameObject)node.data;
		targetPosition = go.transform.position;
		CameraManager.Instance.SetCamera(targetPosition);
		
		node.SetCheckedWithoutNotify(true);
		//tree.Rebuild();
	}

	public void FocusList(Node node)
	{
		tree.FocusOn(node);
		//node.isSelected=true;
		//node.SetCheckedWithoutNotify(true);
		//tree.Rebuild();
	}
	
	public void FocusItem(List<GameObject> selections){
		int itemCount = selections.Count;
		if(itemCount > 0){
			Node lastNode = tree.FindFirstNodeByDataRecursive(selections[itemCount-1]);
			tree.FocusOn(lastNode);
			//lastNode.SetSelectedWithoutNotify(true);
		}

	}
public void PopulateList()
{
    tree.nodes.Clear();
    Dictionary<Transform, Node> transformToNodeMap = new Dictionary<Transform, Node>();

    foreach (Transform child in PrefabManager.PrefabParent)
    {
        BuildTreeRecursive(child, null, transformToNodeMap);
    }
    foreach (Transform child in PathManager.PathParent)
    {
        BuildTreeRecursive(child, null, transformToNodeMap);
    }

    CheckSelection();
    tree.Rebuild();
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
                currentNode = new Node(prefabName) { data = current.gameObject };
            }
            break;

        case "Collection":
            currentNode = new Node(current.name) { data = current.gameObject };
            break;

        case "Path":
            currentNode = new Node(current.name) { data = current.gameObject };
            break;

        case "NodeParent":
            // Handle the "Nodes" GameObject that contains individual nodes
            currentNode = new Node(current.name) { data = current.gameObject };
            break;

        case "Node":
            // Handle individual nodes under a NodeParent
            currentNode = new Node(current.name) { data = current.gameObject };
            break;

        case "EasyRoads":
            break;

        default:
            return; // Skip other tags
    }

    if (currentNode != null)
    {
        // Safeguard: Ensure data is a valid GameObject
        if (currentNode.data is GameObject go && go != null && go.activeInHierarchy)
        {
            if (parentNode != null)
            {
                parentNode.nodes.AddWithoutNotify(currentNode);
            }
            else
            {
                tree.nodes.AddWithoutNotify(currentNode);
            }

            transformToNodeMap[current] = currentNode;
        }
        else
        {
            Debug.LogWarning($"Node '{current.name}' has invalid or destroyed data: {currentNode.data}. Skipping.");
            return;
        }
    }

    // Recursively build the tree for all children
    for (int i = 0; i < current.childCount; i++)
    {
        BuildTreeRecursive(current.GetChild(i), currentNode, transformToNodeMap);
    }
}
	
public void OnSelect(Node node)
{
    GameObject goSelect = (GameObject)node.data;
	
	//allow editing of nodes and clicking on node parent
	if(!goSelect.CompareTag("Node") && !goSelect.CompareTag("NodeParent")){
		ClearRoads();
	}

    // Clear path selection only if selecting a non-path-related object
    if (goSelect.CompareTag("Path") || 
        goSelect.CompareTag("NodeParent") || 
        goSelect.CompareTag("Node"))
    {
		    if (node.data == CameraManager.Instance._selectedRoad)
			{
				//don't reselect
				node.isChecked = false;
				return; 
			}
		
    }

    if (goSelect.CompareTag("Path"))
    {		 
        CameraManager.Instance.Unselect();
        UnselectAllInTree();
		node.isChecked = false;
        SelectRoad(node, goSelect);	
		PopulateList();
		PathWindow.Instance?.UpdateData();
		//FocusFirstNode(node, goSelect);
        return;
    }
	
	if (goSelect.CompareTag("NodeParent")){
		//don't select node collections
		node.isChecked = false;
		return;
	}
	

    // Non-path selection logic (e.g., prefabs, collections, or nodes)
    if (!Keyboard.current.leftShiftKey.isPressed)
    {
        CameraManager.Instance.Unselect();
        UnselectAllInTree();
    }

    if (Keyboard.current.leftAltKey.isPressed)
    {
        SelectChildren(node, true);
        node.ExpandAll();
    }

    node.SetCheckedWithoutNotify(true);
    CameraManager.Instance.SelectPrefabWithoutNotify(goSelect);
    CameraManager.Instance.NotifySelectionChanged();
}

public void ClearRoads()
{

    GameObject[] allPaths = GameObject.FindGameObjectsWithTag("Path");
    foreach (GameObject path in allPaths)
    {
        CameraManager.Instance.DepopulateNodesForRoad(path);
    }
}

public void SelectRoad(Node node, GameObject roadObject)
{
    PathDataHolder pathDataHolder = roadObject.GetComponent<PathDataHolder>();
    if (pathDataHolder == null || pathDataHolder.pathData == null)
    {
        Debug.LogWarning($"Invalid PathDataHolder for '{node.name}'. Clearing selection.");
        return;
    }
    
    GameObject firstNodeGO = CameraManager.Instance.PopulateNodesForRoad(roadObject);
    if (firstNodeGO == null)
    {
        Debug.LogWarning($"Failed to populate nodes for '{roadObject.name}'.");
        return;
    }

    CameraManager.Instance._selectedRoad = pathDataHolder.pathData;
    CameraManager.Instance._selectedObjects.Add(firstNodeGO);
    CameraManager.Instance.SelectPrefabWithoutNotify(firstNodeGO);

    node.nodes.Clear();
    GameObject nodeCollectionGO = firstNodeGO.transform.parent.gameObject;
    Node collectionNode = new Node("Nodes") { data = nodeCollectionGO };
    node.nodes.AddWithoutNotify(collectionNode);

    NodeCollection nodeCollection = nodeCollectionGO.GetComponent<NodeCollection>();
    foreach (Transform nodeTransform in nodeCollection.GetNodes())
    {
        if (nodeTransform != null)
        {
            Node childNode = new Node(nodeTransform.name) { data = nodeTransform.gameObject };
            collectionNode.nodes.AddWithoutNotify(childNode);
        }
    }

    UpdateData();
    CameraManager.Instance.NotifySelectionChanged();
    CameraManager.Instance.UpdateGizmoState();
}

public void FocusFirstNode(Node node, GameObject roadObject)
{
    GameObject firstNodeGO = CameraManager.Instance.PopulateNodesForRoad(roadObject);
    if (firstNodeGO == null)
    {
        Debug.LogWarning($"Failed to populate nodes for '{roadObject.name}' in FocusFirstNode.");
        return;
    }

    Node firstNodeInTree = tree.FindFirstNodeByDataRecursive(firstNodeGO);
    if (firstNodeInTree != null)
    {
        Node current = firstNodeInTree;
        while (current != null && current != tree.rootNode)
        {
            current.isExpanded = true;
            current = current.parentNode;
        }
        firstNodeInTree.SetCheckedWithoutNotify(true);
        node.SetCheckedWithoutNotify(false);

        tree.Rebuild(); // Final rebuild after all changes
        tree.FocusOn(firstNodeInTree); // Final focus, overrides prior state
        Debug.Log($"Focused on first node '{firstNodeInTree.name}' of road '{roadObject.name}'.");
    }
    else
    {
        Debug.LogWarning($"Could not find tree node for first node '{firstNodeGO.name}'.");
        tree.Rebuild();
    }
}
	
	public void UnselectAllInTree(){
		ToggleNodes(false);
	}
	
	public void ToggleNodes(bool isChecked)	{
		int count =0;
		if (tree != null){

			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();
			
			for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					Node childNode = currentNode.nodes[i];
					childNode.SetCheckedWithoutNotify(isChecked);
					count++;
					stack.Push(childNode);					
				}
			}
		}
		tree.Rebuild();

	}
	
	void SelectChildren(Node node, bool selected)
	{
		// Check if the node has children
		if (node != null && node.nodes != null)
		{
			Stack<Node> stack = new Stack<Node>();
			foreach (var child in node.nodes)
			{
				stack.Push(child);
			}

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();
				currentNode.SetCheckedWithoutNotify(selected); // Toggle to true for selection
				CameraManager.Instance.SelectPrefabWithoutNotify((GameObject)node.data);

				// Push all children of current node onto the stack
				for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					stack.Push(currentNode.nodes[i]);
				}
			}
		}
		tree.Rebuild();
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
	
	public void CheckSelection()
	{
		if (tree == null) return;

		Stack<Node> stack = new Stack<Node>();
		stack.Push(tree.rootNode);

		while (stack.Count > 0)
		{
			Node currentNode = stack.Pop();
			currentNode.SetCheckedWithoutNotify(CameraManager.Instance._selectedObjects.Contains((GameObject)currentNode.data));

			for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
			{
				stack.Push(currentNode.nodes[i]);
			}
		}
		tree.Rebuild();
	}
	
	public void UncheckAll(){
		ToggleNodes(false);
	}

	public void UncheckNodes(){
		OnQuery(query.text);
		ToggleNodes(false);
	}
	
	private void CheckNodes(){
		OnQuery(query.text);
		ToggleNodes(true);
	}
	

	private void DeleteCheckedNodes()
	{
		DeleteCheckedNodesStack(tree.rootNode);
		CameraManager.Instance._selectedObjects.Clear();
		tree.Rebuild();
		CameraManager.Instance.UpdateGizmoState();
		
		PrefabManager.NotifyItemsChanged(false);
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

			if (currentNode.data is GameObject go)
			{
				Destroy(go);
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
