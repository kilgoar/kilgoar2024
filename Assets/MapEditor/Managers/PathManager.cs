using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

using UnityEngine;
using static WorldSerialization;

public static class PathManager
{
    #if UNITY_EDITOR
	#region Init
    [InitializeOnLoadMethod]
    public static void Init()
    {
        EditorApplication.update += OnProjectLoad;
    }

    private static void OnProjectLoad()
    {
        DefaultPath = Resources.Load<GameObject>("Paths/Path");
        DefaultNode = Resources.Load<GameObject>("Paths/PathNode");
        PathParent = GameObject.FindGameObjectWithTag("Paths").transform;
        if (DefaultPath != null && DefaultNode != null && PathParent != null)
            EditorApplication.update -= OnProjectLoad;
    }
    #endregion
	#endif
	
	public static void RuntimeInit()
	{
		DefaultPath = Resources.Load<GameObject>("Paths/Path");
        DefaultNode = Resources.Load<GameObject>("Paths/PathNode");
        PathParent = GameObject.FindGameObjectWithTag("Paths").transform;
	}
	
    public static GameObject DefaultPath { get; private set; }
    public static GameObject DefaultNode { get; private set; }
    public static Transform PathParent { get; private set; }

    /// <summary>Paths currently spawned on the map.</summary>
    public static PathDataHolder[] CurrentMapPaths { get => PathParent.GetComponentsInChildren<PathDataHolder>(); }

    public enum PathType
    {
        River = 0,
        Road = 1,
        Powerline = 2,
    }

    public static void SpawnPath(PathData pathData)
    {
        Vector3 averageLocation = Vector3.zero;
        for (int j = 0; j < pathData.nodes.Length; j++)
            averageLocation += pathData.nodes[j];

        averageLocation /= pathData.nodes.Length;
        GameObject newObject = GameObject.Instantiate(DefaultPath, averageLocation + PathParent.position, Quaternion.identity, PathParent);
        newObject.name = pathData.name;

        var pathNodes = new List<GameObject>();
        for (int j = 0; j < pathData.nodes.Length; j++)
        {
            GameObject newNode = GameObject.Instantiate(DefaultNode, newObject.transform);
            newNode.transform.position = pathData.nodes[j] + PathParent.position;
            pathNodes.Add(newNode);
        }
        newObject.GetComponent<PathDataHolder>().pathData = pathData;
    }

    /// <summary>Rotates all paths in map 90° Clockwise or Counter Clockwise.</summary>
    /// <param name="CW">True = 90°, False = 270°</param>
    public static void RotatePaths(bool CW)
    {
        PathParent.transform.Rotate(0, CW ? 90f : -90f, 0, Space.World);
        PathParent.gameObject.GetComponent<LockObject>().UpdateTransform();
    }
	
	#if UNITY_EDITOR
    public static void SpawnPaths(PathData[] paths, int progressID)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.SpawnPaths(paths, progressID));
    }

    public static void DeletePaths(PathDataHolder[] paths, int progressID = 0)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(Coroutines.DeletePaths(paths, progressID));
    }
	#else
	public static void SpawnPaths(PathData[] paths, int progressID = 0)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.SpawnPaths(paths, progressID));
    }

    public static void DeletePaths(PathDataHolder[] paths, int progressID = 0)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(Coroutines.DeletePaths(paths, progressID));
    }
	#endif

    private static class Coroutines
    {
        public static IEnumerator SpawnPaths(PathData[] paths, int progressID = 0)
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
                GameObject.DestroyImmediate(paths[i].gameObject);
            }
			#if UNITY_EDITOR
            Progress.Report(progressID, 0.99f, "Deleted " + paths.Length + " paths.");
            Progress.Finish(progressID, Progress.Status.Succeeded);
			#endif
        }
    }
}