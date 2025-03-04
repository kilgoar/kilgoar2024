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
    private static ERRoadNetwork _roadNetwork;
    private static int _roadIDCounter = 1;
	private static Transform roadTransform;


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
    }

	public static void SpawnPath(PathData pathData)
	{
		if (_roadNetwork == null || pathData == null || pathData.nodes == null || pathData.nodes.Length == 0)
		{
			Debug.LogError("Invalid RoadNetwork or PathData.");
			return;
		}

		Vector3 offset = PathParent.transform.position;
		Vector3[] markers = pathData.nodes.Select(v => (Vector3)v + offset).ToArray();
		string roadName = pathData.name;
		ERRoad newRoad = _roadNetwork.CreateRoad(roadName, markers);

		if (newRoad != null)
		{
			newRoad.SetName(roadName);
			newRoad.SetWidth(pathData.width);
			newRoad.SetMarkerControlType(0, pathData.spline ? ERMarkerControlType.Spline : ERMarkerControlType.StraightXZ);
			newRoad.ClosedTrack(false); // All paths unclosed


		ERRoadType[] roadTypes = _roadNetwork.GetRoadTypes();

		if (roadName.StartsWith("River") || roadName.StartsWith("Powerline") || 
			(roadName.StartsWith("Road") && pathData.width < 5))
		{
			// Set to transparent style (index 0)
			if (roadTypes != null && roadTypes.Length > 0)
			{
				newRoad.SetRoadType(roadTypes[0]);
			}
			else
			{
				Debug.LogWarning("No road types available; cannot set transparent style (index 0).");
			}
		}
		else
		{
			// Set to visible "good" style (index 1)
			if (roadTypes != null && roadTypes.Length > 1)
			{
				newRoad.SetRoadType(roadTypes[1]);
			}
			else
			{
				Debug.LogWarning("Insufficient road types available; cannot set good style (index 1).");
			}
		}			

			newRoad.Refresh();
			PathDataHolder dataHolder = newRoad.gameObject.AddComponent<PathDataHolder>();
			dataHolder.pathData = pathData;
		}
		else
		{
			Debug.LogError($"Failed to create road '{roadName}'.");
		}

		roadTransform.gameObject.SetLayerRecursively(9);
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