using UnityEngine;
using EasyRoads3Dv3;
using System.Linq;
using static WorldSerialization;
using System.Collections.Generic;

public class NodeCollection : MonoBehaviour
{
    public PathData pathData;
    private ERModularRoad modularRoad;
    private PathDataHolder pathDataHolder;
    private ERRoad road;
    private List<Transform> nodeTransforms;
    private Material originalRoadMaterial;
    private Material goldMaterial;
	private bool populating;

    private void Awake()
    {
        nodeTransforms = new List<Transform>();
        goldMaterial = new Material(Shader.Find("Standard"));
        goldMaterial.color = new Color(0.7f, 0.6f, 0f, 1f);
    }

    public void Initialize(ERRoad erRoad)
    {
        road = erRoad;
        if (road == null)
        {
            Debug.LogError("NodeCollection initialized with null ERRoad!");
        }
        else
        {
            Debug.Log($"Initialized NodeCollection with ERRoad: {road.GetName()}");
        }
    }

    private void Start()
    {
        if (pathData == null)
        {
            pathDataHolder = FindPathDataHolder();
            modularRoad = FindModularRoad();

            if (pathDataHolder == null)
            {
                Debug.LogError($"NodeCollection on {gameObject.name} could not find PathDataHolder in parent hierarchy.");
                return;
            }

            if (modularRoad == null)
            {
                Debug.LogError($"NodeCollection on {gameObject.name} could not find ERModularRoad in parent hierarchy.");
                return;
            }

            if (road == null)
            {
                Debug.LogError($"NodeCollection on {gameObject.name} was not initialized with an ERRoad.");
                return;
            }

            pathData = pathDataHolder.pathData;
            Debug.Log($"Start: Set pathData from PathDataHolder. road: {(road != null ? road.GetName() : "null")}, modularRoad: {(modularRoad != null ? modularRoad.name : "null")}");
        }
        else
        {
            modularRoad = FindModularRoad();
            if (modularRoad == null)
            {
                Debug.LogError($"NodeCollection on {gameObject.name} has pathData but could not find ERModularRoad in parent hierarchy.");
            }
            Debug.Log($"Start: pathData was set externally. road: {(road != null ? road.GetName() : "null")}, modularRoad: {(modularRoad != null ? modularRoad.name : "null")}");
        }
    }

    private PathDataHolder FindPathDataHolder()
    {
        Transform parent = transform.parent;
        return parent != null ? parent.GetComponent<PathDataHolder>() : null;
    }

    private ERModularRoad FindModularRoad()
    {
        Transform parent = transform.parent;
        return parent != null ? parent.GetComponent<ERModularRoad>() : null;
    }

	 public void PopulateNodes()
	{
		populating = true;
		try // Use try-finally to ensure populating is reset
		{
			if (pathData == null || pathData.nodes == null || pathData.nodes.Length == 0)
			{
				Debug.LogError("Invalid PathData for spawning nodes.");
				return;
			}

			if (road == null)
			{
				Debug.LogError($"NodeCollection on {gameObject.name} has no valid ERRoad during PopulateNodes.");
				return;
			}

			Vector3 offset = PathManager.PathParent.transform.position;
			Vector3[] markers = pathData.nodes.Select(v => new Vector3(v.x, v.y, v.z) + offset).ToArray();

			// Unsubscribe events from existing nodes
			foreach (Transform node in nodeTransforms)
			{
				if (node != null && node.TryGetComponent<PathNode>(out PathNode pathNode))
				{
					pathNode.OnTransformChanged -= HandleNodeTransformChanged;
				}
			}

			// Reuse or create nodes as needed
			int markerCount = markers.Length;
			List<Transform> newNodeTransforms = new List<Transform>(markerCount);

			for (int i = 0; i < markerCount; i++)
			{
				Transform sphereTransform;
				if (i < nodeTransforms.Count && nodeTransforms[i] != null)
				{
					// Reuse existing node
					sphereTransform = nodeTransforms[i];
					sphereTransform.position = markers[i];
					sphereTransform.name = $"PathNode_{i}";
					if (!sphereTransform.TryGetComponent<PathNode>(out _))
					{
						sphereTransform.gameObject.AddComponent<PathNode>().Initialize(this);
					}
				}
				else
				{
					// Create new node if needed
					if (PathManager.NodePrefab == null)
					{
						Debug.LogError("PathManager.NodePrefab is null. Ensure it’s loaded in Resources/Prefabs/");
						return;
					}
					sphereTransform = ConfigureNode(PathManager.NodePrefab, i, markers[i], false);
				}
				newNodeTransforms.Add(sphereTransform);
			}

			// Remove excess nodes if any
			for (int i = markerCount; i < nodeTransforms.Count; i++)
			{
				if (nodeTransforms[i] != null)
				{
					Object.DestroyImmediate(nodeTransforms[i].gameObject);
				}
			}

			nodeTransforms = newNodeTransforms;

			// Subscribe events to all nodes after population
			foreach (Transform node in nodeTransforms)
			{
				if (node.TryGetComponent<PathNode>(out PathNode pathNode))
				{
					pathNode.OnTransformChanged += HandleNodeTransformChanged;
				}
			}

			Debug.Log($"Populated {nodeTransforms.Count} nodes from pathData.nodes.");

			UpdateRoadMarkers();
			SetRoadMaterialToGold();
		}
		finally
		{
			populating = false; // Always reset populating, even on error or early return
		}
	}

