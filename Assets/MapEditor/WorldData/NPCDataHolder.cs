using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static TerrainManager;

[SelectionBase, DisallowMultipleComponent]
public class NPCDataHolder : MonoBehaviour
{
    public WorldSerialization.NPCData bots;
	
	public void nameNPCData(string name)
	{
		bots.category = name;
	}
	
	public void UpdateCircuitData()
	{
		//circuitData = gameObject.GetComponent<this.circuitData>();
	}
}