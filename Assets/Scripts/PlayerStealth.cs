using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

// Manages the player's visibility level based on movement, environment, and abilities.
// Provides an invisibility ability (requires Guile Suit equipped) with cooldown.
// Other systems (e.g., enemy FOV) can read <see cref="visibilityLevel"/> or subscribe
// to <see cref="OnVisibilityChanged"/> and <see cref="OnDetectionStateChanged"/>.
public class PlayerStealth : MonoBehaviour
{
    // ========================================================================
    // Configuration
    // ========================================================================

    [Header("Visibility Range")]
    [Range(0f, 1f)] public float maxVisibility = 1f;
    [Range(0f, 1f)] public float minVisibility = 0.2f;

    [Header("Movement Visibility Thresholds")]
    public float idleVisibility = 0.1f;
    public float walkVisibility = 0.4f;
    public float runVisibility = 1f;
    public float crouchVisibility = 0.2f;

    [Header("Environmental Factors")]
    public float shadowVisibilityReduction = 0.5f;
    public float coverVisibilityReduction = 0.3f;

    [Header("Detection Rates")]
    [Tooltip("How fast visibility rises when moving")]
    public float detectionIncreaseRate = 0.5f;
    [Tooltip("How fast visibility drops when stationary/hidden")]
    public float detectionDecreaseRate = 0.2f;

    [Header("Shadow Detection")]
    [Tooltip("Layer mask for objects that cast shadows / block light")]
    public LayerMask shadowMask;
    [Tooltip("All scene lights to raycast against for shadow detection")]
    public Light2D[] lights;

    [Header("Invisibility Ability")]
    public KeyCode invisibilityKey = KeyCode.F;
    public float invisibilityDuration = 10f;
    public float invisibilityCooldown = 15f;

    [Header("Invisibility Outline")]
    [Tooltip("Child object showing the player outline while invisible (auto-created if null)")]
    public GameObject outlineObject;
    [Tooltip("Material for the outline sprite (auto-created if null)")]
    public Material outlineMaterial;

    // ========================================================================
    // Public State
    // ========================================================================

    // <summary>Current visibility from 0 (hidden) to 1 (fully visible).
    [HideInInspector] public float visibilityLevel;

    // <summary>True if the player is blocked from all lights by shadow-casting geometry.
    [HideInInspector] public bool inShadow;

    // <summary>Set by external trigger zones (e.g., bushes, cover objects).
    [HideInInspector] public bool isInCover;

    // <summary>Flag that enemy FOV systems can read to skip detection entirely.
    public bool fovIgnore { get; private set; }

    // Events
    public System.Action<float> OnVisibilityChanged;
    public System.Action<bool> OnDetectionStateChanged;

    // ========================================================================
    // Private State
    // ========================================================================

    private bool isInvisible;
    private bool onCooldown;
    private float cooldownTimer;
    private float cooldownTotalTime;
    private bool wasDetected;
    private int originalLayer;

    // Cached references
    private PlayerController playerController;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private ItemInventory inventory;
    private Material originalMaterial;
    private Coroutine invisibilityRoutine;

    // ========================================================================
    // Public Queries
    // ========================================================================

    public bool IsInvisible() => isInvisible;
    public bool IsOnCooldown() => onCooldown;
    public float GetVisibilityLevel() => visibilityLevel;
    public bool IsFullyHidden() => visibilityLevel <= minVisibility + 0.05f;
    public bool IsHighlyVisible() => visibilityLevel >= maxVisibility - 0.05f;

    // Returns cooldown progress from 1 (just started) to 0 (ready).
    public float GetCooldownProgress()
    {
        if (!onCooldown || cooldownTotalTime <= 0f) return 0f;
        return Mathf.Clamp01(cooldownTimer / cooldownTotalTime);
    }

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inventory = GetComponent<ItemInventory>();

        if (playerController == null)
        {
            Debug.LogError($"{name}: PlayerStealth requires a PlayerController component.");
        }

