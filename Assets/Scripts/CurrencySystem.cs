using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Singleton managing the player's gold currency.
// Persists across scenes via DontDestroyOnLoad.
public class CurrencySystem : MonoBehaviour
{
    public static CurrencySystem Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Text element displaying the current gold amount")]
    public Text goldText;

    [Tooltip("Prefab for the floating '+X Gold' popup")]
    public GameObject goldChangePrefab;

    [Tooltip("Parent transform for popup instances")]
    public Transform goldChangeParent;

    [Header("Settings")]
    public int currentGold;

    private const float POPUP_LIFETIME = 2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Adds gold and shows a floating popup indicating the amount gained.
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
        ShowGoldPopup(amount);
    }

    // Attempts to spend the given amount. Returns true if the player had enough gold.
    public bool SpendGold(int amount)
    {
        if (currentGold < amount) return false;

        currentGold -= amount;
        UpdateGoldUI();
        return true;
    }

    // Returns the current gold balance.
    public int GetCurrentGold() => currentGold;

    // Refreshes the gold display text.
    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"{currentGold} Gold";
        }
    }

    // Instantiates a temporary floating text popup showing the gold gained.
    private void ShowGoldPopup(int amount)
    {
        if (goldChangePrefab == null || goldChangeParent == null) return;

        GameObject popup = Instantiate(goldChangePrefab, goldChangeParent);

        Text popupText = popup.GetComponent<Text>();
        if (popupText != null)
        {
            popupText.text = $"+{amount} Gold";
            popupText.color = Color.yellow;
        }

        Destroy(popup, POPUP_LIFETIME);
    }

}
