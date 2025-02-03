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
			AppManager.Instance.ActivateWindow(1);
		}
	}
	
	public static bool ValidBundlePath(string bundleRoot)
	{
		bundleRoot= bundleRoot+ SettingsManager.BundlePathExt;
		if (!Directory.Exists(SettingsManager.application.rustDirectory))		{
			Debug.LogError("Directory does not exist: " + bundleRoot);
			return false;
		}

		if (!SettingsManager.application.rustDirectory.EndsWith("Rust") && 
			!SettingsManager.application.rustDirectory.EndsWith("RustStaging"))		{
			Debug.LogError("Not a valid Rust install directory: " + SettingsManager.application.rustDirectory);
			return false;
		}

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
		AppManager.Instance.ActivateWindow(0);
		
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

	public static List<string> AssetPaths { get; private set; } = new List<string>();

	public static bool IsInitialised { get; private set; }

    public static Dictionary<string, string> MaterialLookup { get; private set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, Shader> ShaderCache    { get; private set; } = new Dictionary<string, Shader>(System.StringComparer.OrdinalIgnoreCase);
	


	private static T GetAsset<T>(string filePath) where T : UnityEngine.Object
	{
		if (!BundleLookup.TryGetValue(filePath, out AssetBundle bundle))
			return null;

		return bundle.LoadAsset<T>(filePath);
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
			if (asset != null)
				AssetCache.Add(filePath, asset);
		}
		return asset;
	}

	public static GameObject LoadPrefab(string filePath)
    {
		
        if (AssetCache.ContainsKey(filePath))
            return AssetCache[filePath] as GameObject;


        GameObject val = GetAsset<GameObject>(filePath);
           if (val != null)
            {
				PrefabManager.Setup(val, filePath);
				AssetCache.Add(filePath, val);
				//PrefabManager.Callbacks.OnPrefabLoaded(val);
				return val;
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
			filename = filename.Replace('_',' ');
			string[] extension = filename.Split('.');
			filename = extension[0];
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
	
	//triangulate prefab identity from path information
	public static uint fragmentToID(string fragment, string parent, string monument)
	{
		string[] parseFragment;
		string newFragment;
		uint parentID = 0;
		uint ID = 0;
		uint returnID = 0;
			
		if (SettingsManager.fragmentIDs.fragmentNamelist.TryGetValue("/" + parent + "/", out parentID))
		{		}
		else
		{		}
		
		parseFragment = fragment.Split(' ');
		newFragment = parseFragment[0].ToLower();
		
		if (monument == "Oilrig 1")
		{
			if (PrefabLookup.TryGetValue("plo#"+newFragment, out ID))
			{ returnID=ID;	}
			else
			{	}
		}
		else if (monument == "Oilrig 2")
		{
			if (PrefabLookup.TryGetValue("pso#"+newFragment, out ID))
			{	returnID=ID;	}
			else
			{		}
		}
		
		

		if (PrefabLookup.TryGetValue(newFragment, out ID))
		{	returnID = ID;	}
		else
		{	
			newFragment = specialParse(newFragment, monument); 
			if (PrefabLookup.TryGetValue(newFragment, out ID))
			{	returnID = ID;	}
		}
		
		
		if (parentID!=0)
		{	returnID = parentID;	}
		
		if (SettingsManager.fragmentIDs.fragmentNamelist.TryGetValue(fragment, out ID))
			{	
				return ID;		
			}
		else
		{	return returnID;	}
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
			if (str2 == "arctic research base a")
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
	
	/// <summary>Dumps every asset found in the Rust content bundle to a text file.</summary>
	public static void AssetDump()
	{
		using (StreamWriter streamWriter = new StreamWriter(AssetDumpPath, false))
			foreach (var item in BundleLookup.Keys)
				streamWriter.WriteLine(item + " : " + ToID(item));
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
		
		/*
		ShaderCache.Clear();
		Debug.Log($"Shader cache cleared and ready for loading");
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
		
		foreach (var shader in ShaderCache)
		{
			string shaderName = shader.Key;
			string filePath = Path.Combine("E:/shaders/", shaderName + ".shader");
			
			try
			{
				// Ensure the directory exists
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				
				// Write shader to file
				using (StreamWriter writer = new StreamWriter(filePath))
				{
					
					writer.Write(shader.Value.ToString()); // Assuming ToString() provides the shader's source code
				}
				Debug.Log($"Shader {shaderName} written to {filePath}");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Failed to write shader {shaderName} to file. Exception: {e.Message}");
			}
		}
		*/
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
			
			//LoadShaderCache();
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
					BundleLookup.Add(filename, asset);
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

			Manifest = GetAsset<GameManifest>(ManifestPath);
			if (Manifest == null)
			{
				Debug.LogError("Couldn't load GameManifest.");
				Dispose();
				#if UNITY_EDITOR
				Progress.Finish(ID.parent, Progress.Status.Failed);
				#endif
				yield break;
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
					
					if(Manifest.pooledStrings[i].str.EndsWith(".prefab"))
					{
						if(Manifest.pooledStrings[i].str.Contains("prefabs_large_oilrig"))
						{
							monumentTag = "plo#";
						}
						else if(Manifest.pooledStrings[i].str.Contains("prefabs_small_oilrig"))
						{
							monumentTag = "pso#";
						}
						
						parse = Manifest.pooledStrings[i].str.Split('/');
						name = parse[parse.Length -1];
						name = name.Replace(".prefab", "");
						name = monumentTag + name;
						
						try
						{
							PrefabLookup.Add(name, Manifest.pooledStrings[i].hash);
						}
						catch
						{
							
						}
					}
					if (ToID(Manifest.pooledStrings[i].str) != 0)
						AssetPaths.Add(Manifest.pooledStrings[i].str);
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

	public static IEnumerator SetMaterials(int materialID)
	{
		bool performance = false; // Set to true for file-based material loading
		int count = 0;

		Shader standardShader = Shader.Find("Standard");
		Shader specularShader = Shader.Find("Standard (Specular setup)");

		Dictionary<Shader, List<Material>> shaderGroups = new Dictionary<Shader, List<Material>>();

		// File-based Material Processing
		if (File.Exists(MaterialsListPath) && performance)
		{
			string[] materials = File.ReadAllLines(MaterialsListPath);
			for (int i = 0; i < materials.Length; i++)
			{
				var lineSplit = materials[i].Split(':');
				lineSplit[0] = lineSplit[0].Trim(); // Shader Name
				lineSplit[1] = lineSplit[1].Trim(); // Material Path

	#if UNITY_EDITOR
				Progress.Report(materialID, (float)i / materials.Length, "Processing: " + lineSplit[1]);
	#endif

				Material mat = LoadAsset<Material>(lineSplit[1]); // Uses AssetCache automatically
				if (mat == null)
				{
					Debug.LogWarning($"{lineSplit[1]} is not a valid asset.");
					continue;
				}

				// Group materials by shader type
				Shader targetShader = null;
				switch (lineSplit[0])
				{
					case "Standard":
						targetShader = standardShader;
						break;
					case "Specular":
						targetShader = specularShader;
						break;
					case "Foliage":
						mat.DisableKeyword("_TINTENABLED_ON");
						break;
					default:
						Debug.LogWarning($"{lineSplit[0]} is not a valid shader.");
						continue;
				}

				if (targetShader != null)
				{
					if (!shaderGroups.ContainsKey(targetShader))
						shaderGroups[targetShader] = new List<Material>();

					shaderGroups[targetShader].Add(mat);
				}

				yield return null;
			}
		}
		else if (!performance) // Load from in-memory MaterialLookup
		{
			foreach (string path in MaterialLookup.Values)
			{
				Material mat = LoadAsset<Material>(path); // Uses AssetCache automatically
				if (mat == null) continue;

				Shader shader = mat.shader;
				if (!shaderGroups.ContainsKey(shader))
					shaderGroups[shader] = new List<Material>();

				shaderGroups[shader].Add(mat);
				yield return null;
			}
			Debug.Log($"processed {MaterialLookup.Count} materials");
		}

		// Update grouped materials
		int batchSize = 10;
		foreach (var group in shaderGroups)
		{
			Shader shader = group.Key;
			List<Material> materials = group.Value;

			for (int i = 0; i < materials.Count; i++)
			{
				Material mat = materials[i];

				/*
				// Skip if the shader is already correct
				if (mat.shader == shader)
				{
					continue;
				}
				*/
				// Update the material with the correct shader
				yield return UpdateShader(mat);

				// Yield after processing a batch of materials
				if ((i + 1) % batchSize == 0)
					yield return null;
			}
			Debug.Log($"updated {materials.Count} shaders");
		}

	#if UNITY_EDITOR
		Progress.Report(materialID, 0.99f, "Materials processed.");
		Progress.Finish(materialID, Progress.Status.Succeeded);
	#endif
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


		
		Shader standardShader = Shader.Find("Standard");
		Shader specularShader = Shader.Find("Standard (Specular setup)");
		Shader decalShader = Shader.Find("Legacy Shaders/Decal");
		
		// Skip if the shader is Core/Foliage
		if (mat.shader.name.Contains("Core/Foliage"))
		{
			//set this shader's render queue to transparent
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			//Debug.Log($"Material '{mat.name}' uses shader '{mat.shader.name}' and will not be updated.");
			yield break;
		}
		
		
		if (mat.shader.name.Contains("Nature/Water"))
		{
			mat.shader = specularShader;
			mat.SetOverrideTag("RenderType", "Transparent");
			yield break;
		}
		
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
		}
		
		int renderQueue = mat.renderQueue;

		// Determine mode based on render queue
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
		else if (renderQueue > (int)UnityEngine.Rendering.RenderQueue.Geometry &&
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
		else if (renderQueue > 2450) // Transparent range
		{
			mat.shader = specularShader;
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

		//Debug.Log($"Material '{mat.name}' updated successfully. Mode: {mat.GetFloat("_Mode")}, Render Queue: {mat.renderQueue}");
		yield return null;
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