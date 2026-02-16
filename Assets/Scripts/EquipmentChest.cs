using UnityEngine;

// Interactive equipment chest that pauses the game and opens a stash UI
// when the player interacts with it. Close with Escape.
public class EquipmentChest : MonoBehaviour
{
    [Tooltip("Root panel of the chest menu UI")]
    public GameObject menuUI;

    [Tooltip("Reference to the stash UI manager that populates chest contents")]
    public StashUIManager stashUI;

    private bool isOpen;
    private ItemInventory playerInventory;

    private void Start()
    {
        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    // Opens the chest UI and pauses the game. Called by the player interaction system.
    // <param name="inventory">The player's current inventory to display/modify.</param>
    public void Interact(ItemInventory inventory)
    {
        if (isOpen) return;

        playerInventory = inventory;
        menuUI.SetActive(true);
        Time.timeScale = 0f;
        isOpen = true;

        if (stashUI != null)
        {
            stashUI.Open(inventory);
        }
    }

    // Closes the chest UI and resumes the game.
    public void Close()
    {
        if (!isOpen) return;

        menuUI.SetActive(false);
        Time.timeScale = 1f;
        isOpen = false;
    }
}
