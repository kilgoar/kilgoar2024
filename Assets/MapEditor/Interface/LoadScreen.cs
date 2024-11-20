using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadScreen : MonoBehaviour
{
    public RectTransform transform;

    void Update()
    {
        transform.Rotate(0, 0, 100 * Time.deltaTime);
    }
}
