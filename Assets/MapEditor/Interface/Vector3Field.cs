using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Vector3Field : MonoBehaviour
{
    public InputField xField, yField, zField;
	
    private void Awake()    {
        Init();
    }

    public void Init()    {
        Setup();
    }

    public void Setup()    {
        xField.onEndEdit.AddListener(OnFieldChanged);
        yField.onEndEdit.AddListener(OnFieldChanged);
        zField.onEndEdit.AddListener(OnFieldChanged);
    }

    private void OnFieldChanged(string value)    {
        Vector3 currentValue = GetVector3();
    }

    public Vector3 GetVector3()    {
        return new Vector3(
            float.Parse(xField.text),
            float.Parse(yField.text),
            float.Parse(zField.text)
        );
    }

    public void SetVector3(Vector3 value)    {
        xField.text = value.x.ToString();
        yField.text = value.y.ToString();
        zField.text = value.z.ToString();
    }
}