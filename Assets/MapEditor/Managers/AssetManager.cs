using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif
using UnityEngine;
using RustMapEditor.Variables;
using System.Text.RegularExpressions;
using EasyRoads3Dv3;
using Rust;
using System.Reflection;

public static class AssetManager
{
	#if UNITY_EDITOR
	#region Init
	[InitializeOnLoadMethod]
	private static void Init()
	{
		EditorApplication.update += OnProjectLoad;
		Callbacks.BundlesLoaded += HideLoadScreen;
		Callbacks.BundlesDisposed += FileWindowUpdate;

	}

	private static void OnProjectLoad()
	{
		EditorApplication.update -= OnProjectLoad;
		if (!IsInitialised && SettingsManager.LoadBundleOnLaunch)
			Initialise(SettingsManager.application.rustDirectory + SettingsManager.BundlePathExt);
	}
	#endregion
	#endif
	
	public static void RuntimeInit()	{

		Callbacks.BundlesLoaded += HideLoadScreen;
		Callbacks.BundlesDisposed += FileWindowUpdate;

		string bundlePath = SettingsManager.application.rustDirectory + SettingsManager.BundlePathExt;
		
		string bundleTry = Path.GetFullPath(bundlePath).Substring(Path.GetPathRoot(bundlePath).Length);

		if (!Directory.Exists(bundlePath))		{
			List<string> drives = Directory.GetLogicalDrives().ToList();
			
			foreach (string drive in drives)			{
				string alternativePath = Path.Combine(drive, bundleTry);

				if (Directory.Exists(alternativePath))				{
					FilePreset app = SettingsManager.application;
					app.rustDirectory = alternativePath;
					SettingsManager.application = app;
					SettingsManager.SaveSettings();
					bundlePath = alternativePath;
					break;
				}
			}
			
		}
		
		if (!IsInitialised && SettingsManager.application.loadbundleonlaunch )		{
			Initialise(bundlePath);
		}
		else
		{
			AppManager.Instance.LoadWindowStates();
			AppManager.Instance.ActivateWindow(1);
		}
	}
	
	public static bool ValidBundlePath(string bundleRoot)
	{
		
		if (!Directory.Exists(SettingsManager.application.rustDirectory))		{
				Debug.LogError("Directory does not exist: " + bundleRoot);
			return false;
		}
		

		if (!bundleRoot.ToLower().EndsWith("rust") && 
			!bundleRoot.ToLower().EndsWith("ruststaging"))		{
				Debug.LogError("Not a valid Rust install directory: " + bundleRoot);
			return false;
		}

		bundleRoot= Path.Combine(bundleRoot,"Bundles","Bundles");
		var rootBundle = AssetBundle.LoadFromFile(bundleRoot);
		if (rootBundle == null)		{
			Debug.LogError("Couldn't load root AssetBundle - " + bundleRoot);
			return false;
		}
		rootBundle.Unload(false);
		return true;
	}
	
	private static void FileWindowUpdate()
	{
		SettingsWindow.Instance.UpdateButtonStates();
	}
	

	
	private static void HideLoadScreen()
	{
		LoadScreen.Instance.Hide();
		var application = SettingsManager.application;
		MapManager.CreateMap(application.newSize, application.newSplat, application.newBiome, application.newHeight * 1000f);
		AppManager.Instance.LoadWindowStates();
	}
	
	public static class Callbacks
    {
		public delegate void Bundle();

		/// <summary>Called after Rust Asset Bundles are loaded into the editor. </summary>
		public static event Bundle BundlesLoaded;

		/// <summary>Called after Rust Asset Bundles are unloaded from the editor. </summary>
		public static event Bundle BundlesDisposed;

		public static void OnBundlesLoaded() => BundlesLoaded?.Invoke();
		public static void OnBundlesDisposed() => BundlesDisposed?.Invoke();
	}

	public static GameManifest Manifest { get; private set; }
	
	public const string ManifestPath = "assets/manifest.asset";
	public const string AssetDumpPath = "AssetDump.txt";
	public const string MaterialsListPath = "MaterialsList.txt";
	public const string VolumesListPath = "VolumesList.txt";

	public static Dictionary<uint, string> IDLookup { get; private set; } = new Dictionary<uint, string>();
	public static Dictionary<string, uint> PathLookup { get; private set; } = new Dictionary<string, uint>();
	public static Dictionary<string, AssetBundle> BundleLookup { get; private set; } = new Dictionary<string, AssetBundle>();
	public static Dictionary<string, uint> PrefabLookup { get; private set; } = new Dictionary<string, uint>();
	
	public static Dictionary<string, AssetBundle> BundleCache { get; private set; } = new Dictionary<string, AssetBundle>(System.StringComparer.OrdinalIgnoreCase);
	private static Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
	public static Dictionary<string, UnityEngine.Object> AssetCache { get; private set; } = new Dictionary<string, UnityEngine.Object>();
	public static Dictionary<string, GameObject> VolumesCache { get; private set; } = new Dictionary<string, GameObject>();
	public static Dictionary<string, Texture2D> PreviewCache { get; private set; } = new Dictionary<string, Texture2D>();
	
