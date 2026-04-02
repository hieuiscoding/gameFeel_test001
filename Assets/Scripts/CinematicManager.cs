using UnityEngine;
using Unity.Cinemachine; // dung chuan Cinemachine moi nhat cua bac
using DG.Tweening;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance { get; private set; }

    [Header("cinematic settings")]
    [SerializeField] private CinemachineCamera zoomCam; // camera dac ta
    [SerializeField] private float slowMoTimeScale = 0.1f; // toc do game luc bi cham
    [SerializeField] private float cinematicDuration = 0.5f; // thoi gian quay cham

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TriggerBowlingCinematic(Transform target)
    {
        if (zoomCam == null) return;

        // 1. khoa muc tieu vao con quai xau so
        zoomCam.Follow = target;

        // 2. day priority len cao de chiem quyen dieu khien man hinh
        zoomCam.Priority = 100;

        // 3. slow-motion giam thoi gian game
        Time.timeScale = slowMoTimeScale;

        // 4. hen gio tra moi thu ve binh thuong
        // LUY Y: phai dung SetUpdate(true) de bo qua TimeScale, neu khong DOTween se dem gio cham theo game
        DOVirtual.DelayedCall(cinematicDuration, () =>
        {
            Time.timeScale = 1f;
            zoomCam.Priority = 0; // tra quyen lai cho camera chinh
        }).SetUpdate(true);
    }
}