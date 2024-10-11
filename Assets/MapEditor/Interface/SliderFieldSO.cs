using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SliderField/SliderFieldSO", fileName = "SliderFieldSO")]
public class SliderFieldSO : ScriptableObject
{
	public float maxSetting;
	public float minSetting;
	public string title;
	public bool whole;
}
