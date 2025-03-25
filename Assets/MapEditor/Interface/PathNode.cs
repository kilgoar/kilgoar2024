using UnityEngine;
using System;

public class PathNode : MonoBehaviour
{
    private NodeCollection nodeCollection;
    public event Action OnTransformChanged;
	public event Action<PathNode> OnNodeDestroyed;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;
	private float lastChangeTime;
	private const float DEBOUNCE_DELAY = 0.1f; // Adjust as needed


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


private void Update()
{
    float distance = Vector3.Distance(transform.position, lastPosition);
    if (distance < 0.001f)
        return;

    lastPosition = transform.position;
    if (Time.time - lastChangeTime >= DEBOUNCE_DELAY)
    {
        ReportTransformChange("Position");
        lastChangeTime = Time.time;
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
        OnNodeDestroyed?.Invoke(this); // Notify the collection this node is destroyed
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}