	public static Dictionary<string, string> GuidToPath { get; private set; } = new Dictionary<string, string>();
	public static Dictionary<string, string> PathToGuid { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	
	public static  Dictionary<string, uint[]> MonumentLayers { get; private set; }  = new Dictionary<string, uint[]>();
	
	public static List<uint> MonumentList { get; private set; } = new List<uint>();
	public static List<uint> ColliderBlocks { get; private set; } = new List<uint>();
	

	public static List<string> AssetPaths { get; private set; } = new List<string>();

	public static bool IsInitialised { get; private set; }

    public static Dictionary<string, string> MaterialLookup { get; private set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, Shader> ShaderCache    { get; private set; } = new Dictionary<string, Shader>(System.StringComparer.OrdinalIgnoreCase);
	


	public static T GetAsset<T>(string filePath) where T : UnityEngine.Object
	{
		if (!BundleLookup.TryGetValue(filePath, out AssetBundle bundle))
			return null;

		return bundle.LoadAsset<T>(filePath);
	}
	
	public static UnityEngine.Object GetAssetByGuid(string guid)
	{
		if (string.IsNullOrEmpty(guid))
		{
			Debug.LogWarning("GUID is empty.");
			return null;
		}

		if (GuidToPath.TryGetValue(guid, out string path))
		{
			if (AssetCache.TryGetValue(path, out UnityEngine.Object cachedAsset))
			{
				Debug.Log($"Loaded resource from cache for GUID: {guid}, Path: {path}");
				return cachedAsset;
			}

			UnityEngine.Object asset = GetAsset<UnityEngine.Object>(path);
			if (asset != null)
			{
				AssetCache[path] = asset;
				Debug.Log($"Loaded resource from bundle for GUID: {guid}, Path: {path}");
				return asset;
			}
			else
			{
				Debug.LogError($"Failed to load asset from bundle for GUID: {guid}, Path: {path}. Path not in BundleLookup or asset missing.");
				return null;
			}
		}
		else
		{
			Debug.LogError($"GUID not found in GuidToPath: {guid}. Total GUIDs loaded: {GuidToPath.Count}");
			return null;
		}
	}
	
	public static T GetAssetByGuid<T>(string guid) where T : UnityEngine.Object
	{
		return GetAssetByGuid(guid) as T;
	}
	
	#if UNITY_EDITOR
	public static void Initialise(string bundlesRoot)
	{
		/// <summary>Loads the Rust bundles into memory.</summary>
		/// <param name="bundlesRoot">The file path to the Rust bundles file.</param>
		if (!Coroutines.IsInitialising && !IsInitialised)
			EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Initialise(bundlesRoot));
		if (IsInitialised)
			Debug.Log("Bundles already loaded.");
	}

	/// <summary>Loads the Rust bundles at the currently set directory.</summary>
	public static void Initialise() => Initialise(SettingsManager.application.rustDirectory + SettingsManager.BundlePathExt);

	public static void Dispose()
	{
		if (!Coroutines.IsInitialising && IsInitialised)
			EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Dispose());
	}
	#else
	
	public static void Initialise(string bundlesRoot)
	{
		if (!Coroutines.IsInitialising && !IsInitialised)
			CoroutineManager.Instance.StartCoroutine(Coroutines.Initialise(bundlesRoot));
		if (IsInitialised)
			Debug.Log("Bundles already loaded.");
	}

	/// <summary>Loads the Rust bundles at the currently set directory.</summary>
	public static void Initialise() => Initialise(SettingsManager.application.rustDirectory + SettingsManager.BundlePathExt);

	public static void Dispose()
	{
		if (!Coroutines.IsInitialising && IsInitialised)
			CoroutineManager.Instance.StartCoroutine(Coroutines.Dispose());
		

	}
	#endif


    public static T LoadAsset<T>(string filePath) where T : UnityEngine.Object
	{
		T asset;

		if (AssetCache.ContainsKey(filePath))
			asset = AssetCache[filePath] as T;
		else
		{
			asset = GetAsset<T>(filePath);
			if (asset != null){
				AssetCache.Add(filePath, asset);
			}
		}

		return asset;
	}

	public static GameObject LoadPrefab(string filePath)
    {
		
        if (AssetCache.ContainsKey(filePath))
            return AssetCache[filePath] as GameObject;

		GameObject go;
		//if it's a volume we return a default
		if(filePath.Contains("volume")||filePath.Contains("radiation")){
			if(filePath.Contains("sphere")){
				return PrefabManager.DefaultSphereVolume;
			}
			return PrefabManager.DefaultCubeVolume;
		}
		
        go = GetAsset<GameObject>(filePath);
		

		//configure, cache, and return
		if (go != null)    {		
		
			PrefabManager.Setup(go, filePath);
			AssetCache.Add(filePath, go);
			return go;

            }
            Debug.LogWarning("Prefab not loaded from bundle: " + filePath);
            return PrefabManager.DefaultPrefab;

    }
	

	/// <summary>Returns a preview image of the asset located at the filepath. Caches the results.</summary>
	public static Texture2D GetPreview(string filePath)
    {
		#if UNITY_EDITOR
		if (PreviewCache.TryGetValue(filePath, out Texture2D preview))
			return preview;
        else
        {
			var prefab = LoadPrefab(filePath);
			if (prefab.name == "DefaultPrefab")
				return AssetPreview.GetAssetPreview(prefab);

			prefab.SetActive(true);
			var tex = AssetPreview.GetAssetPreview(prefab) ?? new Texture2D(60, 60);
			PreviewCache.Add(filePath, tex);
			prefab.SetActive(false);
			return tex;
        }
		#else
		return new Texture2D(60, 60);
		#endif
    }

	public static bool isMonument(uint id){
		return MonumentList.Contains(id);
	}

