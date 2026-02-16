using UnityEngine;
using UnityEngine.InputSystem;


// Central player controller handling movement, input routing, crouching,
// and interaction with chests and traders. Uses the new Input System.
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    [Header("Stealth Settings")]
    public bool enableCrouch = true;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [HideInInspector] public bool isCrouching;

    [Header("Inventory")]
    [Tooltip("Treasure inventory system (drag & drop in Inspector)")]
    public InventorySystem inventorySystem;

    [Header("Effects")]
    [HideInInspector]
    public float weightMultiplier = 1f;

    // --- Cached Components ---
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerInput playerInput;
    private PlayerStealth playerStealth;
    private ItemInventory inventory;

    // --- Input Actions ---
    private InputAction moveAction;
    private InputAction crouchAction;
    private InputAction pickupItemAction;
    private InputAction removeItemAction;

    // --- Runtime State ---
    private Vector2 movementInput;
    private Vector2 currentVelocity;
    private float lastMoveX;
    private float lastMoveY;

    // --- Interaction Targets ---
    private EquipmentChest currentChestTarget;
    private TreasureTrader currentTraderTarget;
    private TreasurePickup currentPickupTarget;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Awake()
    {
        // Cache components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
        playerStealth = GetComponent<PlayerStealth>();

        // Resolve input actions
        moveAction = playerInput.actions["Move"];
        crouchAction = playerInput.actions["Crouch"];
        pickupItemAction = playerInput.actions["PickupItem"];
        removeItemAction = playerInput.actions["RemoveItem"];
    }

    private void Start()
    {
        inventory = GetComponent<ItemInventory>();
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<ItemInventory>();
        }
    }

    private void Update()
    {
        // Read movement input
        movementInput = moveAction.ReadValue<Vector2>();
        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }

        HandleInteractionInput();
        HandleCrouchInput();
        UpdateAnimations();
        ApplyMovement();
    }

    // ========================================================================
    // Input Handling
    // ========================================================================

    
    // Processes pickup, chest interaction, drop, and item-use inputs.
    private void HandleInteractionInput()
    {
        // E key: pickup items or interact with chests
        if (pickupItemAction.triggered)
        {
            if (currentPickupTarget != null)
            {
                currentPickupTarget.TryPickup();
            }
            else if (currentChestTarget != null)
            {
                currentChestTarget.Interact(inventory);
            }
        }

        // Drop all treasure items
        if (removeItemAction.triggered && inventorySystem != null)
        {
            inventorySystem.DropAllItems();
        }

        // Equipment use keys
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            inventory.UseItem((int)ItemType.Potion, this);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            inventory.UseItem((int)ItemType.GuileSuit, this);
        }
    }

    
    // Reads the crouch action and updates the animator.
    private void HandleCrouchInput()
    {
        if (!enableCrouch) return;

        isCrouching = crouchAction.ReadValue<float>() > 0.5f;

        if (animator != null)
        {
            animator.SetBool("IsCrouching", isCrouching);
        }
    }

    // ========================================================================
    // Movement
    // ========================================================================

    
    // Applies smoothed movement with speed modifiers for crouching,
    // inventory weight, and external effects (e.g., weight boost potion).
    private void ApplyMovement()
    {
        float speedMultiplier = isCrouching ? crouchSpeedMultiplier : 1f;

        // Slow down based on treasure inventory count
        if (inventorySystem != null)
        {
            int itemCount = inventorySystem.GetItemCount();
            if (itemCount == 3)
            {
                speedMultiplier *= 0.7f;
            }
            else if (itemCount >= 4)
            {
                speedMultiplier *= 0.4f;
            }
        }

        // Apply external modifier (weight boost, etc.)
        speedMultiplier *= weightMultiplier;

        // Smooth acceleration / deceleration
        Vector2 targetVelocity = movementInput * moveSpeed * speedMultiplier;
        float lerpRate = (targetVelocity.magnitude > 0f ? acceleration : deceleration) * Time.deltaTime;

        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, lerpRate);
        rb.linearVelocity = currentVelocity;
    }

    // ========================================================================
    // Animation
    // ========================================================================

    
    // Updates blend-tree parameters and stores the last non-zero direction for idle facing.
    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Horizontal", movementInput.x);
        animator.SetFloat("Vertical", movementInput.y);
        animator.SetFloat("Speed", movementInput.sqrMagnitude);

        // Remember last facing direction for directional idle
        if (movementInput.sqrMagnitude > 0.01f)
        {
            lastMoveX = movementInput.x;
            lastMoveY = movementInput.y;
        }

        animator.SetFloat("LastMoveX", lastMoveX);
        animator.SetFloat("LastMoveY", lastMoveY);
    }

    // ========================================================================
    // Interaction Targets (set by trigger zones)
    // ========================================================================

    public void SetChestTarget(EquipmentChest chest) => currentChestTarget = chest;

    public void ClearChestTarget(EquipmentChest chest)
    {
        if (currentChestTarget == chest) currentChestTarget = null;
    }

    public void SetTraderTarget(TreasureTrader trader) => currentTraderTarget = trader;

    public void ClearTraderTarget(TreasureTrader trader)
    {
        if (currentTraderTarget == trader) currentTraderTarget = null;
    }

    public void SetPickupTarget(TreasurePickup pickup) => currentPickupTarget = pickup;

    public void ClearPickupTarget(TreasurePickup pickup)
    {
        if (currentPickupTarget == pickup) currentPickupTarget = null;
    }

    // ========================================================================
    // Input System Event Callback
    // ========================================================================

    
    // Called automatically by PlayerInput when the Move action value changes.
    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }
}
