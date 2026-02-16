using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Periodically spawns treasure pickups within a defined area using weighted rarity selection.
// Tracks active treasure count and respawns when below the cap.
public class TreasureSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public TreasureData[] possibleTreasures;
    public int maxTreasuresOnMap = 15;
    public float spawnInterval = 30f;
    public Vector2 spawnArea = new Vector2(20f, 20f);
    public float spawnZLayer;

    [Header("Rarity Weights")]
    public int commonWeight = 50;
    public int uncommonWeight = 30;
    public int rareWeight = 15;
    public int epicWeight = 4;
    public int legendaryWeight = 1;

    private int currentTreasureCount;

    // Pre-built rarity lookup for O(1) selection after initial build
    private Dictionary<TreasureRarity, List<TreasureData>> treasuresByRarity;

    // Cached wait
    private WaitForSeconds respawnWait;

    // ========================================================================
    // Unity Lifecycle
    // ========================================================================

    private void Start()
    {
        BuildRarityLookup();
        respawnWait = new WaitForSeconds(spawnInterval);

        SpawnInitialTreasures();
        StartCoroutine(RespawnLoop());
    }

    // ========================================================================
    // Spawning
    // ========================================================================

    
    // Spawns half the max capacity at startup.
    private void SpawnInitialTreasures()
    {
        int count = maxTreasuresOnMap / 2;

        for (int i = 0; i < count; i++)
        {
            SpawnTreasureAtRandomLocation();
        }
    }

    
    // Periodically spawns 1–3 treasures if below the cap.
    private IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return respawnWait;

            if (currentTreasureCount >= maxTreasuresOnMap) continue;

            int batch = Random.Range(1, 4);
            for (int i = 0; i < batch && currentTreasureCount < maxTreasuresOnMap; i++)
            {
                SpawnTreasureAtRandomLocation();
            }
        }
    }

    
    // Picks a random rarity, selects a matching treasure, and instantiates it.
    private void SpawnTreasureAtRandomLocation()
    {
        if (possibleTreasures.Length == 0) return;

        TreasureData treasure = GetRandomTreasureByRarity();
        if (treasure == null || treasure.worldPrefab == null) return;

        Vector3 position = GetRandomSpawnPosition();
        position.z = spawnZLayer;

        GameObject instance = Instantiate(treasure.worldPrefab, position, Quaternion.identity);

        TreasurePickup pickup = instance.GetComponent<TreasurePickup>();
        if (pickup != null)
        {
            pickup.treasureData = treasure;
            pickup.SetSpawner(this);
        }

        currentTreasureCount++;
    }

    
    // Called by <see cref="TreasurePickup"/> when a treasure is collected.
    public void OnTreasurePickedUp()
    {
        currentTreasureCount = Mathf.Max(0, currentTreasureCount - 1);
    }

    // ========================================================================
    // Rarity Selection
    // ========================================================================

    
    // Groups possibleTreasures by rarity for fast lookup during spawn.
    private void BuildRarityLookup()
    {
        treasuresByRarity = new Dictionary<TreasureRarity, List<TreasureData>>();

        foreach (TreasureData treasure in possibleTreasures)
        {
            if (!treasuresByRarity.ContainsKey(treasure.rarity))
            {
                treasuresByRarity[treasure.rarity] = new List<TreasureData>();
            }

            treasuresByRarity[treasure.rarity].Add(treasure);
        }
    }

    
    // Weighted random rarity roll, then picks a random treasure of that rarity.
    // Falls back to any treasure if none match the selected rarity.
    private TreasureData GetRandomTreasureByRarity()
    {
        int totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight;
        int roll = Random.Range(0, totalWeight);

        TreasureRarity selectedRarity;

        if (roll < commonWeight)
            selectedRarity = TreasureRarity.Common;
        else if (roll < commonWeight + uncommonWeight)
            selectedRarity = TreasureRarity.Uncommon;
        else if (roll < commonWeight + uncommonWeight + rareWeight)
            selectedRarity = TreasureRarity.Rare;
        else if (roll < commonWeight + uncommonWeight + rareWeight + epicWeight)
            selectedRarity = TreasureRarity.Epic;
        else
            selectedRarity = TreasureRarity.Legendary;

        // Fast lookup from pre-built dictionary
        if (treasuresByRarity.TryGetValue(selectedRarity, out List<TreasureData> matches) && matches.Count > 0)
        {
            return matches[Random.Range(0, matches.Count)];
        }

        // Fallback: any treasure
        return possibleTreasures[Random.Range(0, possibleTreasures.Length)];
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 center = transform.position;
        float halfX = spawnArea.x * 0.5f;
        float halfY = spawnArea.y * 0.5f;

        return new Vector3(
            center.x + Random.Range(-halfX, halfX),
            center.y + Random.Range(-halfY, halfY),
            0f
        );
    }

    // ========================================================================
    // Debug
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, 0f));
    }
}