	public static void SetVolumesCache()
    {
		if (File.Exists(VolumesListPath))
        {

		
			var volumes = File.ReadAllLines(VolumesListPath);
            for (int i = 0; i < volumes.Length; i++)
            {
				var lineSplit = volumes[i].Split(':');
				lineSplit[0] = lineSplit[0].Trim(' '); // Volume Type
				lineSplit[1] = lineSplit[1].Trim(' '); // Prefab Path
				
				
				if (AssetCache.ContainsKey(lineSplit[1]))
				{
					if(!VolumesCache.ContainsKey(lineSplit[1]))
					{
						switch (lineSplit[0])
						{
							case "Cube":
								//VolumesCache.Add(lineSplit[1], (GameObject)AssetCache[lineSplit[1]]);
								//Resources.Load<GameObject>("Prefabs/TranslucentCube").transform.parent = VolumesCache[lineSplit[1]].transform;
								
								 VolumesCache.Add(lineSplit[1], (GameObject)AssetCache[lineSplit[1]]);
								 GameObject transCube = Resources.Load<GameObject>("Prefabs/TranslucentCube");
								 GameObject instantiatedTransCube = UnityEngine.Object.Instantiate(transCube, VolumesCache[lineSplit[1]].transform);
                           
								
								
								break;
							case "Sphere":
								VolumesCache.Add(lineSplit[1], (GameObject)AssetCache[lineSplit[1]]);
								break;
						}
					}
				}
					
					
            }
        }
	}
	
	
	/// <summary>Adds the volume gizmo component to the prefabs in the VolumesList.</summary>
	public static void SetVolumeGizmos()
    {
		if (File.Exists(VolumesListPath))
        {
			var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
			GameObject.DestroyImmediate(cube);
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			var sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
			GameObject.DestroyImmediate(sphere);
			
			
			var volumes = File.ReadAllLines(VolumesListPath);
            for (int i = 0; i < volumes.Length; i++)
            {
				var lineSplit = volumes[i].Split(':');
				lineSplit[0] = lineSplit[0].Trim(' '); // Volume Mesh Type
				lineSplit[1] = lineSplit[1].Trim(' '); // Prefab Path
                
				var prefab = LoadPrefab(lineSplit[1]);
				
				if (prefab.TryGetComponent<VolumeGizmo>(out VolumeGizmo vg))
				{
					Debug.LogWarning("mesh already exists");
				}
				
				else
					
				{
					switch (lineSplit[0])
					{
						case "Cube":
							LoadPrefab(lineSplit[1]).AddComponent<VolumeGizmo>().mesh = cubeMesh;
							break;
						case "Sphere":
							LoadPrefab(lineSplit[1]).AddComponent<VolumeGizmo>().mesh = sphereMesh;
							break;
					}
				}
            }
        }
    }

