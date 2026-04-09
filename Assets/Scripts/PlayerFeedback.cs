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
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;
    [SerializeField] private CanvasGroup bloodOverlay;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("screen flash")]
    [SerializeField] private CanvasGroup whiteFlashOverlay;

    [Header("vfx (particle systems)")]
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem landDust;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem shellFX;
    private Vector3 baseWeaponPos;

    [Header("bullet tracer")]
    [SerializeField] private LineRenderer bulletTracerPrefab;

    [Header("light vfx")]
    [SerializeField] private Light2D muzzleLight;
    [SerializeField] private float flashIntensity = 3f;


    private PlayerController playerController;

    // --- ĐÃ THÊM BIẾN NÀY ĐỂ CACHE LAYER ---
    private int enemyLayerMask;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
        if (weaponPivot != null) baseWeaponPos = weaponPivot.localPosition;

        // --- CACHE LAYERMASK NGAY LÚC BẮT ĐẦU ---
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void OnEnable()
    {
        playerController.OnJump += ApplyJumpFeel;
        playerController.OnLand += ApplyLandFeel;
        playerController.OnShoot += ApplyShootFeel;
        playerController.OnTakeDamage += ApplyDamageFeel;
        playerController.OnDrawTracer += ApplyTracerFeel;
        playerController.OnWeaponSwitched += ApplyWeaponSwitch;
        playerController.OnThrowGrenade += ApplyThrowFeel;
        playerController.OnRoll += ApplyRollFeel;
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
        playerController.OnRoll -= ApplyRollFeel;
    }

    private void ApplyWeaponSwitch(Sprite newSprite)
    {
        if (weaponSpriteRenderer != null && newSprite != null)
        {
            weaponSpriteRenderer.transform.DOKill();
            weaponSpriteRenderer.transform.localScale = Vector3.one;
            weaponSpriteRenderer.sprite = newSprite;
            weaponSpriteRenderer.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0), 0.15f, 10, 1);
        }
    }

    private void ApplyJumpFeel()
    {
        if (jumpDust != null) jumpDust.Play();
    }

    private void ApplyLandFeel()
    {
        if (landDust != null) landDust.Play();
    }

    private void ApplyShootFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            weaponPivot.localPosition = baseWeaponPos;

            weaponPivot.DOLocalMoveX(baseWeaponPos.x - 0.05f, 0.02f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(baseWeaponPos.x, 0.1f));
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
            weaponPivot.localPosition = baseWeaponPos;

            weaponPivot.DOLocalMoveY(baseWeaponPos.y + 0.4f, 0.1f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveY(baseWeaponPos.y, 0.2f));

            weaponPivot.DOPunchRotation(new Vector3(0, 0, 30f), 0.3f, 10, 1f);
        }
    }

    private void ApplyTracerFeel(Vector3 start, Vector3 end, float damage, float knockbackPower)
    {
        if (BulletTracerPool.Instance == null) return;

        // --- LẤY TIA TỪ POOL ---
        LineRenderer tracer = BulletTracerPool.Instance.GetTracer();
        tracer.transform.position = start;

        tracer.useWorldSpace = true;
        tracer.startColor = Color.white;
        tracer.endColor = new Color(1, 1, 1, 0);
        tracer.startWidth = 0.08f;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, start);

        float travelTime = 0.02f;

        // Hiệu ứng tia đạn bay đi
        DOVirtual.Vector3(start, end, travelTime, v => tracer.SetPosition(1, v))
            .SetUpdate(true)
            .OnComplete(() => {
                CheckHitAtPoint(end, damage, (end - start).normalized * knockbackPower);

                // Hiệu ứng đuôi tia đạn co lại
                DOVirtual.Vector3(start, end, 0.03f, v => tracer.SetPosition(0, v))
                    .SetUpdate(true)
                    .OnComplete(() => {
                        // --- TRẢ TIA VỀ POOL THAY VÌ DESTROY ---
                        BulletTracerPool.Instance.ReturnToPool(tracer);
                    });
            });
    }

    private void CheckHitAtPoint(Vector3 point, float damage, Vector2 knockback)
    {
        // --- SỬ DỤNG BIẾN INT ĐÃ CACHE Ở ĐÂY ---
        Collider2D hit = Physics2D.OverlapCircle(point, 0.1f, enemyLayerMask);
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

    private void ApplyRollFeel()
    {
        if (jumpDust != null) jumpDust.Play();
        if (landDust != null) landDust.Play();
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