using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadScreen : MonoBehaviour
{
    public RectTransform transform;
    public Image progress, frame;
    public Text loadMessage;
    public bool isEnabled;
    
    public static LoadScreen Instance { get; private set; }
    
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
        }
    }
    
	public void Progress(float percent)
	{
		if (progress == null || frame == null) return; // Safety check

		// Clamp percent between 0 and 1
		percent = Mathf.Clamp01(percent);

		RectTransform frameRect = frame.rectTransform;
		RectTransform progressRect = progress.rectTransform;

		float frameWidth = frameRect.rect.width;
		float newWidth = frameWidth * 0.95f * percent;

		// This method respects anchors
		progressRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
	}
    
    public void SetMessage(string message)    
    {
        if (loadMessage != null)        
        {
            loadMessage.text = message;
        }
    }
    
    private void OnEnable()
    {
		Application.targetFrameRate = -1;
        StartCoroutine(RotateLoadingScreen());
		Application.runInBackground = true;
        // Disable the camera instance
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.cam.enabled = false;
        }

    }

    private void OnDisable()
    {
		Application.runInBackground = false;
        StopAllCoroutines();

        // Enable the camera instance
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.cam.enabled = true;
        }

    }
    
    public void Show()
    {
        // Enable the game object to show the loading screen
        // Reset progress bar and message for a fresh start
        gameObject.SetActive(true);
        Progress(0);
        SetMessage("Loading Asset Bundles");
        isEnabled = true;
		
		MenuManager.Instance.Hide();
		Compass.Instance.Hide();
    }
    
    public void Hide()
    {
        // Disable the game object to hide the loading screen
        gameObject.SetActive(false);
        isEnabled = false;
		
		MenuManager.Instance.Show();
		Compass.Instance.Show();
    }
    
    IEnumerator RotateLoadingScreen()
    {
        while (true)        
        {
            transform.Rotate(0, 0, -20 * Time.deltaTime);
            yield return null;
        }
    }
}