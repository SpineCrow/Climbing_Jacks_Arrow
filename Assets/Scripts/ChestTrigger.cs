using UnityEngine;

// Trigger zone around an equipment chest. Notifies the player controller
// when they enter or leave interaction range.
[RequireComponent(typeof(EquipmentChest))]
public class ChestTrigger : MonoBehaviour
{
    private EquipmentChest chest;

    private void Awake()
    {
        // Cache once instead of calling GetComponent every trigger event
        chest = GetComponent<EquipmentChest>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetChestTarget(chest);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ClearChestTarget(chest);
        }
    }
}
