using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;

public class TemplateWindow : MonoBehaviour
{
	public Toggle toggle;
	public Text title, footer, label;
	public Button button, buttonbright, close, rescale;
	public Slider slider;
	public Dropdown dropdown;
	public Vector3Field vector3Fields;
	public RangeSlide rangeSlide;
	public InputField inputField;
	
	//close, footer, and title are always included in a window
	//the close button needs to be registered with app manager

	
}