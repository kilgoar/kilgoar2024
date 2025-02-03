using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;
using UIRecycleTreeNamespace;
using static WorldSerialization;
using static PrefabManager;

public class BreakerWindow : MonoBehaviour
{
    public UIRecycleTree tree;
    public List<InputField> fields;
    public List<Button> buttons;
    public Toggle enableFragment;
    public Dropdown breakingList;

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
        buttons[0].onClick.AddListener(ExpandAllInTree);
        buttons[1].onClick.AddListener(SelectAllInTree);
        buttons[2].onClick.AddListener(Build);
        buttons[3].onClick.AddListener(Load);
        buttons[4].onClick.AddListener(Save);
        buttons[5].onClick.AddListener(() => ReplacePrefabScaleWith(selection.colliderScales.capsule));
        buttons[6].onClick.AddListener(() => ReplacePrefabScaleWith(selection.colliderScales.sphere));
        buttons[7].onClick.AddListener(() => ReplacePrefabScaleWith(selection.colliderScales.box));
        buttons[8].onClick.AddListener(LoadID);
        buttons[9].onClick.AddListener(Send);
        buttons[10].onClick.AddListener(SaveOverride);
        //buttons[11].onClick.AddListener(LoadOverride);

		tree.onNodeSelected.AddListener(OnNodeSelect);

        for (int i = 0; i < fields.Count; i++)        {
            int index = i; 
            fields[i].onValueChanged.AddListener((value) => FieldChanged(index, value));
        }
    }

	public void OnNodeSelect(Node node){
		PopulateBreakingData(); 
	}
	
	public void PopulateTree(Transform monument)
	{
		if (tree == null)
		{
			Debug.LogError("UIRecycleTree is not initialized.");
			return;
		}
		
		tree.Clear();
		
		if (monument == null)
		{
			Debug.LogError($"Could not retrieve transform");
			return;
		}
		
		var holder = monument.GetComponent<PrefabDataHolder>();
    	if (holder == null)
		{
			Debug.LogError($"PrefabDataHolder not found");
			return;
		}    
		
		if (!AssetManager.IDLookup.TryGetValue(holder.prefabData.id, out string rootName))
		{	
			Debug.LogError("Invalid Root Prefab");
			return;
		}
		
		if (rootName.EndsWith("(Clone)"))		{
			rootName = rootName.Substring(0, rootName.Length - "(Clone)".Length).TrimEnd();
		}
		RecurseTransform(monument, tree.rootNode, string.Empty, rootName);

		tree.Rebuild();
	}

	void RecurseTransform(Transform currentTransform, Node parentNode, string currentPath, string rootName)
	{
		
		string fragmentName = currentTransform.name;
		string fullPath = string.IsNullOrEmpty(currentPath) ? fragmentName : $"{currentPath}/{fragmentName}";
		string parentName = parentNode?.name ?? "";
		
		Colliders colliderScales = ItemToColliders(currentTransform);
		PrefabData newPrefab  = ItemToPrefab(currentTransform, parentName, rootName);

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
		Node newNode = new Node(fragmentName)		{
			data = fragment,
			styleIndex = PrefabManager.GetFragmentStyle(fragment) //define icon style for item
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

            // Update input fields with selected data
            fields[0].text = selection.prefabData.position.x.ToString();
            fields[1].text = selection.prefabData.position.y.ToString();
            fields[2].text = selection.prefabData.position.z.ToString();
            fields[3].text = selection.prefabData.rotation.x.ToString();
            fields[4].text = selection.prefabData.rotation.y.ToString();
            fields[5].text = selection.prefabData.rotation.z.ToString();
            fields[6].text = selection.prefabData.scale.x.ToString();
            fields[7].text = selection.prefabData.scale.y.ToString();
            fields[8].text = selection.prefabData.scale.z.ToString();
            fields[9].text = selection.colliderScales.capsule.x.ToString();
            fields[10].text = selection.colliderScales.capsule.y.ToString();
            fields[11].text = selection.colliderScales.capsule.z.ToString();
            fields[12].text = selection.colliderScales.sphere.x.ToString();
            fields[13].text = selection.colliderScales.sphere.y.ToString();
            fields[14].text = selection.colliderScales.sphere.z.ToString();
            fields[15].text = selection.colliderScales.box.x.ToString();
            fields[16].text = selection.colliderScales.box.y.ToString();
            fields[17].text = selection.colliderScales.box.z.ToString();
            fields[18].text = selection.name;
            fields[19].text = selection.id.ToString();
			fields[20].text = selection.name;
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
        selection.id = uint.Parse(fields[19].text);

        // Push updated data back to the tree
        tree.selectedNode.data = selection;
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
        DataChanged();
    }

    // Button methods
    void ExpandAllInTree() => tree.ExpandAll();
    
	void SelectAllInTree(){
		
	}
	
    void Build() => Debug.Log("Build logic here");
    void Load() => Debug.Log("Load logic here");
    void Save() => Debug.Log("Save logic here");
    void ReplacePrefabScaleWith(WorldSerialization.VectorData scale) => selection.prefabData.scale = scale;
    void LoadID() => Debug.Log("Load ID logic here");
    void Send() => Debug.Log("Send logic here");
    
	private void SaveOverride(){
			SettingsManager.fragmentIDs.fragmentNamelist[fields[20].text] = uint.Parse(fields[21].text);	
			SettingsManager.fragmentIDs.Serialize();
			SettingsManager.SaveFragmentLookup();
	}
	
    void LoadOverride() => Debug.Log("Load Override logic here");
}
