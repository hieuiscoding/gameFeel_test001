using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(EnemyController))]
public class EnemyFeedback : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform enemyBody; // cai hinh anh cua quai
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem bloodVFX;
    [SerializeField] private ParticleSystem deathVFX; // them hieu ung no xac
    [SerializeField] private AudioClip deathSound; // sound khi quai chet
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f; // 2D volume (AudioManager enforces 2D)

    [Header("hit feel settings")]
    [SerializeField] private Color hitFlashColor = Color.white; // chop trang nhin se "luc" hon
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float wobbleStrength = 15f; // do rung lac khi an dan

    [Header("dash warning")]
    [SerializeField] private Color warningColor = Color.yellow; // mau canh bao rinh moi

    private EnemyController enemyController;
    private Color originalColor;

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color; // luu lai mau goc cua sprite
        }
    }

    void OnEnable()
    {
        enemyController.OnTakeDamage += ApplyHitFeel;
        enemyController.OnDie += ApplyDieFeel;
        enemyController.OnAnticipate += ApplyWarningFeel; // dang ky event gong minh
    }

    void OnDisable()
    {
        enemyController.OnTakeDamage -= ApplyHitFeel;
        enemyController.OnDie -= ApplyDieFeel;
        enemyController.OnAnticipate -= ApplyWarningFeel; // huy dang ky event
    }

    private void ApplyWarningFeel()
    {
        if (enemyBody != null)
        {
            enemyBody.DOKill(true);
            // ep dep xuong giong nhu con meo dang rinh chuot
            enemyBody.DOScale(new Vector3(1.2f, 0.7f, 1f), 0.2f).SetEase(Ease.OutQuad);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            // chop mau canh bao roi tu tra ve mau goc truoc khi lao
            spriteRenderer.color = warningColor;
            spriteRenderer.DOColor(originalColor, 0.3f);
        }
    }

    private void ApplyHitFeel()
    {
        // 1. xit mau
        if (bloodVFX != null) bloodVFX.Play();

        // 2. squash & stretch + rung lac (wobble)
        if (enemyBody != null)
        {
            enemyBody.DOKill(true);

            // bop meo manh hon mot chut
            enemyBody.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f)
                .OnComplete(() => enemyBody.DOScale(Vector3.one, 0.1f));

            // lac nhe truc z tao cam giac chao dao
            enemyBody.DOPunchRotation(new Vector3(0, 0, wobbleStrength), 0.15f, 10, 1f);
        }

        // 3. chop trang roi tra ve mau goc (khong fix cung la mau trang nua)
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = hitFlashColor;
            spriteRenderer.DOColor(originalColor, flashDuration);
        }
    }

    private void ApplyDieFeel()
    {
        // Use AudioManager for 2D, pooled, varied SFX playback
        if (deathSound != null && AudioManager.Instance != null)
        {
            var opts = new AudioManager.SFXPlayOptions
            {
                is2D = true, // enforce non-spatialized 2D playback as requested
                volume = deathSoundVolume,
                volumeVariance = 0.06f,
                pitch = 1f,
                pitchVariance = 0.05f,
                maxDelaySeconds = 0f, // small random delay to avoid perfectly aligned playback
                minIntervalPerClip = 0.15f, // prevent rapid-fire repetition of same death sound
                allowStealWhenBusy = true
            };

            AudioManager.Instance.PlaySFX(deathSound, opts);
        }
        else if (deathSound != null)
        {
            // fallback
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        // 1. tao ra ban sao cua prefab vfx tai vi tri quai chet
        if (deathVFX != null)
        {
            ParticleSystem fx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f); // xoa ban sao nay sau 2s
        }

        // 2. bop bep di sat xuong dat
        if (enemyBody != null)
        {
            enemyBody.DOKill(true);
            enemyBody.DOScale(new Vector3(1.5f, 0.1f, 1f), 0.2f).SetEase(Ease.OutQuad);
        }

        // 3. mo dan roi bien mat
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, 0.2f);
        }
    }
}