using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadScreen : MonoBehaviour
{
    public Image limited;

    void Update()
    {
        limited.rectTransform.Rotate(100 * Time.deltaTime, 0, 0);
    }
}
