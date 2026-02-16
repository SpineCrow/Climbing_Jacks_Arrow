using UnityEngine;

// When the player enters this trigger, the associated item is added to their stash
// and the pickup object is deactivated (pooling-friendly).
public class ItemPickup : MonoBehaviour
{
    [Tooltip("The item data asset this pickup represents")]
    public ItemData itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the player can pick up items
        if (!other.CompareTag("Player")) return;

        ItemInventory inventory = other.GetComponent<ItemInventory>();
        if (inventory != null)
        {
            inventory.AddItem(itemData);
            gameObject.SetActive(false);
        }
    }
}


