using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class TreeLOD : LODComponent
{
    // Public field for LOD states specific to trees
    public TreeLODState[] States;

    //private float oldDistance;
    //private int newLevel = -1;
    //private int currentLODLevel = -1;

    
    protected override void Awake()
    {
    }

    protected override void Start()
    {
        // Ensure all states are initialized properly
        if (States == null || States.Length == 0)
        {
            Debug.LogError($"{gameObject.name} does not have LOD States set up!");
            return;
        }

        RefreshLOD(); // Initial LOD refresh
    }

    protected override void CheckLOD(float distance)
    {
        newLevel = CalculateLODLevel(distance);
        if (newLevel != currentLODLevel)
        {
            UpdateLOD(newLevel);
            oldDistance = distance;
        }
    }

	protected override int CalculateLODLevel(float distance)
	{
		if (States != null) // Replace States with TreeLODStates
		{
			if (States.Length == 0) return 0;

			// Check if the distance exceeds the last LOD state's distance
			if (distance >= States[States.Length - 1].distance)
			{
				return States.Length - 1;
			}

			// Iterate through the LOD states and find the appropriate one
			for (int i = 0; i < States.Length - 1; i++)
			{
				if (distance >= States[i].distance && distance < States[i + 1].distance)
				{
					return i;
				}
			}
		}
		return -1; // Default fallback, shouldn't occur
	}

    protected override void UpdateLOD(int newLevel)
    {
        if (newLevel < 0 || newLevel >= States.Length) return;

        if (currentLODLevel != -1)
        {
            States[currentLODLevel].Hide();
        }
        States[newLevel].Show();
        currentLODLevel = newLevel;
    }

    public override void RefreshLOD()
    {
        CheckLOD(Vector3.Distance(transform.position, CameraManager.Instance.position));
    }

    // Nested class for managing tree-specific LOD states
    [System.Serializable]
    public class TreeLODState : LODState
    {
		//public float distance;
		//public Renderer renderer;
        //public bool isVisible;

        public void Show()
        {
			if (renderer != null)
					{
			renderer.enabled=true;
					}
        }

        public void Hide()
        {
			if (renderer != null)
					{
			renderer.enabled=false;
					}
        }
    }
}