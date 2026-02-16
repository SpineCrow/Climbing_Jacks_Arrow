using UnityEngine;


// Types of equippable items, mapped to inventory slot indices (0, 1, 2).
public enum ItemType
{
    Weapon = 0,
    Potion = 1,
    GuileSuit = 2
}


// Determines whether an item effect targets the player or an enemy.
public enum TargetType { Player, Enemy }


// Available item effect types routed through <see cref="EffectSystem"/>.
public enum EffectType { MovementBoost, WeightBoost, Distract, PushBack, Fear }


// ScriptableObject defining a single item's properties, visuals, and effect parameters.
// Create via Assets > Create > Game > Item.
[CreateAssetMenu(menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public Sprite icon;
    public ItemType itemType;

    [Header("World Representation")]
    [Tooltip("Prefab spawned when the item is equipped or dropped in the world")]
    public GameObject worldPrefab;

    [Header("State")]
    [HideInInspector]
    public bool isEquipped;

    [Header("Effect Configuration")]
    public TargetType targetType;
    public EffectType effectType;

    [Tooltip("Magnitude of the effect (e.g., force amount, boost multiplier)")]
    public float effectStrength = 1f;

    [Tooltip("Duration in seconds. For weapons this doubles as the cooldown time")]
    public float effectDuration = 3f;
}
