using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    void OnEnable()
    {
        Initialize();
        UpdateButtonStates();
    }

    void Start()
    {
        InitializeListeners();
    }

    private void Initialize()
    {
        settings = SettingsManager.application;
        
        prefabRender.value = settings.prefabRenderDistance;
        directoryField.text = settings.rustDirectory;
        assetLoadToggle.isOn = settings.loadbundleonlaunch;
        styleToggle.isOn = settings.terrainTextureSet;

        LoadDriveList();
    }

    private void InitializeListeners()
    {
        // Remove existing listeners to prevent duplicates
        prefabRender.onValueChanged.RemoveAllListeners();
        directoryField.onEndEdit.RemoveAllListeners();
        assetLoadToggle.onValueChanged.RemoveAllListeners();
        styleToggle.onValueChanged.RemoveAllListeners();
        tree.onNodeExpandStateChanged.RemoveAllListeners();
        tree.onSelectionChanged.RemoveAllListeners();
        bundleButtons[0].onClick.RemoveAllListeners();
        bundleButtons[1].onClick.RemoveAllListeners();

        // Add listeners with correct syntax
        prefabRender.onValueChanged.AddListener(CameraChange);
        directoryField.onEndEdit.AddListener(DirectoryChange);
        assetLoadToggle.onValueChanged.AddListener(AssetLoader);
        styleToggle.onValueChanged.AddListener(StyleChange);
        styleToggle.onValueChanged.AddListener(ToggleStyle);
        tree.onNodeExpandStateChanged.AddListener(OnExpand);
        tree.onSelectionChanged.AddListener(OnSelect);
        bundleButtons[0].onClick.AddListener(OnLoadBundle);
        bundleButtons[1].onClick.AddListener(OnUnloadBundle);
    }

    private void OnLoadBundle()
    {
        settings.rustDirectory = directoryField.text;
        SettingsManager.application = settings;
        SettingsManager.SaveSettings();
        AssetManager.RuntimeInit();
    }
    
    private void OnUnloadBundle()
    {
        Debug.LogError("unloading");
        AssetManager.Dispose();
    }
    
    public List<string> PathTests()
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
        drivePaths.Clear();

        // Add known Rust paths first
        List<string> testedPaths = PathTests();
        drivePaths.AddRange(testedPaths);

        // Add all drive roots
        DriveInfo[] drives = DriveInfo.GetDrives();
        foreach (DriveInfo drive in drives)
        {
            if (drive.IsReady)
            {
                drivePaths.Add(drive.Name);
            }
        }

        SettingsManager.AddPathsAsNodes(tree, drivePaths);
        tree.Rebuild(); // Force UI update
    }

    void AssetLoader(bool value) // Toggle requires a bool parameter
    {
        settings.loadbundleonlaunch = value;
        SettingsManager.application = settings;
        SettingsManager.SaveSettings();
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
	
	public void OnExpand(Node node)
	{
		if (node.isExpanded)
		{
			string folderPath = node.fullPath;
			string fullPath = Path.GetFullPath(folderPath); 
			if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				fullPath += Path.DirectorySeparatorChar; 
			List<string> newPaths = SettingsManager.AddFilePaths(fullPath, "map");
			if (newPaths.Count > 0)
			{
				SettingsManager.AddPathsAsNodes(tree, newPaths);
			}
			else
			{
			}
		}
	}
    
	public void OnSelect(Node node)
	{
		Debug.Log($"Selected: {node.fullPath} (isSelected: {node.isSelected})");
		if (node.isSelected)
		{
			Expand(node);
			footer.text = node.name;
			directoryField.text = node.fullPath;
			return;
		}
		node.CollapseAll();
		footer.text = "";
		directoryField.text = "";
	}
    
    void StyleChange(bool value) // Toggle requires a bool parameter
    {
        settings.terrainTextureSet = value;
        TerrainManager.SetTerrainReferences();
        TerrainManager.SetTerrainLayers();
        SettingsManager.SaveSettings();
    }
    
    void CameraChange(float value) // Slider requires a float parameter
    {			
        settings.prefabRenderDistance = value;
        SettingsManager.application = settings;
        CameraManager.Instance.SetRenderLimit();
        SettingsManager.SaveSettings();
    }
        
    void DirectoryChange(string value) // InputField requires a string parameter
    {
        settings.rustDirectory = value;              
        SettingsManager.application = settings;		
        UpdateButtonStates();
        SettingsManager.SaveSettings();
    }
    
    void ToggleStyle(bool value) // Toggle requires a bool parameter
    {
        TerrainManager.SetTerrainLayers();
    }
    
    public void UpdateButtonStates()
    {
        bool isValidBundle = AssetManager.ValidBundlePath(directoryField.text);

        
        footer.text = AssetManager.IsInitialised ? "Bundles Loaded" : "Bundles not loaded";

        bundleButtons[0].interactable = isValidBundle && !AssetManager.IsInitialised;
        bundleButtons[1].interactable = AssetManager.IsInitialised;
    }
}