    private void SetRoadMaterialToGold()
    {
        if (road == null)
        {
            Debug.LogError("Cannot set road material to gold: road is null.");
            return;
        }

        ERRoadType roadType = road.GetRoadType();
        if (roadType == null)
        {
            Debug.LogError("Failed to get road type from ERRoad.");
            return;
        }

        originalRoadMaterial = roadType.roadMaterial;
        if (originalRoadMaterial == null)
        {
            Debug.LogWarning("Original road material from road type is null. Creating a default material.");
            originalRoadMaterial = new Material(Shader.Find("Standard"));
        }

        road.SetMaterial(goldMaterial);
        Debug.Log($"Set road '{road.GetName()}' material to gold color: {goldMaterial.color}");
    }

    private Transform ConfigureNode(GameObject spherePrefab, int index, Vector3 position, bool subscribeEvents = true)
    {
        GameObject sphere = Object.Instantiate(spherePrefab, position, Quaternion.identity, transform);
        sphere.name = $"PathNode_{index}";
        sphere.tag = "Node";
        Transform sphereTransform = sphere.transform;
        sphereTransform.localScale = Vector3.one * 5f;
        sphere.layer = 9;

        PathNode pathNode = sphere.AddComponent<PathNode>();
        pathNode.Initialize(this);
        if (subscribeEvents)
        {
            pathNode.OnTransformChanged += HandleNodeTransformChanged;
        }

        return sphereTransform;
    }

    public Transform GetFirstNode()
    {
        if (nodeTransforms == null || nodeTransforms.Count == 0)
        {
            Debug.LogWarning($"No nodes available in NodeCollection on {gameObject.name}.");
            return null;
        }

        Transform firstNode = nodeTransforms[0];
        if (firstNode == null)
        {
            Debug.LogWarning($"First node in NodeCollection on {gameObject.name} is null or destroyed.");
            return null;
        }

        Debug.Log($"Retrieved first node: {firstNode.name} from NodeCollection on {gameObject.name}");
        return firstNode;
    }

    private void HandleNodeTransformChanged()
    {
		if (populating) return;
		
        UpdatePathData();
        UpdateRoadMarkers();
    }

    public void UpdatePathData()
    {
        if (pathData == null)
        {
            Debug.LogError("Cannot update path data: PathData is null.");
            return;
        }

        if (nodeTransforms.Count == 0)
        {
            Debug.LogWarning("No nodes found to update path data.");
            return;
        }

        Vector3 offset = PathManager.PathParent.transform.position;
        pathData.nodes = nodeTransforms
            .OrderBy(t => t.GetSiblingIndex())
            .Select(t => t.position - offset)
            .Select(p => new VectorData { x = p.x, y = p.y, z = p.z })
            .ToArray();
    }

