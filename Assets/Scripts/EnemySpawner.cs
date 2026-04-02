using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("settings")]
    // da xoa bien enemyPrefab vi gio EnemyPool se quan ly viec do
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
        if (spawnPoints.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform pt = spawnPoints[randomIndex];

        // goi pool lay quai ra thay vi dung instantiate
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