﻿using System.Collections;
using System.Linq;
using UnityEngine;
using RustMapEditor.Variables;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

using static WorldSerialization;
using EasyRoads3Dv3;

public static class PathManager
{
    public static ERRoadNetwork _roadNetwork;
    public static int _roadIDCounter = 1;
	public static Transform roadTransform;
	public static GameObject NodePrefab { get; private set; }
	public static NodeCollection CurrentNodeCollection { get; private set; } // New field for current NodeCollection

    public static void SetCurrentNodeCollection(NodeCollection nodeCollection)
    {
        CurrentNodeCollection = nodeCollection;
    }

    public static void ClearCurrentNodeCollection()
    {
        CurrentNodeCollection = null;
    }

	//road type name keywords
	//names are "Keyword X" where X is an integer enumerating the paths 
	
	//Powerline   (invisible)
	//Road
	//Width 12  ring road
	//width 10  normal road
	//width 4   trails (invisible)
	
	//River (invisible)
	//Rail
	


    #if UNITY_EDITOR
    public static void Init()
    {
		PathParent = GameObject.FindWithTag("Paths").transform;
		roadTransform = GameObject.FindWithTag("EasyRoads").transform;
		roadTransform.SetParent(PathParent, false);
		roadTransform.position = PathParent.position;
		EditorApplication.update += OnProjectLoad;
		NodePrefab = Resources.Load<GameObject>("Prefabs/NodeSphere");
    }

    private static void OnProjectLoad()
    {
		_roadNetwork = new ERRoadNetwork();
		roadTransform.position = PathParent.position;
        EditorApplication.update -= OnProjectLoad;
    }
    #endif

    public static void RuntimeInit()
    {
		PathParent = GameObject.FindWithTag("Paths").transform;
		roadTransform = GameObject.FindWithTag("EasyRoads").transform;
		roadTransform.SetParent(PathParent, false);
		_roadNetwork = new ERRoadNetwork();
		roadTransform.position = PathParent.position;
		NodePrefab = Resources.Load<GameObject>("Prefabs/NodeSphere");
    }

	public static void SpawnPath(PathData pathData)
	{
		if (_roadNetwork == null)
		{
			Debug.LogError("RoadNetwork not initialized.");
			return;
		}

		Vector3 offset = PathParent.transform.position;
		Vector3[] markers = pathData.nodes.Select(v => new Vector3(v.x, v.y, v.z) + offset).ToArray();
		string roadName = pathData.name;

		// Create the road object
		ERRoad newRoad = _roadNetwork.CreateRoad(roadName, markers);
		if (newRoad == null)
		{
			Debug.LogError($"Failed to create road '{roadName}'.");
			return;
		}

		// Configure the road
		ConfigureRoad(newRoad, pathData);

		// Set up the GameObject hierarchy
		GameObject roadObject = newRoad.gameObject;
		roadObject.transform.SetParent(roadTransform, false);

		// Add PathDataHolder
		PathDataHolder dataHolder = roadObject.AddComponent<PathDataHolder>();
		dataHolder.pathData = pathData;
		
		newRoad.Refresh();
		
		NetworkManager.Register(roadObject);
		roadObject.tag = "Path";
		roadObject.SetLayerRecursively(9); // Paths layer
	}
	
