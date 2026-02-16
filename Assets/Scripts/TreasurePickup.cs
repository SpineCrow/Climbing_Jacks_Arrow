using UnityEngine;
using UnityEngine.InputSystem;

// World-space treasure item the player can pick up when in range.
// Reads TreasureData for visuals/rarity and notifies the spawner on collection.
public class TreasurePickup : MonoBehaviour
{
    [Tooltip("ScriptableObject defining this treasure's properties")]
    public TreasureData treasureData;

    private bool isPlayerInRange;
    private bool hasBeenPickedUp;
    private PlayerController playerController;
    private TreasureSpawner spawner;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        if (treasureData != null)
        {
            AddRarityEffects();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (isPlayerInRange && keyboard.eKey.wasPressedThisFrame && !hasBeenPickedUp)
        {
            hasBeenPickedUp = true;
            TryPickup();
        }
    }

    // ========================================================================
    // Pickup Logic
    // ========================================================================

    // Attempts to add this treasure to the player's inventory and destroys the world object.
    // Can be called externally (e.g., by PlayerController) or from this component's Update.
    public void TryPickup()
    {
        if (!isPlayerInRange || playerController == null) return;

        InventorySystem inventory = playerController.inventorySystem;
        if (inventory == null)
        {
            Debug.LogError($"{name}: Player has no InventorySystem assigned!");
            return;
        }

        inventory.AddTreasure(treasureData, gameObject);

        // Notify spawner so it can track the active treasure count
        if (spawner != null)
        {
            spawner.OnTreasurePickedUp();
        }

        Destroy(gameObject);
    }

    // Called by <see cref="TreasureSpawner"/> after instantiation to register the parent spawner.
    public void SetSpawner(TreasureSpawner parentSpawner)
    {
        spawner = parentSpawner;
    }

    // ========================================================================
    // Rarity Visuals
    // ========================================================================

    // Adds particle effects and a glow light based on the treasure's rarity data.
    private void AddRarityEffects()
    {
        // Instantiate rarity particles as a child
        if (treasureData.rarityParticles != null)
        {
            Instantiate(treasureData.rarityParticles, transform);
        }

        // Add or configure glow light
        Light glowLight = GetComponent<Light>();
        if (glowLight == null)
        {
            glowLight = gameObject.AddComponent<Light>();
        }

        glowLight.range = 2f;
        glowLight.intensity = 0.5f;
        glowLight.color = treasureData.glowColor;
    }

    // ========================================================================
    // Trigger Detection
    // ========================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            isPlayerInRange = true;
            playerController = pc;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null && pc == playerController)
        {
            isPlayerInRange = false;
            playerController = null;
            hasBeenPickedUp = false; // Allow re-attempt if player re-enters
        }
    }
}

