using UnityEngine;

// Rarity tiers for treasure items. Each tier has a gold value multiplier
// applied in <see cref="InventorySystem.CalculateGoldValue"/>.
public enum TreasureRarity
{
    Common,     // 1x
    Uncommon,   // 2x
    Rare,       // 5x
    Epic,       // 10x
    Legendary   // 25x
}

// ScriptableObject defining a collectible treasure's identity, value, and visuals.
// Create via Assets > Create > Inventory > Treasure.
[CreateAssetMenu(fileName = "New Treasure", menuName = "Inventory/Treasure")]
public class TreasureData : ScriptableObject
{
    [Header("Identity")]
    public string treasureName;
    public Sprite icon;
    public GameObject worldPrefab;

    [Header("Value")]
    public TreasureRarity rarity;

    [Tooltip("Base gold value before rarity multiplier is applied")]
    [Min(1)]
    public int baseGoldValue = 10;

    [Header("Visuals")]
    [Tooltip("Color of the glow effect indicating rarity")]
    public Color glowColor = Color.yellow;

    [Tooltip("Optional particle system for rarity-specific sparkle effects")]
    public ParticleSystem rarityParticles;
}

