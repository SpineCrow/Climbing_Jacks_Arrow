using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Manages the player's equipment slots (Weapon, Potion, Guile Suit) and stash.
// Handles equipping, unequipping, cooldowns, and weapon/potion usage.
public class ItemInventory : MonoBehaviour
{
    // --- Equipment ---
    [Tooltip("Fixed slots: 0 = Weapon, 1 = Potion, 2 = Guile Suit")]
    public ItemData[] slots = new ItemData[3];

    [Tooltip("All picked-up items not currently equipped")]
    public List<ItemData> stash = new List<ItemData>();

    [Header("Equipment Handling")]
    [Tooltip("Transform where the equipped item's world prefab is parented")]
    public Transform equipmentSlot;
    private GameObject currentEquippedObject;

    [Header("Potion Cooldown")]
    public float potionCooldownTime = 10f;
    private float potionCooldownTimer;

    [Header("Weapon Cooldown")]
    public float weaponCooldownTime = 1f;
    private float weaponCooldownTimer;

    [Header("Weapon Attack")]
    [Tooltip("Default projectile/attack prefab instantiated on weapon use")]
    public GameObject baseWeaponAttackPrefab;

    // Cached references
    private PlayerController playerController;
    private PlayerEffects playerEffects;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        // Cache player components
        playerController = GetComponent<PlayerController>();
        playerEffects = GetComponent<PlayerEffects>();