	public static string pathToName(string path)
	{
			path = path.Replace(@"\","/");
			string[] pathFragment = path.Split('/');
			string filename = pathFragment[pathFragment.Length-1];
			//filename = filename.Replace('_',' ');
			//string[] extension = filename.Split('.');
			//filename = extension[0];
			return filename;
	}

	public static string ToName(uint i)
	{
		if ((int)i == 0)
			return i.ToString();
		if (IDLookup.TryGetValue(i, out string str))
		{
			return pathToName(str);
		}
		return i.ToString();
	}
	
	public static uint fragmentToID(string fragment, string parent, string monument)
	{
		string newFragment = Regex.Replace(fragment, @"\s?\(.*?\)$", "").ToLower();
		string newParent = Regex.Replace(parent, @"\s?\(.*?\)$", "");
		uint parentID = 0;
		uint ID = 0;
		uint returnID = 0;


		try
			{
				ID = SettingsManager.fragmentIDs.fragmentNamelist[fragment];
				return ID;
			}
		catch (KeyNotFoundException)
			{
			}

		if (SettingsManager.fragmentIDs.fragmentNamelist.TryGetValue("/" + newParent + "/", out parentID))
		{
			return parentID;
		}
		
		// Final lookup for direct fragment match
		if (SettingsManager.fragmentIDs.fragmentNamelist.TryGetValue(newFragment, out ID))
		{
			return ID;
		}
		
		// Attempt to get ID from PrefabLookup
		if (PrefabLookup.TryGetValue(newFragment, out ID))
		{
			returnID = ID;
		}
		else
		{
			newFragment = specialParse(newFragment, monument);
			if (PrefabLookup.TryGetValue(newFragment, out ID))
			{
				returnID = ID;
			}
		}
		
		// Special case for Oilrig
		if (monument == "assets/bundled/prefabs/autospawn/monument/offshore/oilrig_2.prefab")
		{
			if (PrefabLookup.TryGetValue("oilrig_small/" + newFragment, out ID))
			{
				returnID = ID;
			}
		}
		
		// If parent ID was found, return it
		if (parentID != 0)
		{
			return returnID;
		}
		

		
		return returnID;
	}


	public static string specialParse(string str, string str2)
	{
		
		string[] parse, parse2;

		
	
		if(str.Contains("GCD1"))

		{
							parse = str.Split('_');
							str = parse[1];
		}			
		else if(str.Contains("GCD") || str.Contains("BCD") || str.Contains("RCD") || str.Contains("GDC"))
		{
							parse = str.Split('_');
							if(str2 == "Oilrig 2")
							{
								str = parse[1];
							}
							else
							{
								str = parse[2];
							}
		}
		else if (str.Contains("outbuilding") || str.Contains("rowhouse"))
			{
						//remove color tags
						parse2 = str.Split('-');
						str = parse2[0];
			}
		else
		{			
			if (str2 == "assets/bundled/prefabs/autospawn/monument/arctic_bases/arctic_research_base_a.prefab")
			{
				string[] parse4;
				int trash = 0;
				parse4 = str.Split('_');
													
					if (int.TryParse(parse4[parse4.GetLength(0)-1], out trash))
					{
						if ((!str.Contains("trail") && trash != 300 && trash != 600 && trash != 900))
						{
							str = str.Replace("_" + trash.ToString(), "");
										//parse4[parse4.GetLength(0)-1] = "";
										//str = string.Join("_",parse4);
										//str = str.Remove(str.Length-1);
						}
						else if (str.Contains("rock"))
						{
							str = str.Replace("_" + trash.ToString(), "");
							Debug.LogError(str);
							//parse4[parse4.GetLength(0)-1] = "";
							//str = string.Join("_",parse4);
							//str = str.Remove(str.Length-1);
							
						}
					}
			}
		}
			
		
		
		return str;
	}

	public static uint partialToID(string str, string str2)
	{
		string path, prefab, folder;
		string[] parse, parse2, parse3;
		folder = "";

		
	
		if(str.Contains("GCD1"))

		{
							parse = str.Split('_');
							str = parse[1];
		}			
		else if(str.Contains("GCD") || str.Contains("BCD") || str.Contains("RCD") || str.Contains("GDC"))
					{
							parse = str.Split('_');
							if(str2 == "Oilrig 2")
							{
								str = parse[1];
							}
							else
							{
								str = parse[2];
							}
					}
		
		
		
		parse3 = str.Split(' ');
		str = parse3[0].ToLower();
		//remove most number tags
		
		if (str2 == "arctic research base a")
		{
			string[] parse4;
			int trash = 0;
			parse4 = str.Split('_');
												
				if (int.TryParse(parse4[parse4.GetLength(0)-1], out trash))
				{
					if (!str.Contains("trail") && trash != 300 && trash != 600 && trash != 900)
					{
									parse4[parse4.GetLength(0)-1] = "";
									str = string.Join("_",parse4);
									str = str.Remove(str.Length-1);
					}
				}
		}
		else if (str2 == "Oilrig 1")
		{
			folder = "prefabs_large_oilrig";
		}
		else if (str2 == "Oilrig 2")
		{
			folder = "prefabs_small_oilrig";
		}
		
		
		if (string.IsNullOrEmpty(str))
			return 0;
		

		
		if (string.IsNullOrEmpty(folder))
		{

			foreach (KeyValuePair<string, uint> kvp in PathLookup)
			{
				path = kvp.Key;
				parse = path.Split('/');
				prefab = parse[parse.Length -1];
				
				if ((prefab == (str+".prefab")))
				{
					return kvp.Value;
				}
			}
			
			//if can't find the rowhouse or outbuilding try again, without color tags
			if (str.Contains("outbuilding") || str.Contains("rowhouse"))
			{
				foreach (KeyValuePair<string, uint> kvp in PathLookup)
				{
					path = kvp.Key;
						parse = path.Split('/');
						prefab = parse[parse.Length -1];
						
						//remove color tags
						parse2 = str.Split('-');
						str = parse2[0];
						if ((prefab == (str+".prefab")))
						{
							return kvp.Value;
						}
				}
			}
			
		}
		
		else
		{
		
			foreach (KeyValuePair<string, uint> kvp in PathLookup)
			{
				path = kvp.Key;
				parse = path.Split('/');
				prefab = parse[parse.Length -1];
				
				if ((prefab == (str+".prefab")) && path.Contains(folder))
				{
					return kvp.Value;
				}
			}
			
			foreach (KeyValuePair<string, uint> kvp in PathLookup)
			{
				path = kvp.Key;
				parse = path.Split('/');
				prefab = parse[parse.Length -1];
				
				if ((prefab == (str+".prefab")))
				{
					return kvp.Value;
				}
			}
			
		}
		
		return 0;
	}
		
public static void AssetDump()
{
    if (Manifest == null)
    {
        Debug.LogError("Manifest is null. Cannot dump contents.");
        return;
    }
    if (BundleLookup.Count == 0)
    {
        Debug.LogError("BundleLookup is empty. Ensure bundles are loaded before dumping.");
        return;
    }

    string dumpPath = "ManifestDump.txt";
    using (StreamWriter streamWriter = new StreamWriter(dumpPath, false))
    {
        // Header
        streamWriter.WriteLine("=== Rust Manifest and Bundle Dump ===");
        streamWriter.WriteLine($"Date: {DateTime.Now}");
        streamWriter.WriteLine();

        // Section 1: Pooled Strings (Paths and Hashes)
        streamWriter.WriteLine("--- Pooled Strings (Manifest) ---");
        streamWriter.WriteLine($"Total entries: {Manifest.pooledStrings.Length}");
        var pooledStringsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Track duplicates
        foreach (var pooledString in Manifest.pooledStrings)
        {
            string path = pooledString.str;
            uint hash = pooledString.hash;
            string entry = $"{path} : Hash={hash}";
            if (PathToGuid.TryGetValue(path, out string guid))
            {
                entry += $" : GUID={guid}";
            }
            streamWriter.WriteLine(entry);
            pooledStringsSet.Add(path);
        }
        streamWriter.WriteLine();

        // Section 2: GUID Paths
        streamWriter.WriteLine("--- GUID Paths (Manifest) ---");
        streamWriter.WriteLine($"Total entries: {Manifest.guidPaths.Length}");
        int guidPathUniqueCount = 0;
        foreach (var guidPath in Manifest.guidPaths)
        {
            string path = guidPath.name;
            string guid = guidPath.guid;
            string entry = $"{path} : GUID={guid}";
            if (PathLookup.TryGetValue(path, out uint hash))
            {
                entry += $" : Hash={hash}";
            }
            if (!pooledStringsSet.Contains(path))
            {
                streamWriter.WriteLine(entry);
                guidPathUniqueCount++;
                pooledStringsSet.Add(path); // Add to set to avoid duplicates in next section
            }
        }
        streamWriter.WriteLine($"Unique entries (not in Pooled Strings): {guidPathUniqueCount}");
        streamWriter.WriteLine();

        // Section 3: Prefab Properties
        streamWriter.WriteLine("--- Prefab Properties (Manifest) ---");
        streamWriter.WriteLine($"Total entries: {Manifest.prefabProperties.Length}");
        int prefabUniqueCount = 0;
        foreach (var prefab in Manifest.prefabProperties)
        {
            string path = prefab.name;
            string guid = prefab.guid;
            string entry = $"{path} : GUID={guid}";
            if (PathLookup.TryGetValue(path, out uint hash))
            {
                entry += $" : Hash={hash}";
            }
            if (!pooledStringsSet.Contains(path))
            {
                streamWriter.WriteLine(entry);
                prefabUniqueCount++;
                pooledStringsSet.Add(path); // Add to set for next section
            }
        }
        streamWriter.WriteLine($"Unique entries (not in Pooled Strings): {prefabUniqueCount}");
        streamWriter.WriteLine();

        // Section 4: All Bundle Assets (from BundleLookup)
        streamWriter.WriteLine("--- All Bundle Assets (BundleLookup) ---");
        streamWriter.WriteLine($"Total entries: {BundleLookup.Count}");
        int bundleUniqueCount = 0;
        foreach (var assetPath in BundleLookup.Keys.OrderBy(k => k)) // Sort for readability
        {
            string path = assetPath;
            string entry = $"{path}";
            bool hasAdditionalInfo = false;

            if (PathLookup.TryGetValue(path, out uint hash))
            {
                entry += $" : Hash={hash}";
                hasAdditionalInfo = true;
            }
            if (PathToGuid.TryGetValue(path, out string guid))
            {
                entry += $" : GUID={guid}";
                hasAdditionalInfo = true;
            }

            // Include all assets, even if already in previous sections, for completeness
            streamWriter.WriteLine(entry);
            if (!pooledStringsSet.Contains(path))
            {
                bundleUniqueCount++;
            }
        }
        streamWriter.WriteLine($"Unique entries (not in previous sections): {bundleUniqueCount}");
    }
    Debug.Log($"Manifest and bundle contents dumped to {dumpPath}");
}

	public static string ToPath(uint i)
	{
		if ((int)i == 0)
			return i.ToString();
		if (IDLookup.TryGetValue(i, out string str))
			return str;
		return i.ToString();
	}

	public static uint ToID(string str)
	{
		if (string.IsNullOrEmpty(str))
			return 0;
		if (PathLookup.TryGetValue(str, out uint num))
			return num;
		return 0;
	}
	
	public static string MaterialPath(string str)
	{
		str = str+".mat";
		if (string.IsNullOrEmpty(str)){
			return "";
		}
		if (MaterialLookup.TryGetValue(str, out string path)){
			return path;
		}
		
		return "";
	}	
	
public static void LoadShaderCache()
{
    ShaderCache.Clear();
    Debug.Log($"Shader cache cleared and ready for loading. Total assets in BundleLookup: {BundleLookup.Count}");
    foreach (string path in BundleLookup.Keys)
    {
        if (path.EndsWith(".shader", StringComparison.Ordinal))
        {
            try
            {  
                Shader shader = LoadAsset<Shader>(path);
                if (shader != null)
                {
                    ShaderCache[shader.name] = shader;
                    Debug.Log($"Loaded shader into ShaderCache: {shader.name} (Path: {path})");
                }
                else
                {
                    Debug.LogWarning($"Shader not found at path: {path}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error loading shader from path: {path}. Exception: {e.Message}");
            }
        }
    }

}

	public static void UpdateShader(Material mat){
		CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.UpdateShader(mat));
	}
	
	
	public static void FixRenderMode(Material mat, Shader shader = null){
		CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.UpdateShader(mat));
	}
	
