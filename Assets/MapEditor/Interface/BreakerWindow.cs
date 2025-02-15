using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;
using UIRecycleTreeNamespace;
using LZ4;
using System.IO;

using static WorldSerialization;
using static PrefabManager;

public class BreakerWindow : MonoBehaviour
{
    public UIRecycleTree tree;
    public List<InputField> fields;
    public List<Button> buttons;
    public Toggle enableFragment;
    public Dropdown breakingList;
	public Text footer, footer2;
	
	private Transform currentMonument;
	private string monumentName;
	
    private BreakingData selection;
	
	public static BreakerWindow Instance { get; private set; }

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
	
    void Start()    {
		
		PopulateDropdown();

        buttons[0].onClick.AddListener(ExpandAllInTree);
        buttons[1].onClick.AddListener(SelectAllInTree);
        buttons[2].onClick.AddListener(Build);
        buttons[3].onClick.AddListener(Load);
        buttons[4].onClick.AddListener(Save);
        buttons[5].onClick.AddListener(() => FillCapsuleCollider(selection.colliderScales.capsule));
        buttons[6].onClick.AddListener(() => FillSphereCollider(selection.colliderScales.sphere));
        buttons[7].onClick.AddListener(() => FillBoxCollider(selection.colliderScales.box));
        buttons[8].onClick.AddListener(LoadID);
        buttons[9].onClick.AddListener(SendID);
        buttons[10].onClick.AddListener(SaveOverride);
		buttons[12].onClick.AddListener(CollapseAllInTree);

		tree.onNodeSelected.AddListener(OnNodeSelect);
		tree.onNodeCheckedChanged.AddListener(OnChecked);

        for (int i = 0; i < fields.Count; i++)        {
            int index = i; 
            fields[i].onValueChanged.AddListener((value) => FieldChanged(index, value));
        }
		
		enableFragment.onValueChanged.AddListener(_ =>EnableChanged()); 
		
    }
	
	public void OnChecked(Node node){
		if(node.isChecked == true){
			if(!Keyboard.current.leftShiftKey.isPressed){
				UnselectAllInTree();
				node.SetCheckedWithoutNotify(true);
			}
			if(Keyboard.current.leftAltKey.isPressed){
				SelectChildren(node, true);
				node.ExpandAll();
				PopulateBreakingData();
			}
			return;
		}
		if(Keyboard.current.leftAltKey.isPressed){
			SelectChildren(node, false);
		}
	}

	public void OnNodeSelect(Node node){
		
		node.SetCheckedWithoutNotify(true);

			if(!Keyboard.current.leftShiftKey.isPressed){
				UnselectAllInTree();
				node.SetCheckedWithoutNotify(true);
			}
			if(Keyboard.current.leftAltKey.isPressed){
				SelectChildren(node, true);
				node.ExpandAll();
				PopulateBreakingData();
			}	

		PopulateBreakingData();
	}
	
	public void PopulateTree(Transform monument)
	{
		//verify tree and clear
		if (tree == null)
		{
			Debug.LogError("UIRecycleTree is not initialized.");
			return;
		}
		tree.Clear();
		
		//verify parameter
		if (monument == null)
		{
			Debug.LogError($"Could not retrieve transform");
			return;
		}
		
		//verify data holder
		var holder = monument.GetComponent<PrefabDataHolder>();
    	if (holder == null)
		{
			Debug.LogError($"PrefabDataHolder not found");
			return;
		}    

		//validate id
		if (!AssetManager.IDLookup.TryGetValue(holder.prefabData.id, out string rootName))
		{	
			Debug.LogError("Invalid Root Prefab");
			return;
		}

		//remove clone name and populate tree
		if (rootName.EndsWith("(Clone)"))		{
			rootName = rootName.Substring(0, rootName.Length - "(Clone)".Length).TrimEnd();
		}
		
		currentMonument = monument;
		monumentName = System.IO.Path.GetFileNameWithoutExtension(rootName); 
		Debug.LogError(rootName + " " + monumentName);
		
		RecurseTransform(monument, tree.rootNode, string.Empty, rootName);

		tree.Rebuild();
	}


