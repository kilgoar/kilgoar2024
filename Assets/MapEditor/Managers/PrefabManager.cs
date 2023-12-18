using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static WorldSerialization;
using System.IO;
using System;
using RustMapEditor.Variables;

public static class PrefabManager
{
    #region Init
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.update += OnProjectLoad;
    }

    private static void OnProjectLoad()
    {
        DefaultPrefab = Resources.Load<GameObject>("Prefabs/DefaultPrefab");
		
		ElectricsParent = GameObject.FindGameObjectWithTag("Electrics").transform;
        NPCsParent = GameObject.FindGameObjectWithTag("NPC").transform;
		PrefabParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		ModifiersParent = GameObject.FindGameObjectWithTag("Modifiers").transform;
        if (DefaultPrefab != null && PrefabParent != null)
        {
            EditorApplication.update -= OnProjectLoad;
            if (!AssetManager.IsInitialised && SettingsManager.LoadBundleOnLaunch)
                AssetManager.Initialise(SettingsManager.RustDirectory + SettingsManager.BundlePathExt);
        }
    }
    #endregion

    public static class Callbacks
    {
        public delegate void PrefabManagerCallback(GameObject prefab);

        /// <summary>Called after prefab is loaded and setup from bundle. </summary>
        public static event PrefabManagerCallback PrefabLoaded;
        /// <summary>Called after prefab category is renamed.</summary>
        public static event PrefabManagerCallback PrefabCategoryChanged;
        /// <summary>Called after prefab ID is changed.</summary>
        public static event PrefabManagerCallback PrefabIDChanged;

        public static void OnPrefabLoaded(GameObject prefab) => PrefabLoaded?.Invoke(prefab);
        public static void OnPrefabCategoryChanged(GameObject prefab) => PrefabCategoryChanged?.Invoke(prefab);
        public static void OnPrefabIDChanged(GameObject prefab) => PrefabIDChanged?.Invoke(prefab);
    }

    public static GameObject DefaultPrefab { get; private set; }
	
    public static Transform PrefabParent { get; private set; }
    public static GameObject PrefabToSpawn;

	public static Transform CustomPrefabParent { get; private set; }
	public static string monumentName = "";
	public static Transform ElectricsParent { get; private set; }
	public static Transform ModifiersParent { get; private set; }
	public static Transform NPCsParent { get; private set; }
   

   	public static CircuitDataHolder[] CurrentMapElectrics { get => ElectricsParent.gameObject.GetComponentsInChildren<CircuitDataHolder>(); }
	public static NPCDataHolder[] CurrentMapNPCs { get => NPCsParent.gameObject.GetComponentsInChildren<NPCDataHolder>(); }
	public static ModifierDataHolder CurrentModifiers { get => ModifiersParent.gameObject.GetComponentInChildren<ModifierDataHolder>(); }
	
	
    /// <summary>List of prefab names from the asset bundle.</summary>
    private static List<string> Prefabs;

    /// <summary>Prefabs currently spawned on the map.</summary>
    public static PrefabDataHolder[] CurrentMapPrefabs { get => PrefabParent.gameObject.GetComponentsInChildren<PrefabDataHolder>(); }

    public static Dictionary<string, Transform> PrefabCategories = new Dictionary<string, Transform>();

    public static bool IsChangingPrefabs { get; private set; }

    /// <summary>Loads, sets up and returns the prefab at the asset path.</summary>
    /// <param name="path">The prefab path in the bundle file.</param>
    public static GameObject Load(string path)
    {
        if (AssetManager.IsInitialised)
            return AssetManager.LoadPrefab(path);
        return DefaultPrefab;
    }

    /// <summary>Loads, sets up and returns the prefab at the prefab id.</summary>
    /// <param name="id">The prefab manifest id.</param>
    public static GameObject Load(uint id) => Load(AssetManager.ToPath(id));

    /// <summary>Searches through all prefabs found in bundle files, returning matches.</summary>
    /// <returns>List of strings containing the path matching the <paramref name="key"/>.</returns>
    public static List<string> Search(string key)
    {
        if (Prefabs == null)
        {
            Prefabs = new List<string>();
            foreach (var i in AssetManager.AssetPaths)
                if (i.EndsWith(".prefab"))
                    Prefabs.Add(i);

            Prefabs.OrderBy(x => x);
        }

        return Prefabs.Where(x => x.Contains(key)).ToList();
    }

    /// <summary>Gets the parent prefab category transform from the hierachy.</summary>
    public static Transform GetParent(string category)
    {
        if (PrefabCategories.TryGetValue(category, out Transform transform))
            return transform;

        var obj = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/PrefabCategory"), PrefabParent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.name = category;
        PrefabCategories.Add(category, obj.transform);
        return obj.transform;
    }

    /// <summary>Sets up the prefabs loaded from the bundle file for use in the editor.</summary>
    /// <param name="go">GameObject to process, should be from one of the asset bundles.</param>
    /// <param name="filePath">Asset filepath of the gameobject, used to get and set the PrefabID.</param>
    public static GameObject Setup(GameObject go, string filePath)
    {
        go.SetLayerRecursively(8);
        go.SetTagRecursively("Untagged");
        go.SetStaticRecursively(true);
        go.RemoveNameUnderscore();

        foreach (var item in go.GetComponentsInChildren<MeshCollider>())
        {
            item.cookingOptions = MeshColliderCookingOptions.None;
            item.enabled = true;
            item.isTrigger = false;
            item.convex = true;
        }
        foreach (var item in go.GetComponentsInChildren<Animator>())
        {
            item.enabled = false;
            item.runtimeAnimatorController = null;
        }
        foreach (var item in go.GetComponentsInChildren<Light>())
            item.enabled = false;
        foreach (var item in go.GetComponentsInChildren<Canvas>())
            item.enabled = false;
        foreach (var item in go.GetComponentsInChildren<CanvasGroup>())
            item.enabled = false;
        foreach (var item in go.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = item.emission;
            emission.enabled = false;
        }

        PrefabDataHolder prefabDataHolder = go.AddComponent<PrefabDataHolder>();
        prefabDataHolder.prefabData = new PrefabData() { id = AssetManager.ToID(filePath) };

        go.SetActive(false);
        return go;
    }

    /// <summary>Spawns a prefab, updates the PrefabData and parents to the selected transform.</summary>
    public static void Spawn(GameObject go, PrefabData prefabData, Transform parent)
    {
        GameObject newObj = GameObject.Instantiate(go, parent);
        newObj.transform.localPosition = new Vector3(prefabData.position.x, prefabData.position.y, prefabData.position.z);
        newObj.transform.rotation = Quaternion.Euler(new Vector3(prefabData.rotation.x, prefabData.rotation.y, prefabData.rotation.z));
        newObj.transform.localScale = new Vector3(prefabData.scale.x, prefabData.scale.y, prefabData.scale.z);
        newObj.name = go.name;
        newObj.GetComponent<PrefabDataHolder>().prefabData = prefabData;
        newObj.SetActive(true);
    }
	
	public static void SpawnCustoms(GameObject go, PrefabData prefabData, Transform parent)
	{
		GameObject newObj = GameObject.Instantiate(go, parent);
		newObj.transform.localPosition = new Vector3(prefabData.position.x, prefabData.position.y, prefabData.position.z);

		// Set global rotation
		newObj.transform.rotation = Quaternion.Euler(new Vector3(prefabData.rotation.x, prefabData.rotation.y, prefabData.rotation.z));
		// Set rotation relative to parent
		newObj.transform.localRotation = Quaternion.Euler(new Vector3(prefabData.rotation.x, prefabData.rotation.y, prefabData.rotation.z));

		newObj.transform.localScale = new Vector3(prefabData.scale.x, prefabData.scale.y, prefabData.scale.z);
		newObj.name = go.name;
		newObj.GetComponent<PrefabDataHolder>().prefabData = prefabData;
		newObj.SetActive(true);
	}
	

    /// <summary>Spawns a prefab and parents to the selected transform.</summary>
    public static void Spawn(GameObject go, Transform transform, string name)
    {
        GameObject newObj = GameObject.Instantiate(go, PrefabParent);
        newObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
        newObj.transform.localScale = transform.localScale;
        newObj.name = name;
        newObj.SetActive(true);
    }
	
	//spawn default prefabs for circuitry
	
	public static void Spawn(GameObject go, CircuitData circuitData, Transform parent)
	{
		
		GameObject newObj = GameObject.Instantiate(go, parent);
        newObj.transform.localPosition = new Vector3(circuitData.wiring.x, circuitData.wiring.y, circuitData.wiring.z);
        newObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        string[] path = circuitData.path.Split('/');
		newObj.name = path[path.Length-1];
		
		CircuitDataHolder electrics = newObj.AddComponent<CircuitDataHolder>();
		electrics.circuitData = circuitData;
        newObj.SetActive(true);
		
	}
	
	public static void Spawn(GameObject go, ModifierData modifiers, Transform parent)
	{
		
		GameObject newObj = GameObject.Instantiate(go, parent);
		ModifierDataHolder mod = newObj.AddComponent<ModifierDataHolder>();
		mod.modifierData = modifiers;
        newObj.SetActive(true);
		Debug.LogError("modifier data found");
	}
	
	public static void Spawn(GameObject go, NPCData bots, Transform parent)
	{
		
		GameObject newObj = GameObject.Instantiate(go, parent);
        newObj.transform.localPosition = new Vector3(bots.scientist.x, bots.scientist.y, bots.scientist.z);
		
		NPCDataHolder npcs = newObj.AddComponent<NPCDataHolder>();
		npcs.bots = bots;
		newObj.GetComponent<NPCDataHolder>().bots = bots;
        newObj.SetActive(true);
		
	}

    /// <summary>Spawns the prefab set in PrefabToSpawn at the spawnPos</summary>
    public static void SpawnPrefab(Vector3 spawnPos)
    {
        if (PrefabToSpawn != null)
        {
            GameObject newObj = GameObject.Instantiate(PrefabToSpawn, spawnPos, Quaternion.Euler(0, 0, 0));
            newObj.name = PrefabToSpawn.name;
            newObj.SetActive(true);
            PrefabToSpawn = null;
        }
    }

	public static GameObject SpawnPrefab(GameObject g, PrefabData prefabData, Transform parent = null)
    {

        GameObject newObj = GameObject.Instantiate(g);
        newObj.transform.parent = parent;
        newObj.transform.position = new Vector3(prefabData.position.x, prefabData.position.y, prefabData.position.z) + TerrainManager.GetMapOffset();
        newObj.transform.rotation = Quaternion.Euler(new Vector3(prefabData.rotation.x, prefabData.rotation.y, prefabData.rotation.z));
        newObj.transform.localScale = new Vector3(prefabData.scale.x, prefabData.scale.y, prefabData.scale.z);
        newObj.GetComponent<PrefabDataHolder>().prefabData = prefabData;
        return newObj;

    }

	
    /// <summary>Spawns prefabs for map load.</summary>
    public static void SpawnPrefabs(PrefabData[] prefabs, int progressID)
    {
        if (!IsChangingPrefabs)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnPrefabs(prefabs, progressID));
    }
	public static void SpawnPrefabs(PrefabData[] prefabs, int progressID, Transform parent)
    {
        if (!IsChangingPrefabs)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnPrefabs(prefabs, progressID, parent));
    }
    public static void SpawnCircuits(CircuitData[] circuits, int progressID)
    {
        if (!IsChangingPrefabs)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnCircuits(circuits, progressID));
    }
	public static void SpawnModifiers(ModifierData modifiers)
	{
		Spawn(DefaultPrefab, modifiers, ModifiersParent);
	}
	public static void SpawnNPCs(NPCData[] bots, int progressID)
    {
        if (!IsChangingPrefabs)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnNPCs(bots, progressID));
    }

    /// <summary>Deletes prefabs from scene.</summary>
    public static IEnumerator DeletePrefabs(PrefabDataHolder[] prefabs, int progressID = 0)
    {
        if (!IsChangingPrefabs)
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.DeletePrefabs(prefabs, progressID));
    }
	
	public static void DeleteCircuits(CircuitDataHolder[] circuits, int progressID = 0)
    {
        if (!IsChangingPrefabs)
           EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.DeleteCircuits(circuits, progressID));
    }
	
	public static void DeleteModifiers(ModifierDataHolder modifiers)
    {
		if (modifiers != null)
		{
			GameObject.DestroyImmediate(modifiers.gameObject);
		}
    }
	
	public static void DeleteNPCs(NPCDataHolder[] npcs, int progressID = 0)
    {
        if (!IsChangingPrefabs)
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.DeleteNPCs(npcs, progressID));
    }

    /// <summary>Replaces the selected prefabs with ones from the Rust bundles.</summary>
    public static void ReplaceWithLoaded(PrefabDataHolder[] prefabs, int progressID)
    {
        if (AssetManager.IsInitialised && !IsChangingPrefabs)
        {
            IsChangingPrefabs = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.ReplaceWithLoaded(prefabs, progressID));
        }
    }

    /// <summary>Replaces the selected prefabs with the default prefabs.</summary>
    public static void ReplaceWithDefault(PrefabDataHolder[] prefabs, int progressID)
    {
        if (!IsChangingPrefabs)
        {
            IsChangingPrefabs = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.ReplaceWithDefault(prefabs, progressID));
        }
    }

    public static void RenamePrefabCategories(PrefabDataHolder[] prefabs, string name)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.RenamePrefabCategories(prefabs, name));
    }
	
	public static void RenameNPCs(NPCDataHolder[] bots, string name)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.RenameNPCs(bots, name));
    }

    public static void RenamePrefabIDs(PrefabDataHolder[] prefabs, uint id, bool replace)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.RenamePrefabIDs(prefabs, id, replace));
    }

    /// <summary>Rotates all prefabs in map 90° Clockwise or Counter Clockwise.</summary>
    /// <param name="CW">True = 90°, False = 270°</param>
    public static void RotatePrefabs(bool CW)
    {
        PrefabParent.Rotate(0, 90, 0, Space.World);
        PrefabParent.gameObject.GetComponent<LockObject>().UpdateTransform();
    }

	public static void placeCustomPrefab(string loadPath,Vector3 position, Vector3 rotation, Vector3 scale, Transform parent = null)
	{
			var world = new WorldSerialization();
            world.LoadREPrefab(loadPath);
			
			GameObject newObj = new GameObject(loadPath);
			
			string baseName = loadPath.Split('\\').Last().Split('.')[0];
			string newObjName = baseName + "" + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10);

			while(PrefabManager.PrefabCategories.ContainsKey(newObjName)) 
			{
				newObjName = baseName + " " + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10);
			}

			PrefabManager.PrefabCategories.Add(newObjName, newObj.transform);
			newObj.name = newObjName;
		
			
			newObj.transform.parent = parent;
			newObj.transform.localPosition = position;
			newObj.transform.localRotation = Quaternion.Euler(rotation);
			newObj.transform.localScale = scale;
			
			
			MapManager.MergeOffsetREPrefab(WorldConverter.WorldToREPrefab(world), newObj.transform, loadPath);
	}
	
	public static Colliders ItemToColliders(Transform item)
	{
		Colliders colliderScales = new Colliders();
	
		try
		{
		if (item.TryGetComponent(typeof(SphereCollider), out Component comp))
				{
					SphereCollider collider = item.GetComponent(typeof(SphereCollider)) as SphereCollider;
					colliderScales.sphere.x = collider.radius*2f;
					colliderScales.sphere.y = collider.radius*2f;
					colliderScales.sphere.z = collider.radius*2f;
				}
				
		if (item.TryGetComponent(typeof(BoxCollider), out Component compt))
				{
					BoxCollider collider = item.GetComponent(typeof(BoxCollider)) as BoxCollider;
					colliderScales.box.x = collider.size.x;
					colliderScales.box.y = collider.size.y;
					colliderScales.box.z = collider.size.z;
				}
				
		if (item.TryGetComponent(typeof(CapsuleCollider), out Component compy))
				{
					CapsuleCollider collider = item.GetComponent(typeof(CapsuleCollider)) as CapsuleCollider;
					colliderScales.capsule.x = collider.radius*2f;
					colliderScales.capsule.z = collider.radius*2f;
					colliderScales.capsule.y = collider.height;
				}
		
		}
		catch (Exception e)
		{
			Debug.LogError(e.Message);
		}
		return colliderScales;
	}
	
	public static Texture2D GetIcon(BreakingData breakingData, IconTextures icons)
	{
		//stop scrap tarp gears trash
		if (breakingData.ignore)
		{
			return icons.trash;
		}
		else if (breakingData.prefabData.id == 0)
		{
			return icons.stop;
		}
		else if((breakingData.colliderScales.box != Vector3.zero) || (breakingData.colliderScales.sphere != Vector3.zero) || (breakingData.colliderScales.capsule != Vector3.zero))
		{
			return icons.tarp;
		}
		else
		{
			return icons.gears;
		}
	}
	
	
	public static PrefabData ItemToPrefab(Transform item, string parentName, string monumentName)
	{
		var prefab = new PrefabData();
		Vector3 scale = new Vector3();
		scale = item.lossyScale;
		
		prefab.category = "";
		
		prefab.position = item.position;
		prefab.rotation = item.eulerAngles;
		prefab.id = AssetManager.fragmentToID(item.name, parentName, monumentName);
		
		prefab.scale = scale;
		
		return prefab;
	}
	
	public static void blacklistedCreatePrefab(string category, uint id, Vector3 adjuster, Transform spawnItem)
    {
		/*
		if(id !=0 && id != 1677700966 && id != 2408482215 && id != 1071933290 && id != 4114395726 && id != 4179136873 && id != 3782603339 && id != 3072760270 && id != 3468856354 && id != 4022753621
		&& id != 3415994284 && id != 447739874 && id != 2316560245 && id != 3052882219 && id != 2962527842 && id != 1872469913 && id != 2364515962 && id != 1369298112 && id != 3450258766 && id != 2483607632)
		{
			if (id == 3286143640)
					id = 3713697082;
		*/	
		
		if (id == 859761354) //system
			return;
		
		if (id == 817541646)
		{
			spawnItem.position -= adjuster;		
			//placeCustomPrefab("fenceReplacer.prefab", spawnItem);
			return;
		}
		
			string[] parse;
			parse = spawnItem.name.Split(' ');
			string itemName = parse[0];
			
			


				//pass over duplicate bushes etc
					if (spawnItem.name.Contains("temperate") || spawnItem.name.Contains("tundra"))
					{
						return;
					}
					else if (spawnItem.name.Contains("snow") || spawnItem.name.Contains("arid") )
					{
							if (spawnItem.name.Contains("ball") || spawnItem.name.Contains("mound")
								|| spawnItem.name.Contains("shipping") || spawnItem.name.Contains("rock")
								|| spawnItem.name.Contains("drum") || spawnItem.name.Contains("crate")
								|| spawnItem.name.Contains("boxes") || spawnItem.name.Contains("Cliff"))
								{
									//do not remove  snowballs or snowmounds of ice lakes etc
									
								}
							else
								{
									return;
								}
					}



			
		Vector3 rotation = spawnItem.eulerAngles;
		Vector3 position = spawnItem.position - adjuster;
		Vector3 scale=spawnItem.lossyScale;
			//ladder fixes
			if(id == 3381066643)
			{
				BoxCollider collider = spawnItem.GetComponent(typeof(BoxCollider)) as BoxCollider;
				scale = collider.size;
			if (monumentName == "airfield 1" || monumentName =="trainyard 1" || monumentName == "water treatment plant 1")
				{
					position.y += scale.y /2f;
				}
				else if(monumentName == "military tunnel 1")
				{
					position.y += scale.y *.4f;
				}
			}
		if (id == 3224970585 || id == 316558065 || id == 4190049974  || id == 3521578167 || id == 3381066643 || id == 3000049339 || id == 602278094 || id == 858853278)
		{
			if ((monumentName == "trainyard 1" || monumentName == "sphere tank" || monumentName == "Oilrig 1" ||
			monumentName == "Oilrig 2") && id != 4190049974 && id !=3521578167)
			{
				//scale is correct
			}
			else
			{
				
				if (spawnItem.TryGetComponent(typeof(SphereCollider), out Component comp))
				{
					SphereCollider collider = spawnItem.GetComponent(typeof(SphereCollider)) as SphereCollider;
					scale.x = collider.radius*2f;
					scale.y = collider.radius*2f;
					scale.z = collider.radius*2f;
					
					if (id == 4190049974){ id = 3224970585;}
					
				}
				
				else if (spawnItem.TryGetComponent(typeof(BoxCollider), out Component compt))
				{
					BoxCollider collider = spawnItem.GetComponent(typeof(BoxCollider)) as BoxCollider;
					//position += collider.center;
					scale = collider.size;
					if(id ==3224970585){ id = 4190049974;}
				}
				
				else if (spawnItem.TryGetComponent(typeof(CapsuleCollider), out Component compy))
				{
					CapsuleCollider collider = spawnItem.GetComponent(typeof(CapsuleCollider)) as CapsuleCollider;
					scale.x = collider.radius*2f;
					scale.z = collider.radius*2f;
					scale.y = collider.height;
					if(id ==3224970585){ id = 4190049974;}
				}
			}
			
			if (scale.x == 1f && scale.y == 1f && scale.z == 1f)
			{
				scale = spawnItem.lossyScale;
			}
		}
		else if (id == 3028561942 || id == 2493569314 || id == 3279304307)
		{
			rotation.y -= 90f;
		}
		else if (spawnItem.name.Contains("hinged"))
		{
			scale.x = 0.01f;
		}
		else if (id == 0)
		{
			return;
		}
		
		Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		GameObject defaultObj = Load(id);
		PrefabData newPrefab = new PrefabData();
		defaultObj.SetActive(true);
		var prefab = new PrefabData();

		prefab.category = category;
		prefab.id = id;
		prefab.position = position;
		prefab.rotation = rotation;
		prefab.scale = scale;
		
		SpawnPrefab(defaultObj, prefab, prefabsParent);
		/*}*/
    }
	//1537983469
	
	public static void placeCube(Vector3 position, Vector3 scale, float scaleDown)
	{
			float offset = scale.x / 2f;
			Vector3 newPosition = new Vector3(position.x, position.y, position.z);
			
			createPrefab("cubeVillage", 1537983469, position, new Vector3(0,0,0), scale);
			scale = scale / scaleDown;
			
			if (scale.x < .25f)
			{
				return;
			}
			
			newPosition = new Vector3(position.x + offset, position.y + offset, position.z + offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x + offset, position.y + offset , position.z - offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x + offset, position.y - offset, position.z - offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x - offset, position.y - offset, position.z - offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x - offset, position.y + offset, position.z - offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x - offset, position.y - offset, position.z + offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x - offset, position.y + offset, position.z + offset);
			placeCube(newPosition, scale, scaleDown);
			newPosition = new Vector3(position.x + offset, position.y - offset, position.z + offset);
			placeCube(newPosition, scale, scaleDown);
			
	}
	
	public static PrefabData prefab(string category, uint id, Vector3 position, Vector3 rotation, Vector3 scale)
	{
		
		GameObject defaultObj = Load(id);
		PrefabData newPrefab = new PrefabData();
		defaultObj.SetActive(true);
		var prefab = new PrefabData();

		prefab.category = category;
		prefab.id = id;
		prefab.position = position;
		prefab.rotation = rotation;
		prefab.scale = scale;
		return prefab;
	}
	
	public static void createPrefab(string category, uint id, Vector3 position, Vector3 rotation, Vector3 scale)
    {
		Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		GameObject defaultObj = Load(id);
		PrefabData newPrefab = new PrefabData();
		defaultObj.SetActive(true);
		var prefab = new PrefabData();

		prefab.category = category;
		prefab.id = id;
		prefab.position = position;
		prefab.rotation = rotation;
		prefab.scale = scale;
		SpawnPrefab(defaultObj, prefab, prefabsParent);
    }
	
	public static void createPrefab(string category, uint id, Transform transItem)
    {
		Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		GameObject defaultObj = Load(id);
		PrefabData newPrefab = new PrefabData();
		defaultObj.SetActive(true);
		var prefab = new PrefabData();

		prefab.category = category;
		prefab.id = id;
		prefab.position = prefabPosition(transItem.position);
		
		prefab.rotation = transItem.eulerAngles;
		prefab.scale = transItem.lossyScale;
		SpawnPrefab(defaultObj, prefab, prefabsParent);
    }
	
	public static void createPrefab(string category, uint id, Transform transItem, Vector3 position, Vector3 rotation)
    {
		Transform prefabsParent = GameObject.FindGameObjectWithTag("Prefabs").transform;
		GameObject defaultObj = Load(id);
		PrefabData newPrefab = new PrefabData();
		defaultObj.SetActive(true);
		var prefab = new PrefabData();

		prefab.category = category;
		prefab.id = id;
		prefab.position = position;
		
		prefab.rotation = rotation;
		prefab.scale = transItem.lossyScale;
		SpawnPrefab(defaultObj, prefab, prefabsParent);
    }
	

	public static void Offset(PrefabDataHolder[] prefabs, CircuitDataHolder[] circuits, Vector3 offset)
	{
		for (int k = 0; k < prefabs.Length; k++)
		{
			prefabs[k].prefabData.position += offset;
			prefabs[k].CastPrefabData();
		}
		for (int k = 0; k < circuits.Length; k++)
		{
			
			circuits[k].circuitData.wiring += offset;
						
			for(int l = 0; l < circuits[k].circuitData.connectionsIn.Length; l++)
			{
				circuits[k].circuitData.connectionsIn[l].wiring += offset;	
			}
			
			for(int l = 0; l < circuits[k].circuitData.connectionsOut.Length; l++)
			{
				circuits[k].circuitData.connectionsOut[l].wiring += offset;
			}
			
			circuits[k].CastCircuitData();
			
		}
		
	}

	//i wrote this ugliness before knowing about protobuf
	public static void BatchReplace(PrefabDataHolder[] prefabs, ReplacerPreset replace)
	{
		bool flag = false;
		Terrain land = GameObject.FindGameObjectWithTag("Land").GetComponent<Terrain>();
		int res = land.terrainData.heightmapResolution;
		float ratio = 1f* TerrainManager.TerrainSize.x / res;
		Quaternion qRotate;
		Vector3 preRotate;
		Vector3 rRotate = new Vector3(0,0,0);
		Vector3 normal = new Vector3(0,0,0);
		Vector3 position = new Vector3(0,0,0);
		Vector3 scale = new Vector3(0,0,0);
		
		float xCheck =0f;
		float yCheck =0f;
		int count = 0;
		int count1= 0;
		for (int k = 0; k < prefabs.Length; k++)
		{
			flag = false;
			
			if (prefabs[k] != null)
			{
				if(prefabs[k].prefabData.id == replace.prefabID0)
				{
					prefabs[k].prefabData.id = replace.replaceID0;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID1)
				{
					prefabs[k].prefabData.id = replace.replaceID1;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID2)
				{
					prefabs[k].prefabData.id = replace.replaceID2;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID3)
				{
					prefabs[k].prefabData.id = replace.replaceID3;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID4)
				{
					prefabs[k].prefabData.id = replace.replaceID4;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID5)
				{
					prefabs[k].prefabData.id = replace.replaceID5;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID6)
				{
					prefabs[k].prefabData.id = replace.replaceID6;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID7)
				{
					prefabs[k].prefabData.id = replace.replaceID7;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID8)
				{
					prefabs[k].prefabData.id = replace.replaceID8;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID9)
				{
					prefabs[k].prefabData.id = replace.replaceID9;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID10)
				{
					prefabs[k].prefabData.id = replace.replaceID10;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID11)
				{
					prefabs[k].prefabData.id = replace.replaceID11;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID12)
				{
					prefabs[k].prefabData.id = replace.replaceID12;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID13)
				{
					prefabs[k].prefabData.id = replace.replaceID13;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID14)
				{
					prefabs[k].prefabData.id = replace.replaceID14;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID15)
				{
					prefabs[k].prefabData.id = replace.replaceID15;
					flag = true;
				}
				else if(prefabs[k].prefabData.id == replace.prefabID16)
				{
					prefabs[k].prefabData.id = replace.replaceID16;
					flag = true;
				}
				

				
				if(flag && replace.rotateToTerrain)
				{
					position.x = prefabs[k].prefabData.position.x;
					position.y = prefabs[k].prefabData.position.y;
					position.z = prefabs[k].prefabData.position.z;
					scale.x = prefabs[k].prefabData.scale.x;
					scale.y = prefabs[k].prefabData.scale.y;
					scale.z = prefabs[k].prefabData.scale.z;
					count++;
					xCheck = ((prefabs[k].prefabData.position.z/ratio)+res/2f);
					yCheck = ((prefabs[k].prefabData.position.x/ratio)+res/2f);
					normal = land.terrainData.GetInterpolatedNormal(1f*yCheck/res, 1f*xCheck/res);
					qRotate = Quaternion.LookRotation(normal);
					preRotate = qRotate.eulerAngles;
					
					if(replace.rotateToY)
					{
						rRotate.y = preRotate.y;
					}
					
					if(replace.rotateToX)
					{
						rRotate.x = preRotate.x+90f;
					}
					
					if(replace.rotateToZ)
					{
						rRotate.z = preRotate.z;
					}
					
					if(replace.scale)
					{
						scale = Vector3.Scale(prefabs[k].prefabData.scale, replace.scaling);
					}

					createPrefab("Decor", prefabs[k].prefabData.id, position, rRotate, scale);
					
					GameObject.DestroyImmediate(prefabs[k].gameObject);
								prefabs[k] = null;

								
				}
				else if(flag && !replace.rotateToTerrain)
				{
					position.x = prefabs[k].prefabData.position.x;
					position.y = prefabs[k].prefabData.position.y;
					position.z = prefabs[k].prefabData.position.z;
					scale.x = prefabs[k].prefabData.scale.x;
					scale.y = prefabs[k].prefabData.scale.y;
					scale.z = prefabs[k].prefabData.scale.z;
					if(replace.scale)
					{
						scale = Vector3.Scale(prefabs[k].prefabData.scale, replace.scaling);
					}
					
					createPrefab("Decor", prefabs[k].prefabData.id, position, prefabs[k].prefabData.rotation, scale);
					

					GameObject.DestroyImmediate(prefabs[k].gameObject);
								prefabs[k] = null;
								count1++;

				}
			}
		}
		Debug.LogError(count1 + count + " prefabs replaced." + count + " prefabs rotated to terrain. " );
	}

	public static void deleteDuplicates(PrefabDataHolder[] prefabs)
	{
		int count = 0;
		Vector3 variance = new Vector3(.005f, .005f, .005f);
		for (int k = 0; k < prefabs.Length; k++)
		{
			for (int l = 0; l <prefabs.Length; l++)
			{
				if (prefabs[k]!=null && prefabs[l]!=null)
				{
					if (prefabs[k].prefabData.id == prefabs[l].prefabData.id 
					&& Mathf.Abs(prefabs[k].prefabData.position.x - prefabs[l].prefabData.position.x) < variance.x 
					&& Mathf.Abs(prefabs[k].prefabData.position.y - prefabs[l].prefabData.position.y) < variance.y
					&& Mathf.Abs(prefabs[k].prefabData.position.z - prefabs[l].prefabData.position.z) < variance.z	
					
					&& Mathf.Abs(prefabs[k].prefabData.rotation.x - prefabs[l].prefabData.rotation.x) < variance.x 
					&& Mathf.Abs(prefabs[k].prefabData.rotation.y - prefabs[l].prefabData.rotation.y) < variance.y
					&& Mathf.Abs(prefabs[k].prefabData.rotation.z - prefabs[l].prefabData.rotation.z) < variance.z	
					
					&& Mathf.Abs(prefabs[k].prefabData.scale.x - prefabs[l].prefabData.scale.x) < variance.x 
					&& Mathf.Abs(prefabs[k].prefabData.scale.y - prefabs[l].prefabData.scale.y) < variance.y
					&& Mathf.Abs(prefabs[k].prefabData.scale.z - prefabs[l].prefabData.scale.z) < variance.z	)
					{
						if (k!=l)
						{
							Debug.LogError(prefabs[k].prefabData.id + " x:" + prefabs[k].prefabData.position.x +
							" y:" + prefabs[k].prefabData.position.y +
							" z:" + prefabs[k].prefabData.position.z);
							GameObject.DestroyImmediate(prefabs[k].gameObject);
							prefabs[k] = null;
							count ++;
						}
						
					}
				}
			}
		}
		Debug.LogError(count + " prefabs deleted.");
	}

	public static void addSpawners()
	{
		uint id = 0;
		Vector3 rotation = new Vector3(0f, 0f, 0f);
		Vector3 position = new Vector3(750f, 1f, 750f);
		Vector3 scale = new Vector3(1f, 1f, 1f);
		createPrefab("Decor", id,	position, rotation, scale);
		position = new Vector3(750f, 1f, 0f);
		createPrefab("Decor", id,	position, rotation, scale);
		position = new Vector3(0f, 1f, 750f);
		createPrefab("Decor", id,	position, rotation, scale);
		
	}

	public static void keepPrefabList(PrefabDataHolder[] prefabs, uint[] keepersList)
	{
		
		bool keeper;
		for (int k = 0; k < prefabs.Length; k++)
		{
			
			keeper = false;
			
			if (prefabs[k] != null)
			{
				for (int l =0; l < keepersList.Length; l++)
				{
					
					if( prefabs[k].prefabData.id == keepersList[l])
					{
						keeper = true;
					}
					
				}
				if (!keeper)
					{
						GameObject.DestroyImmediate(prefabs[k].gameObject);
						prefabs[k] = null;
					}						
			}
		}
		
	}

	public static void keepElectrics(PrefabDataHolder[] prefabs, CircuitDataHolder[] circuits)
	{
		uint[] electricityList = new uint[]{1331920001,3467084113,1523703314,4129440825,500822506,1174518703,
			1479592929, 2864014888,1841596500,3165678508,3622071578,
			2179325520, 4224395968,1802909967,2055550712,850739563,2873681431,1268553078, 
			3767520300, 34236153, 3192000101,4124892809};
		bool electricPrefab;
		for (int k = 0; k < prefabs.Length; k++)
		{
			
			electricPrefab = false;
			
			if (prefabs[k] != null)
			{
				for (int l =0; l < electricityList.Length; l++)
				{
					
					if( prefabs[k].prefabData.id == electricityList[l])
					{
						electricPrefab = true;
					}
					
				}
				if (!electricPrefab)
					{
						GameObject.DestroyImmediate(prefabs[k].gameObject);
						prefabs[k] = null;
					}						
			}
		}
		
	}

	public static void removeElectrics(PrefabDataHolder[] prefabs, CircuitDataHolder[] circuits)
	{
		uint[] electricityList = new uint[]{1331920001,3467084113,1523703314,4129440825,500822506,1174518703,
			1479592929, 2864014888,1841596500,3165678508,3622071578,
			2179325520, 4224395968,1802909967,2055550712,850739563,2873681431,1268553078, 
			3767520300, 34236153, 3192000101,4124892809};
		bool electricPrefab;
		for (int k = 0; k < prefabs.Length; k++)
		{
			
			electricPrefab = false;
			
			if (prefabs[k] != null)
			{
				for (int l =0; l < electricityList.Length; l++)
				{
					
					if( prefabs[k].prefabData.id == electricityList[l])
						electricPrefab = true;					
					
				}
				if (electricPrefab)
					{
						GameObject.DestroyImmediate(prefabs[k].gameObject);
						prefabs[k] = null;
					}						
			}
		}
		
		for (int k = 0; k < circuits.Length; k++)
		{
			
			if (circuits[k] != null)
			{
				GameObject.DestroyImmediate(circuits[k].gameObject);
				circuits[k] = null;
			}
			
		}
		
	}

	public static bool isLOD(string name)
	{
		return (name.Contains("LOD") || (name.Contains("shadow_proxy")));
	}

	public static MonumentData deLOD(MonumentData monument)
	{
		MonumentData fragments = new MonumentData();


	
					for (int i = 0; i < monument.category.Count; i++)
						{
							if (!isLOD(monument.category[i].breakingData.name))
								fragments.category.Add(monument.category[i]);
							
							for (int j = 0; j < monument.category[i].child.Count; j++)
							{
								if (!isLOD(monument.category[i].child[j].breakingData.name))
									fragments.category[i].child.Add(monument.category[i].child[j]);
								
									for (int m = 0; m < monument.category[i].child[j].grandchild.Count; m++)
									{
										if (!isLOD(monument.category[i].child[j].grandchild[m].breakingData.name))
											fragments.category[i].child[j].grandchild.Add(monument.category[i].child[j].grandchild[m]);									
										
										for (int n = 0; n < monument.category[i].child[j].grandchild[m].greatgrandchild.Count; n++)
											{
												if (!isLOD(monument.category[i].child[j].grandchild[m].greatgrandchild[n].breakingData.name))
													fragments.category[i].child[j].grandchild[m].greatgrandchild.Add(monument.category[i].child[j].grandchild[m].greatgrandchild[n]);

												for (int o = 0; o < monument.category[i].child[j].grandchild[m].greatgrandchild[n].greatgreatgrandchild.Count ; o++)
												{
													if (!isLOD(monument.category[i].child[j].grandchild[m].greatgrandchild[n].greatgreatgrandchild[o].breakingData.name))
														fragments.category[i].child[j].grandchild[m].greatgrandchild[n].greatgreatgrandchild.Add(monument.category[i].child[j].grandchild[m].greatgrandchild[n].greatgreatgrandchild[o]);
												}
											
											}
											
									}
									
							}
						}
			
		
		return fragments;
	}

	public static MonumentData monumentFragments(PrefabDataHolder[] prefabs)
	{
		MonumentData fragments = new MonumentData();
		BreakingData breaking = new BreakingData();
		Transform categoryItem, childItem, grandchildItem, greatGrandchildItem,greatgreatGrandchildItem;
		int idCount = 0;
		
		try
        {
			
			for (int k = 0; k < prefabs.Length; k++)
			{
				if (prefabs[k] != null)
				{
					fragments.monumentName = prefabs[k].name;
		
						for (int i = 0; i < prefabs[k].transform.childCount; i++)
							{
								categoryItem = prefabs[k].transform.GetChild(i);
								breaking.name = categoryItem.name;
								breaking.parent = fragments.monumentName;
								breaking.treeID = idCount;
								breaking.prefabData  = ItemToPrefab(categoryItem, breaking.parent, fragments.monumentName);
								breaking.colliderScales = ItemToColliders(categoryItem);
								fragments.category.Add(new CategoryData(breaking));
								idCount++;
								
								for (int j = 0; j < categoryItem.childCount; j++)
								{
									
									childItem = categoryItem.GetChild(j);
									breaking.name = childItem.name;
									breaking.parent = categoryItem.name;
									breaking.treeID = idCount;
									breaking.prefabData  = ItemToPrefab(childItem, breaking.parent, fragments.monumentName);
									breaking.colliderScales = ItemToColliders(childItem);
									fragments.category[i].child.Add(new ChildrenData(breaking));
									idCount++;
									
										for (int m = 0; m < childItem.childCount; m++)
										{
											grandchildItem = childItem.GetChild(m);
											breaking.name = grandchildItem.name;
											breaking.parent = childItem.name;
											breaking.treeID = idCount;
											breaking.prefabData  = ItemToPrefab(grandchildItem, breaking.parent, fragments.monumentName);
											breaking.colliderScales = ItemToColliders(grandchildItem);
											fragments.category[i].child[j].grandchild.Add(new GrandchildrenData(breaking));
											idCount++;
											
											for (int n = 0; n < grandchildItem.childCount; n++)
												{
													greatGrandchildItem = grandchildItem.GetChild(n);
													breaking.name = greatGrandchildItem.name;
													breaking.parent = grandchildItem.name;
													breaking.treeID = idCount;
													breaking.prefabData  = ItemToPrefab(greatGrandchildItem, breaking.parent, fragments.monumentName);
													breaking.colliderScales = ItemToColliders(greatGrandchildItem);
													fragments.category[i].child[j].grandchild[m].greatgrandchild.Add(new GreatGrandchildrenData(breaking));
													idCount++;

													for (int o = 0; o < greatGrandchildItem.childCount; o++)
													{
														greatgreatGrandchildItem = greatGrandchildItem.GetChild(o);
														breaking.name = greatgreatGrandchildItem.name;
														breaking.parent = greatGrandchildItem.name;
														breaking.treeID = idCount;
														breaking.prefabData  = ItemToPrefab(greatgreatGrandchildItem, breaking.parent, fragments.monumentName);
														breaking.colliderScales = ItemToColliders(greatgreatGrandchildItem);
														fragments.category[i].child[j].grandchild[m].greatgrandchild[n].greatgreatgrandchild.Add(new GreatGreatGrandchildrenData(breaking));
														idCount++;
													}
												
												}
												
										}
										
								}
							}
				}
			}
		}
		catch(Exception e)
		{
			
			Debug.LogError(e.Message);
		}
		return fragments;
	}
	
	public static void loadFragments(MonumentData monumentFragments, BreakerTreeView breakerTree)
	{
		breakerTree.LoadFragments(monumentFragments);
	}

	public static Vector3 transformPosition(Vector3 prefabPosition)
	{
		float adjustZ = 500f;
		float adjustXY = TerrainManager.TerrainSize.x / 2f;
		Vector3 adjuster = new Vector3(adjustXY,adjustZ,adjustXY);
		return (prefabPosition + adjuster);
	}

	public static Vector3 prefabPosition(Vector3 transformPosition)
	{
		float adjustZ = 500f;
		float adjustXY = TerrainManager.TerrainSize.x / 2f;
		Vector3 adjuster = new Vector3(adjustXY,adjustZ,adjustXY);
		return (transformPosition - adjuster);
	}

	public static bool spacing(PrefabDataHolder[] prefabs, Vector3 position, float distance)
	{
		bool tooClose =  false;
		for (int k = 0; k < prefabs.Length; k++)
		{
			tooClose |= (Vector3.Distance(prefabs[k].prefabData.position, position) < distance);
		}
		return tooClose;
	}

	public static bool sphereCollision(Vector3 position, float radius, int mask)
	{
		Collider[] hitcolliders = Physics.OverlapSphere(transformPosition(position), radius, mask);
		
		if (hitcolliders.GetLength(0) > 0)
		{	return true; }
		else
		{  return false; }	
	}

	public static bool inTerrain(PrefabData prefab)
	{
		
		Vector3 direction = new Vector3();
		Vector3 frontAngle =  new Vector3(70f,0f,0f);
		Vector3 sideAngle = new Vector3(0f,135f,0f);
		Vector3 debugAdjustment = new Vector3(90f,0f,0f);
		Vector3 rotateAgain = new Vector3(0f,0f,0f);
		Vector3 rayOrigin = new Vector3(0f,0f,0f);
		RaycastHit hit;
		int rayTest = 0;
		//int count = 0;
		int groundmask = 1<<10;
		GameObject raycaster = GameObject.Find("Raycaster");
		
		
		
		
		raycaster.transform.rotation = Quaternion.Euler(prefab.rotation + frontAngle);
		direction = Quaternion.Euler(prefab.rotation + frontAngle) * raycaster.transform.forward;
		

		
		
		rayOrigin = transformPosition(prefab.position);
		rayOrigin -= raycaster.transform.forward * 10f;
		rayOrigin -= raycaster.transform.up * -10f;
		raycaster.transform.position = rayOrigin;
		
		Ray cliffray = new Ray(rayOrigin, direction);
		
		
		rotateAgain = raycaster.transform.rotation.eulerAngles;
		rotateAgain.x += debugAdjustment.x;
		//createPrefab("decor",  576463322, prefabPosition(cliffray.origin), rotateAgain, new Vector3(1f,8f,1f));
		
		if (Physics.Raycast(cliffray, out hit, 18f, groundmask))
			{	rayTest ++;	}
			else
			{   return false;	}
		
		raycaster.transform.Rotate(100f,0f,0f);
		rotateAgain.x += 100f;
		cliffray.direction = raycaster.transform.forward;
		//createPrefab("decor",  576463322, prefabPosition(cliffray.origin), rotateAgain, new Vector3(1f,8f,1f));	
			
		if (Physics.Raycast(cliffray, out hit, 18f, groundmask))
			{	rayTest ++;	}
			else
			{   return false;	}
		
		raycaster.transform.Rotate(0f,0f,45f);
		rotateAgain.z += 45f;
		cliffray.direction = raycaster.transform.forward;
		
		//createPrefab("decor",  576463322, prefabPosition(cliffray.origin), rotateAgain, new Vector3(1f,8f,1f));	
		
		if (Physics.Raycast(cliffray, out hit, 18f, groundmask))
			{	rayTest ++;	}
			else
			{   return false;	}
		
		raycaster.transform.Rotate(0f,0f,-90f);
		rotateAgain.z -= 90f;
		cliffray.direction = raycaster.transform.forward;
		
		//createPrefab("decor",  576463322, prefabPosition(cliffray.origin), rotateAgain, new Vector3(1f,8f,1f));	
			
		if (Physics.Raycast(cliffray, out hit, 18f, groundmask))
			{	rayTest ++;	}
			else
			{   return false;	}
		
		/*
		
		for (float testAngle = 0f; testAngle < 98f; testAngle += angle)
		{
			count ++;
			
			createPrefab("decor",  576463322, prefabPosition(cliffray.origin), rotateAgain, new Vector3(1f,18f,1f));
			
			
			if (Physics.Raycast(cliffray, out hit, 18f, groundmask))
			{
				
				rayTest ++;
			}
			else
			{
				return false;
			}
			
			raycaster.transform.Rotate(angle,0f,0f);
			rotateAgain.x += angle;
			cliffray.direction = raycaster.transform.forward;
		}
		*/
		//Debug.LogError(rayTest);
		return true;
	}
	
	public static void FoliageCube(Vector3 glassPosition, Vector3 glassScale)
	{
				Vector3 distance = new Vector3(0,0,0);
				Vector3 foliageScale = new Vector3(0,0,0);
				Vector3 foliageRotation = new Vector3(0,0,0);
				Vector3 foliageLocation = new Vector3(0,0,0);
				
				uint creepingcornerB = 2447885804;
				uint creepingcornerA = 738251630;
				uint creepingcornerC = 648907673;
				uint creepingplantfall = 2166677703;
				uint creepingwall600 = 1431389280;
				
				int roll;
				uint foliage=0;
				float foliageRatio = 0f;
				int foliageRoll = 0;
				int cornerRoll=0;
			
				
				foliageRatio = ((glassScale.x / glassScale.y) + (glassScale.x / glassScale.z)) / 2f;
				
								
								if(foliageRatio > .8f && foliageRatio < 1.2f)
								{
									distance.x = glassScale.x / 2f;
									distance.y = glassScale.y / 2f;
									distance.z = glassScale.z / 2f;
									
									foliageScale.x = glassScale.x /6.8f;
									foliageScale.y = glassScale.y /6.8f;
									foliageScale.z = glassScale.z /6.8f;
									
									
									foliageRoll = UnityEngine.Random.Range(2,5);
									
									for (int f = 0; f < foliageRoll; f++)
									{										
									roll = UnityEngine.Random.Range(0,5);
										cornerRoll = UnityEngine.Random.Range(0,4);
										
										switch (roll)
										{
											case 0:
											
												foliage = creepingcornerB;
												
													switch (cornerRoll)
													{
														case 0:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 270f;
															foliageRotation.z = 0f;
															break;
														case 1:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 0f;
															foliageRotation.z = 0f;
															break;
														case 2:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 180f;
															foliageRotation.z = 0f;
															break;
														case 3:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 90f;
															foliageRotation.z = 0f;
															break;
													}
												break;
												
											case 1:
											
												foliage = creepingcornerA;
												
													switch (cornerRoll)
													{
														case 0:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 90f;
															foliageRotation.z = 0f;
															break;
														case 1:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 180f;
															foliageRotation.z = 0f;
															break;
														case 2:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 0f;
															foliageRotation.z = 0f;
															break;
														case 3:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 270f;
															foliageRotation.z = 0f;
															break;
													}
												break;

											case 2:
											
												foliage = creepingcornerC;
												
													switch (cornerRoll)
													{
														case 0:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 90f;
															foliageRotation.z = 0f;
															break;
														case 1:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 180f;
															foliageRotation.z = 0f;
															break;
														case 2:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 0f;
															foliageRotation.z = 0f;
															break;
														case 3:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y - distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 270f;
															foliageRotation.z = 0f;
															break;
													}
												break;	
												
											case 3:
											
												foliage = creepingplantfall;
												
													switch (cornerRoll)
													{
														case 0:
															foliageLocation.x = glassPosition.x;
															foliageLocation.y = glassPosition.y + distance.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 180f;
															foliageRotation.z = 0f;
															break;
														case 1:
															foliageLocation.x = glassPosition.x;
															foliageLocation.y = glassPosition.y + distance.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 0f;
															foliageRotation.z = 0f;
															break;
														case 2:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y + distance.y;
															foliageLocation.z = glassPosition.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 90f;
															foliageRotation.z = 0f;
															break;
														case 3:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y + distance.y;
															foliageLocation.z = glassPosition.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 270f;
															foliageRotation.z = 0f;
															break;
													}
												break;
											
											case 4:
											
												foliage = creepingwall600;
												
													switch (cornerRoll)
													{
														case 0:
															foliageLocation.x = glassPosition.x + distance.x;
															foliageLocation.y = glassPosition.y;
															foliageLocation.z = glassPosition.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 90f;
															foliageRotation.z = 0f;
															break;
														case 1:
															foliageLocation.x = glassPosition.x - distance.x;
															foliageLocation.y = glassPosition.y;
															foliageLocation.z = glassPosition.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 270f;
															foliageRotation.z = 0f;
															break;
														case 2:
															foliageLocation.x = glassPosition.x;
															foliageLocation.y = glassPosition.y;
															foliageLocation.z = glassPosition.z + distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 0f;
															foliageRotation.z = 0f;
															break;
														case 3:
															foliageLocation.x = glassPosition.x;
															foliageLocation.y = glassPosition.y;
															foliageLocation.z = glassPosition.z - distance.z;
															foliageRotation.x = 0f;
															foliageRotation.y = 180f;
															foliageRotation.z = 0f;
															break;
													}
												break;
										}
									
									createPrefab("Decor", foliage, foliageLocation, foliageRotation, foliageScale);
									}
								}
	}
	
	public static void addMonumentMarker(string mark)
	{
		Vector3 location = new Vector3(0,0,0);
		Vector3 rotation = new Vector3(0,0,0);
		Vector3 scale = new Vector3(1,1,1);
		createPrefab(mark, 1724395471, location, rotation, scale);
	}
	
	public static uint GetPalette(int index)
	{
	uint id=0;
				
				uint yellow = 2337881356;
				uint white = 2269472079;
				uint red = 579459297;
				uint navy = 241986762;
				uint junkyard = 1115909638;
				uint green = 1776925867;
				//uint snowblue = 2600171998;
				uint blue = 2473172851;
				uint black = 2722544497;
				
		uint palette1=0;
		uint palette2=0;
		uint palette3=0;
		uint palette4=0;
			
		int roll2 = UnityEngine.Random.Range(0,3);						
		switch (index)
								{
																		case 0:
									palette1=yellow;
										palette2=yellow;
											palette3=yellow;
												palette4=yellow;
								break;
									
																		case 1:
									palette1=white;
										palette2=white;
											palette3=white;
												palette4=white;
								break;
									
																		case 2:
									palette1=red;
										palette2=red;
											palette3=red;
												palette4=red;
								break;
									
																		case 3:
									palette1=navy;
										palette2=navy;
											palette3=navy;
												palette4=navy;
								break;
									
																		case 4:
									palette1=junkyard;
										palette2=junkyard;
											palette3=junkyard;
												palette4=junkyard;
								break;
									
																		case 5:
									palette1=green;
										palette2=green;
											palette3=green;
												palette4=green;
								break;
									
																		case 6:
									palette1=blue;
										palette2=blue;
											palette3=blue;
												palette4=blue;
								break;

									
															default:
									palette1=black;
										palette2=black;
											palette3=black;
												palette4=black;
								break;
								}
		
		int roll = UnityEngine.Random.Range(0,4);
								
								switch (roll)
								{
									case 0:
										id = palette1;
										break;
									case 1:
										id = palette2;
										break;
									case 2:
										id = palette3;
										break;
									case 3:
										id = palette4;
										break;
								}
		return id;
	}
	
	public static void VehicleScrambler(PrefabDataHolder[] prefabs)
	{
		Vector3 position, rotation, scale;
		uint prefabID=0;
		int count=0;
		for (int k = 0; k < prefabs.Length; k++)
		{
			if (prefabs[k] != null)
			{
				prefabID = prefabs[k].prefabData.id;
				if (prefabID == 79597103 || prefabID == 3004602158|| prefabID == 3264578399|| prefabID == 2420618133|| prefabID == 246584577|| prefabID == 1837053258
				|| prefabID == 2832497307|| prefabID == 2300745015|| prefabID == 2082081653)
				{
					position = prefabs[k].prefabData.position;
					rotation = prefabs[k].prefabData.rotation;
					scale = prefabs[k].prefabData.scale;
					prefabID = scrambleVehicles(prefabID);
					createPrefab("Decor",prefabID,position,rotation,scale);	
						
						GameObject.DestroyImmediate(prefabs[k].gameObject);
						prefabs[k] = null;
						count ++;
				}
			}
		}
		Debug.LogError(count + "vehicles scrambled");
	}
	
	public static uint scrambleVehicles(uint prefabID)
	{
		//3273130215 - snow compact
		//3527606348 - snow van
		//3031482036 - snow pickup
		uint[] vehicles = new uint[]{/*compact cars*/79597103,2515395145,1460121016,4193915652,786199644,3435737336,
		2832497307,1896963608,3379016644,2722459803,2665725503,1837053258,677248866,1195466190,
		4214970653,302351425,1247022702,2300745015,4144215382,3367157997,3273130215,
		/*sedans*/3463123806,788274617,2784745032,836014881,3774700171,579044360,1677134398,1677134398,2420618133,
		246584577,8081016,
		/*vans*/2578743929,1236856645,3176682456,2587701539,3420081938,1680874363,946452295,1257336463,3234787709,
		2009259066,3017742883,3489554649,2052946794,1989568183,201458809,696480706,3030494899,283413573,2824451945,
		2076044469,647644014,3497012406,469377923,284962212,536546043,3293125979,3199560384,4048358114,1703786065,
		3246835063,1877110501,1875099655,3674679932,3095093162,3137641591,1536042368,802735396,
		/*pickuptrucks*/1304550409,1427838182,2636007931,997608629,3417439261,2353996331,2742523209,1660134405,
		3539282559,2403566413,3990694053,3957097701,1357952166,1550102226,3899602347,3116567664,2804921169,700152619,
		2661281391,1831500501,3264578399,3004602158,2082081653,1300485700,1779061701,165520484,1529531764,933232738,1047391926};
		
		int vehicleMax = vehicles.GetLength(0);
		int index = UnityEngine.Random.Range(0, vehicleMax);
		return vehicles[index];
		
	}
	
	public static uint ScrambleContainer(uint containerID, int palette)
	{
	uint id = containerID;
	
				uint yellow = 2337881356;
				uint white = 2269472079;
				uint red = 579459297;
				uint navy = 241986762;
				uint junkyard = 1115909638;
				uint green = 1776925867;
				uint snowblue = 2600171998;
				uint blue = 2473172851;
				uint black = 2722544497;
				
				
					
					if(containerID == blue || containerID == red ||
							containerID == yellow || containerID == black ||
							containerID == white || containerID == snowblue || 
							containerID == green || containerID == navy ||
							containerID == junkyard)
							{
								id = GetPalette(palette);
							}
		return id;
	}

	public static void deletePrefabsOffArid(PrefabDataHolder[] prefabs)
	{
		int count = 0;
		int xCheck =0;
		int yCheck =0;
		float[,,] biomeMap = TerrainManager.Biome;
		int res = biomeMap.GetLength(0);
		float ratio = 1f* TerrainManager.TerrainSize.x / res;
		
		for (int k = 0; k < prefabs.Length; k++)
		{
			if (prefabs[k] != null)
			{
								xCheck = (int)((prefabs[k].prefabData.position.z/ratio)+res/2f);
								yCheck = (int)((prefabs[k].prefabData.position.x/ratio)+res/2f);
								if (xCheck > 0 && yCheck > 0 && xCheck < res && yCheck < res)
								{
									if (biomeMap[xCheck,yCheck,0] == 0f)
									{
										GameObject.DestroyImmediate(prefabs[k].gameObject);
										prefabs[k] = null;
										count ++;
									}
								}
			}
		}
		Debug.LogError(count + " prefabs removed");
	}

	public static void deletePrefabIDs(PrefabDataHolder[] prefabs, uint ID)
	{
		int count = 0;

		for (int k = 0; k < prefabs.Length; k++)
		{
			if (prefabs[k] != null)
			{
				if ( prefabs[k].prefabData.id == ID )
				{			
								GameObject.DestroyImmediate(prefabs[k].gameObject);
								prefabs[k] = null;
								count ++;
				}
			}
		}
		Debug.LogError(count + " prefabs removed");
	}


	public static void deletePrefabIDs(PrefabDataHolder[] prefabs, List<GeologyItem> geologyItems)
	{
		int count = 0;
		uint ID = 0;
		
		if (geologyItems != null)
		{
				for (int i  = 0; i < geologyItems.Count; i++)
				{
					if (geologyItems[i].custom)
					{
						GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();
						foreach(GameObject gameObject in allGameObjects)
						{
							if (gameObject.name == geologyItems[i].customPrefab)
							{ GameObject.DestroyImmediate(gameObject);}
						}
					}
					else
					{
						ID = geologyItems[i].prefabID;
						for (int k = 0; k < prefabs.Length; k++)
						{
							if (prefabs[k] != null)
							{
								if ( prefabs[k].prefabData.id == ID )
								{			
												GameObject.DestroyImmediate(prefabs[k].gameObject);
												prefabs[k] = null;
												count ++;
								}
							}
						}
					}

				}
		}


		
		Debug.LogError(count + " prefabs removed");
	}
	

	public static void deleteAllPrefabs(PrefabDataHolder[] prefabs)
	{
		int count = 0;

		for (int k = 0; k < prefabs.Length; k++)
		{

				
							GameObject.DestroyImmediate(prefabs[k].gameObject);
							prefabs[k] = null;
							count ++;
		}
		Debug.LogError(count + " prefabs removed");
	}
	
	public static void SpawnPrefabs(List<BreakingData> fragment, Transform parent)
        {
		
			float adjustZ = 500f;
			float adjustXY = TerrainManager.TerrainSize.x / 2f;
			Vector3 adjuster = new Vector3(adjustXY,adjustZ,adjustXY);
			
			
            for (int i = 0; i < fragment.Count; i++)
            {
				if(!fragment[i].ignore)
				{
					if (fragment[i].prefabData.id != 0)
					{
						fragment[i].prefabData.position -= adjuster;
						Spawn(Load(fragment[i].prefabData.id), fragment[i].prefabData, parent);	
						fragment[i].prefabData.position += adjuster;
					}
				}
            }
        }
	
	public static void EnableColliders(PrefabDataHolder[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {                
                prefabs[i].EnableColliders();
            }
        }
		
	public static void DisableColliders(PrefabDataHolder[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {                
                prefabs[i].DisableColliders();
            }
        }
	
    private static class Coroutines
    {
        public static IEnumerator SpawnPrefabs(PrefabData[] prefabs, int progressID)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 4f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / prefabs.Length, "Spawning Prefabs: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
                Spawn(Load(prefabs[i].id), prefabs[i], PrefabParent);
            }

            Progress.Report(progressID, 0.99f, "Spawned " + prefabs.Length + " prefabs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }

        public static IEnumerator DeletePrefabs(PrefabDataHolder[] prefabs, int progressID = 0)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (progressID == 0)
                progressID = Progress.Start("Delete Prefabs", null, Progress.Options.Sticky);

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.25f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / prefabs.Length, "Deleting Prefabs: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
                GameObject.DestroyImmediate(prefabs[i].gameObject);
            }
			
			List<GameObject> children = new List<GameObject>();
			foreach (Transform child in PrefabParent)
			{
				children.Add(child.gameObject);
			}
			children.ForEach(child => GameObject.DestroyImmediate(child));
			
			
            Progress.Report(progressID, 0.99f, "Deleted " + prefabs.Length + " prefabs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }

        public static IEnumerator ReplaceWithLoaded(PrefabDataHolder[] prefabs, int progressID)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 4f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / prefabs.Length, "Replacing Prefabs: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
                prefabs[i].UpdatePrefabData();
                Spawn(Load(prefabs[i].prefabData.id), prefabs[i].prefabData, GetParent(prefabs[i].prefabData.category));
                GameObject.DestroyImmediate(prefabs[i].gameObject);
            }

            Progress.Report(progressID, 0.99f, "Replaced " + prefabs.Length + " prefabs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
            IsChangingPrefabs = false;
        }

        public static IEnumerator ReplaceWithDefault(PrefabDataHolder[] prefabs, int progressID)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.05f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / prefabs.Length, "Replacing Prefabs: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
                prefabs[i].UpdatePrefabData();
                Spawn(DefaultPrefab, prefabs[i].prefabData, GetParent(prefabs[i].prefabData.category));
                GameObject.DestroyImmediate(prefabs[i].gameObject);
            }

            Progress.Report(progressID, 0.99f, "Replaced " + prefabs.Length + " prefabs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
            IsChangingPrefabs = false;
        }

		public static IEnumerator SpawnPrefabs(PrefabData[] prefabs, int progressID, Transform parent)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 4f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / prefabs.Length, "Spawning Prefabs: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
				
                SpawnCustoms(Load(prefabs[i].id), prefabs[i], parent);
            }
            Progress.Report(progressID, 0.99f, "Spawned " + prefabs.Length + " prefabs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }
		
		public static IEnumerator SpawnCircuits(CircuitData[] circuitData, int progressID)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = circuitData.Length-1; i > -1; i--)
            {
                if (sw.Elapsed.TotalSeconds > 4f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / circuitData.Length, "Spawning Electric: " + i + " / " + circuitData.Length);
                    sw.Restart();
                }
                Spawn(DefaultPrefab, circuitData[i], ElectricsParent);
            }
            Progress.Report(progressID, 0.99f, "Spawned " + circuitData.Length + " circuits.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }

		public static IEnumerator SpawnNPCs(NPCData[] bots, int progressID)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
			
            for (int i = 0; i < bots.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 4f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / bots.Length, "Spawning NPCs: " + i + " / " + bots.Length);
                    sw.Restart();
                }
                Spawn(DefaultPrefab, bots[i], NPCsParent);
            }
            Progress.Report(progressID, 0.99f, "Spawned " + bots.Length + " npcs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }
		
		public static IEnumerator DeleteCircuits(CircuitDataHolder[] circuits, int progressID = 0)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (progressID == 0)
                progressID = Progress.Start("Delete Circuits", null, Progress.Options.Sticky);

            for (int i = 0; i < circuits.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.25f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / circuits.Length, "Deleting Circuits: " + i + " / " + circuits.Length);
                    sw.Restart();
                }
                GameObject.DestroyImmediate(circuits[i].gameObject);
            }
            Progress.Report(progressID, 0.99f, "Deleted " + circuits.Length + " circuits.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }
		
		public static IEnumerator DeleteNPCs(NPCDataHolder[] npcs, int progressID = 0)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (progressID == 0)
                progressID = Progress.Start("Delete NPCs", null, Progress.Options.Sticky);

            for (int i = 0; i < npcs.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.25f)
                {
                    yield return null;
                    Progress.Report(progressID, (float)i / npcs.Length, "Deleting Circuits: " + i + " / " + npcs.Length);
                    sw.Restart();
                }
                GameObject.DestroyImmediate(npcs[i].gameObject);
            }
            Progress.Report(progressID, 0.99f, "Deleted " + npcs.Length + " NPCs.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
        }
		
		public static IEnumerator RenameNPCs(NPCDataHolder[] bots, string name)
        {
		
			ProgressManager.RemoveProgressBars("Rename NPC Categories");
            int progressId = Progress.Start("Rename NPC Categories", null, Progress.Options.Sticky);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < bots.Length; i++)
            {
				bots[i].bots.category = name;
				if (sw.Elapsed.TotalSeconds > 0.2f)
                {
                    yield return null;
                    Progress.Report(progressId, (float)i / bots.Length, "Renaming NPC Categories: " + i + " / " + bots.Length);
                    sw.Restart();
                }
			}
			
			
            

            Progress.Report(progressId, 0.99f, "Renamed: " + bots.Length + " npcs.");
            Progress.Finish(progressId);
        }
           

        public static IEnumerator RenamePrefabCategories(PrefabDataHolder[] prefabs, string name)
        {
			
            ProgressManager.RemoveProgressBars("Rename Prefab Categories");
            int progressId = Progress.Start("Rename Prefab Categories", null, Progress.Options.Sticky);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
			string shopkeeper = "";

            for (int i = 0; i < prefabs.Length; i++)
            {
				
				if (prefabs[i].prefabData.id == 1724395471)
				{
					//do not rename monument marker
				}
				else if (prefabs[i].prefabData.id == 856899687 || prefabs[i].prefabData.id == 3604512213)
				{
					prefabs[i].prefabData.category = "Dungeon";
				}
				else if (prefabs[i].prefabData.category == "Dungeon")
				{
					//do not rename Dungeon
				}				
				else if (prefabs[i].prefabData.id == 858853278)
				{
					//preserve shopkeeper tags
					shopkeeper = prefabs[i].prefabData.category.Split(':').Last();
					prefabs[i].prefabData.category = name + shopkeeper;
				}
				else
				{					
					prefabs[i].prefabData.category = name;
				}

                if (sw.Elapsed.TotalSeconds > 0.2f)
                {
                    yield return null;
                    Progress.Report(progressId, (float)i / prefabs.Length, "Renaming Prefab: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
            }

            Progress.Report(progressId, 0.99f, "Renamed: " + prefabs.Length + " prefabs.");
            Progress.Finish(progressId);
        }

        public static IEnumerator RenamePrefabIDs(PrefabDataHolder[] prefabs, uint id, bool replace)
        {
            ProgressManager.RemoveProgressBars("Rename Prefab IDs");
            int progressId = Progress.Start("Rename Prefab IDs", null, Progress.Options.Sticky);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < prefabs.Length; i++)
            {
                prefabs[i].prefabData.id = id;
                if (replace)
                {
                    prefabs[i].UpdatePrefabData();
                    Spawn(Load(prefabs[i].prefabData.id), prefabs[i].prefabData, GetParent(prefabs[i].prefabData.category));
                    GameObject.DestroyImmediate(prefabs[i].gameObject);
                }
                if (sw.Elapsed.TotalSeconds > 0.2f)
                {
                    yield return null;
                    Progress.Report(progressId, (float)i / prefabs.Length, "Renaming Prefab: " + i + " / " + prefabs.Length);
                    sw.Restart();
                }
            }

            Progress.Report(progressId, 0.99f, "Renamed: " + prefabs.Length + " prefabs.");
            Progress.Finish(progressId);
        }
    }
}