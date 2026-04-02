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
            weaponPivot.DOKill(); // dung moi hieu ung dang chay

            // CHOT CHAN: Ep ve vi tri va goc chuan truoc khi giat
            weaponPivot.localPosition = Vector3.zero;
            weaponPivot.localRotation = Quaternion.identity;

            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f));
        }

        if (impulseSource != null) impulseSource.GenerateImpulse(0.2f);
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shellFX != null) shellFX.Play();

        // 1. SỬA CẢ MUZZLE LIGHT CHO CHẮC CỐP
        if (muzzleLight != null)
        {
            muzzleLight.DOKill();
            muzzleLight.intensity = flashIntensity;

            DOTween.To(() => muzzleLight.intensity, x => muzzleLight.intensity = x, 0f, 0.1f)
                .SetTarget(muzzleLight); // <--- Đóng mác chủ nhân vào đây
        }

        // 2. SỬA GLOBAL FLASH LIGHT
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
            whiteFlashOverlay.alpha = 0.4f; // chop trang 40%
            whiteFlashOverlay.DOFade(0f, 0.1f); // bien mat cuc nhanh
        }
    }

    private void ApplyThrowFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();

            // CHOT CHAN: Ep ve vi tri va goc chuan truoc khi vung tay
            weaponPivot.localPosition = Vector3.zero;
            weaponPivot.localRotation = Quaternion.identity;

            // giat sung len tren tao cam giac vung tay nem
            weaponPivot.DOLocalMoveY(0.4f, 0.1f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveY(0f, 0.2f));

            // xoay sung mot chut xiu
            weaponPivot.DOLocalRotate(new Vector3(0, 0, 30f), 0.1f)
                .OnComplete(() => weaponPivot.DOLocalRotate(Vector3.zero, 0.2f));
        }
    }

    private void ApplyTracerFeel(Vector3 start, Vector3 end)
    {
        if (bulletTracerPrefab == null) return;

        // goi pool lay vet dan ra thay vi instantiate
        GameObject tracerObj = SimpleVFXPool.Instance.Spawn(bulletTracerPrefab.gameObject, start, Quaternion.identity);
        LineRenderer tracer = tracerObj.GetComponent<LineRenderer>();

        tracer.startColor = Color.white;
        tracer.endColor = new Color(1, 1, 1, 0);
        tracer.startWidth = 0.08f;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, start);

        DOVirtual.Vector3(start, end, 0.02f, v => tracer.SetPosition(1, v))
            .SetUpdate(true)
            .OnComplete(() => {
                DOVirtual.Vector3(start, end, 0.03f, v => tracer.SetPosition(0, v))
                    .SetUpdate(true)
                    .OnComplete(() => {
                        // ban xong thi tra lai vao pool
                        SimpleVFXPool.Instance.Despawn(tracerObj);
                    });
            });
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