using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;

public abstract class LODComponent : RenderableComponent
{
    public Material sharedMaterial;  // If all LOD levels share the same base material

	private float distance, oldDistance;
	private int newLevel = -1;
		
    protected LODState[] States;
    protected int currentLODLevel = -1;

       private Coroutine lodCoroutine;

		protected virtual void Awake() 
		{
		}

		protected virtual void Start()
		{
		}

		protected abstract void CheckLOD(float distance);


		protected virtual int CalculateLODLevel(float distance)
		{
			if (States.Length == 0) return -1;  

			if (distance >= States[States.Length - 1].distance)
			{
				return States.Length - 1;  
			}

			for (int i = 0; i < States.Length - 1; i++)
			{
				if (distance >= States[i].distance && distance < States[i + 1].distance)
				{
					return i;  
				}
			}

			return -1; // This should not occur due to the previous checks
		}

		protected virtual void UpdateLOD(int newLevel)
		{
			if (States!=null){
				
				if (newLevel < 0 || newLevel >= States.Length) return;
				

				// Hide the current LOD state
				if (currentLODLevel != -1)
				{
					States[currentLODLevel].Hide();
				}
				// Show the new LOD state
				States[newLevel].Show();
				currentLODLevel = newLevel;
			}
		}

		public virtual void RefreshLOD()
		{
			distance = Vector3.Distance(transform.position, CameraManager.Instance.position);
			CheckLOD(distance);
		}
		
		// Inner class for representing LOD states
		public class LODState
		{
			public float distance;
			public MeshFilter meshFilter;
			public Renderer renderer;
			public Mesh mesh;
			public bool isVisible;

			public void Show()
			{
				if (!isVisible)
				{
					if (renderer != null)
					{
						renderer.enabled = true;
					}
					if (meshFilter != null && mesh != null)
					{
						meshFilter.mesh = mesh;
					}
					isVisible = true;
				}
			}

			public void Hide()
			{
				if (isVisible)
				{
					if (renderer != null)
					{
						renderer.enabled = false;
					}
					isVisible = false;
				}
			}
		}
}