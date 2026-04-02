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
    }

    void OnDisable()
    {
        playerController.OnJump -= ApplyJumpFeel;
        playerController.OnLand -= ApplyLandFeel;
        playerController.OnShoot -= ApplyShootFeel;
        playerController.OnTakeDamage -= ApplyDamageFeel;
        playerController.OnDrawTracer -= ApplyTracerFeel;
        playerController.OnWeaponSwitched -= ApplyWeaponSwitch;
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
            DOTween.To(() => muzzleLight.intensity, x => muzzleLight.intensity = x, 0f, 0.1f);
        }
    }

    private void ApplyTracerFeel(Vector3 start, Vector3 end)
    {
        if (bulletTracerPrefab == null) return;
        LineRenderer tracer = Instantiate(bulletTracerPrefab, start, Quaternion.identity);

        tracer.startColor = Color.white;
        tracer.endColor = new Color(1, 1, 1, 0);
        tracer.startWidth = 0.08f;

        // Ban đầu thu gọn vệt đạn lại đúng 1 điểm ở nòng súng
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, start);

        DOVirtual.Vector3(start, end, 0.02f, v => tracer.SetPosition(1, v))
            .SetUpdate(true) 
            .OnComplete(() => {
              
                DOVirtual.Vector3(start, end, 0.03f, v => tracer.SetPosition(0, v))
                    .SetUpdate(true)
                    .OnComplete(() => Destroy(tracer.gameObject));
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