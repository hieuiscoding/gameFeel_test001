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

    [Header("audio settings")]
    [SerializeField] private AudioClip deathSound; // sound khi quai chet
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float deathPitchVariance = 0.2f; // random pitch tren inspector

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

    // --- ĐÂY LÀ PHẦN THÊM MỚI ---
    [Header("POP-OUT MẮT KHI CHẾT")]
    [SerializeField] private SpriteRenderer leftEye; // Sprite mắt trái
    [SerializeField] private SpriteRenderer rightEye; // Sprite mắt phải
    [SerializeField] private float eyeFlyDistance = 3f; // Khoảng cách bay xa
    [SerializeField] private float eyeFlyDuration = 0.6f; // Thời gian bay
    [SerializeField] private float eyeArcHeight = 1f; // Độ cao cầu vồng
    [SerializeField] private float eyeFadeDuration = 0.8f; // Thời gian mờ đi
    private Vector3 leftEyeStartOffset; // Luu vi tri ban dau
    private Vector3 rightEyeStartOffset;
    // ----------------------------
    [SerializeField] private GameObject headObject;
    private EnemyController enemyController;
    private Color originalColor;
    private float lastHitVfxTime; // luu thoi gian lan cuoi chay hit vfx

    // Khai báo thêm 2 biến này ngay trên hàm Awake
    private Transform leftEyeParent;
    private Transform rightEyeParent;

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // --- SỬA LẠI ĐOẠN NÀY ĐỂ NHỚ ĐÚNG ÔNG BỐ ---
        if (leftEye != null)
        {
            leftEyeStartOffset = leftEye.transform.localPosition;
            leftEyeParent = leftEye.transform.parent; // Nhớ luôn ông bố hiện tại
        }
        if (rightEye != null)
        {
            rightEyeStartOffset = rightEye.transform.localPosition;
            rightEyeParent = rightEye.transform.parent;
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
            spriteRenderer.color = originalColor;
            spriteRenderer.DOFade(1f, 0f); // Reset lại độ mờ phòng khi xác chết bị mờ đi
        }

        if (enemyBody != null)
        {
            enemyBody.DOKill();
            float currentFaceDir = Mathf.Sign(enemyBody.localScale.x);
            enemyBody.localScale = new Vector3(currentFaceDir, 1f, 1f);
            enemyBody.rotation = Quaternion.identity; // Đảm bảo quái đứng thẳng
        }

        // RESET LẠI ANIMATOR VỀ TRẠNG THÁI GỐC
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // --- ĐÂY LÀ PHẦN THÊM MỚI RESET MẮT (Hệ trọng cho Pooling) ---
        if (leftEye != null) ResetEye(leftEye, leftEyeStartOffset);
        if (rightEye != null) ResetEye(rightEye, rightEyeStartOffset);
        // THÊM DÒNG NÀY: Lắp lại cái đầu
        if (headObject != null) headObject.SetActive(true);
    }

    // Helper method de reset con mat
    private void ResetEye(SpriteRenderer eye, Vector3 startLocalPos)
    {
        eye.DOKill();

        // SỬA DÒNG NÀY: Trả về đúng ông bố đã ghi nhớ, không dùng enemyBody nữa
        eye.transform.parent = (eye == leftEye) ? leftEyeParent : rightEyeParent;

        eye.transform.localPosition = startLocalPos;
        eye.transform.localRotation = Quaternion.identity;
        eye.color = Color.white;
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
        // 1. CHẠY AUDIO
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
            GameObject tempAudio = new GameObject("temp_death_sound");
            tempAudio.transform.position = transform.position;
            AudioSource source = tempAudio.AddComponent<AudioSource>();

            source.clip = deathSound;
            source.volume = deathSoundVolume;
            source.pitch = 1f + UnityEngine.Random.Range(-deathPitchVariance, deathPitchVariance);
            source.Play();

            Destroy(tempAudio, deathSound.length);
        }

        // 2. CHẠY VFX (Máu nổ)
        if (deathVFX != null)
        {
            ParticleSystem fx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }

        // 3. NHẢ COIN
        if (coinPrefab != null && CoinPool.Instance != null)
        {
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                CoinPool.Instance.Spawn(transform.position);
            }
        }
        else if (coinPrefab != null)
        {
            int coinCount = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coinCount; i++)
            {
                Instantiate(coinPrefab, transform.position, Quaternion.identity);
            }
        }

        // 4. CHẠY ANIMATOR CHẾT (Cái thân gục xuống)
        if (anim != null) anim.SetTrigger("doDie");

        // 5. MẮT BAY TUNG TOÉ
        float faceDir = Mathf.Sign(enemyBody.localScale.x);

        if (leftEye != null) LaunchEye(leftEye, -1f, faceDir);
        if (rightEye != null) LaunchEye(rightEye, 1f, faceDir);

        // 6. GIẤU CÁI ĐẦU ĐI (Tạo cảm giác nổ đầu)
        if (headObject != null) headObject.SetActive(false);

        // 7. FADE MỜ CÁI THÂN SAU KHI CHẾT
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.DOFade(0f, 1f).SetDelay(2f);
        }
    }

    // --- ĐÂY LÀ PHẦN THÊM MỚI HELPER METHOD ĐỂ BẮN MẮT ---
    private void LaunchEye(SpriteRenderer eye, float directionMultiplier, float facingDirection)
    {
        // Phải tách mắt ra khỏi cha (enemyBody) để nó bay độc lập, 
        // không bị ảnh hưởng bởi animation chết hay lật mặt của quái.
        eye.transform.parent = null;

        // Tính toán vị trí hạ cánh (Local Space relative to the start point, converted to World)
        // Mắt sẽ bay sang bên (theo hướng facingDirection và directionMultiplier) và rớt xuống đất.
        float finalXDir = directionMultiplier * facingDirection;
        Vector3 localStartPos = (eye == leftEye) ? leftEyeStartOffset : rightEyeStartOffset;

        // Vị trí mục tiêu: Bay sang bên eyeFlyDistance mét, rớt xuống đất local level (y=0)
        Vector3 targetLocalPos = localStartPos + new Vector3(finalXDir * eyeFlyDistance, -localStartPos.y, 0f);

        // Convert local target back to a world position *based on where the enemy was when it died*
        Vector3 targetWorldPos = enemyBody.TransformPoint(targetLocalPos);

        // --- XỬ LÝ BAY BẰNG DOTween ---
        eye.DOKill(); // Dung tweens hien tai

        // 1. DOJump: Bay theo hình cầu vồng mượt mà
        eye.transform.DOJump(targetWorldPos, eyeArcHeight, 1, eyeFlyDuration).SetEase(Ease.OutQuad);

        // 2. DORotate: Xoay vòng vòng điên cuồng khi bay
        eye.transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-720f, 720f)), eyeFlyDuration, RotateMode.FastBeyond360);

        // 3. DOFade: Mờ dần và biến mất khi tiếp đất
        eye.DOFade(0f, eyeFadeDuration).SetDelay(eyeFlyDuration * 0.5f); // Fade bắt đầu ở giữa chặng đường bay
    }
}