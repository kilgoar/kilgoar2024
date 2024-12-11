using UnityEngine;
using System.Collections.Generic;

public class CCTV_RC : MonoBehaviour
{
    public Transform yawTransform;

    void Awake()
    {
        // Find the CCTV_YAW transform in the hierarchy
        if (yawTransform == null)
        {
            yawTransform = transform.Find("CCTV_YAW");
            if (yawTransform == null)
            {
                return;
            }
        }

        // Add MeshCull component to the CCTV_YAW object
        MeshCull meshCull = yawTransform.gameObject.AddComponent<MeshCull>();
        if (meshCull == null)
        {
        }
    }

    void Start()
    {
        // Any setup or initialization that should happen after Awake
    }

    // Update is called once per frame
    void Update()
    {
        // Camera control logic can go here
    }
}