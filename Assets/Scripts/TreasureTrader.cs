using UnityEngine;
using UnityEngine.UI;


// NPC trader that buys all treasures from the player's inventory in exchange for gold.
// The player presses T while in trigger range to execute a trade.
public class TreasureTrader : MonoBehaviour
{
    [Header("Trader Identity")]
    public string traderName = "Merchant";

    [Header("UI References")]
    [Tooltip("Root panel showing the trade prompt (hidden when player leaves range)")]
    public GameObject tradePromptUI;

    [Tooltip("Text displaying the total gold value of the player's current inventory")]
    public Text tradeValueText;

    [Tooltip("Text displaying trader dialogue messages")]
    public Text traderDialogueText;

    // --- Dialogue pools ---
    private static readonly string[] WelcomeMessages =
    {
        "Let's see what treasures you've found!",
        "Show me what you've collected, adventurer!",
        "I'll give you good gold for those trinkets!",
        "Business is business — let's trade!"
    };

    private static readonly string[] ThanksMessages =
    {
        "Pleasure doing business!",
        "These will fetch a fine price!",
        "Come back with more treasures!",
        "A fair trade indeed!"
    };

    // --- Runtime state ---
    private bool isPlayerInRange;
    private PlayerController playerController;
    private InventorySystem playerInventory;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.T))
        {
            TryTradeWithPlayer();
        }

        UpdateTradeValueDisplay();
    }

    // ========================================================================
    // Trigger Detection
    // ========================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        isPlayerInRange = true;
        playerController = pc;
        playerInventory = pc.inventorySystem;

        // Notify the player controller so it can reference this trader
        pc.SetTraderTarget(this);

        ShowTradePrompt(true);
        SetDialogue(GetRandomMessage(WelcomeMessages));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null || pc != playerController) return;

        pc.ClearTraderTarget(this);

        isPlayerInRange = false;
        playerController = null;
        playerInventory = null;

        ShowTradePrompt(false);
    }

    // ========================================================================
    // Trading
    // ========================================================================

    
    // Sells all items in the player's treasure inventory and adds the gold.
    private void TryTradeWithPlayer()
    {
        if (playerInventory == null) return;

        int totalValue = playerInventory.GetTotalInventoryValue();

        if (totalValue <= 0)
        {
            SetDialogue("You have nothing to trade!");
            return;
        }

        int goldEarned = playerInventory.SellAllItems();
        CurrencySystem.Instance.AddGold(goldEarned);

        SetDialogue(GetRandomMessage(ThanksMessages));
    }

    // ========================================================================
    // UI Helpers
    // ========================================================================

    
    // Continuously updates the trade value text while the player is in range.
    private void UpdateTradeValueDisplay()
    {
        if (tradeValueText == null || playerInventory == null) return;

        int totalValue = playerInventory.GetTotalInventoryValue();
        tradeValueText.text = $"Trade Value: {totalValue} Gold";
        tradeValueText.color = totalValue > 0 ? Color.yellow : Color.gray;
    }

    private void ShowTradePrompt(bool show)
    {
        if (tradePromptUI != null)
        {
            tradePromptUI.SetActive(show);
        }
    }

    private void SetDialogue(string message)
    {
        if (traderDialogueText != null)
        {
            traderDialogueText.text = message;
        }
    }

    private static string GetRandomMessage(string[] messages)
    {
        return messages[Random.Range(0, messages.Length)];
    }
}