    public static void ConfigureRoad(ERRoad road, PathData pathData)
    {

        road.SetName(pathData.name);
        road.SetWidth(pathData.width);
        road.SetMarkerControlType(0, pathData.spline ? ERMarkerControlType.Spline : ERMarkerControlType.StraightXZ);
        road.ClosedTrack(false);

        ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
        if (modularRoad == null)
        {
            Debug.LogError($"ERModularRoad component not found on road '{pathData.name}'.");
            return;
        }

        ERRoadType[] roadTypes = _roadNetwork.GetRoadTypes();
        string[] nameParts = pathData.name.Split(' ');
        string roadTypePrefix = nameParts[0].ToLower();

        bool isVisible = true;
        switch (roadTypePrefix)
        {
            case "river":
            case "powerline":
                isVisible = false;
                if (roadTypes != null && roadTypes.Length > 0)
                {
                    road.SetRoadType(roadTypes[0]); // Transparent style
                }
                break;

            case "road":
                if (roadTypes != null && roadTypes.Length > 1)
                {
                    road.SetRoadType(roadTypes[1]); // Visible "good" style
                    modularRoad.lanes = pathData.width >= 10 ? 2 : 1;
                }
                break;

            case "rail":
                if (roadTypes != null && roadTypes.Length > 1)
                {
                    road.SetRoadType(roadTypes[1]); // Visible style
                    modularRoad.roadWidth = 2f; // Fixed width for rails
                    modularRoad.lanes = 0;
                }
                break;

            default:
                Debug.LogWarning($"Unknown road type prefix '{roadTypePrefix}' in '{pathData.name}'. Using default visible style.");
                if (roadTypes != null && roadTypes.Length > 1)
                {
                    road.SetRoadType(roadTypes[1]);
                }
                break;
        }

        modularRoad.roadWidth = pathData.width;
        modularRoad.indent = pathData.innerPadding;
        modularRoad.surrounding = pathData.outerPadding;
        modularRoad.fadeInDistance = pathData.innerFade;
        modularRoad.fadeOutDistance = pathData.outerFade;
        modularRoad.terrainContoursOffset = pathData.terrainOffset;
        modularRoad.splatIndex = pathData.splat;
        modularRoad.startConnectionFlag = pathData.start;
        modularRoad.endConnectionFlag = pathData.end;
    }


    public static void RotatePaths(bool CW)
    {
        if (_roadNetwork != null)
        {
            PathParent.transform.Rotate(0, CW ? 90f : -90f, 0, Space.World);
            foreach (ERRoad road in _roadNetwork.GetRoadObjects())
            {
                Vector3[] newMarkerPositions = road.GetMarkerPositions().Select(p => PathParent.rotation * (p - PathParent.position) + PathParent.position).ToArray();
                road.SetMarkerPositions(newMarkerPositions);
                road.Refresh();
            }
        }
        else
        {
            Debug.LogWarning("RoadNetwork is not initialized. Cannot rotate paths.");
        }
    }

    #if UNITY_EDITOR
    public static void SpawnPaths(WorldSerialization.PathData[] paths, int progressID)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnPaths(paths, progressID));
    }

    public static void DeletePaths(PathDataHolder[] paths, int delPath = 0)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.DeletePaths(paths, delPath));
    }
    #else
    public static void SpawnPaths(WorldSerialization.PathData[] paths, int progressID = 0)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.SpawnPaths(paths, progressID));
    }

    public static void DeletePaths(PathDataHolder[] paths, int delPath = 0)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.DeletePaths(paths, delPath));
    }
    #endif

    private static class Coroutines
    {
        public static IEnumerator SpawnPaths(WorldSerialization.PathData[] paths, int progressID = 0)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < paths.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.1f)
                {
                    yield return null;
                    #if UNITY_EDITOR
                    Progress.Report(progressID, (float)i / paths.Length, "Spawning Paths: " + i + " / " + paths.Length);
                    #endif
                    sw.Restart();
                }
                SpawnPath(paths[i]);
            }
            #if UNITY_EDITOR
            Progress.Report(progressID, 0.99f, "Spawned " + paths.Length + " paths.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
            #endif
        }

        public static IEnumerator DeletePaths(PathDataHolder[] paths, int progressID = 0)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            #if UNITY_EDITOR
            if (progressID == 0)
                progressID = Progress.Start("Delete Paths", null, Progress.Options.Sticky);
            #endif

            for (int i = 0; i < paths.Length; i++)
            {
                if (sw.Elapsed.TotalSeconds > 0.1f)
                {
                    yield return null;
                    #if UNITY_EDITOR
                    Progress.Report(progressID, (float)i / paths.Length, "Deleting Paths: " + i + " / " + paths.Length);
                    #endif
                    sw.Restart();
                }
				Debug.LogError(paths[i].gameObject.name);
                ERRoad roadToDelete = _roadNetwork.GetRoadByName(paths[i].pathData.name);

                if (roadToDelete != null)
                {
                    roadToDelete.Destroy();
                }
                else
                {
                    Debug.LogWarning($"Could not find road named {paths[i].gameObject.name} to delete.");
                }

                
            }

            #if UNITY_EDITOR
            Progress.Report(progressID, 0.99f, "Deleted " + paths.Length + " paths.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
            #endif
        }
    }

    public static Transform PathParent { get; private set; }
    public static PathDataHolder[] CurrentMapPaths { get => PathParent.GetComponentsInChildren<PathDataHolder>(); }
}