using UnityEngine;

// Interface for controlling enemy detection capabilities.
// Implemented by EnemyAI to allow external systems to toggle player detection.
public interface IEnemyDetection
{
    void EnableDetection();
    void DisableDetection();
    bool CanDetectPlayer();
}
