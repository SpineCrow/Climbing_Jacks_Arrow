using UnityEngine;

// Static utility that routes item effects to the correct component on a target GameObject.
// Called by the item/inventory system when an item with an effect is used.
public static class EffectSystem
{
    // Applies the effect defined on the given item to the target GameObject.
    // Silently no-ops if the required component is missing (via null-conditional).
    public static void ApplyEffect(ItemData item, GameObject target)
    {
        switch (item.effectType)
        {
            case EffectType.WeightBoost:
                target.GetComponent<PlayerEffects>()?.ApplyWeightBoost(item.effectStrength, item.effectDuration);
                break;

            case EffectType.Distract:
                target.GetComponent<EnemyAI>()?.Distract(item.effectDuration);
                break;

            case EffectType.PushBack:
                target.GetComponent<EnemyEffects>()?.PushBack(item.effectStrength);
                break;

            case EffectType.Fear:
                target.GetComponent<EnemyAI>()?.Flee(item.effectDuration);
                break;
        }
    }
}
