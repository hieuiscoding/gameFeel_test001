using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Transform[] spawnPoints;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefab == null) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform pt = spawnPoints[randomIndex];

        Instantiate(enemyPrefab, pt.position, Quaternion.identity);
    }
}