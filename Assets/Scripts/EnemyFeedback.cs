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

    [Header("audio settings")]
    [SerializeField] private AudioClip deathSound; // sound khi quai chet
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float deathPitchVariance = 0.2f; // them bien random pitch tren inspector

    [Header("hit feel settings")]
    [SerializeField] private Color hitFlashColor = Color.white; // chop trang
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float wobbleStrength = 15f; // do rung lac khi an dan
    [SerializeField] private int bloodEmitCount = 5; // so hat mau xit ra moi vien dan
    [SerializeField] private float vfxCooldown = 0.05f; // cooldown de tranh goi vfx qua day

    [Header("dash warning")]
    [SerializeField] private Color warningColor = Color.yellow; // mau canh bao rinh moi

    [Header("loot")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int minCoins = 1;
    [SerializeField] private int maxCoins = 3;

    private EnemyController enemyController;
    private Color originalColor;
    private float lastHitVfxTime; // luu thoi gian lan cuoi chay hit vfx

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

        // moi lan quai duoc bat len tu pool thi tra ve hinh dang ban dau
        ResetVisuals();
    }

    void OnDisable()
    {
        enemyController.OnTakeDamage -= ApplyHitFeel;
        enemyController.OnDie -= ApplyDieFeel;
        enemyController.OnAnticipate -= ApplyWarningFeel; // huy dang ky event
    }

    private void ResetVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = originalColor; // tra lai alpha la 1
        }

        if (enemyBody != null)
        {
            enemyBody.DOKill();
            enemyBody.localScale = Vector3.one; // tra lai kich thuoc 1 1 1
        }
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
        // 1. xit mau (dung emit thay vi play de toi uu fps)
        if (bloodVFX != null)
        {
            bloodVFX.Emit(bloodEmitCount);
        }

        // kiem tra cooldown cho cac hieu ung nang hon nhu tween
        if (Time.time < lastHitVfxTime + vfxCooldown) return;
        lastHitVfxTime = Time.time;

        // 2. squash and stretch + rung lac
        if (enemyBody != null)
        {
            enemyBody.DOKill(true);

            // bop meo manh hon mot chut
            enemyBody.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f)
                .OnComplete(() => enemyBody.DOScale(Vector3.one, 0.1f));

            // lac nhe truc z tao cam giac chao dao
            enemyBody.DOPunchRotation(new Vector3(0, 0, wobbleStrength), 0.15f, 10, 1f);
        }

        // 3. chop trang roi tra ve mau goc
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = hitFlashColor;
            spriteRenderer.DOColor(originalColor, flashDuration);
        }
    }

    private void ApplyDieFeel()
    {
        if (deathSound != null && AudioManager.Instance != null)
        {
            var opts = new AudioManager.SFXPlayOptions
            {
                is2D = true,
                volume = deathSoundVolume,
                volumeVariance = 0.06f,
                pitch = 1f,
                pitchVariance = deathPitchVariance, // su dung bien tren inspector
                maxDelaySeconds = 0f,
                minIntervalPerClip = 0.15f,
                allowStealWhenBusy = true
            };

            AudioManager.Instance.PlaySFX(deathSound, opts);
        }
        else if (deathSound != null)
        {
            // fallback ho tro random pitch tao the hien tot hon
            GameObject tempAudio = new GameObject("temp_death_sound");
            tempAudio.transform.position = transform.position;
            AudioSource source = tempAudio.AddComponent<AudioSource>();

            source.clip = deathSound;
            source.volume = deathSoundVolume;
            source.pitch = 1f + UnityEngine.Random.Range(-deathPitchVariance, deathPitchVariance);
            source.Play();

            Destroy(tempAudio, deathSound.length); // huy sau khi chay xong am thanh
        }

        if (deathVFX != null)
        {
            // Thay vì Destroy, nếu bác có ParticlePool thì dùng, còn không thì giữ nguyên Instantiate 
            // nhưng nhớ Check null cho kỹ
            ParticleSystem fx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }

        // 2. LOGIC NHẢ ĐỒNG XU (SỬA LẠI CHUẨN)
        if (coinPrefab != null && CoinPool.Instance != null)
        {
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                // CHỈ GỌI MỘT DÒNG DUY NHẤT NÀY
                CoinPool.Instance.Spawn(transform.position);
            }
        }
        else if (coinPrefab != null)
        {
            // Fallback nếu bác quên chưa đặt CoinPool vào Scene
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Instantiate(coinPrefab, transform.position, Quaternion.identity);
            }
        }

        // 2. bop bep di sat xuong dat
        if (enemyBody != null)
        {
            enemyBody.DOKill(true);
            enemyBody.DOScale(new Vector3(1.5f, 0.1f, 1f), 0.2f).SetEase(Ease.OutQuad);
        }

        // 3. mo dan roi bien mat TRUOC KHI bi thu hoi vao pool
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill(); // dam bao kill cac tween truoc do

            // Cho 4.5 giay roi moi bat dau fade mo di trong 0.5 giay
            spriteRenderer.DOFade(0f, 0.5f).SetDelay(4.5f);
        }
    }
}