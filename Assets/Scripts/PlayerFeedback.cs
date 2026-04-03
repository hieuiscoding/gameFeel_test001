using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponSpriteRenderer; // them o de luu hinh anh sung
    [SerializeField] private CanvasGroup bloodOverlay;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("screen flash")]
    [SerializeField] private CanvasGroup whiteFlashOverlay;

    [Header("vfx (particle systems)")]
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem landDust;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem shellFX;

    [Header("bullet tracer")]
    [SerializeField] private LineRenderer bulletTracerPrefab; 

    [Header("light vfx")]
    [SerializeField] private Light2D muzzleLight;
    [SerializeField] private float flashIntensity = 3f;
    
    [Header("global light vfx")]
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D globalFlashLight;
    [SerializeField] private float globalFlashIntensity = 1.2f; // do sang toan ban do
    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        playerController.OnJump += ApplyJumpFeel;
        playerController.OnLand += ApplyLandFeel;
        playerController.OnShoot += ApplyShootFeel;
        playerController.OnTakeDamage += ApplyDamageFeel;
        playerController.OnDrawTracer += ApplyTracerFeel; // ve vệt dan
        playerController.OnWeaponSwitched += ApplyWeaponSwitch; // doi sung
        playerController.OnThrowGrenade += ApplyThrowFeel;

    }

    void OnDisable()
    {
        playerController.OnJump -= ApplyJumpFeel;
        playerController.OnLand -= ApplyLandFeel;
        playerController.OnShoot -= ApplyShootFeel;
        playerController.OnTakeDamage -= ApplyDamageFeel;
        playerController.OnDrawTracer -= ApplyTracerFeel;
        playerController.OnWeaponSwitched -= ApplyWeaponSwitch;
        playerController.OnThrowGrenade -= ApplyThrowFeel;
    }

    void Start()
    {
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
    }

    private void ApplyWeaponSwitch(Sprite newSprite)
    {
        if (weaponSpriteRenderer != null && newSprite != null)
        {
            // kill tween dang chay de tranh loi hinh anh
            weaponSpriteRenderer.transform.DOKill();

            weaponSpriteRenderer.sprite = newSprite;

            // hieu ung nhe nhe khi rut sung ra
            weaponSpriteRenderer.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0), 0.15f, 10, 1);
        }
    }

    private void ApplyJumpFeel()
    {
        transform.DOKill();
        transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.15f).OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
        if (jumpDust != null) jumpDust.Play();
    }

    private void ApplyLandFeel()
    {
        transform.DOKill();
        transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f).OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
        if (landDust != null) landDust.Play();
    }

    private void ApplyShootFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();

            // FIX: Chỉ reset vị trí tịnh tiến (Move), TUYỆT ĐỐI KHÔNG reset góc xoay (Rotation)
            weaponPivot.localPosition = Vector3.zero;

            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f));
        }

        if (impulseSource != null) impulseSource.GenerateImpulse(0.2f);
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shellFX != null) shellFX.Play();

        if (muzzleLight != null)
        {
            muzzleLight.DOKill();
            muzzleLight.intensity = flashIntensity;
            DOTween.To(() => muzzleLight.intensity, x => muzzleLight.intensity = x, 0f, 0.1f)
                .SetTarget(muzzleLight);
        }

        if (globalFlashLight != null)
        {
            globalFlashLight.DOKill();
            globalFlashLight.intensity = globalFlashIntensity;
            DOTween.To(() => globalFlashLight.intensity, x => globalFlashLight.intensity = x, 0f, 0.15f)
                .SetTarget(globalFlashLight);
        }
        if (whiteFlashOverlay != null)
        {
            whiteFlashOverlay.DOKill();
            whiteFlashOverlay.alpha = 0.4f;
            whiteFlashOverlay.DOFade(0f, 0.1f);
        }
    }

    private void ApplyThrowFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();

            // FIX: Bỏ reset góc xoay
            weaponPivot.localPosition = Vector3.zero;

            weaponPivot.DOLocalMoveY(0.4f, 0.1f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveY(0f, 0.2f));

            // Chỉ cộng dồn góc xoay nhẹ để tạo cảm giác vung tay, không bẻ gãy hệ thống Aim
            weaponPivot.DOPunchRotation(new Vector3(0, 0, 30f), 0.3f, 10, 1f);
        }
    }


    private void ApplyTracerFeel(Vector3 start, Vector3 end, float damage, float knockbackPower)
    {
        if (bulletTracerPrefab == null) return;
        LineRenderer tracer = Instantiate(bulletTracerPrefab, start, Quaternion.identity);

        // --- DÒNG MỚI ĐỂ FIX LỖI 100% ---
        // Ép buộc LineRenderer phải dùng tọa độ thế giới, bỏ qua mọi vấn đề về hierarchy
        tracer.useWorldSpace = true;

        tracer.startColor = Color.white;
        tracer.endColor = new Color(1, 1, 1, 0);
        tracer.startWidth = 0.08f;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, start);

        // ... (Toàn bộ phần code còn lại của hàm ApplyTracerFeel giữ nguyên) ...

        // 1. Đạn bay đi (Phần đầu tia đạn kéo dài tới điểm end)
        float travelTime = 0.02f;
        DOVirtual.Vector3(start, end, travelTime, v => tracer.SetPosition(1, v))
            .SetUpdate(true)
            .OnComplete(() => {
                // --- ĐẠN THẬT LÀ ĐÂY: Khi tia đạn chạm đích mới tính sát thương ---
                CheckHitAtPoint(end, damage, (end - start).normalized * knockbackPower);

                // 2. Phần đuôi tia đạn co lại (Tia đạn biến mất)
                DOVirtual.Vector3(start, end, 0.03f, v => tracer.SetPosition(0, v))
                    .SetUpdate(true)
                    .OnComplete(() => Destroy(tracer.gameObject));
            });
    }

    private void CheckHitAtPoint(Vector3 point, float damage, Vector2 knockback)
    {
        // Quét một vòng tròn nhỏ tại điểm cuối của tia đạn để tìm quái
        Collider2D hit = Physics2D.OverlapCircle(point, 0.1f, LayerMask.GetMask("Enemy"));
        if (hit != null)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, knockback);
            }
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

        if (impulseSource != null) impulseSource.GenerateImpulse(1.0f);
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float originalVolume = AudioListener.volume;
        AudioListener.volume = 0.15f;
        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        AudioListener.volume = originalVolume;
    }
}