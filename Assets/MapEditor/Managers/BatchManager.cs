using System.Collections.Generic;
using UnityEngine;

public static class BatchManager
{
	/*
    private static Dictionary<(Mesh, Material), List<RendererBatch>> batches = new Dictionary<(Mesh, Material), List<RendererBatch>>();
    private static Dictionary<(Mesh, Material), ComputeBuffer> instanceBuffers = new Dictionary<(Mesh, Material), ComputeBuffer>();
    private static Dictionary<(Mesh, Material), ComputeBuffer> argsBuffers = new Dictionary<(Mesh, Material), ComputeBuffer>();
    private static HashSet<(Mesh, Material)> dirtyBatches = new HashSet<(Mesh, Material)>();
	*/
	
	public static GameObject EmptyLOD;
	public static Renderer collapsedRenderer;
	public static MeshFilter collapsedFilter;
	
    
    static BatchManager()
    {
        EmptyLOD = GameObject.FindGameObjectWithTag("EmptyLOD");

        if (EmptyLOD == null)
        {
            Debug.LogError("No GameObject with the tag 'EmptyLOD' found in the scene.");
            return;
        }
        EmptyLOD.AddComponent<MeshRenderer>();
        EmptyLOD.AddComponent<MeshFilter>();
		
		collapsedRenderer = EmptyLOD.GetComponent<MeshRenderer>();
        collapsedFilter = EmptyLOD.GetComponent<MeshFilter>();
    }
	
	/*
    public static void RegisterBatch(RendererBatch rendererBatch)
    {
        var mesh = rendererBatch.GetMeshFilter()?.sharedMesh;
        var material = rendererBatch.GetMeshRenderer()?.sharedMaterial;
        var key = (mesh, material);

        if (key.Item1 == null || key.Item2 == null) 
        {
            Debug.LogWarning($"Mesh or Material is null for {rendererBatch.gameObject.name}. Cannot batch.");
            return;
        }

        if (!batches.ContainsKey(key))
        {
            batches.Add(key, new List<RendererBatch>());
            SetupBuffers(key);
        }
        batches[key].Add(rendererBatch);
        dirtyBatches.Add(key); // Mark as dirty since a new object was added
    }

    public static void UnregisterBatch(RendererBatch rendererBatch)
    {
        var mesh = rendererBatch.GetMeshFilter()?.sharedMesh;
        var material = rendererBatch.GetMeshRenderer()?.sharedMaterial;
        var key = (mesh, material);

        if (batches.TryGetValue(key, out var batchList))
        {
            batchList.Remove(rendererBatch);
            dirtyBatches.Add(key); // Mark as dirty since an object was removed
            if (batchList.Count == 0)
            {
                ReleaseBuffers(key);
                batches.Remove(key);
            }
        }
    }

    public static void InstanceDataChanged(RendererBatch rendererBatch)
    {
        var mesh = rendererBatch.GetMeshFilter()?.sharedMesh;
        var material = rendererBatch.GetMeshRenderer()?.sharedMaterial;
        var key = (mesh, material);

        if (batches.TryGetValue(key, out var _))
        {
            dirtyBatches.Add(key); // Mark as dirty since data has changed
        }
    }

    private static void SetupBuffers((Mesh, Material) key)
    {
        var mesh = key.Item1;
        var material = key.Item2;

        argsBuffers[key] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { mesh.GetIndexCount(0), 0, mesh.GetIndexStart(0), mesh.GetBaseVertex(0), 0 };
        argsBuffers[key].SetData(args);

        instanceBuffers[key] = new ComputeBuffer(1023, InstanceData.Size()); // Pre-allocate for max instances or dynamically resize
    }

    private static void ReleaseBuffers((Mesh, Material) key)
    {
        if (argsBuffers.TryGetValue(key, out var argsBuffer))
        {
            argsBuffer.Release();
            argsBuffers.Remove(key);
        }

        if (instanceBuffers.TryGetValue(key, out var instanceBuffer))
        {
            instanceBuffer.Release();
            instanceBuffers.Remove(key);
        }
    }

    public static void UpdateAndRender()
    {
        foreach (var key in dirtyBatches)
        {
            UpdateBatch(key);
        }
        dirtyBatches.Clear();

        Render();
    }

	public struct InstanceData
	{
		public Matrix4x4 WorldMatrix;
		// Other properties as needed

		public InstanceData(Matrix4x4 worldMatrix)
		{
			WorldMatrix = worldMatrix;
		}

		public static int Size()
		{
			return sizeof(float) * 16; // Size for Matrix4x4
		}
	}

	public static void UpdateBatch((Mesh, Material) key)
	{
		if (batches.TryGetValue(key, out var batchList))
		{
			var instanceData = new InstanceData[batchList.Count];
			for (int i = 0; i < batchList.Count; i++)
			{
				// Use the transform of the game object to get the correct world matrix
				instanceData[i] = new InstanceData(batchList[i].transform.localToWorldMatrix);
			}
			instanceBuffers[key].SetData(instanceData);

			uint[] args = new uint[5];
			argsBuffers[key].GetData(args);
			args[1] = (uint)batchList.Count; // Update instance count
			argsBuffers[key].SetData(args);
		}
	}

    public static void Render()
    {
		Debug.Log(batches.Count + " batches rendering...");
        foreach (var batch in batches)
        {
            var key = batch.Key;
            var material = key.Item2;

            if (material == null) continue; // Skip if material is null

            material.SetBuffer("instanceData", instanceBuffers[key]);
            Graphics.DrawMeshInstancedIndirect(key.Item1, 0, key.Item2, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffers[key]);
        }
    }
	*/
}
