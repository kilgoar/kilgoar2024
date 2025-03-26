using System;
using System.Collections;
using System.Collections.Generic;
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

	public static void ApplyTerrainSmoothing(ERRoad road, float size = 5f, int type = 0)
    {
        if (_roadNetwork == null)
        {
            Debug.LogError("RoadNetwork not initialized.");
            return;
        }

        Terrain terrain = TerrainManager.Land;
        if (terrain == null)
        {
            Debug.LogError("No active terrain found in the scene.");
            return;
        }


            ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
            if (modularRoad == null)
            {
                Debug.LogWarning($"ERModularRoad component not found on road '{road.GetName()}'. Skipping smoothing.");
                return;
            }


            // Smooth step is used internally by TerrainSmooth to alternate between steps
            int smoothStep = 0;

            // Apply smoothing using the EasyRoads3D method
            OCDDDQOQOC.TerrainSmooth(
                terrain: terrain,
                road: modularRoad,
                size: size,
                type: type,
                smoothStep: ref smoothStep
            );

            Debug.Log($"Applied terrain smoothing to road '{road.GetName()}' with size={size}, type={type}");
 
    }




public static void UpdateTerrainHeightmap(ERRoad road, PathData pathData)
{
    if (_roadNetwork == null || TerrainManager.Land == null)
    {
        Debug.LogError("RoadNetwork or terrain not initialized.");
        return;
    }

    Terrain terrain = TerrainManager.Land;
    TerrainData terrainData = terrain.terrainData;
    Vector3 terrainPosition = terrain.transform.position;
    int heightmapRes = TerrainManager.HeightMapRes;

    float outerPadding = Mathf.Max(pathData.outerPadding, 0f);
    float outerFade = Mathf.Max(pathData.outerFade, 0f);
    float innerPadding = Mathf.Max(pathData.innerPadding, 0f);
    float innerFade = Mathf.Max(pathData.innerFade, 0f);
    float terrainOffset = pathData.terrainOffset; // Depth to sink (positive = downward)
    float roadWidth = pathData.width;
    float halfRoadWidth = roadWidth * 0.5f;

    float totalOuterWidth = outerPadding + outerFade;
    float totalInnerWidth = innerPadding + innerFade;
    float totalWidthPerSide = halfRoadWidth + totalInnerWidth + totalOuterWidth;
    float totalWidth = roadWidth + 2f * (totalInnerWidth + totalOuterWidth);

    // Create influence mesh
    GameObject influenceMeshObj = CreateInfluenceMesh(road, pathData);
    if (influenceMeshObj == null)
    {
        Debug.LogError($"Failed to create influence mesh for road '{road.GetName()}'. Aborting heightmap update.");
        return;
    }

    Bounds bounds = influenceMeshObj.GetComponent<MeshCollider>().bounds;
    if (bounds.size == Vector3.zero)
    {
        Debug.LogWarning($"Influence mesh bounds are zero for road '{road.GetName()}'. Using marker bounds.");
        bounds = new Bounds(road.GetMarkerPosition(0), Vector3.one * 10f);
        bounds.Expand(totalWidthPerSide * 2f);
    }

    Vector3 boundsMin = bounds.min - terrainPosition;
    Vector3 boundsMax = bounds.max - terrainPosition;
    float heightmapScale = terrainData.heightmapScale.x;

    int xStartIndex = Mathf.FloorToInt(boundsMin.x / heightmapScale);
    int xEndIndex = Mathf.CeilToInt(boundsMax.x / heightmapScale);
    int zStartIndex = Mathf.FloorToInt(boundsMin.z / heightmapScale);
    int zEndIndex = Mathf.CeilToInt(boundsMax.z / heightmapScale);

    xStartIndex = Mathf.Clamp(xStartIndex, 0, heightmapRes - 1);
    xEndIndex = Mathf.Clamp(xEndIndex, 0, heightmapRes);
    zStartIndex = Mathf.Clamp(zStartIndex, 0, heightmapRes - 1);
    zEndIndex = Mathf.Clamp(zEndIndex, 0, heightmapRes);

    if (xEndIndex <= xStartIndex || zEndIndex <= zStartIndex)
    {
        Debug.LogWarning($"Invalid heightmap region for road '{road.GetName()}': xStart={xStartIndex}, xEnd={xEndIndex}, zStart={zStartIndex}, zEnd={zEndIndex}. Bounds: {bounds}");
        UnityEngine.Object.Destroy(influenceMeshObj);
        return;
    }

    int width = xEndIndex - xStartIndex;
    int height = zEndIndex - zStartIndex;
    float[,] heights = terrainData.GetHeights(xStartIndex, zStartIndex, width, height);

    const int influenceLayer = 30;
    LayerMask layerMask = (1 << influenceLayer);

    float terrainHeight = terrainData.size.y;
    float raycastHeight = terrainHeight + 100f;
    float raycastDistance = raycastHeight + 510f;

    // Define distances from center (in world units)
    float roadCoreEdge = halfRoadWidth; // End of road core
    float innerFadeStart = roadCoreEdge; // Start of inner fade
    float innerFadeEnd = roadCoreEdge + innerFade; // End of inner fade
    float innerPaddingEnd = innerFadeEnd + innerPadding; // End of inner padding
    float outerPaddingStart = innerPaddingEnd; // Start of outer padding
    float outerPaddingEnd = outerPaddingStart + outerPadding; // End of outer padding
    float outerFadeEnd = outerPaddingEnd + outerFade; // End of outer fade

    for (int i = 0; i < width; i++)
    {
        for (int j = 0; j < height; j++)
        {
            Vector3 worldPos = new Vector3(
                terrainPosition.x + (xStartIndex + i) * heightmapScale,
                terrainPosition.y + raycastHeight,
                terrainPosition.z + (zStartIndex + j) * heightmapScale
            );

            RaycastHit hit;
            if (Physics.Raycast(worldPos, Vector3.down, out hit, raycastDistance, layerMask) && hit.collider.gameObject == influenceMeshObj)
            {
                float u = hit.textureCoord.x;
                float baseHeight = (hit.point.y - terrainPosition.y) / terrainHeight;
                float sunkenHeight = (hit.point.y - terrainPosition.y - terrainOffset) / terrainHeight;

                // Convert UV to distance from center (in world units)
                float distanceFromCenter = Mathf.Abs((u - 0.5f) * totalWidth);

                float targetHeight = baseHeight;
                float blendFactor = 0f;

                if (distanceFromCenter <= roadCoreEdge) // Road core
                {
                    targetHeight = sunkenHeight;
                    blendFactor = 1f;
                }
                else if (distanceFromCenter <= innerFadeEnd) // Inner fade
                {
                    float fadeProgress = (distanceFromCenter - innerFadeStart) / (innerFadeEnd - innerFadeStart);
                    targetHeight = Mathf.Lerp(sunkenHeight, baseHeight, fadeProgress);
                    blendFactor = 1f;
                }
                else if (distanceFromCenter <= innerPaddingEnd) // Inner padding
                {
                    targetHeight = baseHeight;
                    blendFactor = 1f;
                }
                else if (distanceFromCenter <= outerPaddingEnd) // Outer padding
                {
                    targetHeight = baseHeight;
                    blendFactor = 1f;
                }
                else if (distanceFromCenter <= outerFadeEnd) // Outer fade
                {
                    float fadeProgress = (distanceFromCenter - outerPaddingEnd) / (outerFadeEnd - outerPaddingEnd);
                    targetHeight = baseHeight;
                    blendFactor = 1f - fadeProgress; // Fade out to original terrain height
                }

                if (blendFactor > 0f)
                {
                    float newHeight = Mathf.Lerp(heights[j, i], targetHeight, blendFactor);
                    heights[j, i] = Mathf.Clamp01(newHeight);
                }
            }
        }
    }

    TerrainManager.RegisterHeightMapUndo(TerrainManager.TerrainType.Land, $"Update Heightmap for '{road.GetName()}'");
    terrainData.SetHeights(xStartIndex, zStartIndex, heights);
    terrain.Flush();
	
	influenceMeshObj.SetActive(false);
    UnityEngine.Object.Destroy(influenceMeshObj);

    Debug.Log($"Updated terrain heightmap for road '{road.GetName()}' with width={roadWidth}, outerPadding={outerPadding}, outerFade={outerFade}, innerPadding={innerPadding}, innerFade={innerFade}, terrainOffset={terrainOffset}");
}


	public static GameObject CreateInfluenceMesh(ERRoad road, PathData pathData)
	{
		ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
		if (modularRoad == null)
		{
			Debug.LogWarning($"ERModularRoad component not found on road '{road.GetName()}'. Using fallback geometry.");
		}

		List<Vector3> roadPoints = new List<Vector3>();
		if (modularRoad != null && modularRoad.soSplinePoints != null && modularRoad.soSplinePoints.Count > 0)
		{
			roadPoints.AddRange(modularRoad.soSplinePoints);
		}
		else
		{
			for (int i = 0; i < road.GetMarkerCount(); i++)
			{
				roadPoints.Add(road.GetMarkerPosition(i));
			}
		}

		if (roadPoints.Count < 2)
		{
			Debug.LogError($"Road '{road.GetName()}' has insufficient points ({roadPoints.Count}) for influence mesh creation.");
			return null;
		}


		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		float accumulatedLength = 0f;

	 
	float outerPadding = Mathf.Max(pathData.outerPadding, 0f);
	float outerFade = Mathf.Max(pathData.outerFade, 0f);
	float innerPadding = Mathf.Max(pathData.innerPadding, 0f);
	float innerFade = Mathf.Max(pathData.innerFade, 0f);
	float roadWidth = pathData.width;
	float halfRoadWidth = roadWidth * 0.5f;

	float totalOuterWidth = outerPadding + outerFade;
	float totalInnerWidth = innerPadding + innerFade;

	float effectiveOuterWidth = totalOuterWidth;
	float totalWidthPerSide = halfRoadWidth + totalInnerWidth + effectiveOuterWidth;

	// UV proportions
	float totalWidth = roadWidth + 2f * (totalInnerWidth + effectiveOuterWidth);
	float uvOuterFade = outerFade / totalWidth;
	float uvOuterPadding = (outerFade + outerPadding) / totalWidth;
	float uvInnerFade = (effectiveOuterWidth + innerFade) / totalWidth;
	float uvInnerPadding = (effectiveOuterWidth + innerFade + innerPadding) / totalWidth;

	List<int> segmentStartIndices = new List<int>();

	for (int i = 0; i < roadPoints.Count; i++)
	{
		Vector3 point = roadPoints[i];
		Vector3 nextPoint = (i < roadPoints.Count - 1) ? roadPoints[i + 1] : point;
		Vector3 direction = (nextPoint - point).normalized;
		if (direction == Vector3.zero) continue;

		Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

		// Vertex positions
		Vector3 leftOuterEdge = point - right * totalWidthPerSide;
		Vector3 leftOuterPadding = point - right * (halfRoadWidth + outerPadding);
		Vector3 leftInnerPadding = point - right * (halfRoadWidth - innerPadding);
		Vector3 leftInnerFade = point - right * (halfRoadWidth - innerPadding - innerFade);
		Vector3 rightInnerFade = point + right * (halfRoadWidth - innerPadding - innerFade);
		Vector3 rightInnerPadding = point + right * (halfRoadWidth - innerPadding);
		Vector3 rightOuterPadding = point + right * (halfRoadWidth + outerPadding);
		Vector3 rightOuterEdge = point + right * totalWidthPerSide;

		leftOuterEdge.y = point.y;
		leftOuterPadding.y = point.y;
		leftInnerPadding.y = point.y;
		leftInnerFade.y = point.y;
		rightInnerFade.y = point.y;
		rightInnerPadding.y = point.y;
		rightOuterPadding.y = point.y;
		rightOuterEdge.y = point.y;

		segmentStartIndices.Add(vertices.Count);

		vertices.Add(leftOuterEdge);
		vertices.Add(leftOuterPadding);
		vertices.Add(leftInnerPadding);
		vertices.Add(leftInnerFade);
		vertices.Add(rightInnerFade);
		vertices.Add(rightInnerPadding);
		vertices.Add(rightOuterPadding);
		vertices.Add(rightOuterEdge);

		if (i > 0) accumulatedLength += Vector3.Distance(point, roadPoints[i - 1]);
		uvs.Add(new Vector2(0f, accumulatedLength));           // Left outer edge
		uvs.Add(new Vector2(uvOuterFade, accumulatedLength));  // Left outer padding (or topo edge if outerTopoWidth is used)
		uvs.Add(new Vector2(uvOuterPadding, accumulatedLength)); // Left inner padding
		uvs.Add(new Vector2(uvInnerFade, accumulatedLength));    // Left inner fade
		uvs.Add(new Vector2(1f - uvInnerFade, accumulatedLength)); // Right inner fade
		uvs.Add(new Vector2(1f - uvOuterPadding, accumulatedLength)); // Right inner padding
		uvs.Add(new Vector2(1f - uvOuterFade, accumulatedLength)); // Right outer padding (or topo edge)
		uvs.Add(new Vector2(1f, accumulatedLength));           // Right outer edge
	}

		for (int i = 0; i < segmentStartIndices.Count - 1; i++)
		{
			int baseIndex = segmentStartIndices[i];
			int nextBaseIndex = segmentStartIndices[i + 1];

			triangles.Add(baseIndex);   triangles.Add(nextBaseIndex);  triangles.Add(baseIndex + 1);
			triangles.Add(baseIndex + 1); triangles.Add(nextBaseIndex);  triangles.Add(nextBaseIndex + 1);
			triangles.Add(baseIndex + 1); triangles.Add(nextBaseIndex + 1); triangles.Add(baseIndex + 2);
			triangles.Add(baseIndex + 2); triangles.Add(nextBaseIndex + 1); triangles.Add(nextBaseIndex + 2);
			triangles.Add(baseIndex + 2); triangles.Add(nextBaseIndex + 2); triangles.Add(baseIndex + 3);
			triangles.Add(baseIndex + 3); triangles.Add(nextBaseIndex + 2); triangles.Add(nextBaseIndex + 3);
			triangles.Add(baseIndex + 3); triangles.Add(nextBaseIndex + 3); triangles.Add(baseIndex + 4);
			triangles.Add(baseIndex + 4); triangles.Add(nextBaseIndex + 3); triangles.Add(nextBaseIndex + 4);
			triangles.Add(baseIndex + 4); triangles.Add(nextBaseIndex + 4); triangles.Add(baseIndex + 5);
			triangles.Add(baseIndex + 5); triangles.Add(nextBaseIndex + 4); triangles.Add(nextBaseIndex + 5);
			triangles.Add(baseIndex + 5); triangles.Add(nextBaseIndex + 5); triangles.Add(baseIndex + 6);
			triangles.Add(baseIndex + 6); triangles.Add(nextBaseIndex + 5); triangles.Add(nextBaseIndex + 6);
			triangles.Add(baseIndex + 6); triangles.Add(nextBaseIndex + 6); triangles.Add(baseIndex + 7);
			triangles.Add(baseIndex + 7); triangles.Add(nextBaseIndex + 6); triangles.Add(nextBaseIndex + 7);
		}

		if (vertices.Count == 0 || triangles.Count == 0)
		{
			Debug.LogError($"Influence mesh for '{road.GetName()}' has no valid vertices or triangles.");
			return null;
		}

		GameObject meshObj = new GameObject($"InfluenceMesh_{road.GetName()}");
		meshObj.transform.SetParent(road.gameObject.transform, false);
		Mesh mesh = new Mesh
		{
			name = $"InfluenceMesh_{road.GetName()}",
			vertices = vertices.ToArray(),
			uv = uvs.ToArray(),
			triangles = triangles.ToArray()
		};
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		MeshFilter filter = meshObj.AddComponent<MeshFilter>();
		filter.sharedMesh = mesh;
		MeshCollider collider = meshObj.AddComponent<MeshCollider>();
		collider.sharedMesh = mesh;
		meshObj.layer = 30;

		return meshObj;
	}

