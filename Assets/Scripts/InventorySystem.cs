using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


// Holds a reference to a picked-up treasure for display, selling, and dropping.
[System.Serializable]
public class InventoryItem
{
    public Sprite icon;
    public GameObject prefab;
    public TreasureData treasureData;
    public int goldValue;
}


// Manages the treasure inventory (separate from equipment).
// Handles picking up treasures, selling them to traders, and dropping on death.
public class InventorySystem : MonoBehaviour
{
    private static readonly int[] RARITY_MULTIPLIERS = { 1, 2, 5, 10, 25 };

    [Tooltip("UI Image slots representing each inventory position")]
    public Image[] inventorySlots;

    [Tooltip("World position where dropped items are spawned")]
    public Transform dropLocation;

    private InventoryItem[] items = new InventoryItem[4];

    // ========================================================================
    // Add / Remove
    // ========================================================================

    
    // Adds a treasure to the first empty slot. Calculates and stores its gold value.
    public void AddTreasure(TreasureData treasureData, GameObject prefab)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) continue;

            int goldValue = CalculateGoldValue(treasureData);

            items[i] = new InventoryItem
            {
                icon = treasureData.icon,
                prefab = prefab,
                treasureData = treasureData,
                goldValue = goldValue
            };

            SetSlotVisual(i, treasureData.icon);
            break;
        }
    }

    
    // Sells all held treasures and returns the total gold earned.
    public int SellAllItems()
    {
        int totalGold = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i]?.treasureData == null) continue;

            totalGold += items[i].goldValue;
            ClearSlot(i);
        }

        return totalGold;
    }

    
    // Drops all items near the drop location with a random scatter impulse.
    public void DropAllItems()
    {
        const float dropDistance = 1.5f;
        const float dropForce = 3f;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            if (items[i].prefab != null)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                Vector3 dropPosition = dropLocation.position + (Vector3)(randomDir * dropDistance);

                GameObject droppedItem = items[i].prefab;
                droppedItem.transform.position = dropPosition;
                droppedItem.SetActive(true);

                Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(randomDir * dropForce, ForceMode2D.Impulse);
                }
            }
            else
            {
                Debug.LogWarning($"Slot {i}: item prefab is missing or destroyed.");
            }

            ClearSlot(i);
        }
    }

    // ========================================================================
    // Queries
    // ========================================================================

    
    // Returns the combined gold value of all held treasures (for UI preview).
    public int GetTotalInventoryValue()
    {
        int total = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i]?.treasureData != null)
            {
                total += items[i].goldValue;
            }
        }

        return total;
    }

    
    // Returns the number of occupied inventory slots.
    public int GetItemCount()
    {
        int count = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) count++;
        }

        return count;
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    
    // Calculates gold value: base value × rarity multiplier.
    private int CalculateGoldValue(TreasureData treasureData)
    {
        int rarityIndex = (int)treasureData.rarity;
        int multiplier = (rarityIndex >= 0 && rarityIndex < RARITY_MULTIPLIERS.Length)
            ? RARITY_MULTIPLIERS[rarityIndex]
            : 1;

        return treasureData.baseGoldValue * multiplier;
    }

    
    // Sets a slot's UI image to the given sprite.
    private void SetSlotVisual(int index, Sprite icon)
    {
        if (index >= inventorySlots.Length) return;

        inventorySlots[index].sprite = icon;
        inventorySlots[index].color = Color.white;
    }

    
    // Clears both the data and UI for a given slot.
    private void ClearSlot(int index)
    {
        items[index] = null;

        if (index < inventorySlots.Length)
        {
            inventorySlots[index].sprite = null;
            inventorySlots[index].color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
