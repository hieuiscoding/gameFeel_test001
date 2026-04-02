using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("settings")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private int initialPoolSize = 20; // so luong quai tao san

    private Queue<EnemyController> pool = new Queue<EnemyController>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // tao san quai bo vao kho luc moi load game
        for (int i = 0; i < initialPoolSize; i++)
        {
            EnemyController enemy = Instantiate(enemyPrefab, transform);
            enemy.gameObject.SetActive(false);
            pool.Enqueue(enemy);
        }
    }

    public EnemyController SpawnEnemy(Vector3 position)
    {
        EnemyController enemy;

        // neu kho het quai (vi du ban can 21 con ma kho chi co 20), tao them
        if (pool.Count == 0)
        {
            enemy = Instantiate(enemyPrefab, transform);
        }
        else
        {
            enemy = pool.Dequeue();
        }

        enemy.transform.position = position;
        enemy.gameObject.SetActive(true);
        enemy.ResetEnemy(); // reset lai mau, trang thai

        return enemy;
    }

    public void ReturnToPool(EnemyController enemy)
    {
        enemy.gameObject.SetActive(false);
        pool.Enqueue(enemy);
    }
}