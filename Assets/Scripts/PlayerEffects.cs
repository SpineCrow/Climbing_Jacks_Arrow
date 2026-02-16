using UnityEngine;
using System.Collections;

// Applies temporary stat-modifying effects to the player (e.g., weight boost from potions).
// Requires a <see cref="PlayerController"/> on the same GameObject.
[RequireComponent(typeof(PlayerController))]
public class PlayerEffects : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    // Temporarily overrides the player's weight multiplier, increasing effective carry speed.
    // <param name="strength">Multiplier value (e.g., 2 = double speed, ignoring slowdown).</param>
    // <param name="duration">Duration in seconds before reverting to default.</param>
    public void ApplyWeightBoost(float strength, float duration)
    {
        StartCoroutine(WeightBoostRoutine(strength, duration));
    }

    private IEnumerator WeightBoostRoutine(float strength, float duration)
    {
        player.weightMultiplier = strength;
        yield return new WaitForSeconds(duration);
        player.weightMultiplier = 1f;
    }
}
