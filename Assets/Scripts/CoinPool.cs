using UnityEngine;
using System.Collections.Generic;

public class CoinPool : MonoBehaviour
{
    public static CoinPool Instance { get; private set; }

    [SerializeField] private Coin coinPrefab;
    [SerializeField] private int initialPoolSize = 30;

    private Queue<Coin> pool = new Queue<Coin>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            Coin newCoin = Instantiate(coinPrefab, transform);
            newCoin.gameObject.SetActive(false); // Tắt đi cất vào kho
            pool.Enqueue(newCoin);
        }
    }

    public Coin Spawn(Vector3 position)
    {
        Coin coin;
        if (pool.Count > 0)
        {
            coin = pool.Dequeue();
        }
        else
        {
            // Nếu hết tiền trong kho thì đẻ thêm
            coin = Instantiate(coinPrefab, transform);
        }

        coin.transform.position = position;
        coin.gameObject.SetActive(true); // Bật lên
        return coin;
    }

    public void ReturnToPool(Coin coin)
    {
        coin.gameObject.SetActive(false);
        pool.Enqueue(coin);
    }
}