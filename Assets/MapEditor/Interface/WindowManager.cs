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
	Vector3 mouseMovement = new Vector3 (0f,0f,0f);
	public Toggle lockToggle;
	public GameObject WindowsPanel;
	public GameObject Window;
	public UIRecycleTree Tree;
	public bool isRescaling = false;
	public Button rescaleButton;

	private RectTransform windowRectTransform;
    private RectTransform treeRectTransform;

    private void Start()
    {
		if (Tree != null){
        windowRectTransform = Window.GetComponent<RectTransform>();
        treeRectTransform = Tree.GetComponent<RectTransform>();
		Tree.onNodeSelected.AddListener(SetFocus);
		SyncTreeWithWindow();
		}
    }

	public void SetFocus(Node node)	{
			transform.SetAsLastSibling();
			WindowsPanel.transform.SetAsLastSibling();
			SyncTreeWithWindow();
	}

    public void OnPointerDown(PointerEventData eventData)
    {
		if(!lockToggle.isOn)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(rescaleButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))	{
				isRescaling = true;
				Rescale(eventData);
				SyncTreeWithWindow();
			}
			else { 
				isRescaling = false; 
				transform.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0f); }
			
				transform.SetAsLastSibling();
				WindowsPanel.transform.SetAsLastSibling();
				SyncTreeWithWindow();
		}
    }
	
	public void OnDrag(PointerEventData eventData) {
		if (lockToggle.isOn)
			return;
		
		if (eventData.button != PointerEventData.InputButton.Left)
			return;
		
		if (isRescaling){
				Rescale(eventData);
				return;
			}
			
		transform.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0f);
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
        rectTransform.localPosition = localPoint - offset;		
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

}