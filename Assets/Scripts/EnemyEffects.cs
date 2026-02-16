using UnityEngine;
using System.Collections;

// Handles physics-based effects applied to enemies (e.g., knockback from items).
// Requires a Rigidbody2D on the same GameObject.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyEffects : MonoBehaviour
{
    private Rigidbody2D rb;
    private Transform cachedPlayerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Cache the player transform to avoid FindFirstObjectByType every push
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            cachedPlayerTransform = player.transform;
        }
    }

    // Applies an impulse force pushing the enemy away from the player.
    // <param name="force">Impulse magnitude.</param>
    public void PushBack(float force)
    {
        if (cachedPlayerTransform == null) return;

        Vector2 direction = ((Vector2)transform.position - (Vector2)cachedPlayerTransform.position).normalized;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }
}
