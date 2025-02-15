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
	public Button deleteChecked, checkAll, uncheckAll, findInBreaker;
	private int currentMatchIndex = 0; 
	private List<Node> matchingNodes = new List<Node>();
	public List<InputField> prefabDataFields = new List<InputField>();
	
	private PrefabDataHolder holder = new PrefabDataHolder();
	private PrefabData selection = new PrefabData();
	
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

	public void SetSelection(GameObject go){
		try
		{
			if (go != null)
			{
				holder = go.GetComponent<PrefabDataHolder>();
			}
			else
			{
				holder = null; // Explicitly set to null if the cast fails
			}

			if (holder != null)
			{
				// If PrefabData component exists, use its values
				selection = holder.prefabData; // Assuming 'selection' is already defined and initialized elsewhere
			}
		}
		catch (MissingReferenceException e)
		{
			Debug.LogError("A MissingReferenceException occurred in set selection: " + e.Message);
			// Clear all fields in case of an error
			for (int i = 0; i < prefabDataFields.Count; i++)
			{
				prefabDataFields[i].text = string.Empty;
			}
			
			//node.parentNode.nodes.Remove(node);
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
		CreateListeners(); //prefab data display
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
	
	public void UpdateData(){
		if(CameraManager.Instance._selectedObjects.Count != 0){
			SetSelection(CameraManager.Instance._selectedObjects[CameraManager.Instance._selectedObjects.Count-1]);
		}
		else{
			DefaultPrefabData();
			return;
		}
		
		if(holder!=null && holder.gameObject !=null){
			holder.UpdatePrefabData();
			RetrievePrefabData(holder.gameObject);
		}
		else{
				
			DefaultPrefabData();
			
			}
	}

	
	public void SendPrefabData(){		
		// Position
		selection.position = new VectorData(
			float.Parse(prefabDataFields[0].text),
			float.Parse(prefabDataFields[1].text),
			float.Parse(prefabDataFields[2].text));

		// Rotation
		selection.rotation = new VectorData(
			float.Parse(prefabDataFields[3].text),
			float.Parse(prefabDataFields[4].text),
			float.Parse(prefabDataFields[5].text));

		// Scale
		selection.scale = new VectorData(
			float.Parse(prefabDataFields[6].text),
			float.Parse(prefabDataFields[7].text),
			float.Parse(prefabDataFields[8].text));

		// Category
		selection.category = prefabDataFields[9].text;

		// ID
		selection.id = uint.Parse(prefabDataFields[10].text);
		holder.CastPrefabData();
	}
	
	public void DefaultPrefabData(){
		DestroyListeners();

			
			// Position
			prefabDataFields[0].text = "";
			prefabDataFields[1].text = "";
			prefabDataFields[2].text = "";

			// Rotation
			prefabDataFields[3].text = "";
			prefabDataFields[4].text = "";
			prefabDataFields[5].text = "";

			// Scale
			prefabDataFields[6].text = "";
			prefabDataFields[7].text = "";
			prefabDataFields[8].text = "";

			// Category
			prefabDataFields[9].text = "";

			// ID
			prefabDataFields[10].text = "";

		
		
	}
	
	public void RetrievePrefabData(GameObject obj)
	{
		DestroyListeners();
		SetSelection(obj);
		
			if (selection!=null){
				// Position
				prefabDataFields[0].text = selection.position.x.ToString();
				prefabDataFields[1].text = selection.position.y.ToString();
				prefabDataFields[2].text = selection.position.z.ToString();

				// Rotation
				prefabDataFields[3].text = selection.rotation.x.ToString();
				prefabDataFields[4].text = selection.rotation.y.ToString();
				prefabDataFields[5].text = selection.rotation.z.ToString();

				// Scale
				prefabDataFields[6].text = selection.scale.x.ToString();
				prefabDataFields[7].text = selection.scale.y.ToString();
				prefabDataFields[8].text = selection.scale.z.ToString();

				// Category
				prefabDataFields[9].text = selection.category;

				// ID
				prefabDataFields[10].text = selection.id.ToString();
			}
			else
			{
				// If no PrefabData component found, use transform data
				Transform transform = obj.transform;
				if (transform != null)
				{
					// Position
					prefabDataFields[0].text = transform.position.x.ToString();
					prefabDataFields[1].text = transform.position.y.ToString();
					prefabDataFields[2].text = transform.position.z.ToString();

					// Rotation (convert from Quaternion to Euler angles)
					Vector3 eulerAngles = transform.rotation.eulerAngles;
					prefabDataFields[3].text = eulerAngles.x.ToString();
					prefabDataFields[4].text = eulerAngles.y.ToString();
					prefabDataFields[5].text = eulerAngles.z.ToString();

					// Scale
					prefabDataFields[6].text = transform.localScale.x.ToString();
					prefabDataFields[7].text = transform.localScale.y.ToString();
					prefabDataFields[8].text = transform.localScale.z.ToString();

					// Clear category and ID since they're not available
					prefabDataFields[9].text = string.Empty;
					prefabDataFields[10].text = string.Empty;
				}
				else
				{
					// If we can't even get the transform, clear all fields
					for (int i = 0; i < prefabDataFields.Count; i++)
					{
						prefabDataFields[i].text = string.Empty;
					}

				}
			}
		
		
		CreateListeners();
	}
	
	public void CreateListeners()
	{
		// Listeners for VectorData (position, rotation, scale)
		for (int i = 0; i < 9; i++)
		{
			int index = i; // Capture the loop variable for closure
			prefabDataFields[i].onEndEdit.AddListener(text =>
			{
				if (float.TryParse(text, out float value))
				{
					int vectorIndex = index % 3;
					switch (index / 3)
					{
						case 0: // Position
							switch (vectorIndex)
							{
								case 0: selection.position.x = value; break;
								case 1: selection.position.y = value; break;
								case 2: selection.position.z = value; break;
							}
							break;
						case 1: // Rotation
							switch (vectorIndex)
							{
								case 0: selection.rotation.x = value; break;
								case 1: selection.rotation.y = value; break;
								case 2: selection.rotation.z = value; break;
							}
							break;
						case 2: // Scale
							switch (vectorIndex)
							{
								case 0: selection.scale.x = value; break;
								case 1: selection.scale.y = value; break;
								case 2: selection.scale.z = value; break;
							}
							break;
					}
				}
				SendPrefabData();
			});
		}

		// Listener for Category
		prefabDataFields[9].onEndEdit.AddListener(text => 
		{
			selection.category = text;
			SendPrefabData();
		});

		// Listener for ID
		prefabDataFields[10].onEndEdit.AddListener(text =>
		{
			if (uint.TryParse(text, out uint id))
			{
				selection.id = id;
			}
			SendPrefabData();
		});
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
		else{
			selection=null;
			holder=null;
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
