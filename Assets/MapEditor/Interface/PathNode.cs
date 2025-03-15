using UnityEngine;
using System;

public class PathNode : MonoBehaviour
{
    private NodeCollection nodeCollection;
    public event Action OnTransformChanged;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    public void Initialize(NodeCollection collection)
    {
        nodeCollection = collection;
        if (nodeCollection == null)
        {
            Debug.LogError($"PathNode on {gameObject.name} failed to initialize: NodeCollection is null.");
        }
        // Store initial transform values
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        // Check for transform changes every frame
        if (transform.position != lastPosition)
        {
            lastPosition = transform.position;
            ReportTransformChange("Position");
        }
        if (transform.rotation != lastRotation)
        {
            lastRotation = transform.rotation;
            ReportTransformChange("Rotation");
        }
        if (transform.localScale != lastScale)
        {
            lastScale = transform.localScale;
            ReportTransformChange("Scale");
        }
    }

    private void ReportTransformChange(string changedProperty)
    {
        //Debug.Log($"PathNode {gameObject.name} {changedProperty} changed. New position: {transform.position}");
        OnTransformChanged?.Invoke();
    }

    public void ForceUpdate()
    {
        ReportTransformChange("Manual");
    }

    private void OnDestroy()
    {

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}