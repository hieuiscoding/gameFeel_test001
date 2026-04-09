using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class BulletTracerPool : MonoBehaviour
{
    public static BulletTracerPool Instance;

    [SerializeField] private LineRenderer tracerPrefab;
    [SerializeField] private int poolSize = 30;

    private Queue<LineRenderer> pool = new Queue<LineRenderer>();

    void Awake()
    {
        Instance = this;

        // Khởi tạo sẵn các tia đạn trong bộ nhớ
        for (int i = 0; i < poolSize; i++)
        {
            LineRenderer tracer = Instantiate(tracerPrefab, transform);
            tracer.gameObject.SetActive(false);
            pool.Enqueue(tracer);
        }
    }

    public LineRenderer GetTracer()
    {
        if (pool.Count > 0)
        {
            LineRenderer tracer = pool.Dequeue();
            tracer.gameObject.SetActive(true);
            return tracer;
        }

        // Nếu hết tia trong pool, tạo mới (phòng trường hợp bắn quá nhanh)
        return Instantiate(tracerPrefab, transform);
    }

    public void ReturnToPool(LineRenderer tracer)
    {
        tracer.DOKill(); // Dừng mọi hiệu ứng DOTween đang chạy trên tia này
        tracer.gameObject.SetActive(false);
        pool.Enqueue(tracer);
    }
}