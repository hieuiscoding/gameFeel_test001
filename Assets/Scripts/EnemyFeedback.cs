using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(EnemyController))]
public class EnemyFeedback : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform enemyBody; // cai hinh anh cua quai
    [SerializeField] private SpriteRenderer spriteRenderer; // sprite renderer chinh cua quai
    [SerializeField] private ParticleSystem bloodVFX;
    [SerializeField] private ParticleSystem deathVFX; // them hieu ung no xac

    [SerializeField] private AudioSource voiceSource; // Cai loa gan tren co quai

    [Header("audio settings")]
    [SerializeField] private AudioClip deathSound; // sound khi quai chet
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float deathPitchVariance = 0.2f; // random pitch tren inspector

    [Header("horror sound design")]
    [SerializeField] private AudioClip[] idleSounds; // tieng ren ri, tho doc
    [SerializeField] private AudioClip[] anticipateSounds; // tieng ruc rich, be khop
    [SerializeField] private AudioClip[] dashSounds; // tieng gam thet xuyen thau
    [SerializeField] private float minIdleSoundInterval = 2f; // thoi gian it nhat de phat tieng
    [SerializeField] private float maxIdleSoundInterval = 5f; // thoi gian lau nhat de phat tieng
    private float nextIdleSoundTime;

    [Header("hit feel settings")]
    [SerializeField] private Color hitFlashColor = Color.white; // chop trang
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float wobbleStrength = 15f; // do rung lac khi an dan
    [SerializeField] private int bloodEmitCount = 5; // so hat mau xit ra moi vien dan
    [SerializeField] private float vfxCooldown = 0.05f; // cooldown tranh goi vfx qua day

    [Header("dash warning")]
    [SerializeField] private Color warningColor = Color.yellow; // mau canh bao rinh moi

    [Header("loot")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int minCoins = 1;
    [SerializeField] private int maxCoins = 3;

    [Header("animation")]
    [SerializeField] private Animator anim;

    [Header("POP-OUT MẮT KHI CHẾT")]
    [SerializeField] private SpriteRenderer leftEye;
    [SerializeField] private SpriteRenderer rightEye;
    [SerializeField] private float eyeFlyDistance = 3f; // Khoảng cách bay xa
    [SerializeField] private float eyeFlyDuration = 0.6f; // Thời gian bay
    [SerializeField] private float eyeArcHeight = 1f; // Độ cao cầu vồng
    [SerializeField] private float eyeFadeDuration = 1.0f; // Thời gian mờ đi

    private Vector3 leftEyeStartOffset; // Luu vi tri ban dau
    private Vector3 rightEyeStartOffset;
    private Transform leftEyeParent;
    private Transform rightEyeParent;
    private Vector3 leftEyeStartScale;
    private Vector3 rightEyeStartScale;

    [SerializeField] private GameObject headObject;
    private EnemyController enemyController;
    private Color originalColor;
    private float lastHitVfxTime; // luu thoi gian lan cuoi chay hit vfx
    private UnityEngine.Rendering.SortingGroup sortingGroup;

    // --- BIẾN CHO HOẠT ẢNH IDLE ---
    private Rigidbody2D rb;
    private readonly int isIdleHash = Animator.StringToHash("isIdle");
    private readonly int doDieHash = Animator.StringToHash("doDie");
    private readonly int deathTypeHash = Animator.StringToHash("DeathType");

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody2D>(); // Lay Rigidbody de do van toc

        if (anim == null) anim = GetComponentInChildren<Animator>();

        sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (leftEye != null)
        {
            leftEyeStartOffset = leftEye.transform.localPosition;
            leftEyeStartScale = leftEye.transform.localScale;
            leftEyeParent = leftEye.transform.parent;
        }
        if (rightEye != null)
        {
            rightEyeStartOffset = rightEye.transform.localPosition;
            rightEyeStartScale = rightEye.transform.localScale;
            rightEyeParent = rightEye.transform.parent;
        }
    }

    void OnEnable()
    {
        enemyController.OnTakeDamage += ApplyHitFeel;
        enemyController.OnDie += ApplyDieFeel;
        enemyController.OnAnticipate += ApplyWarningFeel;
        enemyController.OnDash += ApplyDashFeel;

        ResetVisuals();

        if (sortingGroup != null) sortingGroup.sortingOrder = UnityEngine.Random.Range(0, 1000);

        // dat thoi gian cho tieng ren ri dau tien
        nextIdleSoundTime = Time.time + UnityEngine.Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

    void OnDisable()
    {
        enemyController.OnTakeDamage -= ApplyHitFeel;
        enemyController.OnDie -= ApplyDieFeel;
        enemyController.OnAnticipate -= ApplyWarningFeel;
        enemyController.OnDash -= ApplyDashFeel;

        if (leftEye != null && leftEyeParent != null)
        {
            leftEye.transform.parent = leftEyeParent;
            leftEye.gameObject.SetActive(false);
        }
        if (rightEye != null && rightEyeParent != null)
        {
            rightEye.transform.parent = rightEyeParent;
            rightEye.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (enemyController == null || enemyController.isDead) return;

        // --- ĐỒNG BỘ TRẠNG THÁI IDLE ---
        if (anim != null && rb != null)
        {
            bool isIdle = Mathf.Abs(rb.linearVelocity.x) <= 0.1f;
            anim.SetBool(isIdleHash, isIdle);
        }

        // Phat tieng ren ri ngau nhien khi quai dang ruot duoi
        if (enemyController.currentState == EnemyController.EnemyState.chase)
        {
            if (Time.time >= nextIdleSoundTime)
            {
                PlayRandomSound(idleSounds, 0.6f, 0.15f);
                nextIdleSoundTime = Time.time + UnityEngine.Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
            }
        }
    }

    private void ResetVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = originalColor;
            spriteRenderer.DOFade(1f, 0f);
        }

        if (enemyBody != null)
        {
            enemyBody.gameObject.SetActive(true);
            enemyBody.DOKill();
            float currentFaceDir = Mathf.Sign(enemyBody.localScale.x);
            enemyBody.localScale = new Vector3(currentFaceDir, 1f, 1f);
            enemyBody.rotation = Quaternion.identity;
        }

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (leftEye != null) ResetEye(leftEye, leftEyeStartOffset);
        if (rightEye != null) ResetEye(rightEye, rightEyeStartOffset);

        if (headObject != null) headObject.SetActive(true);
    }

    private void ResetEye(SpriteRenderer eye, Vector3 startLocalPos)
    {
        eye.DOKill();
        eye.transform.parent = (eye == leftEye) ? leftEyeParent : rightEyeParent;
        eye.transform.localPosition = startLocalPos;
        eye.transform.localRotation = Quaternion.identity;
        eye.color = Color.white;

        eye.transform.localScale = (eye == leftEye) ? leftEyeStartScale : rightEyeStartScale;
        eye.gameObject.SetActive(true);
    }

    private void ApplyWarningFeel()
    {
        if (enemyBody != null) enemyBody.DOKill(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = warningColor;
            spriteRenderer.DOColor(originalColor, 0.3f);
        }

        PlayRandomSound(anticipateSounds, 0.8f, 0.1f);
    }

    private void ApplyDashFeel()
    {
        PlayRandomSound(dashSounds, 1f, 0.1f);
    }

    private void ApplyHitFeel()
    {
        if (bloodVFX != null) bloodVFX.Emit(bloodEmitCount);

        if (Time.time < lastHitVfxTime + vfxCooldown) return;
        lastHitVfxTime = Time.time;

        if (enemyBody != null)
        {
            enemyBody.DOKill(true);
            enemyBody.DOPunchRotation(new Vector3(0, 0, wobbleStrength), 0.15f, 10, 1f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = hitFlashColor;
            spriteRenderer.DOColor(originalColor, flashDuration);
        }
    }

    private void ApplyDieFeel()
    {
        // Tat tieng loa ngay lap tuc khi chet
        if (voiceSource != null) voiceSource.Stop();

        bool isOverkill = UnityEngine.Random.value < 0.3f;

        if (deathSound != null && AudioManager.Instance != null)
        {
            var opts = new AudioManager.SFXPlayOptions
            {
                is2D = true,
                volume = deathSoundVolume,
                volumeVariance = 0.06f,
                pitch = 1f,
                pitchVariance = deathPitchVariance,
                maxDelaySeconds = 0f,
                minIntervalPerClip = 0.15f,
                allowStealWhenBusy = true
            };
            AudioManager.Instance.PlaySFX(deathSound, opts);
        }
        else if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        if (deathVFX != null && !isOverkill)
        {
            if (VFXManager.Instance != null)
                VFXManager.Instance.PlayVFX(deathVFX, transform.position, Quaternion.identity);
        }

        if (coinPrefab != null && CoinPool.Instance != null)
        {
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++) CoinPool.Instance.Spawn(transform.position);
        }
        else if (coinPrefab != null)
        {
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++) Instantiate(coinPrefab, transform.position, Quaternion.identity);
        }

        if (isOverkill)
        {
            if (enemyBody != null) enemyBody.gameObject.SetActive(false);
            if (headObject != null) headObject.SetActive(false);

            if (deathVFX != null && VFXManager.Instance != null)
            {
                ParticleSystem fx = VFXManager.Instance.PlayVFX(deathVFX, transform.position, Quaternion.identity, 1.2f);
                fx.Emit(30);
            }
        }
        else
        {
            if (anim != null)
            {
                int randomDeath = UnityEngine.Random.Range(0, 2);
                anim.SetInteger(deathTypeHash, randomDeath);
                anim.SetTrigger(doDieHash);
            }

            if (headObject != null) headObject.SetActive(false);
        }

        float faceDir = Mathf.Sign(enemyBody.localScale.x);
        if (leftEye != null) LaunchEye(leftEye, -1f, faceDir);
        if (rightEye != null) LaunchEye(rightEye, 1f, faceDir);
    }

    private void LaunchEye(SpriteRenderer eye, float directionMultiplier, float facingDirection)
    {
        eye.transform.parent = null;
        float finalXDir = directionMultiplier * facingDirection;

        Vector3 startWorldPos = eye.transform.position;
        Vector3 targetWorldPos = startWorldPos + new Vector3(finalXDir * eyeFlyDistance, 0f, 0f);

        // NHO DAT TEN LAYER SAN NHA LA "Ground" (viet hoa chu G)
        int groundLayer = LayerMask.GetMask("Ground");
        RaycastHit2D hit = Physics2D.Raycast(targetWorldPos, Vector2.down, 15f, groundLayer);

        if (hit.collider != null)
        {
            targetWorldPos.y = hit.point.y + 0.1f;
        }
        else
        {
            targetWorldPos.y -= 5f;
        }

        eye.DOKill();

        eye.transform.DOJump(targetWorldPos, eyeArcHeight, 1, eyeFlyDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Vector3 currentScale = eye.transform.localScale;
            eye.transform.DOScale(new Vector3(currentScale.x * 1.5f, currentScale.y * 0.4f, 1f), 0.1f).SetLoops(2, LoopType.Yoyo);

            eye.DOFade(0f, eyeFadeDuration).SetDelay(0.3f).OnComplete(() =>
            {
                eye.gameObject.SetActive(false);
            });
        });

        eye.transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-720f, 720f)), eyeFlyDuration, RotateMode.FastBeyond360);
    }

    private void PlayRandomSound(AudioClip[] clips, float volume, float pitchVariance)
    {
        if (clips == null || clips.Length == 0 || voiceSource == null) return;

        AudioClip clipToPlay = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clipToPlay == null) return;

        voiceSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
        voiceSource.PlayOneShot(clipToPlay, volume);
    }
}