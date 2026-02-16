using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


// UI manager for the equipment stash panel inside an EquipmentChest.
// Displays equippable items from the stash and handles equip/unequip actions.
public class StashUIManager : MonoBehaviour
{
    [Header("Stash Grid")]
    [Tooltip("Button prefab for each stash item (needs Image + optional child Text)")]
    public GameObject stashButtonPrefab;

    [Tooltip("Parent transform that holds the stash item buttons")]
    public Transform stashGrid;

    [Header("Equipped Item Labels")]
    public Text equippedWeaponText;
    public Text equippedPotionText;
    public Text equippedGuileSuitText;

    private ItemInventory playerInventory;

    // ========================================================================
    // Public API
    // ========================================================================

    
    // Opens the stash panel for the given player inventory and refreshes the UI.
    public void Open(ItemInventory inventory)
    {
        playerInventory = inventory;
        RefreshUI();
    }

    
    // Rebuilds the entire stash UI: clears old buttons, updates equipped labels,
    // and creates new buttons for each equippable item in the stash.
    public void RefreshUI()
    {
        ClearStashGrid();
        UpdateEquippedDisplay();

        List<ItemData> equippable = GetEquippableItems(playerInventory.stash);

        foreach (ItemData item in equippable)
        {
            CreateStashButton(item);
        }
    }

    // ========================================================================
    // Unequip Buttons (wire to UI Button onClick)
    // ========================================================================

    public void UnequipWeapon() { UnequipAndRefresh((int)ItemType.Weapon); }
    public void UnequipPotion() { UnequipAndRefresh((int)ItemType.Potion); }
    public void UnequipGuileSuit() { UnequipAndRefresh((int)ItemType.GuileSuit); }

    // ========================================================================
    // Internal
    // ========================================================================

    
    // Removes all child objects from the stash grid.
    private void ClearStashGrid()
    {
        foreach (Transform child in stashGrid)
        {
            Destroy(child.gameObject);
        }
    }

    
    // Updates the three equipped-item text labels with current slot contents.
    private void UpdateEquippedDisplay()
    {
        equippedWeaponText.text = playerInventory.slots[(int)ItemType.Weapon]?.itemName ?? "None";
        equippedPotionText.text = playerInventory.slots[(int)ItemType.Potion]?.itemName ?? "None";
        equippedGuileSuitText.text = playerInventory.slots[(int)ItemType.GuileSuit]?.itemName ?? "None";
    }

    
    // Instantiates a button in the stash grid for the given item.
    // Clicking the button equips the item and refreshes the UI.
    private void CreateStashButton(ItemData item)
    {
        GameObject buttonObj = Instantiate(stashButtonPrefab, stashGrid);

        // Set icon
        Image icon = buttonObj.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = item.icon;
        }

        // Set name label (optional child Text)
        Text nameText = buttonObj.GetComponentInChildren<Text>();
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        // Wire click to equip
        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => EquipItem(item));
        }
    }

    
    // Filters the stash to return only items that fit the three equipment slots.
    private List<ItemData> GetEquippableItems(List<ItemData> allStashItems)
    {
        List<ItemData> equippable = new List<ItemData>();

        foreach (ItemData item in allStashItems)
        {
            if (item.itemType == ItemType.Weapon ||
                item.itemType == ItemType.Potion ||
                item.itemType == ItemType.GuileSuit)
            {
                equippable.Add(item);
            }
        }

        return equippable;
    }

    
    // Equips an item from the stash using the inventory's EquipFromStash method
    // (which handles returning the previous occupant to the stash) and refreshes the UI.
    private void EquipItem(ItemData item)
    {
        playerInventory.EquipFromStash(item);
        RefreshUI();
    }

    
    // Unequips the item in the given slot and refreshes the UI.
    private void UnequipAndRefresh(int slotIndex)
    {
        playerInventory.UnequipSlot(slotIndex);
        RefreshUI();
    }
}
