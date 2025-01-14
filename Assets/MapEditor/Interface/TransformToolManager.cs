using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static WorldSerialization;
using UnityEngine.InputSystem;
using RTG;

public class TransformToolManager : MonoBehaviour
{
	/*
    public static TransformToolManager Instance { get; private set; }
    
    private ObjectTransformGizmo gizmo; // RTG Gizmo component

    private void Start()
    {
        SetupGizmo();
    }

    private void SetupGizmo()
    {
        if (RTGizmosEngine.Get != null)
        {
            gizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
            if (gizmo != null)
            {
                gizmo.SetTargetObject(this.gameObject);
            }
            else
            {
                Debug.LogError("Failed to create gizmo.");
            }
        }
        else
        {
            Debug.LogError("RTGizmosEngine.Get is null.");
        }
    }

    public static void ToggleTransformTool(bool show)
    {
        if (Instance != null)
        {
            if (show)
                Instance.Show();
            else
                Instance.Hide();
        }
        else
        {
            Debug.LogError("TransformToolManager instance not found.");
        }
    }
    
    public void Hide()
    {
        if (gizmo != null)
        {
            gizmo.SetEnabled(false);
        }
        else
        {
            Debug.LogWarning("Gizmo is null when trying to hide.");
        }
    }
    
    public void Show()
    {
        if (gizmo != null)
        {
            gizmo.SetEnabled(true);
        }
        else
        {
            Debug.LogWarning("Gizmo is null when trying to show.");
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
	*/
}