public static void PaintRoadLayers(ERRoad road, WorldSerialization.PathData pathData, float strength = 1f, float outerTopoWidth = 0f, int outerTopology = -1)
{
    Terrain terrain = TerrainManager.Land;
    if (terrain == null) { Debug.LogError("No active terrain found in the scene."); return; }

    ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
    if (modularRoad == null) { Debug.LogError($"ERModularRoad component not found on road '{road.GetName()}'."); return; }

    GameObject influenceMeshObj = CreateSplatTopologyMesh(road, pathData, outerTopoWidth);
    if (influenceMeshObj == null) { Debug.LogError($"Failed to create splat/topology mesh for road '{road.GetName()}'."); return; }

    // Splat and topology maps
    float[,,] groundMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Ground);
    int splatWidth = groundMap.GetLength(0);
    int splatHeight = groundMap.GetLength(1);
    float splatSizeX = terrain.terrainData.size.x / splatWidth;
    float splatSizeZ = terrain.terrainData.size.z / splatHeight;

    int topologyWidth = Mathf.FloorToInt(splatWidth / TerrainManager.SplatRatio);
    int topologyHeight = Mathf.FloorToInt(splatHeight / TerrainManager.SplatRatio);

    int splatIndex = TerrainSplat.TypeToIndex(pathData.splat);
    int topologyIndex = TerrainTopology.TypeToIndex(pathData.topology);
    int outerTopologyIndex = (outerTopology != -1) ? TerrainTopology.TypeToIndex(outerTopology) : -1;

    if (splatIndex < 0 || splatIndex >= TerrainManager.LayerCount(TerrainManager.LayerType.Ground))
    { Debug.LogError($"Splat index {splatIndex} out of range."); UnityEngine.Object.Destroy(influenceMeshObj); return; }
    if (topologyIndex < 0 || topologyIndex >= TerrainTopology.COUNT)
    { Debug.LogError($"Topology index {topologyIndex} out of range."); UnityEngine.Object.Destroy(influenceMeshObj); return; }
    if (outerTopology != -1 && (outerTopologyIndex < 0 || outerTopologyIndex >= TerrainTopology.COUNT))
    { Debug.LogError($"Outer topology index {outerTopologyIndex} out of range."); UnityEngine.Object.Destroy(influenceMeshObj); return; }

    float[,,] topologyMap = TerrainManager.GetSplatMap(TerrainManager.LayerType.Topology, topologyIndex);
    float[,,] outerTopologyMap = (outerTopology != -1) ? TerrainManager.GetSplatMap(TerrainManager.LayerType.Topology, outerTopologyIndex) : null;

    Vector3 terrainPos = terrain.transform.position;
    TerrainManager.RegisterSplatMapUndo($"Paint Road Layers '{road.GetName()}'");

    // Calculate bounds
    Bounds bounds = influenceMeshObj.GetComponent<MeshCollider>().bounds;
    Vector3 boundsMin = bounds.min - terrainPos;
    Vector3 boundsMax = bounds.max - terrainPos;

    int xStartIndex = Mathf.FloorToInt(boundsMin.x / splatSizeX);
    int xEndIndex = Mathf.CeilToInt(boundsMax.x / splatSizeX);
    int zStartIndex = Mathf.FloorToInt(boundsMin.z / splatSizeZ);
    int zEndIndex = Mathf.CeilToInt(boundsMax.z / splatSizeZ);

    xStartIndex = Mathf.Clamp(xStartIndex, 0, splatWidth - 1);
    xEndIndex = Mathf.Clamp(xEndIndex, 0, splatWidth);
    zStartIndex = Mathf.Clamp(zStartIndex, 0, splatHeight - 1);
    zEndIndex = Mathf.Clamp(zEndIndex, 0, splatHeight);

    if (xEndIndex <= xStartIndex || zEndIndex <= zStartIndex)
    {
        Debug.LogWarning($"Invalid splat map region for road '{road.GetName()}': xStart={xStartIndex}, xEnd={xEndIndex}, zStart={zStartIndex}, zEnd={zEndIndex}");
        UnityEngine.Object.Destroy(influenceMeshObj);
        return;
    }

    int width = xEndIndex - xStartIndex;
    int height = zEndIndex - zStartIndex;

    const int influenceLayer = 30;
    LayerMask layerMask = 1 << influenceLayer;
    float rayHeight = terrain.terrainData.size.y + 100f;
    float raycastDistance = rayHeight + 510f;

    float roadWidth = pathData.width;
    float halfRoadWidth = roadWidth * 0.5f;
    float blendWidth = 1f; // 1
    float totalWidth = roadWidth + 2f * (blendWidth + outerTopoWidth);

    // Define zones
    float coreEdge = halfRoadWidth;
    float blendEdge = halfRoadWidth + blendWidth;
    float outerEdge = halfRoadWidth + blendWidth + outerTopoWidth;

    for (int i = 0; i < width; i++)
    {
        for (int j = 0; j < height; j++)
        {
            Vector3 rayOrigin = new Vector3(
                terrainPos.x + (xStartIndex + i) * splatSizeX + splatSizeX * 0.5f,
                terrainPos.y + rayHeight,
                terrainPos.z + (zStartIndex + j) * splatSizeZ + splatSizeZ * 0.5f
            );

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, layerMask))
            {
                float u = hit.textureCoord.x;
                float distanceFromCenter = Mathf.Abs((u - 0.5f) * totalWidth);

                // Core section (splat and internal topology)
                if (distanceFromCenter <= blendEdge)
                {
                    // Splat painting
                    float splatStrength = strength;
                    if (distanceFromCenter > coreEdge)
                    {
                        float fadeProgress = .5f;
                        splatStrength *= fadeProgress;
                    }

                    if (splatStrength > 0f)
                    {
                        int x = zStartIndex + j;
                        int z = xStartIndex + i;
                        float currentGround = groundMap[x, z, splatIndex];
                        float newGroundValue = Mathf.Lerp(currentGround, 1f, splatStrength);
                        groundMap[x, z, splatIndex] = newGroundValue;

                        int groundLayerCount = TerrainManager.LayerCount(TerrainManager.LayerType.Ground);
                        float totalOtherGround = 0f;
                        for (int k = 0; k < groundLayerCount; k++)
                            if (k != splatIndex) totalOtherGround += groundMap[x, z, k];

                        if (totalOtherGround > 0f)
                        {
                            float scale = (1f - newGroundValue) / totalOtherGround;
                            for (int k = 0; k < groundLayerCount; k++)
                                if (k != splatIndex) groundMap[x, z, k] *= scale;
                        }
                    }

                    // Internal topology
                    if (distanceFromCenter <= coreEdge)
                    {
                        int topoX = Mathf.FloorToInt((zStartIndex + j) / TerrainManager.SplatRatio);
                        int topoZ = Mathf.FloorToInt((xStartIndex + i) / TerrainManager.SplatRatio);
                        if (topoX >= 0 && topoX < topologyWidth && topoZ >= 0 && topoZ < topologyHeight)
                        {
                            topologyMap[topoX, topoZ, 0] = 1f;
                            topologyMap[topoX, topoZ, 1] = 0f;
                        }
                    }
                }
                // Outer topology section
                else if (outerTopoWidth > 0f && outerTopology != -1 && distanceFromCenter <= outerEdge)
                {
                    int topoX = Mathf.FloorToInt((zStartIndex + j) / TerrainManager.SplatRatio);
                    int topoZ = Mathf.FloorToInt((xStartIndex + i) / TerrainManager.SplatRatio);
                    if (topoX >= 0 && topoX < topologyWidth && topoZ >= 0 && topoZ < topologyHeight)
                    {
                        outerTopologyMap[topoX, topoZ, 0] = 1f;
                        outerTopologyMap[topoX, topoZ, 1] = 0f;
                    }
                }
            }
        }
    }

    // Apply changes
    TerrainManager.SetSplatMap(groundMap, TerrainManager.LayerType.Ground);
    TerrainManager.SetSplatMap(topologyMap, TerrainManager.LayerType.Topology, topologyIndex);

    bool[,] topologyBitmap = TerrainManager.ConvertSplatToBitmap(topologyMap);
    bool[,] downscaledBitmap = TerrainManager.DownscaleBitmap(topologyBitmap);
    TopologyData.SetTopology(TerrainTopology.IndexToType(topologyIndex), 0, 0, downscaledBitmap.GetLength(0), downscaledBitmap.GetLength(1), downscaledBitmap);

    if (outerTopology != -1 && outerTopologyMap != null)
    {
        TerrainManager.SetSplatMap(outerTopologyMap, TerrainManager.LayerType.Topology, outerTopologyIndex);
        bool[,] outerTopologyBitmap = TerrainManager.ConvertSplatToBitmap(outerTopologyMap);
        bool[,] outerDownscaledBitmap = TerrainManager.DownscaleBitmap(outerTopologyBitmap);
        TopologyData.SetTopology(TerrainTopology.IndexToType(outerTopologyIndex), 0, 0, outerDownscaledBitmap.GetLength(0), outerDownscaledBitmap.GetLength(1), outerDownscaledBitmap);
        TerrainManager.Callbacks.InvokeLayerUpdated(TerrainManager.LayerType.Topology, outerTopologyIndex);
    }

    TerrainManager.Callbacks.InvokeLayerUpdated(TerrainManager.LayerType.Ground, 0);
    TerrainManager.Callbacks.InvokeLayerUpdated(TerrainManager.LayerType.Topology, topologyIndex);

    influenceMeshObj.SetActive(false);
    UnityEngine.Object.Destroy(influenceMeshObj);

    Debug.Log($"Painted road layers for '{road.GetName()}': splat={pathData.splat} (index {splatIndex}), topology={pathData.topology} (index {topologyIndex}), outerTopoWidth={outerTopoWidth}, outerTopology={(outerTopology != -1 ? outerTopology.ToString() : "None")}, strength={strength}");
}
    private static void DestroySplatMeshes(List<GameObject> splatMeshList)
    {
        if (splatMeshList != null)
        {
            foreach (GameObject obj in splatMeshList)
            {
                if (obj != null) UnityEngine.Object.Destroy(obj);
            }
        }
    }


	public static void SpawnPath(PathData pathData, bool fresh = false)
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
		if(!fresh){
			ConfigureRoad(newRoad, pathData);
		}
		else{
			ConfigureNewRoad(newRoad, pathData);
		}

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
	
	public static GameObject CreatePathAtPosition(Vector3 startPosition)
    {
        if (_roadNetwork == null)
        {
            Debug.LogError("RoadNetwork not initialized.");
            return null;
        }

        // Define two initial nodes
		Vector3 offset = PathParent.transform.position;
        Vector3 firstNodePosition = startPosition - offset;
        Vector3 secondNodePosition = startPosition + (Vector3.right * 10f) - offset; // Default offset along X-axis


		
        // Build newPathData from PathWindow UI fields
        PathData newPathData = new PathData
        {
            width = float.TryParse(PathWindow.Instance.widthField.text, out float width) ? width : 10f,
            innerPadding = float.TryParse(PathWindow.Instance.innerPaddingField.text, out float innerPadding) ? innerPadding : 1f,
            outerPadding = float.TryParse(PathWindow.Instance.outerPaddingField.text, out float outerPadding) ? outerPadding : 1f,
            innerFade = float.TryParse(PathWindow.Instance.innerFadeField.text, out float innerFade) ? innerFade : 1f,
            outerFade = float.TryParse(PathWindow.Instance.outerFadeField.text, out float outerFade) ? outerFade : 8f,
            splat = (int)PathWindow.Instance.splatEnums[PathWindow.Instance.splatDropdown.value],
            topology = (int)PathWindow.Instance.topologyEnums[PathWindow.Instance.topologyDropdown.value],
            spline = false, // Default, could add a UI toggle if needed
            terrainOffset = 0f, // Default, could add a UI field if needed
            start = false, // Default
            end = false, // Default
            nodes = new VectorData[]
            {
                new VectorData { x = firstNodePosition.x, y = firstNodePosition.y, z = firstNodePosition.z },
                new VectorData { x = secondNodePosition.x, y = secondNodePosition.y, z = secondNodePosition.z }
            }
        };

        // Determine road type and name from template or defaults
        string roadTypePrefix = InferRoadTypePrefix(newPathData);
        newPathData.name = $"{roadTypePrefix} {_roadIDCounter++}";

        // Spawn the path
        SpawnPath(newPathData);
        GameObject newRoadObject = CurrentMapPaths.Last().gameObject;

        Debug.Log($"Created new road '{newPathData.name}' with nodes at {firstNodePosition} and {secondNodePosition}");

        return newRoadObject;
    }
	
	private static string InferRoadTypePrefix(PathData pathData)
	{
		// Log for debugging
		Debug.Log($"InferRoadTypePrefix: topology={pathData.topology}, width={pathData.width}, splat={pathData.splat}");

		// Check topology using TerrainTopology.TypeToIndex for consistency
		int riverIndex = TerrainTopology.TypeToIndex((int)TerrainTopology.Enum.River);
		int railIndex = TerrainTopology.TypeToIndex((int)TerrainTopology.Enum.Rail);
		int roadIndex = TerrainTopology.TypeToIndex((int)TerrainTopology.Enum.Road);

		if (pathData.topology == riverIndex)
		{
			return "River";
		}
		else if (pathData.width == 0f)
		{
			return "Powerline";
		}
		else if (pathData.topology == railIndex)
		{
			return "Rail";
		}
		else if (pathData.topology == roadIndex || pathData.topology == TerrainTopology.TypeToIndex((int)TerrainTopology.Enum.Field)) // Assuming trails might use Field or Road
		{
			if (pathData.width == 4f)
				return "Road"; // Trails are treated as invisible roads but named "Road" here
			else if (pathData.width == 12f)
				return "Road"; // CircleRoad named as "Road" with width distinction
			else
				return "Road"; // Default road
		}

		return "Road";
	}

    public static void ReconfigureRoad(ERRoad road, PathData pathData)
    {
        if (road == null || pathData == null)
        {
            Debug.LogError("Road or PathData is null in ReconfigureRoad.");
            return;
        }

        road.SetWidth(pathData.width);

        ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
        if (modularRoad == null)
        {
            Debug.LogError($"ERModularRoad component not found on road '{pathData.name}'.");
            return;
        }

        modularRoad.roadWidth = pathData.width;
        modularRoad.indent = pathData.innerPadding;
        modularRoad.surrounding = pathData.outerPadding;
        modularRoad.fadeInDistance = pathData.innerFade;
        modularRoad.fadeOutDistance = pathData.outerFade;
        modularRoad.splatIndex = pathData.splat;

        // Refresh the road to apply changes
        road.Refresh();
    }

    public static void ConfigureNewRoad(ERRoad road, PathData pathData)
    {
        if (road == null || pathData == null)
        {
            Debug.LogError("Road or PathData is null in ConfigureNewRoad.");
            return;
        }

        if (PathWindow.Instance == null)
        {
            Debug.LogError("PathWindow.Instance is null. Cannot access RoadType for new road configuration.");
            return;
        }

        // Get the selected RoadType directly from the dropdown
        PathWindow.RoadType selectedRoadType = PathWindow.Instance.roadTypeEnums[PathWindow.Instance.roadTypeDropdown.value];

        // Set basic road properties directly from PathData
		
        road.SetName(pathData.name);
        road.SetWidth(pathData.width);
        road.SetMarkerControlType(0, pathData.spline ? ERMarkerControlType.Spline : ERMarkerControlType.StraightXZ);
        road.ClosedTrack(false);
		


        // Get the ERModularRoad component
        ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
        if (modularRoad == null)
        {
            Debug.LogError($"ERModularRoad component not found on road '{pathData.name}'.");
            return;
        }

        // Apply all settings from PathData directly
        modularRoad.roadWidth = pathData.width;
        modularRoad.indent = pathData.innerPadding;
        modularRoad.surrounding = pathData.outerPadding;
        modularRoad.fadeInDistance = pathData.innerFade;
        modularRoad.fadeOutDistance = pathData.outerFade;
        modularRoad.splatIndex = pathData.splat;
        modularRoad.terrainContoursOffset = pathData.terrainOffset;
        modularRoad.startConnectionFlag = pathData.start;
        modularRoad.endConnectionFlag = pathData.end;

        // Get available road types from the network
        ERRoadType[] roadTypes = _roadNetwork.GetRoadTypes();
        int roadTypeIndex = (roadTypes != null && roadTypes.Length > 1) ? 1 : 0; // Default to visible style
        bool isVisible = true;

        // Configure visibility and lanes based on the explicit RoadType from the dropdown
        switch (selectedRoadType)
        {
            case PathWindow.RoadType.River:
            case PathWindow.RoadType.Powerline:
            case PathWindow.RoadType.Trail: // Trails are invisible as per your comment
                isVisible = false;
                roadTypeIndex = (roadTypes != null && roadTypes.Length > 0) ? 0 : 0; // Transparent style
                modularRoad.lanes = 0; // No lanes for invisible types
                break;
            case PathWindow.RoadType.Road:
                modularRoad.lanes = pathData.width >= 12 ? 2 : 1; // Adjust lanes based on width
                break;
            case PathWindow.RoadType.CircleRoad:
                modularRoad.lanes = 2; // CircleRoad always 2 lanes
                break;
            case PathWindow.RoadType.Rail:
                modularRoad.lanes = 0; // Rail has no lanes
                break;
        }

        // Apply the road type (visible or invisible)
        if (roadTypes != null && roadTypes.Length > roadTypeIndex)
        {
            road.SetRoadType(roadTypes[roadTypeIndex]);
        }
		
		road.SetTerrainDeformation(true);

        // Ensure width is set again after road type (some road types may override it)
        road.SetWidth(pathData.width);
        modularRoad.roadWidth = pathData.width;

        // Refresh the road to apply changes
        road.Refresh();

        Debug.Log($"Configured new road '{pathData.name}' with width={pathData.width}, splat={pathData.splat}, topology={pathData.topology}, visible={isVisible}, roadType={selectedRoadType}");
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
					road.SetRoadType(roadTypes[1]); // Visible style
					// Make trails invisible
					if (pathData.width == 4f)
					{
						isVisible = false;
						road.SetRoadType(roadTypes[0]); // Transparent style for trails
					}
					modularRoad.lanes = pathData.width >= 12 ? 2 : 1;
				}
				break;

            case "rail":
                if (roadTypes != null && roadTypes.Length > 1)
                {
                    road.SetRoadType(roadTypes[1]); // Visible style
                    //modularRoad.roadWidth = 2f; // Fixed width for rails
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

		road.SetWidth(pathData.width);
        modularRoad.roadWidth = pathData.width;
        modularRoad.indent = pathData.innerPadding;
        modularRoad.surrounding = pathData.outerPadding;
        modularRoad.fadeInDistance = pathData.innerFade;
        modularRoad.fadeOutDistance = pathData.outerFade;
        modularRoad.terrainContoursOffset = pathData.terrainOffset;
        modularRoad.splatIndex = pathData.splat;
        modularRoad.startConnectionFlag = pathData.start;
        modularRoad.endConnectionFlag = pathData.end;
		
		road.Refresh();
    }

	public static GameObject CreateSplatTopologyMesh(ERRoad road, PathData pathData, float outerTopoWidth)
	{
		ERModularRoad modularRoad = road.gameObject.GetComponent<ERModularRoad>();
		if (modularRoad == null)
		{
			Debug.LogWarning($"ERModularRoad component not found on road '{road.GetName()}'. Using fallback geometry.");
		}

		// Gather road points
		List<Vector3> roadPoints = new List<Vector3>();
		if (modularRoad != null && modularRoad.soSplinePoints != null && modularRoad.soSplinePoints.Count > 0)
		{
			roadPoints.AddRange(modularRoad.soSplinePoints);
		}
		else
		{
			for (int i = 0; i < road.GetMarkerCount(); i++)
			{
				roadPoints.Add(road.GetMarkerPosition(i));
			}
		}

		if (roadPoints.Count < 2)
		{
			Debug.LogError($"Road '{road.GetName()}' has insufficient points ({roadPoints.Count}) for splat/topology mesh creation.");
			return null;
		}

		// Mesh data
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		float accumulatedLength = 0f;

		// Dimensions
		float roadWidth = pathData.width;
		float halfRoadWidth = roadWidth * 0.5f;
		float blendWidth = 1f; 
		float totalWidthPerSide = halfRoadWidth + blendWidth + outerTopoWidth;
		float totalWidth = roadWidth + 2f * (blendWidth + outerTopoWidth);

		// UV proportions
		float uvBlendStart = (halfRoadWidth) / totalWidth;
		float uvBlendEnd = (halfRoadWidth + blendWidth) / totalWidth;
		float uvOuterEnd = 1f;

		List<int> segmentStartIndices = new List<int>();

		for (int i = 0; i < roadPoints.Count; i++)
		{
			Vector3 point = roadPoints[i];
			Vector3 nextPoint = (i < roadPoints.Count - 1) ? roadPoints[i + 1] : point;
			Vector3 direction = (nextPoint - point).normalized;
			if (direction == Vector3.zero) continue;

			Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

			// Vertex positions for three sections per side
			Vector3 leftOuterEdge = point - right * totalWidthPerSide;
			Vector3 leftBlendEdge = point - right * (halfRoadWidth + blendWidth);
			Vector3 leftCoreEdge = point - right * halfRoadWidth;
			Vector3 rightCoreEdge = point + right * halfRoadWidth;
			Vector3 rightBlendEdge = point + right * (halfRoadWidth + blendWidth);
			Vector3 rightOuterEdge = point + right * totalWidthPerSide;

			// Set Y to match road height
			leftOuterEdge.y = point.y;
			leftBlendEdge.y = point.y;
			leftCoreEdge.y = point.y;
			rightCoreEdge.y = point.y;
			rightBlendEdge.y = point.y;
			rightOuterEdge.y = point.y;

			segmentStartIndices.Add(vertices.Count);

			// Add vertices
			vertices.Add(leftOuterEdge);  // 0: Left outer edge
			vertices.Add(leftBlendEdge);  // 1: Left blend edge
			vertices.Add(leftCoreEdge);   // 2: Left core edge
			vertices.Add(rightCoreEdge);  // 3: Right core edge
			vertices.Add(rightBlendEdge); // 4: Right blend edge
			vertices.Add(rightOuterEdge); // 5: Right outer edge

			// UVs
			if (i > 0) accumulatedLength += Vector3.Distance(point, roadPoints[i - 1]);
			uvs.Add(new Vector2(0f, accumulatedLength));           // Left outer edge
			uvs.Add(new Vector2(uvBlendStart, accumulatedLength)); // Left blend edge
			uvs.Add(new Vector2(uvBlendEnd, accumulatedLength));   // Left core edge
			uvs.Add(new Vector2(1f - uvBlendEnd, accumulatedLength)); // Right core edge
			uvs.Add(new Vector2(1f - uvBlendStart, accumulatedLength)); // Right blend edge
			uvs.Add(new Vector2(1f, accumulatedLength));           // Right outer edge
		}

		// Generate triangles
		for (int i = 0; i < segmentStartIndices.Count - 1; i++)
		{
			int baseIndex = segmentStartIndices[i];
			int nextBaseIndex = segmentStartIndices[i + 1];

			triangles.Add(baseIndex);   triangles.Add(nextBaseIndex);  triangles.Add(baseIndex + 1);
			triangles.Add(baseIndex + 1); triangles.Add(nextBaseIndex);  triangles.Add(nextBaseIndex + 1);
			triangles.Add(baseIndex + 1); triangles.Add(nextBaseIndex + 1); triangles.Add(baseIndex + 2);
			triangles.Add(baseIndex + 2); triangles.Add(nextBaseIndex + 1); triangles.Add(nextBaseIndex + 2);
			triangles.Add(baseIndex + 2); triangles.Add(nextBaseIndex + 2); triangles.Add(baseIndex + 3);
			triangles.Add(baseIndex + 3); triangles.Add(nextBaseIndex + 2); triangles.Add(nextBaseIndex + 3);
			triangles.Add(baseIndex + 3); triangles.Add(nextBaseIndex + 3); triangles.Add(baseIndex + 4);
			triangles.Add(baseIndex + 4); triangles.Add(nextBaseIndex + 3); triangles.Add(nextBaseIndex + 4);
			triangles.Add(baseIndex + 4); triangles.Add(nextBaseIndex + 4); triangles.Add(baseIndex + 5);
			triangles.Add(baseIndex + 5); triangles.Add(nextBaseIndex + 4); triangles.Add(nextBaseIndex + 5);
		}

		if (vertices.Count == 0 || triangles.Count == 0)
		{
			Debug.LogError($"Splat/topology mesh for '{road.GetName()}' has no valid vertices or triangles.");
			return null;
		}

		// Create mesh object
		GameObject meshObj = new GameObject($"SplatTopologyMesh_{road.GetName()}");
		meshObj.transform.SetParent(road.gameObject.transform, false);
		Mesh mesh = new Mesh
		{
			name = $"SplatTopologyMesh_{road.GetName()}",
			vertices = vertices.ToArray(),
			uv = uvs.ToArray(),
			triangles = triangles.ToArray()
		};
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		MeshFilter filter = meshObj.AddComponent<MeshFilter>();
		filter.sharedMesh = mesh;
		MeshCollider collider = meshObj.AddComponent<MeshCollider>();
		collider.sharedMesh = mesh;
		meshObj.layer = 30; // Influence layer

		Debug.Log($"Created splat/topology mesh for '{road.GetName()}': {vertices.Count} vertices, bounds: {mesh.bounds}, outerTopoWidth: {outerTopoWidth}");
		return meshObj;
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