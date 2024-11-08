using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static WorldSerialization;
using UnityEngine.InputSystem;
	
public class TransformToolManager : MonoBehaviour
{
	public static TransformToolManager Instance { get; private set; }
	
    public GameObject TransformTool;
    public GameObject xPos, yPos, zPos, xPosChild, yPosChild, zPosChild;
    public Camera cam;
	private bool isDragging;
	private Vector2 lastMousePosition;
	
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
	
	public void Hide(){
		foreach (Renderer renderer in TransformTool.GetComponentsInChildren<Renderer>())        {
            renderer.enabled = false;
        }

        foreach (Collider collider in TransformTool.GetComponentsInChildren<Collider>())        {
            collider.enabled = false;
        }
	}
	
	public void Show(){
		foreach (Renderer renderer in TransformTool.GetComponentsInChildren<Renderer>())        {
            renderer.enabled = true;
        }

        foreach (Collider collider in TransformTool.GetComponentsInChildren<Collider>())        {
            collider.enabled = true;
        }
	}
	
	void Update()
	{
		BillboardArrows();
		ScaleTool();
	}
	
	void ScaleTool()
	{
		float distance = Vector3.Distance(TransformTool.transform.position, cam.transform.position);
		
		float minSize = 1f; 
		float maxSize = 80f;  
		float baseScale = 1f;

		float scaleFactor = distance * 0.12f;
		TransformTool.transform.localScale = Vector3.one * scaleFactor;
	}
	
	void BillboardArrows()
	{
		if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        Vector3 camForwardOnY = new Vector3(camForward.x, 0f, camForward.z).normalized;
		Vector3 camForwardOnZ = new Vector3(0f, camForward.y, camForward.z).normalized;
		Vector3 camForwardOnX = new Vector3(camForward.x, camForward.y, 0f).normalized;
		

		if (camForwardOnX.sqrMagnitude < Mathf.Epsilon)		{
			camForwardOnX = Vector3.right; 
		}
		if (camForwardOnY.sqrMagnitude < Mathf.Epsilon)		{
			camForwardOnY = Vector3.up; 
		}
		if (camForwardOnZ.sqrMagnitude < Mathf.Epsilon)		{
			camForwardOnZ = Vector3.forward;
		}
       
		xPos.transform.rotation = Quaternion.LookRotation(camForwardOnX, Vector3.up) * Quaternion.Euler(0, 0, -90);
		yPos.transform.rotation = Quaternion.LookRotation(camForwardOnY, Vector3.up) * Quaternion.Euler(0, 0, 0);
		zPos.transform.rotation = Quaternion.LookRotation(camForwardOnZ, Vector3.up) * Quaternion.Euler(0, -90, 0);
	}
	

}