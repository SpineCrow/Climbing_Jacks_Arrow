using UnityEngine;
using UnityEngine.UI;

/// HUD manager that displays equipment slots, item names, invisibility bar,
/// and potion cooldown. Refreshes every frame from the live inventory/stealth state.
public class UIManager : MonoBehaviour
{
    [Header("Equipment Slot Icons")]
    public Image weaponSlot;
    public Image potionSlot;
    public Image guileSuitSlot;
    public Sprite emptySlotSprite;

    [Header("Equipment Slot Labels")]
    public Text weaponNameText;
    public Text potionNameText;
    public Text guileSuitNameText;

    [Header("Invisibility Bar")]
    public Slider invisibilityBar;

    [Header("Potion Cooldown")]
    public Slider potionCooldownSlider;
    public Text potionCooldownText;

    // Cached references
    private ItemInventory inventory;
    private PlayerStealth playerStealth;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        inventory = FindFirstObjectByType<ItemInventory>();
        playerStealth = FindFirstObjectByType<PlayerStealth>();
    }

    private void Update()
    {
        UpdateInventoryUI();
        UpdateInvisibilityUI();
        UpdatePotionCooldownUI();
    }

    // ========================================================================
    // Equipment Slot Click Handlers (wire to UI Button onClick)
    // ========================================================================

    public void OnClickWeaponSlot() { EquipAndRefresh(ItemType.Weapon); }
    public void OnClickPotionSlot() { EquipAndRefresh(ItemType.Potion); }
    public void OnClickGuileSuitSlot() { EquipAndRefresh(ItemType.GuileSuit); }

    public void OnRightClickWeaponSlot() => inventory.UnequipSlot((int)ItemType.Weapon);
    public void OnRightClickPotionSlot() => inventory.UnequipSlot((int)ItemType.Potion);
    public void OnRightClickGuileSuitSlot() => inventory.UnequipSlot((int)ItemType.GuileSuit);

    // ========================================================================
    // Inventory UI
    // ========================================================================

    /// Updates all three equipment slot icons and name labels from the live inventory.
    private void UpdateInventoryUI()
    {
        if (inventory == null) return;

        UpdateSlot(weaponSlot, weaponNameText, inventory.slots[(int)ItemType.Weapon]);
        UpdateSlot(potionSlot, potionNameText, inventory.slots[(int)ItemType.Potion]);
        UpdateSlot(guileSuitSlot, guileSuitNameText, inventory.slots[(int)ItemType.GuileSuit]);
    }

    // Sets a single slot's icon and optional name text based on the equipped item (or empty).
    private void UpdateSlot(Image slotImage, Text nameText, ItemData item)
    {
        slotImage.sprite = item != null ? item.icon : emptySlotSprite;

        if (nameText != null)
        {
            nameText.text = item != null ? item.itemName : "Empty";
            nameText.color = item != null ? Color.white : Color.gray;
        }
    }

    // ========================================================================
    // Invisibility Bar
    // ========================================================================

    // Reflects invisibility state: full while active, draining during cooldown, empty when ready.
    private void UpdateInvisibilityUI()
    {
        if (playerStealth == null || invisibilityBar == null) return;

        if (playerStealth.IsInvisible())
        {
            invisibilityBar.value = 1f;
        }
        else if (playerStealth.IsOnCooldown())
        {
            invisibilityBar.value = Mathf.Clamp01(playerStealth.GetCooldownProgress());
        }
        else
        {
            invisibilityBar.value = 0f;
        }
    }

    // ========================================================================
    // Potion Cooldown
    // ========================================================================

    // Displays potion cooldown progress on a slider and remaining seconds as text.
    private void UpdatePotionCooldownUI()
    {
        if (inventory == null || potionCooldownSlider == null) return;

        float progress = inventory.GetPotionCooldownProgress();
        potionCooldownSlider.value = progress;

        if (potionCooldownText != null)
        {
            if (progress > 0f)
            {
                int secondsRemaining = Mathf.CeilToInt(progress * inventory.potionCooldownTime);
                potionCooldownText.text = secondsRemaining.ToString();
                potionCooldownText.color = Color.red;
            }
            else
            {
                potionCooldownText.text = "Ready";
                potionCooldownText.color = Color.green;
            }
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private void EquipAndRefresh(ItemType type)
    {
        inventory.EquipItem((int)type);
        UpdateInventoryUI();
    }
}
