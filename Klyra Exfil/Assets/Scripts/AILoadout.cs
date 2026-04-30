using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;
using System.Collections;

/// <summary>
/// Simple loadout for AI - gives them weapons on spawn.
/// Attach to AI prefabs.
/// </summary>
public class AILoadout : MonoBehaviour
{
    [Header("AI Loadout")]
    [Tooltip("Item Definitions to give this AI (weapons, etc.)")]
    public Opsive.Shared.Inventory.ItemDefinitionBase[] items;

    [Tooltip("Amounts for each item")]
    public int[] itemAmounts;

    [Tooltip("Auto-equip first item?")]
    public bool autoEquipFirst = true;

    void Start()
    {
        StartCoroutine(ApplyLoadout());
    }

    IEnumerator ApplyLoadout()
    {
        // Wait a frame for UCC to initialize
        yield return null;

        var inventory = GetComponent<InventoryBase>();
        if (inventory == null)
        {
            Debug.LogWarning($"AILoadout: No inventory on {gameObject.name}");
            yield break;
        }

        if (items == null || items.Length == 0)
        {
            Debug.LogWarning($"AILoadout: No items assigned on {gameObject.name}");
            yield break;
        }

        // NOTE: This script is not needed if you use Opsive's Default Loadout.
        // The Default Loadout in the Inventory component will auto-equip items.
        // Just make sure "Equip" is checked on your items in the Default Loadout!

        Debug.LogWarning($"AILoadout: This script is deprecated. Use Opsive's Default Loadout instead on {gameObject.name}");

        // Destroy this component since it's not needed
        Destroy(this);

        Debug.Log($"AILoadout: Applied loadout to {gameObject.name}");
    }
}
