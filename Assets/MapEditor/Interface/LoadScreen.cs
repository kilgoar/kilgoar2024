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
        if (Instance == null)        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else        {
            Destroy(gameObject);
        }
    }
    
	public void Progress(float percent)
	{
		if (progress == null || frame == null) return; // Safety check

		RectTransform frameRect = frame.rectTransform;
		RectTransform progressRect = progress.rectTransform;

		float frameWidth = frameRect.rect.width;
		float newWidth = frameWidth * .9f * percent;

		// This method respects anchors
		progressRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

	}
    
    public void SetMessage(string message)    {
        
		if (loadMessage != null)        {
            loadMessage.text = message;
        }
    }
    
    private void OnEnable()    {
        StartCoroutine(RotateLoadingScreen());
    }
    
    private void OnDisable()    {
        StopAllCoroutines();
    }
    
    public void Show()
    {
        // Enable the game object to show the loading screen
        // Reset progress bar and message for a fresh start
        gameObject.SetActive(true);
        Progress(0);
        SetMessage("Loading Map");
		isEnabled = true;
    }
    
    public void Hide()
    {
        // Disable the game object to hide the loading screen
        gameObject.SetActive(false);
		isEnabled = false;
    }
	
    IEnumerator RotateLoadingScreen()
    {
        while (true)        {
            transform.Rotate(0, 0, -100 * Time.deltaTime);
            yield return null;
        }
    }
}
