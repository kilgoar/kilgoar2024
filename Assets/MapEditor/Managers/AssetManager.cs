using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public static class AssetManager
{
	#region Init
	[InitializeOnLoadMethod]
	private static void Init()
	{
		EditorApplication.update += OnProjectLoad;
	}

	private static void OnProjectLoad()
	{
		EditorApplication.update -= OnProjectLoad;
		if (!IsInitialised && SettingsManager.LoadBundleOnLaunch)
			Initialise(SettingsManager.RustDirectory + SettingsManager.BundlePathExt);
	}
	#endregion

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
	public static Dictionary<string, Object> AssetCache { get; private set; } = new Dictionary<string, Object>();
	public static Dictionary<string, GameObject> VolumesCache { get; private set; } = new Dictionary<string, GameObject>();
	public static Dictionary<string, Texture2D> PreviewCache { get; private set; } = new Dictionary<string, Texture2D>();

	public static List<string> AssetPaths { get; private set; } = new List<string>();

	public static bool IsInitialised { get; private set; }

	/// <summary>Loads the Rust bundles into memory.</summary>
	/// <param name="bundlesRoot">The file path to the Rust bundles file.</param>
	public static void Initialise(string bundlesRoot)
	{
		if (!Coroutines.IsInitialising && !IsInitialised)
			EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Initialise(bundlesRoot));
		if (IsInitialised)
			Debug.Log("Bundles already loaded.");
	}

	/// <summary>Loads the Rust bundles at the currently set directory.</summary>
	public static void Initialise() => Initialise(SettingsManager.RustDirectory + SettingsManager.BundlePathExt);

	public static void Dispose()
	{
		if (!Coroutines.IsInitialising && IsInitialised)
			EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.Dispose());
	}

	private static T GetAsset<T>(string filePath) where T : Object
	{
		if (!BundleLookup.TryGetValue(filePath, out AssetBundle bundle))
			return null;

		return bundle.LoadAsset<T>(filePath);
	}

    public static T LoadAsset<T>(string filePath) where T : Object
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

		else
		{
            GameObject val = GetAsset<GameObject>(filePath);
            if (val != null)
            {
				PrefabManager.Setup(val, filePath);
				AssetCache.Add(filePath, val);
				PrefabManager.Callbacks.OnPrefabLoaded(val);
				return val;
            }
            Debug.LogWarning("Prefab not loaded from bundle: " + filePath);
            return PrefabManager.DefaultPrefab;
        }
    }

	/// <summary>Returns a preview image of the asset located at the filepath. Caches the results.</summary>
	public static Texture2D GetPreview(string filePath)
    {
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
    }

	//i can't make this work
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
								 GameObject instantiatedTransCube = Object.Instantiate(transCube, VolumesCache[lineSplit[1]].transform);
                           
								
								
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
					Debug.LogError("mesh already exists");
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

	private static class Coroutines
    {
		public static bool IsInitialising { get; private set; }

		public static IEnumerator Initialise(string bundlesRoot)
		{
			IsInitialising = true;
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
			yield return EditorCoroutineUtility.StartCoroutineOwnerless(SetMaterials(materialID));

			IsInitialised = true; IsInitialising = false;
			SetVolumeGizmos();
			//SetVolumesCache();
			Callbacks.OnBundlesLoaded();
			PrefabManager.ReplaceWithLoaded(PrefabManager.CurrentMapPrefabs, prefabID);
		}

		public static IEnumerator Dispose() 
		{
			IsInitialising = true;
			ProgressManager.RemoveProgressBars("Unload Asset Bundles");

			int progressID = Progress.Start("Unload Asset Bundles", null, Progress.Options.Sticky);
			int bundleID = Progress.Start("Bundles", null, Progress.Options.Sticky, progressID);
			int prefabID = Progress.Start("Prefabs", null, Progress.Options.Sticky, progressID);
			Progress.Report(bundleID, 0f);
			Progress.Report(prefabID, 0f);
			PrefabManager.ReplaceWithDefault(PrefabManager.CurrentMapPrefabs, prefabID);

			while (PrefabManager.IsChangingPrefabs)
				yield return null;

			for (int i = 0; i < BundleCache.Count; i++)
            {
				Progress.Report(bundleID, (float)i / BundleCache.Count, "Unloading: " + BundleCache.ElementAt(i).Key);
				BundleCache.ElementAt(i).Value.Unload(true);
				yield return null;
            }
			
			int bundleCount = BundleCache.Count;
			BundleLookup.Clear();
			BundleCache.Clear();
			AssetCache.Clear();

			Progress.Report(bundleID, 0.99f, "Unloaded: " + bundleCount + " bundles.");
			Progress.Finish(bundleID, Progress.Status.Succeeded);
			IsInitialised = false; IsInitialising = false;
		}

		public static IEnumerator LoadBundles(string bundleRoot, (int progress, int bundle, int material) ID)
        {
			if (!Directory.Exists(SettingsManager.RustDirectory))
			{
				Debug.LogError("Directory does not exist: " + bundleRoot);
				IsInitialising = false;
				yield break;
			}

			if (!SettingsManager.RustDirectory.EndsWith("Rust") && !SettingsManager.RustDirectory.EndsWith("RustStaging"))
			{
				Debug.LogError("Not a valid Rust install directory: " + SettingsManager.RustDirectory);
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
				Progress.Report(ID.bundle, (float)i / bundles.Length, "Loading: " + bundles[i]);
				var bundlePath = Path.GetDirectoryName(bundleRoot) + Path.DirectorySeparatorChar + bundles[i];
				if (File.Exists(bundlePath)) 
				{
                    var asset = AssetBundle.LoadFromFileAsync(bundlePath);
                    while (!asset.isDone)
                        yield return null;

                    if (asset == null)
                    {
                        Debug.LogError("Couldn't load AssetBundle - " + bundlePath);
                        IsInitialising = false;
                        yield break;
                    }
                    BundleCache.Add(bundles[i], asset.assetBundle);
                }
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
			}

			Progress.Report(ID.bundle, 0.99f, "Loaded " + BundleCache.Count + " bundles.");
			Progress.Finish(ID.bundle, Progress.Status.Succeeded);

			Manifest = GetAsset<GameManifest>(ManifestPath);
			if (Manifest == null)
			{
				Debug.LogError("Couldn't load GameManifest.");
				Dispose();
				Progress.Finish(ID.parent, Progress.Status.Failed);
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
			if (File.Exists(MaterialsListPath))
			{
				Shader std = Shader.Find("Standard");
				Shader spc = Shader.Find("Standard (Specular setup)");
				string[] materials = File.ReadAllLines(MaterialsListPath);
				for (int i = 0; i < materials.Length; i++)
				{
					var lineSplit = materials[i].Split(':');
					lineSplit[0] = lineSplit[0].Trim(' '); // Shader Name
					lineSplit[1] = lineSplit[1].Trim(' '); // Material Path
					Progress.Report(materialID, (float)i / materials.Length, "Setting: " + lineSplit[1]);
					switch (lineSplit[0])
					{
						case "Standard":
							Material matStd = LoadAsset<Material>(lineSplit[1]);
							if (matStd == null)
                            {
								Debug.LogWarning(lineSplit[1] + " is not a valid asset.");
								break;
                            }
							EditorCoroutineUtility.StartCoroutineOwnerless(UpdateShader(matStd, std));
							break;

						case "Specular":
							Material matSpc= LoadAsset<Material>(lineSplit[1]);
							if (matSpc == null)
							{
								Debug.LogWarning(lineSplit[1] + " is not a valid asset.");
								break;
							}
							EditorCoroutineUtility.StartCoroutineOwnerless(UpdateShader(matSpc, spc));
							break;

						case "Foliage":
							Material mat = LoadAsset<Material>(lineSplit[1]);
							if(mat == null)
                            {
								Debug.LogWarning(lineSplit[1] + " is not a valid asset.");
								break;
							}
							mat.DisableKeyword("_TINTENABLED_ON");
							break;

						default:
							Debug.LogWarning(lineSplit[0] + " is not a valid shader.");
							break;
					}
					yield return null;
				}
				Progress.Report(materialID, 0.99f, "Set " + materials.Length + " materials.");
				Progress.Finish(materialID, Progress.Status.Succeeded);
			}
		}

		public static IEnumerator UpdateShader(Material mat, Shader shader)
		{
			mat.shader = shader;
			yield return null;
			switch (mat.GetFloat("_Mode"))
			{
				case 0f:
					mat.SetOverrideTag("RenderType", "");
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					mat.SetInt("_ZWrite", 1);
					mat.DisableKeyword("_ALPHATEST_ON");
					mat.DisableKeyword("_ALPHABLEND_ON");
					mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					SetKeyword(mat, "_NORMALMAP", mat.GetTexture("_BumpMap") || mat.GetTexture("_DetailNormalMap"));
					if (mat.HasProperty("_SPECGLOSSMAP"))
						SetKeyword(mat, "_SPECGLOSSMAP", mat.GetTexture("_SpecGlossMap"));
					mat.renderQueue = -1;
					break;

				case 1f:
					mat.SetOverrideTag("RenderType", "TransparentCutout");
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					mat.SetInt("_ZWrite", 1);
					mat.EnableKeyword("_ALPHATEST_ON");
					mat.DisableKeyword("_ALPHABLEND_ON");
					mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					mat.EnableKeyword("_NORMALMAP");
					mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
					break;

				case 2f:
					mat.SetOverrideTag("RenderType", "Transparent");
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					mat.SetInt("_ZWrite", 0);
					mat.DisableKeyword("_ALPHATEST_ON");
					mat.EnableKeyword("_ALPHABLEND_ON");
					mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					mat.EnableKeyword("_NORMALMAP");
					mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
					break;

				case 3f:
					mat.SetOverrideTag("RenderType", "Transparent");
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					mat.SetInt("_ZWrite", 0);
					mat.DisableKeyword("_ALPHATEST_ON");
					mat.DisableKeyword("_ALPHABLEND_ON");
					mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					mat.EnableKeyword("_NORMALMAP");
					mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
					break;
			}
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