    public void UpdateRoadMarkers()
    {
        if (road == null)
        {
            Debug.LogError("ERRoad reference is null, cannot update road markers.");
            return;
        }

        Vector3[] newPositions = nodeTransforms.Select(t => t.position).ToArray();
        int markerCount = road.GetMarkerCount();

        if (markerCount != newPositions.Length)
        {
            Debug.Log($"Marker count mismatch: ERRoad has {markerCount} markers, nodeTransforms has {newPositions.Length}. Adjusting markers...");
            while (markerCount > newPositions.Length)
            {
                road.DeleteMarker(markerCount - 1);
                markerCount--;
            }
            while (markerCount < newPositions.Length)
            {
                road.AddMarker(newPositions[markerCount]);
                markerCount++;
            }
        }

        road.ClosedTrack(false);
        road.SetMarkerPositions(newPositions);
        road.Refresh();

        Debug.Log($"Updated ERRoad '{road.GetName()}' with {newPositions.Length} markers.");
    }

    public void RemoveNode(Transform nodeTransform)
    {
        if (nodeTransforms.Contains(nodeTransform))
        {
            if (nodeTransform.TryGetComponent<PathNode>(out PathNode pathNode))
            {
                pathNode.OnTransformChanged -= HandleNodeTransformChanged;
            }
            nodeTransforms.Remove(nodeTransform);
            Debug.Log($"Removed PathNode {nodeTransform.name} from NodeCollection.");
            UpdatePathData();
            UpdateRoadMarkers();
        }
    }

    public int DetermineInsertIndex(IReadOnlyList<Transform> nodes, Vector3 hitPoint)
    {
        if (nodes.Count == 0) return 0;

        float minDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            float dist = Vector3.Distance(hitPoint, nodes[i].position);
            Debug.Log($"Node {i} at {nodes[i].position}, distance to hitPoint {hitPoint}: {dist}");
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        Debug.Log($"Closest node is at index {closestIndex}, distance: {minDist}, hitPoint: {hitPoint}");

        if (closestIndex == 0)
        {
            Debug.Log("Closest is first node, inserting at start (index 0)");
            return 0;
        }
        else if (closestIndex == nodes.Count - 1)
        {
            Debug.Log("Closest is last node, appending at end (index -1)");
            return -1;
        }

        float distToPrev = Vector3.Distance(hitPoint, nodes[closestIndex - 1].position);
        float distToNext = Vector3.Distance(hitPoint, nodes[closestIndex + 1].position);
        int insertIndex = distToPrev < distToNext ? closestIndex : closestIndex + 1;

        Debug.Log($"Closest interior node at {closestIndex}, distToPrev: {distToPrev}, distToNext: {distToNext}, inserting at {insertIndex}");
        return insertIndex;
    }

    public void AddNodeAtPosition(Vector3 hitPoint, List<GameObject> selectedObjects = null)
    {
        if (pathData == null)
        {
            Debug.LogError("Cannot add node: PathData is null.");
            return;
        }

        if (selectedObjects != null)
        {
            selectedObjects.Clear();
        }

        int insertIndex = DetermineInsertIndex(GetNodes(), hitPoint);

        if (insertIndex == -1)
        {
            if (PrefabManager.DefaultSphereVolume == null)
            {
                Debug.LogError("PrefabManager.DefaultSphereVolume is null. Ensure it’s loaded in Resources/Prefabs/");
                return;
            }
            Transform sphereTransform = ConfigureNode(PrefabManager.DefaultSphereVolume, nodeTransforms.Count, hitPoint);
            nodeTransforms.Add(sphereTransform);
            Debug.Log($"Appended new PathNode to end (position: {hitPoint}) at index {nodeTransforms.Count - 1}.");

            Vector3 offset = PathManager.PathParent.transform.position;
            VectorData newNode = new VectorData { x = hitPoint.x - offset.x, y = hitPoint.y - offset.y, z = hitPoint.z - offset.z };
            List<VectorData> updatedNodes = pathData.nodes != null ? pathData.nodes.ToList() : new List<VectorData>();
            updatedNodes.Add(newNode);
            pathData.nodes = updatedNodes.ToArray();

            if (selectedObjects != null)
            {
                selectedObjects.Add(sphereTransform.gameObject);
                Debug.Log($"Reset selection to new node at end: {sphereTransform.name}");
            }

            UpdateRoadMarkers();
        }
        else
        {
            Vector3 offset = PathManager.PathParent.transform.position;
            VectorData newNode = new VectorData { x = hitPoint.x - offset.x, y = hitPoint.y - offset.y, z = hitPoint.z - offset.z };
            List<VectorData> updatedNodes = pathData.nodes != null ? pathData.nodes.ToList() : new List<VectorData>();
            updatedNodes.Insert(insertIndex, newNode);
            pathData.nodes = updatedNodes.ToArray();
            PopulateNodes();
            if (selectedObjects != null && insertIndex >= 0 && insertIndex < nodeTransforms.Count)
            {
                selectedObjects.Add(nodeTransforms[insertIndex].gameObject);
                Debug.Log($"Reset selection to new node at index {insertIndex}: {nodeTransforms[insertIndex].name}");
            }
        }
    }

