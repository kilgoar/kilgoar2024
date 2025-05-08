using System;
using System.IO;
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

public class FileWindow : MonoBehaviour
{
	FilePreset settings;

	public Slider newSizeSlider, newHeightSlider;
	public Dropdown splatDrop, biomeDrop, recentDrop;
	public UIRecycleTree tree;
	public InputField pathField, filenameField;
	public Button open, save, saveJson, savePrefab, saveMonument;
	public Text footer;

	private List<TerrainSplat.Enum> splatEnums = new List<TerrainSplat.Enum>();
	private List<TerrainBiome.Enum> biomeEnums = new List<TerrainBiome.Enum>();
	private Dictionary<string,int> recentFiles = new Dictionary<string,int>();
	
	

	public void Start()
	{
		settings = SettingsManager.application;
		

		PopulateLists();
		splatDrop.value = splatEnums.IndexOf(settings.newSplat);
		biomeDrop.value = biomeEnums.IndexOf(settings.newBiome);
		newSizeSlider.value = settings.newSize;
		newHeightSlider.value = settings.newHeight;
		

		splatDrop.onValueChanged.AddListener(delegate { StateChange(); });
		biomeDrop.onValueChanged.AddListener(delegate { StateChange(); });
		recentDrop.onValueChanged.AddListener(delegate { OnDropChanged(); });

		newSizeSlider.onValueChanged.AddListener(delegate { StateChange(); });
		newHeightSlider.onValueChanged.AddListener(delegate { StateChange(); });
		
		tree.onNodeExpandStateChanged.AddListener(OnExpand);
		tree.onSelectionChanged.AddListener(OnSelect);	
		
		filenameField.onValueChanged.AddListener(PathChanged);
		
		open.onClick.AddListener(OpenFile);
		save.onClick.AddListener(SaveFile);
		savePrefab.onClick.AddListener(SavePrefab);
		saveMonument.onClick.AddListener(SaveMonument);
		saveJson.onClick.AddListener(SaveJson);
		LoadDriveList();
		OnDropChanged();
	}
	
	public void OnEnable(){
		tree.FocusOn(tree.selectedNode);
	}
	
	public void OpenFile()	{
		string path = pathField.text;
		path = filenameField.text + ".map";
		AddRecent(path);
		var world = new WorldSerialization();
		world.Load(path);
		MapManager.Load(WorldConverter.WorldToTerrain(world), path);
		footer.text = path + " opened";
	}

	public void SaveFile()	{
		string path = filenameField.text + ".map";
		MapManager.Save(path);
		
		List<string> pathList = new List<string>();
		pathList.Add(path);
		SettingsManager.AddPathsAsNodes(tree, pathList);
		footer.text = path + " saved";
	}

	public void SaveJson(){
		string path = filenameField.text + ".json";
		MapManager.SaveJson(path);
		footer.text = path + " saved";
	}

	public void SavePrefab(){
		string path = filenameField.text + ".prefab";
		MapManager.SaveCustomPrefab(path);
		footer.text = path + " saved";
	}
	
	public void SaveMonument(){
		string path = filenameField.text + ".monument";
		MapManager.SaveMonument(path);
		footer.text = path + " saved";
	}

	public void PathChanged(string change)	{
		
		if (change == "")
		{
			save.interactable = false;
			saveJson.interactable = false;
			savePrefab.interactable = false;
			saveMonument.interactable = false;
			return;
		}
		
		if (File.Exists(change + ".map"))
		{
			open.interactable = true;
		}
		else
		{
			open.interactable = false;
		}

		string directoryPath = System.IO.Path.GetDirectoryName(change);

		if (Directory.Exists(directoryPath))
		{
			save.interactable = true;
			saveJson.interactable = true;
			savePrefab.interactable = true;
			saveMonument.interactable = true;
		}
		else
		{
			save.interactable = false;
			saveJson.interactable = false;
			savePrefab.interactable = false;
			saveMonument.interactable = false;
		}
	}
	
	public string RemoveExtension(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return string.Empty; // Or handle differently based on your needs
		}

		string directory = Path.GetDirectoryName(path);
		string filename = Path.GetFileNameWithoutExtension(path);

		if (string.IsNullOrEmpty(filename))
		{
			return string.Empty; // No filename to process
		}

		if (string.IsNullOrEmpty(directory))
		{
			return filename; // Root path or no directory, return filename only
		}

