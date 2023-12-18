using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static TerrainManager;

[SelectionBase, DisallowMultipleComponent]
public class CircuitDataHolder : MonoBehaviour
{
    public WorldSerialization.CircuitData circuitData;
	
	public void CastCircuitData()
	{
		gameObject.transform.localPosition = circuitData.wiring;
	}
	
	public void UpdateCircuitData()
	{
		//circuitData = gameObject.GetComponent<this.circuitData>();
	}
}