	private static class Coroutines
    {
		public static bool IsInitialising { get; private set; }

		public static IEnumerator Initialise(string bundlesRoot)
		{
			IsInitialising = true;
			#if UNITY_EDITOR
			ProgressManager.RemoveProgressBars("Asset Bundles");

			int progressID = Progress.Start("Load Asset Bundles", null, Progress.Options.Sticky);
			int bundleID = Progress.Start("Bundles", null, Progress.Options.Sticky, progressID);
			int materialID = Progress.Start("Materials", null, Progress.Options.Sticky, progressID);
			int prefabID = Progress.Start("Replace Prefabs", null, Progress.Options.Sticky, progressID);
			Progress.Report(bundleID, 0f);
			Progress.Report(materialID, 0f);
			Progress.Report(prefabID, 0f);
			
			
			yield return EditorCoroutineUtility.StartCoroutineOwnerless(LoadBundles(bundlesRoot, (progressID, bundleID, materialID)));
			
			if (!IsInitialising)
            {
				Progress.Finish(bundleID, Progress.Status.Failed);
				Progress.Finish(materialID, Progress.Status.Failed);
				Progress.Finish(prefabID, Progress.Status.Failed);
				yield break;
			}
			yield return EditorCoroutineUtility.StartCoroutineOwnerless(SetBundleReferences((progressID, bundleID)));
			
			LoadShaderCache();
			//yield return EditorCoroutineUtility.StartCoroutineOwnerless(PopulateMaterialLookup());
			//yield return EditorCoroutineUtility.StartCoroutineOwnerless(SetMaterials(materialID));
			
			#else	
			yield return CoroutineManager.Instance.StartRuntimeCoroutine(LoadBundles(bundlesRoot, (0, 0, 0)));
			yield return CoroutineManager.Instance.StartRuntimeCoroutine(SetBundleReferences((0, 0)));
			
			//a graveyard of do-nothing routines
			//yield return CoroutineManager.Instance.StartRuntimeCoroutine(PopulateMaterialLookup());
			//yield return CoroutineManager.Instance.StartRuntimeCoroutine(SetMaterials(0));			
			#endif
			

			IsInitialised = true; IsInitialising = false;
			SetVolumeGizmos();
			Callbacks.OnBundlesLoaded();

			#if UNITY_EDITOR
			PrefabManager.ReplaceWithLoaded(PrefabManager.CurrentMapPrefabs, prefabID);
			#else
			PrefabManager.ReplaceWithLoaded(PrefabManager.CurrentMapPrefabs, 0);
			#endif
		}
		
