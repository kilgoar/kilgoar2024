using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static TerrainManager;

[SelectionBase, DisallowMultipleComponent]
public class ModifierDataHolder : MonoBehaviour
{
    public WorldSerialization.ModifierData modifierData;

	public void UpdateModifierData()
	{
		
	}
}