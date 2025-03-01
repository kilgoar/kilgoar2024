using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    public Camera targetCamera;
    public InputField xField, yField, zField;
    public Text directionText;
	public Text quadrantText;
	public Transform compass;
    private Vector3 lastPosition;
	

    public static Compass Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    public void Initialize()
    {

        SetupInputFields();
        lastPosition = targetCamera.transform.position;
    }

    public void SetupInputFields()
    {
         xField.onEndEdit.AddListener(OnFieldChanged);
         yField.onEndEdit.AddListener(OnFieldChanged);
         zField.onEndEdit.AddListener(OnFieldChanged);
        UpdateFieldsFromCamera();
    }

	public void SyncScaleWithMenu()
    {
            Vector3 menuScale = MenuManager.Instance.GetMenuScale();
            compass.localScale = menuScale - Vector3.one;
    }

    private void Update()
    {
        if (!targetCamera) return;

        Vector3 currentPos = targetCamera.transform.position;
        if (currentPos != lastPosition)
        {
            UpdateFieldsFromCamera();
            lastPosition = currentPos;
        }
        UpdateOrdinalDisplay(targetCamera.transform.forward);
    }

    private void OnFieldChanged(string value)
    {
        if (!targetCamera) return;

        Vector3 newPosition = GetVector3();
        CameraManager.Instance.SetCameraPosition(newPosition);
        UpdateOrdinalDisplay(targetCamera.transform.forward);
        lastPosition = newPosition;
    }

    public Vector3 GetVector3()
    {
        float x = ParseField(xField?.text);
        float y = ParseField(yField?.text);
        float z = ParseField(zField?.text);
        return new Vector3(x, y, z);
    }

    private float ParseField(string text)
    {
        if (float.TryParse(text, out float result))
        {
            return result;
        }
        return 0f; // Default value if parsing fails
    }

    public void SetVector3(Vector3 value)
    {
        if (!targetCamera) return;

        if (xField) xField.text = value.x.ToString("F2");
        if (yField) yField.text = value.y.ToString("F2");
        if (zField) zField.text = value.z.ToString("F2");
        targetCamera.transform.position = value;
        lastPosition = value;
    }

    private void UpdateFieldsFromCamera()
    {
        if (!targetCamera) return;

        Vector3 pos = targetCamera.transform.position;
        if (xField) xField.text = pos.x.ToString("F2");
        if (yField) yField.text = pos.y.ToString("F2");
        if (zField) zField.text = pos.z.ToString("F2");
    }

    private void UpdateOrdinalDisplay(Vector3 forward)
    {
        if (!directionText) return;

        // Flatten to 2D (x, z plane) and calculate yaw angle
        Vector2 flatForward = new Vector2(forward.x, forward.z).normalized;
        float angle = Mathf.Atan2(flatForward.x, flatForward.y) * Mathf.Rad2Deg;
        string ordinal = GetOrdinalDirection(angle);
        directionText.text = ordinal;
    }

    private string GetOrdinalDirection(float angle)
    {
        // Convert to 0-360 range
        angle = (angle + 360) % 360;

        // Define direction ranges
        if (angle >= 337.5f || angle < 22.5f) return "N";
        if (angle >= 22.5f && angle < 67.5f) return "NE";
        if (angle >= 67.5f && angle < 112.5f) return "E";
        if (angle >= 112.5f && angle < 157.5f) return "SE";
        if (angle >= 157.5f && angle < 202.5f) return "S";
        if (angle >= 202.5f && angle < 247.5f) return "SW";
        if (angle >= 247.5f && angle < 292.5f) return "W";
        if (angle >= 292.5f && angle < 337.5f) return "NW";
        return "NO"; // "NO" could be a typo for "Unknown" or intentional
    }
}