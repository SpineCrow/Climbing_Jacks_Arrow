using UnityEngine;

// Projectile/attack spawned by weapon usage. On first enemy contact,
// applies the weapon's effect via <see cref="EffectSystem"/> and self-destructs.
public class WeaponAttack : MonoBehaviour
{
    [HideInInspector] public ItemData weaponData;
    [HideInInspector] public PlayerController owner;

    [Tooltip("Auto-destroy after this many seconds if nothing is hit")]
    public float lifetime = 3f;

    private bool hasHit;

    private void Start()
    {
        // Safety net: destroy after lifetime to prevent orphaned projectiles
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (!other.CompareTag("Enemy")) return;

        EffectSystem.ApplyEffect(weaponData, other.gameObject);
        hasHit = true;

        // TODO: Spawn hit VFX / play sound here
        Destroy(gameObject);
    }
}

