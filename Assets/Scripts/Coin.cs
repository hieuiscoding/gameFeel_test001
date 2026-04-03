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
    private Animator anim;

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

        // Reset hiển thị
        if (sr != null) { Color c = sr.color; c.a = 1f; sr.color = c; }

        // --- QUAN TRỌNG: RESET ANIMATOR ---
        if (anim != null)
        {
            anim.enabled = true; // Bật lại nếu lần trước bị tắt
            anim.Play(0, -1, 0f); // Chơi lại từ frame đầu tiên
        }

        // 2. LOGIC BUNG XU (Chỉ dùng DOTween cho Vị Trí)
        float targetY = transform.position.y + 1.2f;
        transform.DOMoveY(targetY, 0.15f).SetEase(Ease.OutBack).OnComplete(() => {

            float randomX = transform.position.x + UnityEngine.Random.Range(-scatterRange, scatterRange);
            float groundY = transform.position.y;
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(randomX, transform.position.y), Vector2.down, 15f, groundLayer);
            if (hit.collider != null) groundY = hit.point.y + 0.25f;

            transform.DOJump(new Vector3(randomX, groundY, 0), jumpPower, 1, jumpDuration).SetEase(Ease.OutQuad);
        });
    }

    void OnDisable()
    {
        transform.DOKill();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            isCollected = true;

            // Tắt Animator khi nhặt để DOTween có thể xử lý hiệu ứng mờ dần/bay lên mà không bị ghi đè
            if (anim != null) anim.enabled = false;

            // --- LOGIC ÂM THANH ---
            PlayPickupSound();

            transform.DOKill();
            // Bay lên nhẹ và mờ dần
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

    private void PlayPickupSound()
    {
        if (pickupSound == null) return;

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
            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = pickupSound;
            source.volume = 0.8f;
            source.pitch = 1f + Random.Range(-pickupPitchVariance, pickupPitchVariance);
            source.Play();
            Destroy(tempAudio, pickupSound.length);
        }
    }

    private void DespawnCoin()
    {
        if (CoinPool.Instance != null) CoinPool.Instance.ReturnToPool(this);
        else Destroy(gameObject);
    }
}