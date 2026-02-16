using UnityEngine;
using System;

// Plain data class for serializing player state (position, rotation, health).
// Used by save/load systems. Not a MonoBehaviour — instantiate directly.
[System.Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public Vector3 rotation;
    public float health;

    public PlayerSaveData()
    {
        position = Vector3.zero;
        rotation = Vector3.zero;
        health = 30f;
    }

    public PlayerSaveData(Vector3 position, Vector3 rotation, float health)
    {
        this.position = position;
        this.rotation = rotation;
        this.health = health;
    }
}
