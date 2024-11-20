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

public class SettingsWindow : MonoBehaviour
{
    FilePreset settings;
	
	public Slider prefabRender;
	public InputField directoryField;
	public Toggle styleToggle, assetLoadToggle;
	public UIRecycleTree tree;
	public Text footer;
	
	private List<string> drivePaths = new List<string>();
	
	public Button[] bundleButtons;
	
	public static SettingsWindow Instance { get; private set; }
	
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

	void OnEnable(){
		UpdateButtonStates();
	}
	
	void Start(){
		LoadDriveList();
		settings = SettingsManager.application;
		
		prefabRender.value = settings.prefabRenderDistance;
		
		directoryField.text = settings.rustDirectory;
		
		assetLoadToggle.isOn = settings.loadbundleonlaunch;
		styleToggle.isOn = settings.terrainTextureSet;
		
        prefabRender.onValueChanged.AddListener(delegate { CameraChange(); });
        directoryField.onValueChanged.AddListener(delegate { DirectoryChange(); });
        assetLoadToggle.onValueChanged.AddListener(delegate { AssetLoader(); });
        
		styleToggle.onValueChanged.AddListener(delegate { StyleChange(); });
		styleToggle.onValueChanged.AddListener(delegate { ToggleStyle(); });
		
		tree.onNodeExpandStateChanged.AddListener(OnExpand);
		tree.onSelectionChanged.AddListener(OnSelect);
		
		bundleButtons[0].onClick.AddListener(OnLoadBundle);
		bundleButtons[1].onClick.AddListener(OnUnloadBundle);
		
		UpdateButtonStates();
	}
	
	private void OnLoadBundle(){
		AssetManager.RuntimeInit();
	}
	
	private void OnUnloadBundle(){
		Debug.LogError("unloading");
		AssetManager.Dispose();
	}
	
	public List<string> pathTests()
		{
			string fileGuess = "Program Files (x86)/Steam/steamapps/common/Rust";
			List<string> validPaths = new List<string>();

			DriveInfo[] drives = DriveInfo.GetDrives();

			foreach (DriveInfo drive in drives)
			{
				string fullPath = Path.Combine(drive.RootDirectory.FullName, fileGuess);

				if (Directory.Exists(fullPath))
				{
					validPaths.Add(fullPath);
				}
			}

			return validPaths;
	}

	
	
	
	public void LoadDriveList()
	{
		tree.Clear();
		DriveInfo[] drives = DriveInfo.GetDrives();
		drivePaths.Clear();
		
		List<string> testedPaths = pathTests();
		drivePaths.AddRange(testedPaths);
		
		foreach (DriveInfo drive in drives)
			{
				if (drive.IsReady)				{
					string driveName = drive.Name;					
					drivePaths.Add(driveName);
				}
			}
		
		foreach (string drivePath in drivePaths){
			string path = drivePath;
			if (path.EndsWith("/") || path.EndsWith("\\"))
			{
				path = path.TrimEnd('/', '\\');
			}

			drivePaths = SettingsManager.AddFilePaths(path, "bundles");
			SettingsManager.AddPathsAsNodes(tree, drivePaths);

		}
	}
	
	void AssetLoader(){
		settings.loadbundleonlaunch = assetLoadToggle.isOn;
	}
	
	public void Expand(Node node)
	{
		string folderPath = node.fullPath;
		List<string> paths = SettingsManager.AddFilePaths(folderPath, "map");
		drivePaths.AddRange(paths);
		SettingsManager.AddPathsAsNodes(tree, drivePaths);
		node.isExpanded= true;
	}
	
	public void OnSelect(Node node)
	{
		if(node.isSelected){
			Expand(node);
			if (AssetManager.ValidBundlePath(node.fullPath)){
				directoryField.text = node.fullPath;
			}
			return;
		}
		node.CollapseAll();
	}
	
	public void OnExpand(Node node)	{
		if (node.isExpanded && node.childCount <2){
			string folderPath = node.fullPath;
			List<string> paths = SettingsManager.AddFilePaths(folderPath, "map");
			drivePaths.AddRange(paths);
			SettingsManager.AddPathsAsNodes(tree, drivePaths);
			}
	}
	
	void StyleChange(){
			settings.terrainTextureSet = styleToggle.isOn;
			TerrainManager.SetTerrainReferences();
			TerrainManager.SetTerrainLayers();
			SettingsManager.SaveSettings();
	}
	
	void CameraChange(){			
		settings.prefabRenderDistance = prefabRender.value;
		SettingsManager.application = settings;
		CameraManager.Instance.SetRenderLimit();
		SettingsManager.SaveSettings();
	}
	
	
	void DirectoryChange(){
        settings.rustDirectory = directoryField.text;              
		SettingsManager.application = settings;		
		UpdateButtonStates();
		SettingsManager.SaveSettings();
	}
	
	void ToggleStyle(){
		TerrainManager.SetTerrainLayers();
	}
	
	public void UpdateButtonStates()
    {

        bool isValidBundle = AssetManager.ValidBundlePath(directoryField.text);

        foreach (var button in bundleButtons)    {
            button.interactable = isValidBundle;
        }
		
		if(! isValidBundle)		{
			footer.text = "No valid bundle found";
		}
		else
		{
			footer.text = "Valid bundle";
		}
		
		if(AssetManager.IsInitialised){
			footer.text = "Bundles Loaded";
			bundleButtons[0].interactable = false;
			bundleButtons[1].interactable = true;
		}
		else
		{
			bundleButtons[0].interactable = true;
			bundleButtons[1].interactable = false;
		}
    }
	
}