    public void AddNode(Vector3 position, int index = -1)
    {
        if (PrefabManager.DefaultSphereVolume == null)
        {
            Debug.LogError("PrefabManager.DefaultSphereVolume is null. Ensure it’s loaded in Resources/Prefabs/");
            return;
        }

        Transform sphereTransform = ConfigureNode(PrefabManager.DefaultSphereVolume, nodeTransforms.Count, position);

        if (index >= 0 && index <= nodeTransforms.Count)
        {
            nodeTransforms.Insert(index, sphereTransform);
            Debug.Log($"Inserted new PathNode at index {index} (position: {position}).");
        }
        else
        {
            nodeTransforms.Add(sphereTransform);
            Debug.Log($"Added new PathNode to end (position: {position}).");
        }

        UpdatePathData();
        UpdateRoadMarkers();
    }

    private void OnTransformChildrenChanged()
    {
		if (populating) return;
		
		
        Debug.LogError($"OnTransformChildrenChanged called");
        SyncNodeList();
        UpdatePathData();
        UpdateRoadMarkers();
    }

    private void SyncNodeList()
    {
        Transform[] currentNodes = transform.GetComponentsInChildren<Transform>()
            .Where(t => t != transform && t.CompareTag("Node"))
            .ToArray();

        nodeTransforms.Clear();
        nodeTransforms.AddRange(currentNodes);

        foreach (Transform nodeTransform in nodeTransforms)
        {
            if (!nodeTransform.TryGetComponent<PathNode>(out PathNode pathNode))
            {
                pathNode = nodeTransform.gameObject.AddComponent<PathNode>();
                pathNode.Initialize(this);
            }
            pathNode.OnTransformChanged -= HandleNodeTransformChanged;
            pathNode.OnTransformChanged += HandleNodeTransformChanged;
        }
    }

    public void Refresh()
    {
        SyncNodeList();
        UpdatePathData();
        UpdateRoadMarkers();
    }

    private void OnDestroy()
    {
        foreach (var node in nodeTransforms)
        {
            if (node.TryGetComponent<PathNode>(out var pathNode))
                pathNode.OnTransformChanged -= HandleNodeTransformChanged;
        }

        RevertRoadMaterial();
    }

    private void RevertRoadMaterial()
    {
        if (road != null && originalRoadMaterial != null)
        {
            road.SetMaterial(originalRoadMaterial);
            Debug.Log($"Reverted road '{road.GetName()}' material to original from road type.");
        }
        else
        {
            Debug.LogWarning("Could not revert road material: road or originalRoadMaterial is null.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < nodeTransforms.Count - 1; i++)
        {
            if (nodeTransforms[i] != null && nodeTransforms[i + 1] != null)
            {
                Gizmos.DrawLine(nodeTransforms[i].position, nodeTransforms[i + 1].position);
            }
        }
    }

    public IReadOnlyList<Transform> GetNodes()
    {
        return nodeTransforms.AsReadOnly();
    }
}