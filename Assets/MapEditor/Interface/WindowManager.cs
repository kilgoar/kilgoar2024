using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;

public class WindowManager : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    Vector3 mouseMovement = new Vector3(0f, 0f, 0f);
    public Toggle lockToggle;
    public GameObject WindowsPanel;
    public GameObject Window;
    public UIRecycleTree Tree;
    public bool isRescaling = false;
    public Button rescaleButton;

    private RectTransform windowRectTransform;
    private RectTransform treeRectTransform;
    private Canvas parentCanvas;

    private void Start()
    {
		GameObject canvasObj = GameObject.FindWithTag("AppCanvas");
        parentCanvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null; // Tag search
        if (parentCanvas == null) Debug.LogWarning("No Canvas with tag 'AppCanvas' found.");
		
		windowRectTransform = Window.GetComponent<RectTransform>();
	
        if (Tree != null)
        {

            treeRectTransform = Tree.GetComponent<RectTransform>();
            Tree.onNodeSelected.AddListener(SetFocus);
            SyncTreeWithWindow();
        }
    }

    public void SetFocus(Node node)
    {
        transform.SetAsLastSibling();
        WindowsPanel.transform.SetAsLastSibling();
        SyncTreeWithWindow();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!lockToggle.isOn)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rescaleButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
            {
                isRescaling = true;
                Rescale(eventData);
                SyncTreeWithWindow();
            }
            else
            {
                isRescaling = false;
                Vector3 newPos = transform.localPosition + new Vector3(eventData.delta.x, eventData.delta.y, 0f);
                transform.localPosition = ClampToCanvas(newPos);
            }

            transform.SetAsLastSibling();
            WindowsPanel.transform.SetAsLastSibling();
            SyncTreeWithWindow();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (lockToggle.isOn || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (isRescaling)
        {
            Rescale(eventData);
            return;
        }

        Vector3 newPos = transform.localPosition + new Vector3(eventData.delta.x, eventData.delta.y, 0f);
        transform.localPosition = ClampToCanvas(newPos);
        SyncTreeWithWindow();
        SaveState();
    }

    public void Rescale(PointerEventData eventData)
    {
        RectTransform rectTransform = Window.GetComponent<RectTransform>();
        
        float scaleChange = eventData.delta.x * 0.004f + eventData.delta.y * -0.004f;
        Vector3 newScale = rectTransform.localScale + new Vector3(scaleChange, scaleChange, 0f);
        
        newScale.x = Mathf.Clamp(newScale.x, 0.6f, 3f);
        newScale.y = Mathf.Clamp(newScale.y, 0.6f, 3f);
        
        rectTransform.localScale = newScale;

        Vector2 newMousePosition = eventData.position;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, newMousePosition, eventData.pressEventCamera, out localPoint);
        Vector2 adjustedSize = rectTransform.sizeDelta * rectTransform.localScale;
        Vector2 offset = new Vector2((adjustedSize.x / 2) - 5f, (adjustedSize.y / -2) + 5f);
        rectTransform.localPosition = ClampToCanvas(localPoint - offset);
        SyncTreeWithWindow();
        SaveState();
    }

    private void SaveState()
    {
        if (AppManager.Instance != null)
        {
            AppManager.Instance.SaveWindowStates();
        }
    }

    private void SyncTreeWithWindow()
    {
        if (treeRectTransform != null && windowRectTransform != null)
        {
            treeRectTransform.position = windowRectTransform.position;
            treeRectTransform.localScale = windowRectTransform.localScale;
            int windowSiblingIndex = Window.transform.GetSiblingIndex();
            Tree.transform.SetSiblingIndex(windowSiblingIndex + 1);
        }
    }

	public Vector3 ClampToCanvas(Vector3 newPos)
	{
		if (parentCanvas == null) return newPos;

		RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
		Vector2 size = windowRectTransform.sizeDelta * windowRectTransform.localScale;
		Vector2 halfSize = size * 0.5f;
		
		Vector2 canvasSize = canvasRect.sizeDelta;
		Vector2 halfCanvas = canvasSize * 0.5f;
		Vector2 pivot = windowRectTransform.pivot; // Get the pivot (e.g., (0.5, 0.5))

		// Calculate pivot-adjusted bounds
		Vector2 minPos = new Vector2(size.x * pivot.x, size.y * pivot.y);
		Vector2 maxPos = new Vector2(canvasSize.x - size.x * (1f - pivot.x), 
									 canvasSize.y - size.y * (1f - pivot.y));

		newPos.x = Mathf.Clamp(newPos.x, minPos.x-halfCanvas.x, maxPos.x-halfCanvas.x);
		newPos.y = Mathf.Clamp(newPos.y, minPos.y-halfCanvas.y, maxPos.y-halfCanvas.y);
		newPos.z = 0f;

		return newPos;
	}
}