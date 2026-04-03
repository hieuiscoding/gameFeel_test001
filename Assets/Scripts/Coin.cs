using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [Header("bung settings")]
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float scatterRange = 2.5f;

    [Header("pickup settings")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField][Range(0f, 1f)] private float pickupPitchVariance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private bool isCollected = false;
    private SpriteRenderer sr;
    private Animator anim; // Thêm Animator

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void OnEnable()
    {
        isCollected = false;

        // 1. DỌN DẸP TWEEN VỊ TRÍ CŨ
        transform.DOKill();

        // Reset hình ảnh
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        transform.localScale = Vector3.one;

        // 2. RESET ANIMATOR (Quan trọng khi dùng Pool)
        if (anim != null)
        {
            anim.Play(0, -1, 0f); // Chơi lại animation từ frame đầu tiên
            anim.enabled = true;  // Đảm bảo animator đang bật
        }

        // 3. LOGIC BUNG XU (Giữ nguyên phần bay nhảy)
        transform.DOMoveY(transform.position.y + 1.2f, 0.15f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                float randomX = transform.position.x + UnityEngine.Random.Range(-scatterRange, scatterRange);
                float targetY = transform.position.y;

                RaycastHit2D hit = Physics2D.Raycast(new Vector2(randomX, transform.position.y), Vector2.down, 15f, groundLayer);
                if (hit.collider != null)
                {
                    targetY = hit.point.y + 0.25f;
                }

                transform.DOJump(new Vector3(randomX, targetY, 0), jumpPower, 1, jumpDuration)
                    .SetEase(Ease.OutQuad);
            });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            isCollected = true;

            // Tắt Animator khi bị nhặt để nó không ghi đè Scale lúc mình đang làm hiệu ứng mờ dần
            if (anim != null) anim.enabled = false;

            // --- LOGIC ÂM THANH ---
            if (pickupSound != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(pickupSound, new AudioManager.SFXPlayOptions
                    {
                        is2D = true,
                        volume = 0.8f,
                        pitch = 1f,
                        pitchVariance = pickupPitchVariance
                    });
                }
                else
                {
                    GameObject tempAudio = new GameObject("temp_coin_sound");
                    tempAudio.transform.position = transform.position;
                    AudioSource source = tempAudio.AddComponent<AudioSource>();
                    source.clip = pickupSound;
                    source.volume = 0.8f;
                    source.pitch = 1f + Random.Range(-pickupPitchVariance, pickupPitchVariance);
                    source.Play();
                    Destroy(tempAudio, pickupSound.length);
                }
            }

            transform.DOKill();
            // Bay lên nhẹ khi nhặt
            transform.DOMoveY(transform.position.y + 1f, 0.2f).SetEase(Ease.OutQuad);

            if (sr != null)
            {
                sr.DOFade(0f, 0.2f).OnComplete(() => DespawnCoin());
            }
            else
            {
                DOVirtual.DelayedCall(0.2f, DespawnCoin);
            }
        }
    }

    private void DespawnCoin()
    {
        if (CoinPool.Instance != null)
        {
            CoinPool.Instance.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}