using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;

// ep buoc phai co player controller di kem de doc su kien
[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot; // sung
    [SerializeField] private CanvasGroup bloodOverlay; // ui mau
    [SerializeField] private CinemachineImpulseSource impulseSource; // camera shake

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    // bat buoc phai dang ky su kien trong onenable
    void OnEnable()
    {
        playerController.OnJump += ApplyJumpFeel;
        playerController.OnLand += ApplyLandFeel;
        playerController.OnShoot += ApplyShootFeel;
        playerController.OnTakeDamage += ApplyDamageFeel;
    }

    // bat buoc phai huy dang ky trong ondisable de tranh loi tran bo nho (memory leak)
    void OnDisable()
    {
        playerController.OnJump -= ApplyJumpFeel;
        playerController.OnLand -= ApplyLandFeel;
        playerController.OnShoot -= ApplyShootFeel;
        playerController.OnTakeDamage -= ApplyDamageFeel;
    }

    void Start()
    {
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
    }

    private void ApplyJumpFeel()
    {
        transform.DOKill();
        // keo thuon nguoi (stretch)
        transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.15f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }

    private void ApplyLandFeel()
    {
        transform.DOKill();
        // lun nguoi xuong (squash)
        transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }

    private void ApplyShootFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f));
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(0.2f);
        }
    }

    private void ApplyDamageFeel()
    {
        StartCoroutine(HitStopRoutine(0.06f));

        if (bloodOverlay != null)
        {
            bloodOverlay.DOKill();
            bloodOverlay.alpha = 0.6f;
            bloodOverlay.DOFade(0f, 0.8f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(1.0f);
        }
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}