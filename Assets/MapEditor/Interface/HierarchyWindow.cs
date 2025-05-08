using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;
using RustMapEditor.Variables;

public class HierarchyWindow : MonoBehaviour
{
	public static HierarchyWindow Instance { get; private set; }

    public UIRecycleTree tree;
	public InputField query;
	public Text footer;
	public Button geology, origin, sendIDToBreaker;
	public GeologyItem item;
	
	public GameObject itemTemplate;
	public GameObject content;
	
	private void Awake()
    {
        // Ensure only one instance of HierarchyWindow exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
    }

	
    private void Start()
    {
        AssetManager.Callbacks.BundlesLoaded += OnBundlesLoaded;
		
		query.onValueChanged.AddListener(OnQueryEntered);
		tree.onNodeSelected.AddListener(OnSelect);
		
		tree.onNodeCheckedChanged.AddListener(OnCheck);
		
		geology.interactable = false;
        geology.onClick.AddListener(OnGeologyPressed);
		origin.onClick.AddListener(OnPlaceOrigin);	
		sendIDToBreaker.onClick.AddListener(SendIDToBreaker);
    }

	void SendIDToBreaker(){
		if(BreakerWindow.Instance !=null){
			BreakerWindow.Instance.fields[19].text = footer.text;
		}
	}

	public void OnEnable()
	{
		if (tree.nodesCount == -1){
			tree.Clear();
			LoadTree();
		}
	}


	
	private GeologyItem selectedNodeItem(){
		GeologyItem foundItem = new GeologyItem();
		
		if (tree.selectedNode.hasChildren){
			return null;
		}
		
		if (tree.selectedNode == null){
			return null;
		}
		
		string path;
		path = tree.selectedNode.fullPath;
		
		if(tree.selectedNode.data!=null){
			path=(string)tree.selectedNode.data;
		}
		
		if(path == null){
			return null;
		}
		
		if (path[0] == '~')		{		
			path = path.Replace("~", "", StringComparison.Ordinal);
			path = path.Replace("\\", "/", StringComparison.Ordinal);
			foundItem.custom=true;
			foundItem.customPrefab = path;
			foundItem.prefabID = 0;
			foundItem.emphasis = 1;
		}
		else {
			foundItem.custom=false;
			foundItem.prefabID =  AssetManager.ToID(path + ".prefab");
			foundItem.customPrefab = "";
			foundItem.emphasis = 1;
		}
		return foundItem;
	}
	
	private void OnGeologyPressed()	{
		SettingsManager.geology.geologyItems.Add(selectedNodeItem());
		PopulateItemList();
	}
	
	public void PlacePrefab(Vector3 position){
		if(tree.selectedNode!=null){
			GenerativeManager.SpawnFeature(selectedNodeItem(), position-PrefabManager.PrefabParent.transform.position, Vector3.zero, Vector3.one, PrefabManager.PrefabParent);
			PrefabManager.NotifyItemsChanged();
		}
	}
	
	private void OnPlaceOrigin(){
		if(tree.selectedNode!=null){
		GenerativeManager.SpawnFeature(selectedNodeItem(), Vector3.zero, Vector3.zero, Vector3.one, PrefabManager.PrefabParent);
		PrefabManager.NotifyItemsChanged();
		Transform origin = PrefabManager.PrefabParent.GetChild(PrefabManager.PrefabParent.childCount - 1);
		
		//enable the window for access
		AppManager.Instance.ActivateWindow(5);
		BreakerWindow.Instance.PopulateTree(origin);
		}
	}
	
	public void PopulateItemList()
	{
		ClearItemList();
		foreach (GeologyItem item in SettingsManager.geology.geologyItems)
		{
			var itemCopy = Instantiate(itemTemplate);
			var itemPathText = itemCopy.transform.Find("ItemPath").GetComponent<Text>();
			var itemWeight = itemCopy.transform.Find("WeightField").GetComponent<InputField>();
			var button = itemCopy.transform.Find("RemoveItem").GetComponent<Button>();
			string path;
			
			if (item.custom){
				itemPathText.text = item.customPrefab;
			}
			else{
				path = AssetManager.ToPath(item.prefabID);
				itemPathText.text = path.Replace(".prefab", "", StringComparison.Ordinal);
			}
			
			itemWeight.text = item.emphasis.ToString();
			
			var currentItem = item;
			button.onClick.AddListener(() =>
			{
				SettingsManager.geology.geologyItems.Remove(currentItem); 
				PopulateItemList(); 
			});
			
			itemWeight.onValueChanged.AddListener(value =>
			{
				if (float.TryParse(value, out float newEmphasis))
				{
					currentItem.emphasis = (int)newEmphasis;
				}
			});
			
			itemCopy.transform.SetParent(content.transform, false);
			itemCopy.gameObject.SetActive(true);
		}
	}
	
	public void ClearItemList(){
		foreach (Transform child in content.transform)
		{
			Destroy(child.gameObject);
		}
	}

	private void OnCheck(Node selection){
		if(selection.hasChildren){
			selection.isSelected = true;
			return;
		}
		
		Node faveRoot = tree.rootNode.FindNodeByNameRecursive ("~Favorites");
		SettingsManager.UpdateFavorite(selection);
		
		Node faveNode = new Node(selection.name);
		faveNode.data=selection.fullPath;
		
		if(selection.isChecked){
			faveNode.SetCheckedWithoutNotify(true);
			faveRoot.nodes.Add(faveNode);
		}
		else{
			faveRoot.nodes.Remove(selection);
		}
		
		SettingsManager.CheckFavorites(tree);
	}

	private void OnSelect(Node selection){
		
		if (selection.hasChildren){
			selection.isExpanded=true;
			geology.interactable = false;
			footer.text = "";
			return;
		}
		
		if(string.IsNullOrEmpty(selection.fullPath)){
			geology.interactable = false;
			footer.text = "";
			return;
		}
		
		//determine prefab path for determining ID
		string path = selection.fullPath;
		
		if(selection.data!=null){
			path=(string)selection.data;
		}
		
		uint ID =  AssetManager.ToID(path + ".prefab");
		
		
		if (path[0] == '~')		{
			footer.text = path + ".prefab";
			geology.interactable = true;
			return;
		}
		
		if (ID != 0)	{
			footer.text = "ID " + ID;
			geology.interactable = true;
			return;
		}
		geology.interactable = false;
		footer.text = "";
	}
	
    private void OnBundlesLoaded()
    {
        LoadTree();
    }
	
	
	private void LoadTree(){	
	
		List<string> paths = new List<string>();
		
		foreach (var path in SettingsManager.faves.favoriteCustoms)		{
				paths.Add("~Favorites/" + path);
		}
		paths.AddRange(AssetManager.BundleLookup.Keys);

		string basePath = SettingsManager.AppDataPath() + "Custom";

		List<string> collectionPaths = SettingsManager.GetDataPaths(basePath, "Custom");
		
		paths.AddRange(collectionPaths);


		SettingsManager.ConvertPathsToNodes(tree, paths, ".prefab", ".monument", query.text);
		SettingsManager.CheckFavorites(tree);
		

	}


	private void OnQueryEntered(string query)
    {
		tree.Clear();
        LoadTree();
		if(!query.Equals("",StringComparison.Ordinal)){
			tree.ExpandAll();
			}
		else{
			tree.CollapseAll();
		}
    }
	



	
}
