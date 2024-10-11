using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class VolumeGizmo : MonoBehaviour
{
	
    [HideInInspector]
    public Mesh mesh;
	#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
		
        if (Vector3.Distance(gameObject.transform.position, SceneView.lastActiveSceneView.camera.transform.position) <= SettingsManager.PrefabRenderDistance)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireMesh(mesh, 0, gameObject.transform.position, gameObject.transform.rotation, gameObject.transform.lossyScale);
        }
    }
	#endif
}