		return Path.Combine(directory, filename);
	}
	
	public void OnSelect(Node node)
	{
		if (node.isSelected)
		{
			Expand(node);
			footer.text = node.name;
			filenameField.text = RemoveExtension(node.fullPath);
			return;
		}
		node.CollapseAll();
		footer.text = "";
		filenameField.text = "";
	}
	
	public void OnExpand(Node node)
	{
		node.isSelected = true;
	}

	public void Expand(Node node)
	{
		string folderPath = node.fullPath;
		string fullPath = folderPath;
		if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			fullPath += Path.DirectorySeparatorChar; 
		List<string> paths = SettingsManager.AddFilePaths(fullPath, "map");
		SettingsManager.AddPathsAsNodes(tree, paths); 
		node.isExpanded = true;
	}



	public void OnDropChanged() {
		string selectedText = recentDrop.options[recentDrop.value].text; 		
		string folderPath = Path.GetDirectoryName(selectedText);
		string file = Path.GetFileNameWithoutExtension(selectedText);
		Debug.LogError(selectedText);
		FocusPath(selectedText);

	}
	
	public void FocusPath(string path)
	{
		Node file = FindNodeByPath(tree, path);
		tree.FocusOn(file);
		file.isSelected = true;
	}

	public Node FindNodeByPath(UIRecycleTree tree, string path)
	{
		string drive = Path.GetPathRoot(path);
		string[] parts = path.Substring(drive.Length).Split(Path.DirectorySeparatorChar);

		if (parts.Length == 0)
		{
			Debug.LogError("Invalid path: " + path);
			return null;
		}
		
		Node nextNode = tree.rootNode;
		
		foreach (string folder in parts){
			
			Node[] searchNodes = nextNode.GetAllChildrenRecursive();
			
			foreach (Node node in searchNodes)		{
				if (node.name.Equals(folder))			{
					nextNode = node;
					Expand(node);
				}
			}
		}
		
		
		return nextNode;
	}

	public void LoadDriveList()
	{
		tree.Clear();
		DriveInfo[] drives = DriveInfo.GetDrives();
		List<string> driveRoots = new List<string>();

		foreach (DriveInfo drive in drives)
		{
			if (drive.IsReady)
			{
				string root = drive.Name;
				driveRoots.Add(root);
			}
		}
		SettingsManager.AddPathsAsNodes(tree, driveRoots);
		SettingsManager.AddPathsAsNodes(tree, settings.recentFiles);
	}

	public void AddRecent(string path) {
		if (settings.recentFiles == null) {
			settings.recentFiles = new List<string>();
		}

		// If the path already exists, remove it to ensure it's added as the most recent
		if (settings.recentFiles.Contains(path)) {
			settings.recentFiles.Remove(path);
		}

		// Add the new path to the front of the list
		settings.recentFiles.Insert(0, path);

		// If the list exceeds the limit, remove the oldest entry
		if (settings.recentFiles.Count > 12) {
			settings.recentFiles.RemoveAt(settings.recentFiles.Count - 1);
		}

		SettingsManager.application = settings;
		SettingsManager.SaveSettings();
		PopulateLists();
	}
	
	public void PopulateLists()	{
		
		foreach (TerrainSplat.Enum splat in Enum.GetValues(typeof(TerrainSplat.Enum)))
		{
			splatEnums.Add(splat);
			splatDrop.options.Add(new Dropdown.OptionData(splat.ToString()));
		}

		foreach (TerrainBiome.Enum biome in Enum.GetValues(typeof(TerrainBiome.Enum)))
		{
			biomeEnums.Add(biome);
			biomeDrop.options.Add(new Dropdown.OptionData(biome.ToString()));
		}
		
		if (settings.recentFiles == null)
		{
			settings.recentFiles = new List<string>();
		}

		// Clear existing options in the dropdown to avoid duplicates
		recentDrop.options.Clear();

		// Add sorted files to the dropdown, ordered by their value (most recent first)
		foreach (string file in settings.recentFiles)
		{
			recentDrop.options.Add(new Dropdown.OptionData(file));
		}

		
		recentDrop.RefreshShownValue();
		
	}


	public void StateChange()
	{
		FilePreset application = SettingsManager.application;
		application.newSplat = splatEnums[splatDrop.value]; 
		application.newBiome = biomeEnums[biomeDrop.value];
		application.newSize = (int)newSizeSlider.value;    
		application.newHeight = newHeightSlider.value;		
		SettingsManager.application = application;
	}

	public void NewFile()
	{
		MapManager.CreateMap(SettingsManager.application.newSize, SettingsManager.application.newSplat, SettingsManager.application.newBiome, SettingsManager.application.newHeight * 1000f);
	}
}