        // Restore saved inventory
        if (PersistentInventoryManager.Instance != null)
        {
            PersistentInventoryManager.Instance.LoadInventory(this);
        }
    }

    private void Update()
    {
        // Tick cooldowns
        if (potionCooldownTimer > 0f) potionCooldownTimer -= Time.deltaTime;
        if (weaponCooldownTimer > 0f) weaponCooldownTimer -= Time.deltaTime;

        // Input: potion
        if (Input.GetKeyDown(KeyCode.V))
        {
            UsePotion();
        }

        // Input: weapon attack
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButton(0))
        {
            UseWeapon();
        }
    }

    // ========================================================================
    // Inventory Management
    // ========================================================================

    
    // Adds an item to the stash (unequipped storage).
    public bool AddItem(ItemData item)
    {
        stash.Add(item);
        Debug.Log($"{item.itemName} added to stash.");
        return true;
    }

    
    // Returns all stash items that can be placed into an equipment slot.
    public List<ItemData> GetEquippableItems()
    {
        List<ItemData> equippable = new List<ItemData>();

        foreach (ItemData item in stash)
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

    
    // Persists the current inventory state for scene transitions.
    public void SaveInventoryState()
    {
        if (PersistentInventoryManager.Instance != null)
        {
            PersistentInventoryManager.Instance.SaveInventory(slots, stash);
        }
    }

    // ========================================================================
    // Equip / Unequip
    // ========================================================================

    
    // Equips the item currently in the given slot index, spawning its world prefab.
    public void EquipItem(int index)
    {
        if (slots[index] == null) return;

        UnequipCurrent();

        ItemData item = slots[index];

        if (item.worldPrefab != null && equipmentSlot != null)
        {
            currentEquippedObject = Instantiate(
                item.worldPrefab,
                equipmentSlot.position,
                equipmentSlot.rotation,
                equipmentSlot
            );
            item.isEquipped = true;

            UpdateWeaponCooldownIfNeeded(item);
        }
    }

    
    // Moves an item from the stash into the appropriate equipment slot.
    // If the slot is occupied, the current item is returned to the stash.
    public bool EquipFromStash(ItemData item)
    {
        int slotIndex = (int)item.itemType;

        // Return current slot occupant to stash
        if (slots[slotIndex] != null)
        {
            slots[slotIndex].isEquipped = false;
            stash.Add(slots[slotIndex]);
        }

        // Place new item in slot
        slots[slotIndex] = item;
        stash.Remove(item);
        item.isEquipped = true;

        UpdateWeaponCooldownIfNeeded(item);
        SaveInventoryState();

        return true;
    }

    
    // Removes the item from a specific slot and returns it to the stash.
    public void UnequipSlot(int slotIndex)
    {
        if (slots[slotIndex] == null) return;

        ItemData item = slots[slotIndex];
        item.isEquipped = false;
        stash.Add(item);

        // Destroy the world object if this was actively equipped
        if (currentEquippedObject != null)
        {
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
        }

        slots[slotIndex] = null;
        SaveInventoryState();
    }

    
    // Destroys any currently spawned equipment object and clears equipped flags.
    public void UnequipCurrent()
    {
        if (currentEquippedObject != null)
        {
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
        }

        foreach (ItemData item in slots)
        {
            if (item != null) item.isEquipped = false;
        }
    }

    
    // Updates the weapon cooldown time when a weapon with a custom duration is equipped.
    private void UpdateWeaponCooldownIfNeeded(ItemData item)
    {
        if (item.itemType == ItemType.Weapon && item.effectDuration > 0f)
        {
            weaponCooldownTime = item.effectDuration;
        }
    }

    // ========================================================================
    // Weapon Usage
    // ========================================================================

    
    // Activates the equipped weapon's attack based on its effect type.
    public void UseWeapon()
    {
        ItemData weapon = slots[(int)ItemType.Weapon];

        if (weapon == null)
        {
            Debug.Log("No weapon equipped!");
            return;
        }

        if (weaponCooldownTimer > 0f)
        {
            Debug.Log("Weapon is on cooldown!");
            return;
        }

        switch (weapon.effectType)
        {
            case EffectType.PushBack:
                SpawnPushBackAttack(weapon);
                break;

            case EffectType.Fear:
                SpawnFearAttack(weapon);
                break;
        }

        weaponCooldownTimer = weaponCooldownTime;
    }

    
    // Spawns an area-of-effect shockwave projectile that pushes enemies back.
    private void SpawnPushBackAttack(ItemData weapon)
    {
        GameObject attack = InstantiateWeaponAttack(weapon);
        if (attack == null) return;

        // Scale up for area effect
        attack.transform.localScale = Vector3.one * 1.5f;

        Rigidbody2D rb = attack.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 direction = transform.right * transform.localScale.x;
            rb.AddForce(direction * 8f, ForceMode2D.Impulse);
        }
    }

    
    // Spawns a fear projectile aimed at the mouse cursor position.
    private void SpawnFearAttack(ItemData weapon)
    {
        GameObject attack = InstantiateWeaponAttack(weapon);
        if (attack == null) return;

        Rigidbody2D rb = attack.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 origin = transform.position;
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorld - origin).normalized;

            rb.AddForce(direction * 12f, ForceMode2D.Impulse);
        }
    }

    
    // Shared helper: instantiates the base attack prefab and wires up WeaponAttack data.
    private GameObject InstantiateWeaponAttack(ItemData weapon)
    {
        if (baseWeaponAttackPrefab == null || equipmentSlot == null) return null;

        GameObject attack = Instantiate(baseWeaponAttackPrefab, equipmentSlot.position, equipmentSlot.rotation);

        WeaponAttack weaponAttack = attack.GetComponent<WeaponAttack>();
        if (weaponAttack != null)
        {
            weaponAttack.weaponData = weapon;
            weaponAttack.owner = playerController;
        }

        return attack;
    }

    // ========================================================================
    // Potion Usage
    // ========================================================================

    
    // Consumes the equipped potion, applying its weight boost effect.
    public void UsePotion()
    {
        ItemData potion = slots[(int)ItemType.Potion];
        if (potion == null) return;

        if (potionCooldownTimer > 0f)
        {
            Debug.Log("Potion is on cooldown!");
            return;
        }

        if (playerEffects != null)
        {
            playerEffects.ApplyWeightBoost(potion.effectStrength, potion.effectDuration);
        }

        potionCooldownTimer = potionCooldownTime;
    }

    // ========================================================================
    // Cooldown Queries (for UI)
    // ========================================================================

    public bool IsPotionReady() => potionCooldownTimer <= 0f;
    public bool IsWeaponReady() => weaponCooldownTimer <= 0f;

    
    // Returns 0–1 progress of the potion cooldown (1 = just used, 0 = ready).
    public float GetPotionCooldownProgress()
    {
        return potionCooldownTimer <= 0f ? 0f : Mathf.Clamp01(potionCooldownTimer / potionCooldownTime);
    }

    
    // Returns 0–1 progress of the weapon cooldown (1 = just used, 0 = ready).
    public float GetWeaponCooldownProgress()
    {
        return weaponCooldownTimer <= 0f ? 0f : Mathf.Clamp01(weaponCooldownTimer / weaponCooldownTime);
    }

    // ========================================================================
    // Queries
    // ========================================================================

    
    // Returns true if a Guile Suit is equipped in its slot.
    public bool HasGuileSuit()
    {
        return slots[(int)ItemType.GuileSuit] != null;
    }

    
    // General-purpose item use by slot index. Routes to the correct action.
    public void UseItem(int index, PlayerController player)
    {
        if (slots[index] == null)
        {
            Debug.Log("No item equipped in this slot!");
            return;
        }

        ItemData item = slots[index];

        switch (item.itemType)
        {
            case ItemType.Weapon:
                if (IsWeaponReady())
                {
                    UseWeapon();
                }
                break;

            case ItemType.Potion:
                UsePotion();
                break;

            case ItemType.GuileSuit:
                Debug.Log($"Activating guile suit: {item.itemName}");
                // TODO: Toggle stealth / invisibility
                break;
        }
    }
}