        InitializeOutline();
    }

    private void Update()
    {
        CheckShadowStatus();

        // Invisibility activation
        if (Input.GetKeyDown(invisibilityKey) && !isInvisible)
        {
            ActivateInvisibility();
        }

        // Normal visibility calculation when not invisible
        if (!isInvisible)
        {
            CalculateVisibility();
            CheckDetectionState();
        }

        // Keep outline sprite synced while invisible
        UpdateOutlineSprite();

        // Tick cooldown
        TickCooldown();
    }

    // ========================================================================
    // Shadow Detection
    // ========================================================================

    // Raycasts from the player toward each light source. If all lights are blocked
    // by shadow-casting geometry, the player is considered in shadow.
    private void CheckShadowStatus()
    {
        bool shadowedFromAll = true;

        foreach (Light2D light in lights)
        {
            if (light == null) continue;

            Vector2 toLight = (light.transform.position - transform.position);
            float distance = toLight.magnitude;
            Vector2 direction = toLight / distance;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, shadowMask);

            if (!hit)
            {
                // At least one light can see the player
                shadowedFromAll = false;
                break;
            }
        }

        if (inShadow != shadowedFromAll)
        {
            inShadow = shadowedFromAll;
        }
    }

    // ========================================================================
    // Visibility Calculation
    // ========================================================================

    // Smoothly interpolates visibility toward the target based on movement and environment.
    private void CalculateVisibility()
    {
        float target = CalculateTargetVisibility();
        float rate = (visibilityLevel < target ? detectionIncreaseRate : detectionDecreaseRate) * Time.deltaTime;

        visibilityLevel = Mathf.MoveTowards(visibilityLevel, target, rate);
        visibilityLevel = Mathf.Clamp(visibilityLevel, minVisibility, maxVisibility);

        OnVisibilityChanged?.Invoke(visibilityLevel);
    }

    // Determines the target visibility based on speed, shadow, and cover states.
    private float CalculateTargetVisibility()
    {
        float speedNormalized = rb.linearVelocity.magnitude / playerController.moveSpeed;

        float baseVisibility;
        if (speedNormalized > 0.7f)
        {
            baseVisibility = runVisibility;
        }
        else if (speedNormalized > 0.1f)
        {
            baseVisibility = walkVisibility;
        }
        else
        {
            baseVisibility = idleVisibility;
        }

        // Environmental reductions stack multiplicatively
        if (inShadow) baseVisibility *= shadowVisibilityReduction;
        if (isInCover) baseVisibility *= coverVisibilityReduction;

        return baseVisibility;
    }

    // Fires the detection state changed event when visibility crosses the 0.8 threshold.
    private void CheckDetectionState()
    {
        bool isDetected = visibilityLevel > 0.8f;

        if (isDetected != wasDetected)
        {
            OnDetectionStateChanged?.Invoke(isDetected);
            wasDetected = isDetected;
        }
    }

    // ========================================================================
    // Invisibility Ability
    // ========================================================================

    // Activates invisibility if the player has a Guile Suit equipped and is off cooldown.
    public void ActivateInvisibility()
    {
        if (isInvisible || onCooldown) return;

        if (inventory == null || !inventory.HasGuileSuit())
        {
            Debug.Log("No guile suit equipped!");
            return;
        }

        if (invisibilityRoutine != null) StopCoroutine(invisibilityRoutine);
        invisibilityRoutine = StartCoroutine(InvisibilityRoutine());
    }

    private IEnumerator InvisibilityRoutine()
    {
        // Activate
        isInvisible = true;
        fovIgnore = true;
        visibilityLevel = 0f;

        // Visual feedback: semi-transparent with outline
        if (outlineObject != null) outlineObject.SetActive(true);
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);

        // Move to a layer enemies ignore
        originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("IgnoreEnemies");

        SetAllEnemyDetection(false);

        yield return new WaitForSeconds(invisibilityDuration);

        // Deactivate
        isInvisible = false;
        fovIgnore = false;

        if (outlineObject != null) outlineObject.SetActive(false);
        spriteRenderer.color = Color.white;
        gameObject.layer = originalLayer;

        SetAllEnemyDetection(true);

        // Start cooldown
        onCooldown = true;
        cooldownTotalTime = invisibilityCooldown;
        cooldownTimer = invisibilityCooldown;
    }

    // Enables or disables detection on all active enemies.
    private void SetAllEnemyDetection(bool detectable)
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (EnemyAI enemy in enemies)
        {
            if (detectable)
                enemy.EnableDetection();
            else
                enemy.DisableDetection();
        }
    }

    // ========================================================================
    // Outline
    // ========================================================================

    // Creates the outline child object and material if not assigned in the Inspector.
    private void InitializeOutline()
    {
        if (outlineObject == null)
        {
            outlineObject = new GameObject("PlayerOutline");
            outlineObject.transform.SetParent(transform);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localScale = Vector3.one;

            SpriteRenderer outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = spriteRenderer.sprite;
            outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

            if (outlineMaterial == null)
            {
                outlineMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(0.2f, 0.6f, 1f, 0.7f)
                };
            }

            outlineRenderer.material = outlineMaterial;
        }

        outlineObject.SetActive(false);
        originalMaterial = spriteRenderer.material;
    }

    // Keeps the outline sprite synced with the player's current sprite and flip state.
    private void UpdateOutlineSprite()
    {
        if (!isInvisible || outlineObject == null) return;

        SpriteRenderer outlineRenderer = outlineObject.GetComponent<SpriteRenderer>();
        if (outlineRenderer != null)
        {
            outlineRenderer.sprite = spriteRenderer.sprite;
            outlineRenderer.flipX = spriteRenderer.flipX;
            outlineRenderer.flipY = spriteRenderer.flipY;
        }
    }

    // ========================================================================
    // Cooldown
    // ========================================================================

    private void TickCooldown()
    {
        if (!onCooldown || cooldownTimer <= 0f) return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            cooldownTimer = 0f;
            onCooldown = false;
        }
    }

    // ========================================================================
    // External Setters (called by trigger zones / environmental systems)
    // ========================================================================

    public void SetInShadow(bool value) => inShadow = value;
    public void SetInCover(bool value) => isInCover = value;

    // Forces visibility to a specific value for a duration (e.g., flash grenades).
    // Pass duration = 0 for a permanent override (until next calculation).
    public void SetVisibilityOverride(float visibility, float duration = 0f)
    {
        visibilityLevel = Mathf.Clamp(visibility, minVisibility, maxVisibility);

        if (duration > 0f)
        {
            StartCoroutine(ResetVisibilityAfter(duration));
        }
    }

    private IEnumerator ResetVisibilityAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Normal CalculateVisibility will resume on next Update
    }
}