	void RecurseTransform(Transform currentTransform, Node parentNode, string currentPath, string rootName)
	{
		
		string fragmentName = currentTransform.name;
		string fullPath = string.IsNullOrEmpty(currentPath) ? fragmentName : $"{currentPath}/{fragmentName}";
		string parentName = parentNode?.name ?? "";
		
		Colliders colliderScales = ItemToColliders(currentTransform);
		PrefabData newPrefab  = ItemToPrefab(currentTransform, parentName, rootName, fullPath);

		BreakingData fragment = new BreakingData
		{
			name = fragmentName,
			id = newPrefab.id,
			ignore = false, 
			treeID = 0, 
			colliderScales = colliderScales,
			prefabData = newPrefab, 
			parent = parentName, 
			treePath = fullPath,
			monument = rootName
		};

		
		//node.icon = PrefabManager.GetIcon(monumentFragments.category[i].child[j].grandchild[k].greatgrandchild[m].breakingData, icons);
		//lol
		Node newNode = new Node(fragmentName)		{
			data = fragment,
			styleIndex = PrefabManager.GetFragmentStyle(fragment)
		};

		
		
		if (parentNode != null)		{
			parentNode.nodes.AddWithoutNotify(newNode);
		}
		else		{
			fragment.name = rootName;
			tree.rootNode.nodes.AddWithoutNotify(newNode);
		}

		foreach (Transform child in currentTransform)		{
			RecurseTransform(child, newNode, fullPath, rootName);
		}
	}

	
	void PopulateBreakingData()
	{
		if (tree.selectedNode != null)
		{
			selection = (BreakingData)tree.selectedNode.data;

        foreach (var field in fields)
        {
            field.onValueChanged.RemoveAllListeners();
        }
		enableFragment.onValueChanged.RemoveAllListeners();
		

			// Update input fields with selected data, with error handling
			for (int i = 0; i < fields.Count; i++)
			{
				try
				{
					switch (i)
					{
						case 0:
							fields[i].text = selection.prefabData.position.x.ToString();
							break;
						case 1:
							fields[i].text = selection.prefabData.position.y.ToString();
							break;
						case 2:
							fields[i].text = selection.prefabData.position.z.ToString();
							break;
						case 3:
							fields[i].text = selection.prefabData.rotation.x.ToString();
							break;
						case 4:
							fields[i].text = selection.prefabData.rotation.y.ToString();
							break;
						case 5:
							fields[i].text = selection.prefabData.rotation.z.ToString();
							break;
						case 6:
							fields[i].text = selection.prefabData.scale.x.ToString();
							break;
						case 7:
							fields[i].text = selection.prefabData.scale.y.ToString();
							break;
						case 8:
							fields[i].text = selection.prefabData.scale.z.ToString();
							break;
						case 9:
							fields[i].text = selection.colliderScales.capsule.x.ToString();
							break;
						case 10:
							fields[i].text = selection.colliderScales.capsule.y.ToString();
							break;
						case 11:
							fields[i].text = selection.colliderScales.capsule.z.ToString();
							break;
						case 12:
							fields[i].text = selection.colliderScales.sphere.x.ToString();
							break;
						case 13:
							fields[i].text = selection.colliderScales.sphere.y.ToString();
							break;
						case 14:
							fields[i].text = selection.colliderScales.sphere.z.ToString();
							break;
						case 15:
							fields[i].text = selection.colliderScales.box.x.ToString();
							break;
						case 16:
							fields[i].text = selection.colliderScales.box.y.ToString();
							break;
						case 17:
							fields[i].text = selection.colliderScales.box.z.ToString();
							break;
						case 18:
						case 20:
							fields[i].text = selection.name;
							break;
						case 19:
							fields[i].text = selection.prefabData.id.ToString();
							break;
						case 21:
							try
								{
									fields[21].text = SettingsManager.fragmentIDs.fragmentNamelist[fields[20].text].ToString();
								}
								catch (KeyNotFoundException)
								{
									fields[21].text = "";
								}
							break;
						default:
							//fields[i].text = string.Empty; // Clear or set to a default value if unexpected index
							break;
					}
				}
				catch (Exception e)
				{
					// Log the error but continue with the next field
					Debug.LogError($"Error populating field {i}: {e.Message}");
					//fields[i].text = string.Empty; // Or some default value
				}
			}
			
			enableFragment.isOn = selection.ignore;
			
			
				for (int i = 0; i < fields.Count; i++)
				{
					int index = i; 
					fields[i].onValueChanged.AddListener((value) => FieldChanged(index, value));
				}

			
		enableFragment.onValueChanged.AddListener(_ =>EnableChanged()); 
		}
	}


