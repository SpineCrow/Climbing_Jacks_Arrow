using UnityEngine;
using System.Collections.Generic;


// Central registry of all game items. Supports lookup by name or type.
// Populate manually in the Inspector or via the context menu from Resources/Items.
public class ItemDataBase : MonoBehaviour
{
    public List<ItemData> allItems = new List<ItemData>();

    // Cached dictionary for O(1) name lookups (built on first query)
    private Dictionary<string, ItemData> itemsByName;

    
    // Returns the first item matching the given name, or null if not found.
    // Uses a lazily-built dictionary for fast repeated lookups.
    public ItemData GetItemById(string itemId)
    {
        if (itemsByName == null)
        {
            BuildNameCache();
        }

        itemsByName.TryGetValue(itemId, out ItemData result);
        return result;
    }

    
    // Returns the first item matching the given type, or null if not found.
    public ItemData GetItemByType(ItemType type)
    {
        return allItems.Find(item => item.itemType == type);
    }

    
    // Rebuilds the internal name -> item dictionary. Call after modifying allItems at runtime.
    public void BuildNameCache()
    {
        itemsByName = new Dictionary<string, ItemData>(allItems.Count);

        foreach (ItemData item in allItems)
        {
            if (item != null && !itemsByName.ContainsKey(item.itemName))
            {
                itemsByName[item.itemName] = item;
            }
        }
    }

    
    // Editor utility: loads all ItemData assets from Resources/Items.
    [ContextMenu("Populate From Resources")]
    private void PopulateFromResources()
    {
        allItems.Clear();
        ItemData[] items = Resources.LoadAll<ItemData>("Items");
        allItems.AddRange(items);
        itemsByName = null; // Invalidate cache
        Debug.Log($"Loaded {allItems.Count} items from Resources/Items");
    }
}

