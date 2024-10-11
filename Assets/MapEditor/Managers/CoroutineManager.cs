using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager _instance;
    private bool _isInitialized = false;

    public static CoroutineManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineManager");
                _instance = go.AddComponent<CoroutineManager>();
                DontDestroyOnLoad(go); // Keep the GameObject alive across scenes
            }
			
            return _instance;
        }
    }
	
	private void Awake()
    {
        // Ensure this is the only instance
        if (_instance != null && _instance != this)
        {
			 Debug.LogError("destroying duplicate coroutine");
            Destroy(gameObject);
            return;
        }
		_instance = this;
        _isInitialized = true;
    }

    public bool isInitialized
    {
        get { return _isInitialized; }
    }

	public Coroutine StartRuntimeCoroutine(IEnumerator coroutine)
	{
		if (coroutine == null)
		{
			Debug.LogError("Coroutine is null!");
			return null;
		}
		
		var result = base.StartCoroutine(coroutine);
		return result;
	}
}