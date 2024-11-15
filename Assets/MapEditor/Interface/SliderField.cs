using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SliderField : MonoBehaviour
{
	public SliderFieldSO sfData;
	
	public InputField field;
	public Slider slider;
	public Text text;
	
	public void Awake(){
		Init();
	}
	
	public void Init(){
		Setup();
		Configure();
	}
	
	public void Setup(){
		field = GetComponentInChildren<InputField>();
		slider = GetComponentInChildren<Slider>();
		text = GetComponentInChildren<Text>();
	}
	
	public void Configure(){
		slider.minValue = sfData.minSetting;
		slider.maxValue = sfData.maxSetting;
		text.text = sfData.title;
		slider.wholeNumbers = sfData.whole;
		field.contentType = InputField.ContentType.DecimalNumber;
		field.characterLimit=6;
		if (sfData.whole) {
			field.contentType = InputField.ContentType.IntegerNumber;
		}
	}
	
	
	private void OnValidate(){
		Init();
	}
	
	public void FieldChanged(){
		slider.value = float.Parse(field.text);
	}
	
	public void SliderChanged(){
		if (!sfData.whole){
			field.text = slider.value.ToString("F3");
			return;
		}
		field.text = slider.value.ToString();
	}
}