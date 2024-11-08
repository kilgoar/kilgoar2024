using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class RangeSlide : MonoBehaviour
{
    public SliderFieldSO sfData; 
    public InputField lowField, highField;
    public Slider lowSlider, highSlider;
    public Text titleText;

    private bool isWhole;

    public void Awake()
    {
        Init();
    }

    public void Init()
    {
        Setup();
        Configure();
    }

    // Setup references
    public void Setup()
    {
        titleText = GetComponentInChildren<Text>();
    }

    public void Configure()    {

        lowSlider.minValue = sfData.minSetting;
        highSlider.minValue = sfData.minSetting;

        lowSlider.maxValue = sfData.maxSetting;
        highSlider.maxValue = sfData.maxSetting;

        titleText.text = sfData.title;

        lowSlider.wholeNumbers = isWhole;
        highSlider.wholeNumbers = isWhole;

		if (sfData.whole)		{
			lowField.contentType = InputField.ContentType.IntegerNumber;
			highField.contentType = InputField.ContentType.IntegerNumber;
		}
		else		{
			lowField.contentType = InputField.ContentType.DecimalNumber;
			highField.contentType = InputField.ContentType.DecimalNumber;
		}

        lowField.characterLimit = 6;
        highField.characterLimit = 6;
		
		lowField.onEndEdit.AddListener(FieldChanged);
		highField.onEndEdit.AddListener(FieldChanged);
		lowSlider.onValueChanged.AddListener(SliderChanged);
		highSlider.onValueChanged.AddListener(SliderChanged);
		
    }

	public void FieldChanged(string value)    {
		float lowValue = float.Parse(lowField.text);
		float highValue = float.Parse(highField.text);

		lowValue = Mathf.Clamp(lowValue, lowSlider.minValue, highSlider.value);
		highValue = Mathf.Clamp(highValue, lowSlider.value, highSlider.maxValue);
				
		lowSlider.value = float.Parse(lowField.text);
        highSlider.value = float.Parse(highField.text);
				
	}

    public void SliderChanged(float value)    {
		lowSlider.value = Mathf.Clamp(lowSlider.value, lowSlider.minValue, highSlider.value);
		highSlider.value = Mathf.Clamp(highSlider.value, lowSlider.value, highSlider.maxValue);

		
        if (!sfData.whole){
			lowField.text = lowSlider.value.ToString("F3");
			highField.text = highSlider.value.ToString("F3");
			return;
		}
		lowField.text = lowSlider.value.ToString("F3");
		highField.text = highSlider.value.ToString("F3");
    }

    private void OnValidate()
    {
        Init();
    }
}