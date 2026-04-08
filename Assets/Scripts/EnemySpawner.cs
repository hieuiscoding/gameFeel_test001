using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("spawn timings")]
    [SerializeField] private float minSpawnInterval = 2.5f; // thoi gian ngan nhat de sinh quai
    [SerializeField] private float maxSpawnInterval = 5f;   // thoi gian lau nhat de sinh quai

    [Header("locations")]
    [SerializeField] private Transform[] spawnPoints;

    // dung cach dem gio toi uu bang time.time
    private float nextSpawnTime;

    void Start()
    {
        // random thoi gian ra mat con quai dau tien luc moi vao game
        SetNextSpawnTime();
    }

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            SetNextSpawnTime(); // random thoi gian cho lan tiep theo
        }
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform pt = spawnPoints[randomIndex];

        if (EnemyPool.Instance != null)
        {
            EnemyPool.Instance.SpawnEnemy(pt.position);
        }
        else
        {
            Debug.LogWarning("khong tim thay enemypool trong scene");
        }
    }
}