	void EnableChanged(){
		enableFragment.onValueChanged.RemoveAllListeners();		
		ToggleCheckedIDs(enableFragment.isOn);
		enableFragment.onValueChanged.AddListener(_ =>EnableChanged()); 
	}
	
	public void FocusByPath(string path){
		if (tree != null)
		{
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();
				if(path.Equals(currentNode.fullPath)){
					tree.FocusOn(currentNode);
					currentNode.isSelected=true;
					return;
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
	
	private void ToggleCheckedIDs(bool ignore){
		if (tree != null)
		{
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();

				// Process only checked nodes
				if (currentNode.isChecked)
				{
					// Ensure node.data is valid and contains prefabData
					if (currentNode.data != null)
					{
						BreakingData data = (BreakingData)currentNode.data;
						data.ignore = ignore;
						currentNode.data = data;
						RefreshIcon(currentNode);
					}
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

    void DataChanged()
    {
        if (tree.selectedNode == null) return;

        // Update `selection` based on input field values
        selection.prefabData.position.x = float.Parse(fields[0].text);
        selection.prefabData.position.y = float.Parse(fields[1].text);
        selection.prefabData.position.z = float.Parse(fields[2].text);
        selection.prefabData.rotation.x = float.Parse(fields[3].text);
        selection.prefabData.rotation.y = float.Parse(fields[4].text);
        selection.prefabData.rotation.z = float.Parse(fields[5].text);
        selection.prefabData.scale.x = float.Parse(fields[6].text);
        selection.prefabData.scale.y = float.Parse(fields[7].text);
        selection.prefabData.scale.z = float.Parse(fields[8].text);
        selection.colliderScales.capsule.x = float.Parse(fields[9].text);
        selection.colliderScales.capsule.y = float.Parse(fields[10].text);
        selection.colliderScales.capsule.z = float.Parse(fields[11].text);
        selection.colliderScales.sphere.x = float.Parse(fields[12].text);
        selection.colliderScales.sphere.y = float.Parse(fields[13].text);
        selection.colliderScales.sphere.z = float.Parse(fields[14].text);
        selection.colliderScales.box.x = float.Parse(fields[15].text);
        selection.colliderScales.box.y = float.Parse(fields[16].text);
        selection.colliderScales.box.z = float.Parse(fields[17].text);
        selection.name = fields[18].text;
        selection.prefabData.id = uint.Parse(fields[19].text);
		selection.id  = selection.prefabData.id;
		selection.ignore = enableFragment.isOn;
		
        // Push updated data back to the tree
        tree.selectedNode.data = selection;
		
		//update the node style
		tree.selectedNode.styleIndex = PrefabManager.GetFragmentStyle(selection);

    }
	

    void FieldChanged(int index, string value)
    {

		if (index == 20){
					try
						{
							fields[21].text = SettingsManager.fragmentIDs.fragmentNamelist[fields[20].text].ToString();
						}
						catch (KeyNotFoundException)
						{
							fields[21].text = "";
						}
			return;
		}
    }

    // Button methods
    void ExpandAllInTree() => tree.ExpandAll();
    void CollapseAllInTree() => tree.CollapseAll();
	
	void SelectAllInTree(){
		ToggleNodes(true);
	}
	
	void UnselectAllInTree(){
		ToggleNodes(false);
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

				// Push all children of current node onto the stack
				for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					stack.Push(currentNode.nodes[i]);
				}
			}
		}
		tree.Rebuild();
	}
	

	
	private void ToggleNodes(bool isChecked)	{
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
					
					stack.Push(childNode);					
				}
			}
		}
		tree.Rebuild();
	}
	