	public static IEnumerator PopulateMaterialLookup()
	{
		MaterialLookup.Clear();

		foreach (string path in BundleLookup.Keys)
		{
			if (path != null && path.EndsWith(".mat", StringComparison.Ordinal))
			{
				string[] parse = path.Split('/');
				
				// Check if the array has elements before accessing the last one
				if (parse.Length > 0)
				{
					string name = parse[parse.Length - 1];

					try
					{
						// Add to dictionary only if the name is not null or empty
						if (!string.IsNullOrEmpty(name))
						{
							MaterialLookup.Add(name, path);
						}
					}
					catch (ArgumentException) // Handle if the key already exists in the dictionary
					{
						Debug.LogWarning($"Material with name '{name}' already exists in MaterialLookup. Path: {path}");
					}
					catch (Exception e) // Log other exceptions for debugging
					{
						Debug.LogError($"Unexpected error adding material '{name}' to lookup: {e.Message}");
					}
				}
			}

			yield return null;
		}
	}
		



		public static IEnumerator Dispose() 
		{
			IsInitialising = true;
			ProgressManager.RemoveProgressBars("Unload Asset Bundles");
			
			#if UNITY_EDITOR
			int progressID = Progress.Start("Unload Asset Bundles", null, Progress.Options.Sticky);
			int bundleID = Progress.Start("Bundles", null, Progress.Options.Sticky, progressID);
			int prefabID = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
			Progress.Report(bundleID, 0f);
			Progress.Report(prefabID, 0f);
			PrefabManager.ReplaceWithDefault(PrefabManager.CurrentMapPrefabs, prefabID);
			#else
			PrefabManager.ReplaceWithDefault(PrefabManager.CurrentMapPrefabs, 0);
			#endif
			
			

			while (PrefabManager.IsChangingPrefabs)
				yield return null;

			for (int i = 0; i < BundleCache.Count; i++)
            {
				#if UNITY_EDITOR
				Progress.Report(bundleID, (float)i / BundleCache.Count, "Unloading: " + BundleCache.ElementAt(i).Key);
				#endif
				BundleCache.ElementAt(i).Value.Unload(true);
				yield return null;
            }
			
			int bundleCount = BundleCache.Count;
			BundleLookup.Clear();
			BundleCache.Clear();
			AssetCache.Clear();
			
			#if Unity_editor
			Progress.Report(bundleID, 0.99f, "Unloaded: " + bundleCount + " bundles.");
			Progress.Finish(bundleID, Progress.Status.Succeeded);
			#endif
			
			IsInitialised = false; IsInitialising = false;
			Callbacks.OnBundlesDisposed();
		}

