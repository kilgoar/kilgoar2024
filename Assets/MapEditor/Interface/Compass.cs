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
    }

	public void Hide()
	{
		if (compass != null)
		{
			compass.gameObject.SetActive(false);
		}
	}

	public void Show()
	{
		if (compass != null)
		{
			compass.gameObject.SetActive(true);
		}
	}

	public void SyncScaleWithMenu()
	{
		Vector3 menuScale = MenuManager.Instance.GetMenuScale();
		Vector3 newScale = menuScale - Vector3.one;
		
		// Clamp each component of the vector between 0.6 and 3
		newScale.x = Mathf.Clamp(newScale.x, 0.6f, 3f);
		newScale.y = Mathf.Clamp(newScale.y, 0.6f, 3f);
		newScale.z = Mathf.Clamp(newScale.z, 0.6f, 3f);
		
		compass.localScale = newScale;
	}
	
    private void Update()
    {

        Vector3 currentPos = targetCamera.transform.position;
		Vector3 relativePos = targetCamera.transform.position-PrefabManager.PrefabParent.position;

        if (currentPos != lastPosition)
        {

			xField.SetTextWithoutNotify(relativePos.x.ToString("F2"));
			yField.SetTextWithoutNotify(relativePos.y.ToString("F2"));
			zField.SetTextWithoutNotify(relativePos.z.ToString("F2"));
            
			quadrantText.text = PositionToGridString(relativePos);
			
			lastPosition = currentPos;
        }
        UpdateOrdinalDisplay(targetCamera.transform.forward);
    }

    private void OnFieldChanged(string value)
    {
        if (!targetCamera) return;

        Vector3 newPosition = GetVector3();
        CameraManager.Instance.SetCameraPosition(newPosition + PrefabManager.PrefabParent.position);
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
	
	public string PositionToGridString(Vector3 position)
	{
		float gridUnit = 146.28572f;
		int gridCount = Mathf.FloorToInt(TerrainManager.TerrainSize.x / gridUnit + 0.001f);
		float cellSize = TerrainManager.TerrainSize.x / gridCount;
		Vector2 origin = new Vector2(-TerrainManager.TerrainSize.x / 2f, TerrainManager.TerrainSize.x / 2f);
		Vector2 gridPos = new Vector2((position.x - origin.x) / cellSize, (origin.y - position.z) / cellSize);
		int x = Mathf.FloorToInt(gridPos.x);
		int y = Mathf.FloorToInt(gridPos.y);
		x = Mathf.Max(x, 0);
		int letterNum = x + 1;
		string letters = "";
		while (letterNum > 0)
		{
			letterNum--;
			letters = ((char)(65 + letterNum % 26)).ToString() + letters;
			letterNum /= 26;
		}
		return $"{letters}{y}";
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