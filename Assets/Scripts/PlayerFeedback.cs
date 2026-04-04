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
    private Vector3 baseWeaponPos;
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

    // them bien luu mask
    private int enemyLayerMask;

    void Start()
    {
        // luu thong tin layer vao bien ngay tu dau
        enemyLayerMask = LayerMask.GetMask("Enemy");

        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
        if (weaponPivot != null) baseWeaponPos = weaponPivot.localPosition;
    }



    private void ApplyWeaponSwitch(Sprite newSprite)
    {
        if (weaponSpriteRenderer != null && newSprite != null)
        {
            // kill tween dang chay de tranh loi hinh anh
            weaponSpriteRenderer.transform.DOKill();

            // FIX LỖI KÉO GIÃN VĨNH VIỄN: Luôn trả về scale gốc (1, 1, 1) trước khi tạo hiệu ứng co giãn mới
            weaponSpriteRenderer.transform.localScale = Vector3.one;

            weaponSpriteRenderer.sprite = newSprite;

            // hieu ung nhe nhe khi rut sung ra
            weaponSpriteRenderer.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0), 0.15f, 10, 1);
        }
    }

    private void ApplyJumpFeel()
    {
        // Đã có Animator lo dáng nhảy, feedback chỉ cần nhả bụi
        if (jumpDust != null) jumpDust.Play();
    }

    private void ApplyLandFeel()
    {
        // Đã có Animator lo dáng đáp đất, feedback chỉ gọi bụi
        if (landDust != null) landDust.Play();
    }



    private void ApplyShootFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            weaponPivot.localPosition = baseWeaponPos;

            // SỬA Ở ĐÂY: Thay số -0.5f thành -0.1f hoặc -0.05f 
            // Giảm cả thời gian giật (0.05f -> 0.02f) để súng nảy nhanh hơn khi sấy
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

            // SỬA DÒNG NÀY: Trả về vị trí gốc
            weaponPivot.localPosition = baseWeaponPos;

            weaponPivot.DOLocalMoveY(baseWeaponPos.y + 0.4f, 0.1f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveY(baseWeaponPos.y, 0.2f));

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
        // dung bien da cache thay vi tao mask bang string moi khi ban dan
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
        // Animator đã lo lộn vòng, ở đây mình tạo cảm giác lướt gió bằng cách gọi cả 2 bụi
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