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
	public Button deleteChecked, checkAll, uncheckAll, findInBreaker, saveCustom;
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
		tree.nodes.Clear(); // Clear any existing nodes in the tree
		Dictionary<Transform, Node> transformToNodeMap = new Dictionary<Transform, Node>();

		Transform prefabParentTransform = PrefabManager.PrefabParent.transform;
		foreach (Transform child in prefabParentTransform)
		{
			BuildTreeRecursive(child, null, transformToNodeMap); // Build the tree recursively from each top-level child
		}
		CheckSelection();
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
					currentNode = new Node(prefabName) { data = current.gameObject };
				}
				break;

			case "Collection":
				currentNode = new Node(current.name) { data = current.gameObject };
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
	
	public void OnSelect(Node node){
		GameObject goSelect = (GameObject)node.data;
		
			if(!Keyboard.current.leftShiftKey.isPressed){
				CameraManager.Instance.Unselect();
				UnselectAllInTree();
			}
			
			if(Keyboard.current.leftAltKey.isPressed){
				SelectChildren(node, true);
				node.ExpandAll();
			}
		
		node.SetCheckedWithoutNotify(true);	
		CameraManager.Instance.SelectPrefabWithoutNotify(goSelect);
	}
	
	void UnselectAllInTree(){
		ToggleNodes(false);
	}
	
	private void ToggleNodes(bool isChecked)	{
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
		footer.text= count + " prefabs selected";
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
	
	public void CheckSelection(){
		if (tree != null)
		{
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();
				if(CameraManager.Instance._selectedObjects.Contains((GameObject)currentNode.data)){
					currentNode.SetCheckedWithoutNotify(true);
				}
				else{
					currentNode.SetCheckedWithoutNotify(false);
				}

				// Push all child nodes onto the stack
				for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					Node childNode = currentNode.nodes[i];
					stack.Push(childNode);
				}
			}
		}
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