	private void RefreshIcon(Node node){
		node.styleIndex = PrefabManager.GetFragmentStyle((BreakingData)node.data);
	}
	
	private void RefreshIcons()	{
		if (tree != null){
			
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();
			
			for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					Node childNode = currentNode.nodes[i];
					childNode.styleIndex = PrefabManager.GetFragmentStyle((BreakingData)childNode.data);
					stack.Push(childNode);					
				}
			}
		}
	}
	
	private void LoadID()	{
		LoadCheckedIDs();
		PopulateBreakingData();
	}
	
	private void SendID()  {
		ChangeCheckedIDs( uint.Parse(fields[19].text));
	}
	
		//list based operations
   void SetNodeID(Node node, uint id)
    {
        if (node == null) return;
		BreakingData breakData = (BreakingData)node.data;

        breakData.prefabData.id = id;
		breakData.id  = id;		
		
        // Push updated data back to the tree
        node.data = breakData;
		
		//update the node style
		node.styleIndex = PrefabManager.GetFragmentStyle(breakData);
    }
	
			//list based operations
   void UpdateNodeID(Node node)
    {
        if (node.data == null) return;
		BreakingData breakData = (BreakingData)node.data;
		uint id = AssetManager.fragmentToID(breakData.name, breakData.parent, breakData.monument);
        breakData.prefabData.id = id;
		breakData.id = id;		
        // Push updated data back to the tree
        node.data = breakData;
		
		//update the node style
		node.styleIndex = PrefabManager.GetFragmentStyle(breakData);
    }
	
	private void LoadCheckedIDs(){
		if (tree != null)
		{
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();

				// Process only checked nodes
				if (currentNode.isChecked)
				{
					// Ensure node.data is valid and contains prefabData
					if (currentNode.data != null)
					{
						UpdateNodeID(currentNode);
					}
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
	
	private void ChangeCheckedIDs(uint id){
		if (tree != null)
		{
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();

				// Process only checked nodes
				if (currentNode.isChecked)
				{
					// Ensure node.data is valid and contains prefabData
					if (currentNode.data != null)
					{

						SetNodeID(currentNode, id);
					}
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
	
		
	private void Build()
	{

		if (tree != null)
		{
			List<BreakingData> prefabList = new List<BreakingData>();
			Stack<Node> stack = new Stack<Node>();
			stack.Push(tree.rootNode);

			// Step 1: Iterate over all nodes and collect non-ignored prefabData
			while (stack.Count > 0)
			{
				Node currentNode = stack.Pop();

				// Check if the node has BreakingData and is not ignored
				if (currentNode.data is BreakingData breakingData && !breakingData.ignore)
				{
					// Add the prefabData to the list
					if (breakingData.prefabData != null)
					{
						prefabList.Add(breakingData);
					}
				}

				// Push all child nodes onto the stack
				for (int i = currentNode.nodes.Count - 1; i >= 0; i--)
				{
					Node childNode = currentNode.nodes[i];
					stack.Push(childNode);
				}
			}

			// Step 2: Spawn the prefabs using PrefabManager
			if (prefabList.Count > 0)
			{
				PrefabManager.SpawnPrefabs(prefabList, PrefabManager.PrefabParent);
				//PrefabManager.CastPrefabs();
			}
		}
		footer2.text=PrefabManager.removeDuplicates(PrefabManager.CurrentMapPrefabs);
	}
	
	void PopulateDropdown()
	{
		string[] monuments = SettingsManager.GetPresetTitles(SettingsManager.AppDataPath() + "Presets/Breaker/");
		breakingList.options = monuments.Select(title => new Dropdown.OptionData(title)).ToList();
	}
	
    void Load()
	{
		if (breakingList == null)
		{
			Debug.LogError("breakingList dropdown reference is not set.");
			return;
		}

		// Get the index of the currently selected option
		int selectedIndex = breakingList.value;

		// Get the text of the selected option
		string selectedOptionText = breakingList.options[selectedIndex].text;

		// Pass the selected option's text to LoadTree
		LoadTree(selectedOptionText);
		RefreshIcons();
	}
	
    void Save(){
		if(tree.rootNode.name == null){
			
		monumentName = tree.rootNode.nodes[0].name;
		if (monumentName.EndsWith("(Clone)"))		{
			monumentName = monumentName.Substring(0, monumentName.Length - "(Clone)".Length).TrimEnd();
		}
		}
		SaveTree(monumentName);
	
	}
		
	
	void FillCapsuleCollider(WorldSerialization.VectorData scale){
		selection.prefabData.scale = scale;
		selection.colliderScales.capsule = Vector3.zero;
		PopulateBreakingData();
		tree.selectedNode.styleIndex = PrefabManager.GetFragmentStyle(selection);
	}
	void FillSphereCollider(WorldSerialization.VectorData scale){
		selection.prefabData.scale = scale;
		selection.colliderScales.sphere = Vector3.zero;
		PopulateBreakingData();
		tree.selectedNode.styleIndex = PrefabManager.GetFragmentStyle(selection);
	}
		void FillBoxCollider(WorldSerialization.VectorData scale){
		selection.prefabData.scale = scale;
		selection.colliderScales.box = Vector3.zero;
		PopulateBreakingData();
		tree.selectedNode.styleIndex = PrefabManager.GetFragmentStyle(selection);
	}

	[Serializable]
	public class SerializableNode
	{
		public string name;
		public bool isChecked;
		public BreakingData data; // Assuming BreakingData is serializable
		public List<SerializableNode> children = new List<SerializableNode>();
	}

	[Serializable]
	public class SerializableTree
	{
		public SerializableNode root;
	}
	
	void SaveTree(string filename)
	{
		if (tree == null)
		{
			Debug.LogError("Tree is not initialized.");
			return;
		}

		Node rootNode = null;
		try
		{
			rootNode = tree.rootNode;
		}
		catch (Exception e)
		{
			Debug.LogError("Exception when accessing tree.rootNode: " + e.Message);
			return;
		}
		
		Debug.LogError(rootNode.nodes[0].fullPath);

		SerializableTree serializableTree = new SerializableTree
		{
			root = SerializeNode(rootNode)
		};
		
		// Step 2: Serialize and save the data to a file
		string fileName = SettingsManager.AppDataPath() + $"Presets/Breaker/{filename}.dat";
		try
		{
			using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				// Use LZ4Stream for compression
				using (var compressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Compress))
				{
					// Serialize the object directly into the compression stream
					string jsonData = JsonUtility.ToJson(serializableTree, true);
					byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
					compressionStream.Write(jsonBytes, 0, jsonBytes.Length);
				}
			}
			Debug.Log("Tree saved successfully to " + fileName);
		}
		catch (Exception e)
		{
			Debug.LogError("Error saving tree: " + e.Message);
		}
		PopulateDropdown();
	}
	
	void LoadTree(string filename)
	{
		if (tree == null)
		{
			Debug.LogError("Tree is not initialized.");
			return;
		}

		string fileName = SettingsManager.AppDataPath() + $"Presets/Breaker/{filename}.dat";
		SerializableTree serializableTree = null;

		// Step 1: Read and deserialize the file
		try
		{
			using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				// Use LZ4Stream for decompression
				using (var decompressionStream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
				{
					using (var memoryStream = new MemoryStream())
					{
						decompressionStream.CopyTo(memoryStream);
						byte[] jsonBytes = memoryStream.ToArray();
						string jsonData = System.Text.Encoding.UTF8.GetString(jsonBytes);
						serializableTree = JsonUtility.FromJson<SerializableTree>(jsonData);
					}
				}
			}
			Debug.Log("Tree loaded successfully from " + fileName);
		}
		catch (Exception e)
		{
			Debug.LogError("Error loading tree: " + e.Message);
			return;
		}


		// Step 2: Rebuild the tree from the deserialized data
		if (serializableTree != null && serializableTree.root != null)
		{
			// Clear the existing tree
			tree.Clear();

			// Populate the rootNode with the deserialized data
			Node newRootNode = DeserializeNode(serializableTree.root, null);
			
			foreach (var childNode in newRootNode.nodes)
			{
				tree.rootNode.nodes.AddWithoutNotify(childNode); // Add deserialized children to the existing rootNode
			}

			tree.Rebuild();
		}
		else
		{
			Debug.LogError("Loaded tree data is invalid.");
		}
		
		PopulateDropdown();
	}

	
	SerializableNode SerializeNode(Node node)
	{

		string name = node.name; // Use a fallback if name is null
		bool isChecked = node.isChecked; // Directly assign since it's a bool

		BreakingData data;
		if (node.data == null)
		{
			Debug.LogError("node.data is null; assigning default value.");
			data = default(BreakingData); // Assign default value if node.data is null
		}
		else if (node.data is BreakingData breakingData)
		{
			data = breakingData; // Safe cast
		}
		else
		{
			Debug.LogError("node.data is not of type BreakingData; assigning default value.");
			data = default(BreakingData); // Assign default value if cast fails
		}

		List<SerializableNode> children = new List<SerializableNode>(); // Initialize empty list

		// Construct SerializableNode using the extracted values
		SerializableNode serializableNode = new SerializableNode
		{
			name = name,
			isChecked = isChecked,
			data = data,
			children = children,
			
		};
		
				foreach (var childNode in node.nodes)
				{
					if (childNode != null)
					{
						SerializableNode serializedChild = SerializeNode(childNode);
						if (serializedChild != null)
						{
							serializableNode.children.Add(serializedChild);
						}
					}
				}


			return serializableNode;

	}
	
	// Helper method to recursively deserialize a node
	Node DeserializeNode(SerializableNode serializableNode, Node parentNode)
	{
		if (serializableNode == null)
		{
			Debug.LogError("serializableNode is null; returning null.");
			return null;
		}

		// Extract property values from 'serializableNode' with appropriate checks
		string name = serializableNode.name; // Use a fallback if name is null
		bool isChecked = serializableNode.isChecked; // Directly assign since it's a bool

		BreakingData data = serializableNode.data;
		int styleIndex=0;
		
		if(data is BreakingData bd){
			
			styleIndex = PrefabManager.GetFragmentStyle(bd);
		}
		// Create the Node object using the extracted values
		Node node = new Node(name)
		{
			isChecked = isChecked,
			data = data,
			parentNode = parentNode,
			//styleIndex = styleIndex,
		};

			foreach (var serializedChild in serializableNode.children)
			{
				if (serializedChild != null)
				{
					Node childNode = DeserializeNode(serializedChild, node);
					if (childNode != null)
					{
						node.nodes.AddWithoutNotify(childNode); // Assumes 'nodes' supports adding elements
					}
					else
					{
						Debug.LogError("Child node deserialization failed; skipping.");
					}
				}
				else
				{
					Debug.LogError("Serialized child is null; skipping.");
				}
			}

		return node;
	}
    
	private void SaveOverride(){
			SettingsManager.fragmentIDs.fragmentNamelist[fields[20].text] = uint.Parse(fields[21].text);	
			SettingsManager.fragmentIDs.Serialize();
			SettingsManager.SaveFragmentLookup();
	}
	
    void LoadOverride() => Debug.Log("Load Override logic here");
}
