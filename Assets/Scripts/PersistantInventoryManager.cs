using UnityEngine;
using System.Collections.Generic;

//Singleton that persists equipment and stash data across scene loads.
//Lives on a DontDestroyOnLoad GameObject.
public class PersistentInventoryManager : MonoBehaviour
{
    public static PersistentInventoryManager Instance { get; private set; }

    [Header("Persistent Data")]
    public ItemData[] persistentSlots = new ItemData[3];
    public List<ItemData> persistentStash = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    //Snapshots the player's current slots and stash into persistent storage.
    public void SaveInventory(ItemData[] slots, List<ItemData> stash)
    {
        int count = Mathf.Min(slots.Length, persistentSlots.Length);
        for (int i = 0; i < count; i++)
        {
            persistentSlots[i] = slots[i];
        }

        persistentStash.Clear();
        persistentStash.AddRange(stash);
    }

    
    //Restores saved slots and stash into the target inventory,
    //then re-equips any items that were previously equipped.
    public void LoadInventory(ItemInventory targetInventory)
    {
        // Restore slots
        int count = Mathf.Min(persistentSlots.Length, targetInventory.slots.Length);
        for (int i = 0; i < count; i++)
        {
            targetInventory.slots[i] = persistentSlots[i];
        }

        // Restore stash
        targetInventory.stash.Clear();
        targetInventory.stash.AddRange(persistentStash);

        // Re-equip items that had the equipped flag set
        for (int i = 0; i < targetInventory.slots.Length; i++)
        {
            if (targetInventory.slots[i] != null && targetInventory.slots[i].isEquipped)
            {
                targetInventory.EquipItem(i);
            }
        }
    }
}
