using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using RustMapEditor.Variables;
using static BreakerSerialization;
using UIRecycleTreeNamespace;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class Vector3ContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        
        if (property.DeclaringType == typeof(Vector3))
        {
            if (property.PropertyName == "normalized" || 
                property.PropertyName == "magnitude" || 
                property.PropertyName == "sqrMagnitude")
            {
                property.ShouldSerialize = instance => false;
            }
        }
        
        return property;
    }
}


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
        {
            try
            {
                using (StreamWriter write = new StreamWriter(SettingsPath, false))
                {
                    // Serialize with Newtonsoft.Json instead of JsonUtility
                    string json = JsonConvert.SerializeObject(new EditorSettings(), Formatting.Indented);
                    write.Write(json);
                }
                Debug.Log($"Created new settings file at {SettingsPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating settings file at {SettingsPath}: {e.Message}");
            }
        }

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
		//overwrite [appdatapath]/presets/breakerFragments with (default) presets/breakerFragments.json - only if the default file is larger
		UpdateBreakerFragmentsIfNewer();
		
		LoadFragmentLookup();
		LoadSettings();
    }
	
	public static List<string> GetScriptFiles()
	{
		string scriptsPath = Path.Combine(AppDataPath(), "Presets", "Scripts");
		List<string> scriptFiles = new List<string>();

		try
		{
			if (!Directory.Exists(scriptsPath))
			{
				Debug.LogWarning($"Scripts directory not found at: {scriptsPath}");
				return scriptFiles;
			}

			foreach (string file in Directory.EnumerateFiles(scriptsPath, "*.rmml", SearchOption.TopDirectoryOnly))
			{
				scriptFiles.Add(Path.GetFileName(file)); // Just the filename, not the full path
			}
		}
		catch (UnauthorizedAccessException ex)
		{
			Debug.LogWarning($"Access denied to scripts directory: {ex.Message}");
		}
		catch (IOException ex)
		{
			Debug.LogWarning($"IO error accessing scripts directory: {ex.Message}");
		}

		return scriptFiles;
	}
	
	public static List<string> GetScriptCommands(string scriptName)
	{
		List<string> commands = new List<string>();
		string scriptPath = Path.Combine(AppDataPath(), "Presets", "Scripts", scriptName);

		try
		{
			if (!File.Exists(scriptPath))
			{
				Debug.LogWarning($"Script file not found at: {scriptPath}");
				return commands;
			}

			// Read all lines from the file
			string[] lines = File.ReadAllLines(scriptPath);
			foreach (string line in lines)
			{
				string trimmedLine = line.Trim();
				// Skip empty lines or comments (assuming '#' or '//' as comment starters)
				if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#") && !trimmedLine.StartsWith("//"))
				{
					commands.Add(trimmedLine);
				}
			}
		}
		catch (UnauthorizedAccessException ex)
		{
			Debug.LogWarning($"Access denied to script file {scriptName}: {ex.Message}");
		}
		catch (IOException ex)
		{
			Debug.LogWarning($"IO error reading script file {scriptName}: {ex.Message}");
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Unexpected error reading script file {scriptName}: {ex.Message}");
		}

		return commands;
	}
	
	private static void UpdateBreakerFragmentsIfNewer()
	{
		string defaultFragmentsPath = "Presets/breakerFragments.json";
		string targetFragmentsPath = Path.Combine(AppDataPath(), "Presets", "breakerFragments.json");
		string autosavePath = Path.Combine(AppDataPath(), "Presets", "autosaveFragments.json");

		if (File.Exists(defaultFragmentsPath))
		{
			if (File.Exists(targetFragmentsPath))
			{
				// Save existing file as autosave before overwriting
				File.Copy(targetFragmentsPath, autosavePath, true);
				Debug.Log("Saved current breakerFragments as autosaveFragments.json.");
			}

			FileInfo defaultFileInfo = new FileInfo(defaultFragmentsPath);
			FileInfo targetFileInfo = new FileInfo(targetFragmentsPath);

			if (!File.Exists(targetFragmentsPath) || defaultFileInfo.LastWriteTimeUtc > targetFileInfo.LastWriteTimeUtc)
			{
				File.Copy(defaultFragmentsPath, targetFragmentsPath, true);
				Debug.Log("Updated breakerFragments.json with the default version.");
			}
		}
		else
		{
			Debug.LogWarning("Default breakerFragments.json not found.");
		}
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

	public static void CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
    {
        try
        {
            // Check if the source file exists
            if (File.Exists(sourcePath))
            {
                // Copy the file to the destination
                File.Copy(sourcePath, destinationPath, overwrite);
                
                // Log success
                Debug.Log($"File copied from: {sourcePath} to: {destinationPath}");
            }
            else
            {
                // Log warning if source file does not exist
                Debug.LogWarning($"Source file not found at: {sourcePath}");
            }
        }
        catch (IOException e)
        {
            // Handle IO exceptions (like disk full, file in use, etc.)
            Debug.LogError($"Error copying file: {e.Message}");
        }
        catch (UnauthorizedAccessException e)
        {
            // Handle permission issues
            Debug.LogError($"Permission denied when copying file: {e.Message}");
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
		string absolutePath = Path.GetFullPath(path); // Resolve fully

		if (string.IsNullOrWhiteSpace(absolutePath) || !Directory.Exists(absolutePath))
		{
			Debug.LogWarning($"Invalid path: {absolutePath}");
			return pathsList;
		}

		try
		{
			foreach (string directory in Directory.EnumerateDirectories(absolutePath, "*", SearchOption.TopDirectoryOnly))
			{
				pathsList.Add(Path.GetFullPath(directory) + Path.DirectorySeparatorChar);
			}
			foreach (string file in Directory.EnumerateFiles(absolutePath, "*." + extension, SearchOption.TopDirectoryOnly))
			{
				pathsList.Add(Path.GetFullPath(file));
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Error at {absolutePath}: {ex.Message}");
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
		tree.Rebuild();

	}

	private static void PopulateNodeMap(Node node, Dictionary<string, Node> nodeMap, string parentPath)
	{
		string fullPath = string.IsNullOrEmpty(parentPath) ? node.name : $"{parentPath}/{node.name}";
		nodeMap[fullPath] = node;

		foreach (Node child in node.nodes)		{
			PopulateNodeMap(child, nodeMap, fullPath);
		}
	}
	
	public static void UpdateFavorite(Node node){
		if (node.isChecked){
			AddFavorite(node);
			return;
		}
		RemoveFavorite(node);
	}
	
	public static void AddFavorite(Node node){
			string fullPath = node.fullPath;
			if(node.data!=null){
				fullPath = (string)node.data;
			}

			faves.favoriteCustoms.Add(fullPath);

			
		SaveSettings();
	}
	
	public static void RemoveFavorite(Node node)
	{
		string fullPath = node.fullPath;
		if(node.data!=null){
			fullPath =  (string)node.data;
		}

		faves.favoriteCustoms.Remove(fullPath);


		SaveSettings();
	}
	
	public static void CheckFavorites(UIRecycleTree tree)
	{
		CheckNode(tree.rootNode);
		tree.Rebuild();
	}

	private static void CheckNode(Node node)
	{
		string fullPath = node.fullPath;
		if (node.data != null)
		{
			fullPath = (string)node.data;
		}
		


		if(node.fullPath!= "~Favorites"){


			bool isFavorite = faves.favoriteCustoms.Contains(fullPath);
			node.SetCheckedWithoutNotify(isFavorite);
			// Set style based on whether the node has children
			node.styleIndex = node.hasChildren ? 1 : 0;

			// Handle removal of favorites only if it's under the Favorites directory
			if (node.fullPath.StartsWith("~Favorites/", StringComparison.Ordinal) && !isFavorite)
			{
				Node faveRoot = node.parentNode;

				if (faveRoot != null)
				{
					// Remove node from its parent before processing children to avoid concurrent modification
					faveRoot.nodes.Remove(node);
					return; // Exit early since this node was removed
				}
			}
		
		}

		// Create a copy of the current node's children to avoid issues with collection modification
		var children = new List<Node>(node.nodes);
		foreach (Node child in children)
		{
			CheckNode(child);
		}


		// Ensure 'Favorites' root node is always checked and has style index 1, and is not processed with the other nodes
		if (node.fullPath == "~Favorites")	{
			node.SetCheckedWithoutNotify(true);
			node.styleIndex = 1;
		}
	}
	
	public static void ConvertPathsToNodes(UIRecycleTree tree, List<string> paths, string extension = ".prefab", string searchQuery = "")
	{
		
		tree.Clear();
		Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

		// Create a root node for "~Favorites" explicitly
		Node favoritesRootNode = new Node("~Favorites");
		tree.rootNode.nodes.AddWithoutNotify(favoritesRootNode);
		favoritesRootNode.tree = tree;
							
		foreach (string path in paths)
		{
			// Check if the path matches the extension or starts with "~Favorites/"
			if (path.EndsWith(extension, StringComparison.Ordinal) || 
				extension.Equals("override", StringComparison.Ordinal) || 
				path.StartsWith("~Favorites/", StringComparison.Ordinal))
			{
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
					bool isFavoritePath = path.StartsWith("~Favorites/", StringComparison.Ordinal);

					if (isFavoritePath)
					{


						// Extract the filename (last part of the path)
						string filename = System.IO.Path.GetFileName(path);

						// Create a node for the filename
						Node favoriteNode = new Node(filename);

						// Attach the actual path (without "~Favorites/") to node.data
						string actualPath = path.Substring("~Favorites/".Length);
						/*
						if (!actualPath.EndsWith(extension, StringComparison.Ordinal))
						{
							actualPath += extension; // Add extension if not already present
						}
						*/
						favoriteNode.data = actualPath;
						
						// Add the node directly under the "~Favorites" root
						favoritesRootNode.nodes.AddWithoutNotify(favoriteNode);
						favoriteNode.parentNode = favoritesRootNode;
						favoriteNode.tree = tree;
					}
					else
					{
						// Handle non-"~Favorites/" paths (e.g., "~Geology/")
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
									// Add top-level nodes directly to the tree root
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
		}
		tree.Rebuild();
	}
	

	
	
	
    public const string BundlePathExt = @"\Bundles\Bundles";
	
	public static bool style { get; set; }
    public static string RustDirectory { get; set; }
    public static float PrefabRenderDistance { get; set; }
    public static float PathRenderDistance { get; set; }
    public static float WaterTransparency { get; set; }
    public static bool LoadBundleOnLaunch { get; set; }
    public static bool TerrainTextureSet { get; set; }
	
	public static Favorites faves { get; set; }
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
    public static WindowState[] windowStates { get; set; }
    public static MenuState menuState { get; set; }
	
    /// <summary>Saves the current EditorSettings to a JSON file.</summary>
	public static void SaveSettings()
	{
		try
		{
			using (StreamWriter write = new StreamWriter(SettingsPath, false))
			{
				EditorSettings editorSettings = new EditorSettings
				(
					RustDirectory, PrefabRenderDistance, PathRenderDistance, WaterTransparency, 
					LoadBundleOnLaunch, TerrainTextureSet, style, crazing, perlinSplat, ripple, 
					ocean, terracing, perlin, geology, replacer, city, breaker, macroSources, 
					application, faves, windowStates, menuState
				);
				JsonSerializerSettings settings = new JsonSerializerSettings
				{
					ContractResolver = new Vector3ContractResolver(),
					Formatting = Formatting.Indented
				};
				string json = JsonConvert.SerializeObject(editorSettings, settings);
				write.Write(json);
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Error saving settings to {SettingsPath}: {e.Message}");
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
			string json = JsonConvert.SerializeObject(fragmentIDs, Formatting.Indented);
			write.Write(json);
			fragmentIDs.Deserialize();
		}
	}

	public static void LoadFragmentLookup()
	{
		fragmentIDs = new FragmentLookup();
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/breakerFragments.json"))
		{
			fragmentIDs = JsonConvert.DeserializeObject<FragmentLookup>(reader.ReadToEnd());
			fragmentIDs.Deserialize();
		}
	}

	
	public static void SaveBreakerPreset(string filename)
    {
		breakerSerializer.breaker = breaker;
		breakerSerializer.Save(AppDataPath() + $"Presets/Breaker/{filename}.breaker");

    }
	
	public static void LoadBreakerPreset(string filename)
	{
		breaker = breakerSerializer.Load(Path.Combine(AppDataPath() +  $"Presets/Breaker/{filename}.breaker"));

	}
	
	
	public static void SaveGeologyPreset()
	{
		using (StreamWriter write = new StreamWriter(AppDataPath() + $"Presets/Geology/{geology.title}.json", false))
		{
			string json = JsonConvert.SerializeObject(geology, Formatting.Indented);
			write.Write(json);
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
			string json = JsonConvert.SerializeObject(replacer, Formatting.Indented);
			write.Write(json);
		}
	}
	
	
	
	public static void LoadGeologyPreset(string filename)
	{
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/Geology/{filename}.json"))
		{
			geology = JsonConvert.DeserializeObject<GeologyPreset>(reader.ReadToEnd());
		}
	}
	
	public static GeologyPreset GetGeologyPreset(string filename)
	{
		if (File.Exists(filename))
		{
			using (StreamReader reader = new StreamReader(filename))
			{
				return JsonConvert.DeserializeObject<GeologyPreset>(reader.ReadToEnd());
			}
		}
		else
			return new GeologyPreset("file not found");
	}

	
	public static void LoadReplacerPreset(string filename)
	{
		using (StreamReader reader = new StreamReader(AppDataPath() + $"Presets/Replacer/{filename}.json"))
		{
			replacer = JsonConvert.DeserializeObject<ReplacerPreset>(reader.ReadToEnd());
		}
	}
	
	public static void LoadGeologyMacro(string filename)
	{
		macro = new List<string>();
		string macroPath = AppDataPath() + $"Presets/Geology/Macros/{filename}.macro";
		using (StreamReader reader = new StreamReader(macroPath))
		{
			string jsonContent = reader.ReadToEnd();
			GeologyMacroWrapper wrapper = JsonConvert.DeserializeObject<GeologyMacroWrapper>(jsonContent);
			if (wrapper != null && wrapper.macroList != null)
			{
				macro = wrapper.macroList;
			}
		}
	}
	
	public static void SaveGeologyMacro(string macroTitle)
	{
		GeologyMacroWrapper wrapper = new GeologyMacroWrapper { macroList = macro };
		string jsonContent = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
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
	
	public static bool MacroExists(string macroTitle){
		string macroPath = AppDataPath() + $"Presets/Geology/Macros/{macroTitle}.macro";
		return File.Exists(macroPath);
	}
	
	public static void AddToMacro(string macroTitle)
	{
		string macroPath = AppDataPath() + $"Presets/Geology/{macroTitle}.json";
		macro.Add(macroPath);
	}
	

	public static void LoadSettings()
	{
		if (!File.Exists(SettingsPath)) { Debug.LogError("Config file not found"); return; }
		
		using (StreamReader reader = new StreamReader(SettingsPath))
		{
			EditorSettings editorSettings = JsonConvert.DeserializeObject<EditorSettings>(reader.ReadToEnd());
			
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
			faves = editorSettings.faves;
			windowStates = editorSettings.windowStates;
			menuState = editorSettings.menuState;
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
	public Favorites faves;
	
	public WindowState[] windowStates;
    public MenuState menuState;         

    public EditorSettings
    (
        string rustDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Rust", float prefabRenderDistance = 700f, float pathRenderDistance = 200f, 
        float waterTransparency = 0.2f, bool loadbundleonlaunch = false, bool terrainTextureSet = false, bool style = true, CrazingPreset crazing = new CrazingPreset(), PerlinSplatPreset perlinSplat = new PerlinSplatPreset(),
		RipplePreset ripple = new RipplePreset(), OceanPreset ocean = new OceanPreset(), TerracingPreset terracing = new TerracingPreset(), PerlinPreset perlin = new PerlinPreset(), GeologyPreset geology = new GeologyPreset(), 
		ReplacerPreset replacer = new ReplacerPreset(), RustCityPreset city = new RustCityPreset(), BreakerPreset breaker = new BreakerPreset(), bool macroSources = true, FilePreset application = new FilePreset(), Favorites faves = new Favorites(),        WindowState[] windowStates = null, 
        MenuState menuState = new MenuState() 
   
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
			this.faves = faves;
			this.windowStates = windowStates;
			this.menuState = menuState;
        }
}