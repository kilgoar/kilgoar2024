using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using RustMapEditor.Variables;
using static BreakerSerialization;
using UIRecycleTreeNamespace;


public static class SettingsManager
{
	public static string SettingsPath;
	
    #region Init
	#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void Init()
    {
		SettingsPath = "EditorSettings.json";
        if (!File.Exists(SettingsPath))
            using (StreamWriter write = new StreamWriter(SettingsPath, false))
                write.Write(JsonUtility.ToJson(new EditorSettings(), true));

        LoadSettings();
    }	
    #endif
	#endregion
	
	public static void RuntimeInit()
    {
		SettingsPath = AppDataPath() + "EditorSettings.json";
        if (!File.Exists(SettingsPath)){
                CopyDirectory("Presets", Path.Combine(AppDataPath(), "Presets"));
				CopyDirectory("Custom", Path.Combine(AppDataPath(), "Custom"));
				CopyEditorSettings(Path.Combine(AppDataPath(), "EditorSettings.json"));
		}
		
        LoadSettings();
    }
	
	public static string AppDataPath()
	{
		
		#if UNITY_EDITOR
			return "";
		#else
			return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RustMapper/");
		#endif
	}
	
	public static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string directoryName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destinationDir, directoryName));
        }
    }

    public static void CopyEditorSettings(string destinationFile)
    {
        string sourceFile = "EditorSettings.json"; 

        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destinationFile, true);
            Debug.Log($"Copied EditorSettings.json to: {destinationFile}");
        }
        else
        {
            Debug.LogWarning("EditorSettings.json not found at: " + sourceFile);
        }
    }
	
	public static List<string> AddFilePaths(string path, string extension)
	{
		List<string> pathsList = new List<string>();

		try
		{
			// Validate the path
			if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
			{
				Debug.LogWarning("Invalid directory path: " + path);
				return pathsList;
			}

			// Enumerate directories
			foreach (string directory in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
			{
				pathsList.Add(Path.GetFullPath(directory) + Path.DirectorySeparatorChar);
			}

			// Enumerate files with specified extension
			foreach (string file in Directory.EnumerateFiles(path, "*." + extension, SearchOption.TopDirectoryOnly))
			{
				pathsList.Add(Path.GetFullPath(file));
			}
		}
		catch (UnauthorizedAccessException ex)
		{
			Debug.LogWarning("Access denied to path: " + path + ". " + ex.Message);
		}
		catch (PathTooLongException ex)
		{
			Debug.LogWarning("Path too long: " + path + ". " + ex.Message);
		}
		catch (IOException ex)
		{
			Debug.LogWarning("IO exception for path: " + path + ". " + ex.Message);
		}

		return pathsList;
	}

	public static List<string> GetDataPaths(string path, string root, string extension = ".prefab")    
	{
		List<string> pathsList = new List<string>();

		string[] directories = Directory.GetDirectories(path);
		string[] files = Directory.GetFiles(path);

		int index = path.IndexOf(root, StringComparison.Ordinal);
		
		if (index != -1)		{
			pathsList.Add("~" + path.Substring(index));
		}

		foreach (string directory in directories)   		{
			pathsList.AddRange(GetDataPaths(directory, root));
		}

		foreach (string file in files)    		{
			int fileIndex = file.IndexOf(root, StringComparison.Ordinal);
			
			if (fileIndex != -1)        			{
				pathsList.Add("~" + file.Substring(fileIndex));
			}
		}

		return pathsList;
	}

	
	public static void AddPathsAsNodes(UIRecycleTree tree, List<string> paths)
	{
		Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

		foreach (Node existingNode in tree.rootNode.nodes)
		{
			PopulateNodeMap(existingNode, nodeMap, string.Empty);
		}

		foreach (string path in paths)
		{
			string normalizedPath = path.Replace("\\", "/", StringComparison.Ordinal);
			string[] parts = normalizedPath.Split('/');
			Node currentNode = null;

			for (int i = 0; i < parts.Length; i++)
			{
				string part = parts[i];
				string fullPath = string.Join("/", parts, 0, i + 1);

				if (!nodeMap.TryGetValue(fullPath, out currentNode))
				{
					currentNode = new Node(part);
					nodeMap[fullPath] = currentNode;

					if (i == 0)					{						
						tree.rootNode.nodes.AddWithoutNotify(currentNode);
					}
					else					{
						string parentPath = string.Join("/", parts, 0, i);
						if (nodeMap.TryGetValue(parentPath, out Node parentNode))
						{
							parentNode.nodes.AddWithoutNotify(currentNode);
							currentNode.parentNode = parentNode;
						}
					}

					currentNode.tree = tree;
				}
			}
		}
		tree.Reload();
	}

	private static void PopulateNodeMap(Node node, Dictionary<string, Node> nodeMap, string parentPath)
	{
		string fullPath = string.IsNullOrEmpty(parentPath) ? node.name : $"{parentPath}/{node.name}";
		nodeMap[fullPath] = node;

		foreach (Node child in node.nodes)		{
			PopulateNodeMap(child, nodeMap, fullPath);
		}
	}
	
	public static void ConvertPathsToNodes(UIRecycleTree tree, List<string> paths, string extension = ".prefab", string searchQuery = "")
	{
		tree.Clear();
		Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

		foreach (string path in paths)
		{
			if (path.EndsWith(extension, StringComparison.Ordinal) || extension.Equals("override", StringComparison.Ordinal))
			{
				// Remove the "~Geology" prefix from the path
				string searchPath = path.Replace(extension, "", StringComparison.Ordinal)
										.Replace("\\", "/", StringComparison.Ordinal);
				
				// Strip the prefix "~Geology/" if present
				if (searchPath.StartsWith("~Geology/", StringComparison.Ordinal))
				{
					searchPath = searchPath.Substring("~Geology/".Length);
				}

				// Proceed if it matches the search query or if the query is empty
				if (string.IsNullOrEmpty(searchQuery) || searchPath.Contains(searchQuery, StringComparison.Ordinal))
				{
					string[] parts = searchPath.Split('/');
					Node currentNode = null;

					for (int i = 0; i < parts.Length; i++)
					{
						string part = parts[i];
						string fullPath = string.Join("/", parts, 0, i + 1);

						if (!nodeMap.TryGetValue(fullPath, out currentNode))
						{
							currentNode = new Node(part);
							nodeMap[fullPath] = currentNode;

							if (i == 0)
							{
								// Add children of "~Geology" directly to the tree root
								tree.rootNode.nodes.AddWithoutNotify(currentNode);
							}
							else
							{
								string parentPath = string.Join("/", parts, 0, i);
								if (nodeMap.TryGetValue(parentPath, out Node parentNode))
								{
									parentNode.nodes.AddWithoutNotify(currentNode);
									currentNode.parentNode = parentNode;
								}
							}

							currentNode.tree = tree;
						}
					}
				}
			}
		}
		tree.Reload();
	}
	

	
	
	
    public const string BundlePathExt = @"\Bundles\Bundles";
	
	public static bool style { get; set; }
    public static string RustDirectory { get; set; }
    public static float PrefabRenderDistance { get; set; }
    public static float PathRenderDistance { get; set; }
    public static float WaterTransparency { get; set; }
    public static bool LoadBundleOnLaunch { get; set; }
    public static bool TerrainTextureSet { get; set; }
	
	public static FilePreset application { get; set; }
	public static CrazingPreset crazing { get; set; }
	public static PerlinSplatPreset perlinSplat { get; set; }
	public static RipplePreset ripple { get; set; }
	public static OceanPreset ocean { get; set; }
	public static TerracingPreset terracing { get; set; }
	public static PerlinPreset perlin { get; set; }
	public static GeologyPreset geology { get; set; }
	public static ReplacerPreset replacer { get; set; }
	public static string[] breakerPresets { get; set; }
	public static string[] geologyPresets { get; set; }
	public static string[] geologyPresetLists { get; set; }
    public static string[] PrefabPaths { get; private set; }
	public static List<string> macro { get; set; } = new List<string>();
	public static bool macroSources {get; set; }
 	public static RustCityPreset city {get; set; }
	public static BreakerPreset breaker {get; set;}
	public static FragmentLookup fragmentIDs {get; set;}
	public static BreakerSerialization breakerSerializer = new BreakerSerialization();

	
    /// <summary>Saves the current EditorSettings to a JSON file.</summary>
    public static void SaveSettings()    {
		using (StreamWriter write = new StreamWriter(SettingsPath, false))  {
            EditorSettings editorSettings = new EditorSettings
            (
                RustDirectory, PrefabRenderDistance, PathRenderDistance, WaterTransparency, LoadBundleOnLaunch, TerrainTextureSet, 
				style, crazing, perlinSplat, ripple, ocean, terracing, perlin, geology, replacer,
				city, breaker, macroSources, application
            );
            write.Write(JsonUtility.ToJson(editorSettings, true));
			
        }
    }

	public static Dictionary<string,uint> ListToDict(List<FragmentPair> fragmentPairs)
		{
			Dictionary<string,uint> namelist = new Dictionary<string,uint>();
			foreach(FragmentPair pair in fragmentPairs)
			{
				namelist.Add(pair.fragment, pair.id);
			}
			return namelist;
		}
		
	public static List<FragmentPair> DictToList(Dictionary<string,uint> fragmentNamelist)
		{
			List<FragmentPair> namePairs = new List<FragmentPair>();
			FragmentPair fragPair = new FragmentPair();
			foreach (KeyValuePair<string,uint> pair in fragmentNamelist)
			{
				fragPair.fragment = pair.Key;
				fragPair.id = pair.Value;
				namePairs.Add(fragPair);
			}
			return namePairs;
		}
	
	public static void SaveFragmentLookup()
	{
		using (StreamWriter write = new StreamWriter(AppDataPath() + $"Presets/breakerFragments.json", false))
        {
            write.Write(JsonUtility.ToJson(fragmentIDs, true));
			fragmentIDs.Deserialize();
        }
	}

	public static void LoadFragmentLookup()
    {
		fragmentIDs  = new FragmentLookup();
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/breakerFragments.json"))
			{
				fragmentIDs  = JsonUtility.FromJson<FragmentLookup>(reader.ReadToEnd());
				fragmentIDs.Deserialize();
			}
    }

	
	public static void SaveBreakerPreset(string filename)
    {
		breakerSerializer.breaker = breaker;
		breakerSerializer.Save(AppDataPath() + $"Presets/Breaker/{filename}.breaker");
		/*       
	   using (StreamWriter write = new StreamWriter($"Presets/Breaker/{breaker.title}.breaker", false))
        {
            write.Write(JsonUtility.ToJson(breaker, true));
        }
		*/
    }
	
	public static void LoadBreakerPreset(string filename)
	{
		breaker = breakerSerializer.Load(Path.Combine(AppDataPath() +  $"Presets/Breaker/{filename}.breaker"));
		/*
		using (StreamReader reader = new StreamReader($"Presets/Breaker/{filename}.breaker"))
			{
				
				breaker = JsonUtility.FromJson<BreakerPreset>(reader.ReadToEnd());
			}
		*/
	}
	
	
	public static void SaveGeologyPreset()
    {
        using (StreamWriter write = new StreamWriter(AppDataPath() + $"Presets/Geology/{geology.title}.json", false))
        {
            write.Write(JsonUtility.ToJson(geology, true));
        }
    }
	
	public static void DeleteGeologyPreset()
	{
		string path = AppDataPath() + $"Presets/Geology/{geology.title}.json";
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	
	public static void SaveReplacerPreset()
    {
        using (StreamWriter write = new StreamWriter(AppDataPath() + $"Presets/Geology/{geology.title}.json", false))
        {
            write.Write(JsonUtility.ToJson(replacer, true));
        }
    }
	
	
	
	public static void LoadGeologyPreset(string filename)
	{
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/Geology/{filename}.json"))
			{
				geology = JsonUtility.FromJson<GeologyPreset>(reader.ReadToEnd());
			}
	}
	
	public static GeologyPreset GetGeologyPreset(string filename)
	{
		if (File.Exists(filename))
			{
				using (StreamReader reader = new StreamReader(filename))
					{
						return JsonUtility.FromJson<GeologyPreset>(reader.ReadToEnd());
					}
			}
		else
			return new GeologyPreset("file not found");
	}

	
	public static void LoadReplacerPreset(string filename)
	{
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/Replacer/{filename}.json"))
			{
				replacer = JsonUtility.FromJson<ReplacerPreset>(reader.ReadToEnd());
			}
	}
	
	public static void LoadGeologyMacro(string filename)
	{
		// Initialize the macro list
		macro = new List<string>();

		string macroPath = AppDataPath() + $"Presets/Geology/Macros/{filename}.macro";

		// Read and deserialize JSON content
		using (StreamReader reader = new StreamReader(macroPath))
		{
			string jsonContent = reader.ReadToEnd();
			GeologyMacroWrapper wrapper = JsonUtility.FromJson<GeologyMacroWrapper>(jsonContent);

			if (wrapper != null && wrapper.macroList != null)
			{
				macro = wrapper.macroList;
			}
		}
	}
	
	public static void SaveGeologyMacro(string macroTitle)
	{
		GeologyMacroWrapper wrapper = new GeologyMacroWrapper { macroList = macro };

		string jsonContent = JsonUtility.ToJson(wrapper, true);

		string macroPath = AppDataPath() + $"Presets/Geology/Macros/{macroTitle}.macro";
		using (StreamWriter writer = new StreamWriter(macroPath, false))
		{
			writer.Write(jsonContent);
		}
	}
	
	public static void RemovePreset(int index)
	{
		if (index >= 0 && index < macro.Count)
		{
			macro.RemoveAt(index);
		}
	}
	
	
	public static void AddToMacro(string macroTitle)
	{
		string macroPath = AppDataPath() + $"Presets/Geology/{macroTitle}.json";
		macro.Add(macroPath);
		SaveGeologyMacro(macroTitle);
	}
	

    /// <summary>Loads and sets the current EditorSettings from a JSON file.</summary>
    public static void LoadSettings()
    {
		if (!File.Exists(SettingsPath)){ Debug.LogError("Config file not found"); return; }
		
        using (StreamReader reader = new StreamReader(SettingsPath))
        {
            EditorSettings editorSettings = JsonUtility.FromJson<EditorSettings>(reader.ReadToEnd());
            
			RustDirectory = editorSettings.rustDirectory;
            PrefabRenderDistance = editorSettings.prefabRenderDistance;
            PathRenderDistance = editorSettings.pathRenderDistance;
            WaterTransparency = editorSettings.waterTransparency;
            LoadBundleOnLaunch = editorSettings.loadbundleonlaunch;
            PrefabPaths = editorSettings.prefabPaths;
			style = editorSettings.style;
			crazing = editorSettings.crazing;
			perlinSplat = editorSettings.perlinSplat;
			ripple = editorSettings.ripple;
			ocean = editorSettings.ocean;
			terracing = editorSettings.terracing;
			perlin = editorSettings.perlin;
			geology = editorSettings.geology;
			replacer = editorSettings.replacer;
			city = editorSettings.city;
			macroSources = editorSettings.macroSources;
			application = editorSettings.application;
        }
		
		LoadPresets();
		LoadMacros();
    }
	
	public static void LoadPresets()
	{
		geologyPresets = Directory.GetFiles(AppDataPath() + "Presets/Geology/");
		breakerPresets = Directory.GetFiles(AppDataPath() + "Presets/Breaker");
	}
	
	public static void LoadMacros()
	{
		
		geologyPresetLists = SettingsManager.GetPresetTitles(AppDataPath() + "Presets/Geology/Macros/");
	}
	
	public static string[] GetPresetTitles(string path)
	{
		char[] delimiters = { '/', '.'};
		string[] geologyPresets = Directory.GetFiles(path);
		string[] parse;
		string[] filenames = new string [geologyPresets.Length];
		int filenameID;
		for(int i = 0; i < geologyPresets.Length; i++)
		{
			parse = geologyPresets[i].Split(delimiters);
			filenameID = parse.Length - 2;
			filenames[i] = parse[filenameID];
		}
		return filenames;
	}
	
	public static string[] GetDirectoryTitles(string path)
	{
		
			return Directory.GetDirectories(path);

	}

    /// <summary> Sets the EditorSettings back to default values.</summary>
    public static void SetDefaultSettings()
    {
        RustDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Rust";
        ToolTips.rustDirectoryPath.text = RustDirectory;
        PrefabRenderDistance = 700f;
        PathRenderDistance = 250f;
        WaterTransparency = 0.2f;
        LoadBundleOnLaunch = false;
        Debug.Log("Default Settings set.");
    }
	
}

[Serializable]
public struct EditorSettings
{
    public string rustDirectory;
    public float prefabRenderDistance;
    public float pathRenderDistance;
    public float waterTransparency;
    public bool loadbundleonlaunch;
    public bool terrainTextureSet;
	public bool style;
	
	public FilePreset application;
	public CrazingPreset crazing;
	public PerlinSplatPreset perlinSplat;
	public RipplePreset ripple;
	public OceanPreset ocean;
	public TerracingPreset terracing;
	public PerlinPreset perlin;
	public GeologyPreset geology;
	public ReplacerPreset replacer;
	public string[] prefabPaths;
	public RustCityPreset city;
	public BreakerPreset breaker;
	public bool macroSources;

    public EditorSettings
    (
        string rustDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Rust", float prefabRenderDistance = 700f, float pathRenderDistance = 200f, 
        float waterTransparency = 0.2f, bool loadbundleonlaunch = false, bool terrainTextureSet = false, bool style = true, CrazingPreset crazing = new CrazingPreset(), PerlinSplatPreset perlinSplat = new PerlinSplatPreset(),
		RipplePreset ripple = new RipplePreset(), OceanPreset ocean = new OceanPreset(), TerracingPreset terracing = new TerracingPreset(), PerlinPreset perlin = new PerlinPreset(), GeologyPreset geology = new GeologyPreset(), 
		ReplacerPreset replacer = new ReplacerPreset(), RustCityPreset city = new RustCityPreset(), BreakerPreset breaker = new BreakerPreset(), bool macroSources = true, FilePreset application = new FilePreset()
	)
        {
            this.rustDirectory = rustDirectory;
            this.prefabRenderDistance = prefabRenderDistance;
            this.pathRenderDistance = pathRenderDistance;
            this.waterTransparency = waterTransparency;
            this.loadbundleonlaunch = loadbundleonlaunch;
            this.terrainTextureSet = terrainTextureSet;
			this.style = style;
			this.crazing = crazing;
			this.perlinSplat = perlinSplat;
            this.prefabPaths = SettingsManager.PrefabPaths;
			this.ripple = ripple;
			this.ocean = ocean;
			this.terracing = terracing;
			this.perlin = perlin;
			this.geology = geology;
			this.replacer = replacer;
			this.city = city;
			this.breaker = breaker;
			this.macroSources = macroSources;
			this.application = application;
        }
}