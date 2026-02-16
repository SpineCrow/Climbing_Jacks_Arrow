using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// Trigger-based level transition. Requires the player to have enough gold
// to proceed. Plays a crossfade animation before loading the next scene.

public class LevelLoader : MonoBehaviour
{
    [Tooltip("Animator with a 'Start' trigger for the crossfade transition")]
    public Animator transition;

    [Tooltip("Seconds to wait for the transition animation before loading")]
    public float transitionTime = 1f;

    [Tooltip("Minimum gold required to advance to the next level")]
    public int requiredGold = 100;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Verify gold requirement
        if (CurrencySystem.Instance == null || CurrencySystem.Instance.GetCurrentGold() < requiredGold)
        {
            Debug.Log($"Not enough gold! Need {requiredGold} gold to proceed.");
            return;
        }

        // Persist inventory before scene change
        ItemInventory inventory = other.GetComponent<ItemInventory>();
        if (inventory != null)
        {
            inventory.SaveInventoryState();
        }

        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        StartCoroutine(LoadLevel(nextScene));
    }

    
    // Plays the transition animation, waits, then loads the target scene.
    
    private IEnumerator LoadLevel(int levelIndex)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(levelIndex);
    }
}
