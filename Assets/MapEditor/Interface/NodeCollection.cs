using UnityEngine;
using EasyRoads3Dv3;
using System.Linq;
using static WorldSerialization;
using System.Collections.Generic;
using UIRecycleTreeNamespace;

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
    private bool needsUpdate;

    private void Awake()
    {
        nodeTransforms = new List<Transform>(1000); // Pre-allocate for typical large road size
        goldMaterial = new Material(Shader.Find("EasyRoads3D/Legacy/Unity 2018+ Standard"));
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
        modularRoad = FindModularRoad();
        if (modularRoad == null)
        {
            Debug.LogError($"NodeCollection on {gameObject.name} has pathData but could not find ERModularRoad in parent hierarchy.");
        }
        Debug.Log($"NodeCollection's pathData set. road: {(road != null ? road.GetName() : "null")}, modularRoad: {(modularRoad != null ? modularRoad.name : "null")}");
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
        try
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

            foreach (Transform node in nodeTransforms)
            {
                if (node != null && node.TryGetComponent<PathNode>(out PathNode pathNode))
                {
                    pathNode.OnTransformChanged -= HandleNodeTransformChanged;
                    pathNode.OnNodeDestroyed -= HandleNodeDestroyed;
                }
            }

            nodeTransforms.Clear();
            for (int i = 0; i < markers.Length; i++)
            {
                Transform sphereTransform = ConfigureNode(PathManager.NodePrefab, i, markers[i], false);
                nodeTransforms.Add(sphereTransform);
            }

            foreach (Transform node in nodeTransforms)
            {
                if (node.TryGetComponent<PathNode>(out PathNode pathNode))
                {
                    pathNode.OnTransformChanged -= HandleNodeTransformChanged;
                    pathNode.OnTransformChanged += HandleNodeTransformChanged;
                    pathNode.OnNodeDestroyed -= HandleNodeDestroyed;
                    pathNode.OnNodeDestroyed += HandleNodeDestroyed;
                }
            }

            needsUpdate = true;
            SetRoadMaterialToGold();
        }
        finally
        {
            populating = false;
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
    }

    private Transform ConfigureNode(GameObject spherePrefab, int index, Vector3 position, bool subscribeEvents = true)
    {
        if (spherePrefab == null)
        {
            Debug.LogError("Sphere prefab is null in ConfigureNode.");
            return null;
        }

        GameObject sphere = Object.Instantiate(spherePrefab, position, Quaternion.identity, transform);
        sphere.name = $"PathNode_{index}";
        sphere.tag = "Node";
        Transform sphereTransform = sphere.transform;
        sphereTransform.localScale = Vector3.one * 5f;
        sphere.layer = 9;

        if (!sphere.TryGetComponent<SphereCollider>(out _))
        {
            SphereCollider collider = sphere.AddComponent<SphereCollider>();
            collider.radius = 2.5f;
            collider.isTrigger = true;
        }

        PathNode pathNode = sphere.AddComponent<PathNode>();
        pathNode.Initialize(this);
        if (subscribeEvents)
        {
            pathNode.OnTransformChanged += HandleNodeTransformChanged;
            pathNode.OnNodeDestroyed += HandleNodeDestroyed;
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

        return firstNode;
    }

    private void HandleNodeTransformChanged()
    {
        if (!populating)
        {
            needsUpdate = true;
        }
    }

    private void RemoveFromTree()
    {
        if (ItemsWindow.Instance == null || ItemsWindow.Instance.tree == null)
        {
            Debug.LogWarning("ItemsWindow or its tree is null. Cannot remove NodeCollection from tree.");
            return;
        }

        Node treeNode = ItemsWindow.Instance.tree.FindFirstNodeByDataRecursive(gameObject);
        if (treeNode != null)
        {
            int childCount = treeNode.nodes.Count;
            treeNode.nodes.Clear();

            bool removed = false;
            if (treeNode.parentNode != null)
            {
                removed = treeNode.parentNode.nodes.RemoveWithoutNotify(treeNode);
            }
            else
            {
                removed = ItemsWindow.Instance.tree.nodes.RemoveWithoutNotify(treeNode);
            }

            if (!removed)
            {
                Debug.LogWarning($"Failed to remove '{gameObject.name}' cleanly from tree.");
            }
        }
        else
        {
            Debug.LogWarning($"NodeCollection '{gameObject.name}' not found in tree.");
        }
    }

    private void HandleNodeDestroyed(PathNode destroyedNode)
    {
        if (destroyedNode == null || !nodeTransforms.Contains(destroyedNode.transform)) return;

        int index = nodeTransforms.IndexOf(destroyedNode.transform);
        nodeTransforms.RemoveAt(index);

        if (nodeTransforms.Count > 0)
        {
            int neighborIndex = index >= nodeTransforms.Count ? index - 1 : index;
            Transform neighborNode = nodeTransforms[neighborIndex];
            if (neighborNode != null)
            {
                CameraManager.Instance.Unselect();
                CameraManager.Instance.SelectPrefabWithoutNotify(neighborNode.gameObject);
            }
            needsUpdate = true;
        }
        else
        {
            Transform roadObject = transform.parent;
            if (roadObject != null)
            {
                CameraManager.Instance.DepopulateNodesForRoad(roadObject.gameObject);
                Object.Destroy(roadObject.gameObject);
            }
            else
            {
                Object.Destroy(gameObject);
            }
            CameraManager.Instance.Unselect();
            //RemoveFromTree();
        }
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
            if (markerCount > newPositions.Length)
            {
                while (markerCount > newPositions.Length)
                    road.DeleteMarker(--markerCount);
            }
            else
            {
                while (markerCount < newPositions.Length)
                    road.AddMarker(newPositions[markerCount++]);
            }
        }

        road.ClosedTrack(false);
        road.SetMarkerPositions(newPositions);
        road.Refresh();
    }

    public void RemoveNode(Transform nodeTransform)
    {
        if (nodeTransforms.Contains(nodeTransform))
        {
            if (nodeTransform.TryGetComponent<PathNode>(out PathNode pathNode))
            {
                pathNode.OnTransformChanged -= HandleNodeTransformChanged;
                pathNode.OnNodeDestroyed -= HandleNodeDestroyed;
            }
            nodeTransforms.Remove(nodeTransform);
            needsUpdate = true;
        }
    }

    public int DetermineInsertIndex(IReadOnlyList<Transform> nodes, Vector3 hitPoint)
    {
        if (nodes.Count == 0) return 0;

        float searchRadius = 15f;
        Collider[] nearbyColliders = Physics.OverlapSphere(hitPoint, searchRadius, 1 << 9);

        if (nearbyColliders.Length == 0)
        {
            return Vector3.Distance(hitPoint, nodes[0].position) < Vector3.Distance(hitPoint, nodes[nodes.Count - 1].position) ? 0 : -1;
        }

        float minDist = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Transform nodeTransform = nearbyColliders[i].transform;
            int index = nodeTransforms.IndexOf(nodeTransform);
            if (index != -1)
            {
                float dist = Vector3.Distance(hitPoint, nodeTransform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = index;
                }
            }
        }

        if (closestIndex == -1)
        {
            return Vector3.Distance(hitPoint, nodes[0].position) < Vector3.Distance(hitPoint, nodes[nodes.Count - 1].position) ? 0 : -1;
        }

        if (closestIndex == 0) return 0;
        if (closestIndex == nodes.Count - 1) return -1;

        float distToPrev = Vector3.Distance(hitPoint, nodes[closestIndex - 1].position);
        float distToNext = Vector3.Distance(hitPoint, nodes[closestIndex + 1].position);
        return distToPrev < distToNext ? closestIndex : closestIndex + 1;
    }

    public void AddNodeAtPosition(Vector3 hitPoint, List<GameObject> selectedObjects = null, float minDistance = 0f)
    {
        if (pathData == null || PrefabManager.DefaultSphereVolume == null)
        {
            Debug.LogError("Cannot add node: PathData or DefaultSphereVolume is null.");
            return;
        }

        Collider[] nearbyColliders = Physics.OverlapSphere(hitPoint, minDistance * 1.25f, 1 << 9);
        foreach (Collider collider in nearbyColliders)
        {
            Transform nodeTransform = collider.transform;
            if (nodeTransforms.Contains(nodeTransform) && Vector3.Distance(hitPoint, nodeTransform.position) < minDistance)
            {
                return;
            }
        }

        populating = true;
        try
        {
            if (selectedObjects != null)
                selectedObjects.Clear();

            int insertIndex = DetermineInsertIndex(GetNodes(), hitPoint);

            Transform sphereTransform = ConfigureNode(PrefabManager.DefaultSphereVolume, insertIndex, hitPoint);
            if (sphereTransform == null)
                return;

            Vector3 offset = PathManager.PathParent.transform.position;
            VectorData newNode = new VectorData { x = hitPoint.x - offset.x, y = hitPoint.y - offset.y, z = hitPoint.z - offset.z };

            if (insertIndex == -1) // Append
            {
                nodeTransforms.Add(sphereTransform);
                List<VectorData> updatedNodes = pathData.nodes?.ToList() ?? new List<VectorData>();
                updatedNodes.Add(newNode);
                pathData.nodes = updatedNodes.ToArray();
            }
            else // Insert at start or middle
            {
                nodeTransforms.Insert(insertIndex, sphereTransform);
                List<VectorData> updatedNodes = pathData.nodes?.ToList() ?? new List<VectorData>();
                updatedNodes.Insert(insertIndex, newNode);
                pathData.nodes = updatedNodes.ToArray();
            }

            if (selectedObjects != null)
                selectedObjects.Add(sphereTransform.gameObject);

            needsUpdate = true;
        }
        finally
        {
            populating = false;
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
        if (sphereTransform == null)
            return;

        if (index >= 0 && index <= nodeTransforms.Count)
        {
            nodeTransforms.Insert(index, sphereTransform);
        }
        else
        {
            nodeTransforms.Add(sphereTransform);
        }

        needsUpdate = true;
    }

    private void OnTransformChildrenChanged()
    {
        if (populating) return;

        Debug.LogError($"OnTransformChildrenChanged called");
        SyncNodeList();
        needsUpdate = true;
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
        needsUpdate = true;
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

    private void OnDestroy()
    {
        RevertRoadMaterial();
        foreach (Transform node in nodeTransforms)
        {
            if (node != null && node.TryGetComponent<PathNode>(out PathNode pathNode))
            {
                pathNode.OnTransformChanged -= HandleNodeTransformChanged;
                pathNode.OnNodeDestroyed -= HandleNodeDestroyed;
            }
        }
    }

    public IReadOnlyList<Transform> GetNodes()
    {
        return nodeTransforms.AsReadOnly();
    }

    private void Update()
    {
        if (needsUpdate && !populating)
        {
            UpdatePathData();
            UpdateRoadMarkers();
            needsUpdate = false;
        }
    }
}