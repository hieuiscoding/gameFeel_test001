using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "ScriptableObjects/EnemyStats")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("stats")]
    public float maxHealth = 3f;
    public float moveSpeed = 3f;

    [Header("dash attack")]
    public float dashRange = 4f;
    public float dashSpeed = 12f;
    public float anticipateTime = 0.3f;
    public float dashDuration = 0.25f;
    public float cooldownTime = 1f;
}