		public static IEnumerator LoadBundles(string bundleRoot, (int progress, int bundle, int material) ID)
        {
			if (!Directory.Exists(SettingsManager.application.rustDirectory))
			{
				Debug.LogError("Directory does not exist: " + bundleRoot);
				IsInitialising = false;
				yield break;
			}

			if (!SettingsManager.application.rustDirectory.EndsWith("Rust") && !SettingsManager.application.rustDirectory.EndsWith("RustStaging"))
			{
				Debug.LogError("Not a valid Rust install directory: " + SettingsManager.application.rustDirectory);
				IsInitialising = false;
				yield break;
			}

			var rootBundle = AssetBundle.LoadFromFile(bundleRoot);
			if (rootBundle == null)
			{
				Debug.LogError("Couldn't load root AssetBundle - " + bundleRoot);
				IsInitialising = false;
				yield break;
			}

			var manifestList = rootBundle.LoadAllAssets<AssetBundleManifest>();
			if (manifestList.Length != 1)
			{
				Debug.LogError("Couldn't find AssetBundleManifest - " + manifestList.Length);
				IsInitialising = false;
				yield break;
			}

			var assetManifest = manifestList[0];
			var bundles = assetManifest.GetAllAssetBundles();

			for (int i = 0; i < bundles.Length; i++)
			{
				#if UNITY_EDITOR
				Progress.Report(ID.bundle, (float)i / bundles.Length, "Loading: " + bundles[i]);
				#endif
				LoadScreen.Instance.SetMessage("Mining bitcoin...");
				LoadScreen.Instance.Progress((float)i/bundles.Length);
				var bundlePath = Path.GetDirectoryName(bundleRoot) + Path.DirectorySeparatorChar + bundles[i];
				if (File.Exists(bundlePath)) 
				{
                    var asset = AssetBundle.LoadFromFileAsync(bundlePath);
                    yield return asset;

                    if (asset == null)
                    {
                        Debug.LogError("Couldn't load AssetBundle - " + bundlePath);
                        IsInitialising = false;
                        yield break;
                    }
                    BundleCache.Add(bundles[i], asset.assetBundle);
                }
				yield return null;
			}
			rootBundle.Unload(true);
		}
		
public static IEnumerator SetBundleReferences((int parent, int bundle) ID)
{
    var sw = new System.Diagnostics.Stopwatch();
    sw.Start();

    // Populate BundleLookup with asset names and scene paths from loaded bundles
    foreach (var asset in BundleCache.Values)
    {
        foreach (var filename in asset.GetAllAssetNames())
        {
            BundleLookup.Add(filename, asset);
            if (sw.Elapsed.TotalMilliseconds >= 0.5f)
            {
                yield return null;
                sw.Restart();
            }
        }
        foreach (var filename in asset.GetAllScenePaths())
        {
            BundleLookup.Add(filename + " -scene", asset);
            if (sw.Elapsed.TotalMilliseconds >= 0.5f)
            {
                yield return null;
                sw.Restart();
            }
        }
        yield return null;
    }

    #if UNITY_EDITOR
    Progress.Report(ID.bundle, 0.99f, "Loaded " + BundleCache.Count + " bundles.");
    Progress.Finish(ID.bundle, Progress.Status.Succeeded);
    #endif

    // Load the GameManifest from the bundles
    Manifest = GetAsset<GameManifest>(ManifestPath); // "assets/manifest.asset"
    if (Manifest == null)
    {
        Debug.LogError("Couldn't load GameManifest.");
        Dispose();
        #if UNITY_EDITOR
        Progress.Finish(ID.parent, Progress.Status.Failed);
        #endif
        yield break;
    }

    // Debug: Verify guidPaths and prefabProperties
    Debug.Log($"Manifest.guidPaths length: {Manifest.guidPaths.Length}");
    Debug.Log($"Manifest.prefabProperties length: {Manifest.prefabProperties.Length}");

    // Populate GuidToPath and PathToGuid from Manifest
    GuidToPath.Clear();
    PathToGuid.Clear();
    foreach (var prop in Manifest.prefabProperties)
    {
        GuidToPath[prop.guid] = prop.name;
        PathToGuid[prop.name] = prop.guid;
    }
    foreach (var guidPath in Manifest.guidPaths)
    {
        if (!GuidToPath.ContainsKey(guidPath.guid))
        {
            GuidToPath[guidPath.guid] = guidPath.name;
            PathToGuid[guidPath.name] = guidPath.guid;
        }
    }

    var setLookups = Task.Run(() =>
    {
        string[] parse;
        string name;
        string monumentTag = "";
        for (uint i = 0; i < Manifest.pooledStrings.Length; ++i)
        {
            IDLookup.Add(Manifest.pooledStrings[i].hash, Manifest.pooledStrings[i].str);
            PathLookup.Add(Manifest.pooledStrings[i].str, Manifest.pooledStrings[i].hash);

            monumentTag = "";

            if (Manifest.pooledStrings[i].str.EndsWith(".png"))
            {
                parse = Manifest.pooledStrings[i].str.Split('/');
                monumentTag = parse[parse.Length - 2]; // Extract the second-to-last element as the monumentTag

                if (!MonumentLayers.ContainsKey(monumentTag))
                {
                    MonumentLayers[monumentTag] = new uint[8];
                }

                int index = GetMapIndex(Manifest.pooledStrings[i].str);
                if (index > -1)
                {
                    MonumentLayers[monumentTag][index] = Manifest.pooledStrings[i].hash;
                    //Debug.Log($"Mapped '{Manifest.pooledStrings[i].str}' to MonumentLayers['{monumentTag}'][{index}] with hash {Manifest.pooledStrings[i].hash}");
                }
            }

            if (Manifest.pooledStrings[i].str.EndsWith(".prefab"))
            {
                if (Manifest.pooledStrings[i].str.Contains("prefabs_small_oilrig"))
                {
                    monumentTag = "oilrig_small/";
                }

                if (Manifest.pooledStrings[i].str.Contains("client")) //dangerous 
                {
                    monumentTag = "EVENT SYSTEMS DISABLED ";
                }

                parse = Manifest.pooledStrings[i].str.Split('/');
                name = parse[parse.Length - 1];
                name = name.Replace(".prefab", "");
                name = monumentTag + name;

                try
                {
                    PrefabLookup.Add(name, Manifest.pooledStrings[i].hash);
                }
                catch
                {
                    // Ignore duplicates silently for now
                }
            }

            if (ToID(Manifest.pooledStrings[i].str) != 0)
            {
                AssetPaths.Add(Manifest.pooledStrings[i].str);
            }

            if (Manifest.pooledStrings[i].str.Contains("autospawn/monument", StringComparison.Ordinal))
            {
                MonumentList.Add(ToID(Manifest.pooledStrings[i].str));
            }
        }
        AssetDump();
    });

    while (!setLookups.IsCompleted)
    {
        if (sw.Elapsed.TotalMilliseconds >= 0.1f)
        {
            yield return null;
            sw.Restart();
        }
    }
}

	//fake
	public static IEnumerator UpdateShader(Material mat, Shader shader)
	{
		yield return null;
	}
	
