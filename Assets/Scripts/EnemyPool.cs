using UnityEngine;
using UnityEngine.Pool; // thu vien pool chuan cua unity

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("settings")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxSize = 50;

    // su dung object pool cua unity thay vi queue
    private IObjectPool<EnemyController> pool;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pool = new ObjectPool<EnemyController>(
            createFunc: CreateEnemy,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        // pre-warm: tao san de tranh giat lag luc moi bat game
        var preWarmArray = new EnemyController[defaultCapacity];
        for (int i = 0; i < defaultCapacity; i++) preWarmArray[i] = pool.Get();
        for (int i = 0; i < defaultCapacity; i++) pool.Release(preWarmArray[i]);
    }

    private EnemyController CreateEnemy()
    {
        return Instantiate(enemyPrefab, transform);
    }

    private void OnTakeFromPool(EnemyController enemy)
    {
        enemy.gameObject.SetActive(true);
        enemy.ResetEnemy();
    }

    private void OnReturnedToPool(EnemyController enemy)
    {
        enemy.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(EnemyController enemy)
    {
        Destroy(enemy.gameObject);
    }

    // api de cac script khac goi
    public EnemyController SpawnEnemy(Vector3 position)
    {
        EnemyController enemy = pool.Get();
        enemy.transform.position = position;
        return enemy;
    }

    public void ReturnToPool(EnemyController enemy)
    {
        pool.Release(enemy);
    }
}