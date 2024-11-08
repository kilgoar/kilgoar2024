using System;
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
    public UIRecycleTree tree;
	public InputField query;
	public Text footer;
	public Button geology;
	public GeologyItem item;
	
	public GameObject itemTemplate;
	public GameObject content;
	
    private void Start()
    {
        AssetManager.Callbacks.BundlesLoaded += OnBundlesLoaded;
		query.onEndEdit.AddListener(OnQueryEntered);
		tree.onNodeSelected.AddListener(OnSelect);
		
		geology.interactable = false;
        geology.onClick.AddListener(OnGeologyPressed);
		
	
    }

	public void OnEnable()
	{
		if (tree.nodesCount == -1){
			tree.Clear();
			LoadTree();
		}
	}

	private void OnGeologyPressed()
	{
		if (tree.selectedNode.hasChildren){
			return;
		}
		
		if (tree.selectedNode == null){
			return;
		}
		
		string path;
		path = tree.selectedNode.fullPath;
		
		if(path == null){
			return;
		}
		
		if (path[0] == '~')		{		
			path = path.Replace("~", "", StringComparison.Ordinal);
			path = path.Replace("\\", "/", StringComparison.Ordinal);
			item.custom=true;
			item.customPrefab = path;
			item.prefabID = 0;
			item.emphasis = 1;
		}
		else {
			item.custom=false;
			item.prefabID =  AssetManager.ToID(path + ".prefab");
			item.customPrefab = "";
			item.emphasis = 1;
		}
		
		SettingsManager.geology.geologyItems.Add(item.Clone());
		PopulateItemList();
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

	private void OnSelect(Node selection){
		if (selection.hasChildren){
			geology.interactable = false;
			footer.text = "";
			return;
		}
		
		if(string.IsNullOrEmpty(selection.fullPath)){
			geology.interactable = false;
			footer.text = "";
			return;
		}
		
		string path = selection.fullPath;
		uint ID =  AssetManager.ToID(selection.fullPath + ".prefab");
		
		
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
		List<string> paths = new List<string>(AssetManager.BundleLookup.Keys);

		string basePath = SettingsManager.AppDataPath() + "Custom";

		List<string> collectionPaths = SettingsManager.GetDataPaths(basePath, "Custom");

		paths.AddRange(collectionPaths);

		SettingsManager.ConvertPathsToNodes(tree, paths, ".prefab", query.text);
	}


	private void OnQueryEntered(string query)
    {
        LoadTree();
		if(!query.Equals("",StringComparison.Ordinal))
			tree.ExpandAll();
    }



	
}