	public static IEnumerator UpdateShader(Material mat)
	{
		if (mat == null)
		{
			Debug.LogWarning("Material is null. Skipping update.");
			yield break;
		}

		Shader standardShader = Shader.Find("Custom/Rust/Standard");
		Shader standardFourShader = Shader.Find("Custom/Rust/StandardBlend4Way");
		Shader specularShader = Shader.Find("Standard (Specular setup)");
		Shader decalShader = Shader.Find("Legacy Shaders/Decal");
		Shader standardShaderSpecular = Shader.Find("Custom/Rust/StandardSpecular");
		Shader standardShaderBlend = Shader.Find("Custom/Rust/StandardBlendLayer");
		Shader standardDecal = Shader.Find("Custom/Rust/StandardDecal");
		Shader coreFoliage = Shader.Find("Custom/CoreFoliage");
		Shader standardFourSpecularShader = Shader.Find("Custom/Rust/StandardBlend4WaySpecular");
		Shader standardTerrain  = Shader.Find("Custom/Rust/StandardTerrain");
		Shader coreFoliageBillboard = Shader.Find("Custom/CoreFoliageBillboard");
		
		// Skip if the shader is Core/Foliage
		if (mat.shader.name.Equals("Core/Foliage"))
		{
			mat.shader = coreFoliage;
			yield break;
		}
		
		// Skip if the shader is Core/Foliage
		if (mat.shader.name.Equals("Core/Foliage Billboard"))
		{
			mat.shader = coreFoliageBillboard;
			yield break;
		}
		
		/*
		// Skip if the shader is Core/Foliage
		if (mat.shader.name.Contains("Core/Foliage"))
		{
			//set this shader's render queue to transparent
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			//Debug.Log($"Material '{mat.name}' uses shader '{mat.shader.name}' and will not be updated.");
			yield break;
		}
		*/
		
		
		if (mat.shader.name.Equals("Rust/Standard Terrain"))
		{
			mat.shader = standardTerrain;
			yield break;
		}
		
		
		if (mat.shader.name.Equals("Standard (Specular setup)") ||mat.shader.name.Equals("Rust/Standard") || mat.shader.name.Equals("Rust/Standard + Wind") || mat.shader.name.Equals("Rust/Standard Cloth")
			|| mat.shader.name.Equals("Rust/Standard Particle") || mat.shader.name.Equals("Rust/Standard Snow Area") || mat.shader.name.Equals("Rust/Standard Wire") || mat.shader.name.Equals("Rust/Standard + Specular Glare"))
		{
			mat.shader = standardShader;
			yield break;
		}
		
		if (mat.shader.name.Equals("Rust/Standard Blend 4-Way"))
		{
			mat.shader = standardFourShader;
			yield break;
		}
		
		if ( mat.shader.name.Equals("Rust/Standard (Specular setup)") || mat.shader.name.Equals("Rust/Standard + Wind (Specular setup)") || mat.shader.name.Equals("Rust/Standard + Decal (Specular setup)"))
		{
			mat.shader = standardShaderSpecular;
			yield break;
		}
		
		if (mat.shader.name.Equals("Rust/Standard Blend Layer") || mat.shader.name.Equals("Rust/Standard Terrain Blend (Specular setup)") || mat.shader.name.Equals("Rust/Standard Blend Layer (Specular setup)"))
		{
			mat.shader = standardShaderBlend;
			yield break;
		}
		
		if (mat.shader.name.Equals("Rust/Standard Blend 4-Way (Specular setup)") )
		{
			mat.shader = standardFourSpecularShader;
			yield break;
		}
		
		if (mat.shader.name.Contains("Rust/Standard Decal"))
		{
			mat.shader = standardShader;
			yield break;
		}
		
		if (mat.shader.name.Contains("Nature/Water"))
		{
			mat.shader = standardShaderBlend;
			mat.SetOverrideTag("RenderType", "Transparent");
			yield break;
		}
		

		/*
		if (mat.shader.name.Contains("Decal"))
		{
			mat.shader = decalShader;			
			mat.SetOverrideTag("RenderType", "TransparentCutout");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.EnableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.SetFloat("_Mode", 1f);
			mat.SetFloat("_Cutoff", 0.5f); // Default cutoff
			mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			yield break;
		}
		*/
		
		
		//int renderQueue = mat.renderQueue;

		// Determine mode based on render queue
		/*
		if (renderQueue <= (int)UnityEngine.Rendering.RenderQueue.Geometry) // 2000
		{
			mat.shader = standardShader;
			mat.SetOverrideTag("RenderType", "");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.SetFloat("_Mode", 0f);
			mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
			mat.SetFloat("_Metallic", 0.25f); 
			mat.SetFloat("_Glossiness", 0.25f);
		}
		
		
		if (renderQueue > (int)UnityEngine.Rendering.RenderQueue.Geometry &&
				 renderQueue <= 2450) // Transparent Cutout range
		{
			mat.shader = standardShader;
			mat.SetOverrideTag("RenderType", "TransparentCutout");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.EnableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.SetFloat("_Mode", 1f);
			mat.SetFloat("_Cutoff", 0.5f); // Default cutoff
			mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		}
		*/
		
		/*
		if (renderQueue > 2450) // Transparent range
		{
			mat.shader =  standardShader;
			mat.SetOverrideTag("RenderType", "Transparent");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			mat.SetInt("_ZWrite", 0);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.EnableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.SetFloat("_Mode", 2f);
			mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		}
		else
		{
			//Debug.LogWarning($"Material {mat.name} has an unsupported render queue: {renderQueue}. No changes applied.");
			yield break;
		}
		*/

		//Debug.Log($"Material '{mat.name}' updated successfully. Mode: {mat.GetFloat("_Mode")}, Render Queue: {mat.renderQueue}");
		yield return null;
	}

		private static int GetMapIndex(string fileName)
		{
			if (fileName.Contains("heighttexture")) return 0;
			if (fileName.Contains("splattexture0")) return 1;
			if (fileName.Contains("splattexture1")) return 2;
			if (fileName.Contains("alphatexture")) return 3;
			if (fileName.Contains("biometexture")) return 4;
			if (fileName.Contains("topologytexture")) return 5;
			if (fileName.Contains("watermap")) return 6;
			if (fileName.Contains("watertexture")) return 7;
			return -1; // Invalid index if no match
		}


		private static void SetKeyword(Material mat, string keyword, bool state)
		{
			if (state)
				mat.EnableKeyword(keyword);
			else
				mat.DisableKeyword(keyword);
		}
	}
}