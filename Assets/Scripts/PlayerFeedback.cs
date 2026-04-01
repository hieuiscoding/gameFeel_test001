using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal; // thu vien cho light2d

[RequireComponent(typeof(PlayerController))]
public class PlayerFeedback : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot; // sung
    [SerializeField] private CanvasGroup bloodOverlay; // ui mau
    [SerializeField] private CinemachineImpulseSource impulseSource; // camera shake

    [Header("vfx (particle systems)")]
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem landDust;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem shellFX;

    [Header("bullet tracer")]
    [SerializeField] private LineRenderer bulletTracer;

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
    }

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

    void Update()
    {

    }

    private void ApplyJumpFeel()
    {
        transform.DOKill();
        transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.15f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));

        // phat bui khi nhay
        if (jumpDust != null) jumpDust.Play();
    }

    private void ApplyLandFeel()
    {
        transform.DOKill();
        transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));

        // phat bui khi cham dat
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

        // tia lua sung va vo dan
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shellFX != null) shellFX.Play();

        // chớp den light 2d
        if (muzzleLight != null)
        {
            muzzleLight.DOKill();
            muzzleLight.intensity = flashIntensity;
            DOTween.To(() => muzzleLight.intensity, x => muzzleLight.intensity = x, 0f, 0.1f);
        }

        // 3. ve vet dan
        if (bulletTracer != null)
        {
            bulletTracer.gameObject.SetActive(true);

            bulletTracer.SetPosition(0, playerController.LastShootStart);
            bulletTracer.SetPosition(1, playerController.LastShootEnd);

            // reset mau ve trang tinh truoc khi fade
            Color fullWhite = new Color(1, 1, 1, 1);
            Color clearWhite = new Color(1, 1, 1, 0);
            bulletTracer.startColor = fullWhite;
            bulletTracer.endColor = clearWhite;

            // dung DOVirtual de fade mau cua linerenderer tu 1 ve 0
            DOTween.To(() => bulletTracer.startColor, x => bulletTracer.startColor = x, new Color(1, 1, 1, 0), 0.05f)
                .OnComplete(() => bulletTracer.gameObject.SetActive(false));
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
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}