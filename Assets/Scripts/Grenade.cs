using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class Grenade : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float knockbackPower = 20f;
    [SerializeField] private float explosionDelay = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("flashing settings")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float initialFlashSpeed = 3f;
    [SerializeField] private float maxFlashSpeed = 15f;

    [Header("vfx & sfx")]
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private AudioClip explosionSound;

    private Rigidbody2D rb;
    private CinemachineImpulseSource impulseSource;
    private Color originalColor;

    // Dung timer thu cong de xoa so hoan toan Coroutine (zero rác)
    private float lifeTimer;
    private float flashTimer;
    private bool isFlashingColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    // Dung OnEnable de object tu reset khi duoc bat ra tu Pool
    private void OnEnable()
    {
        lifeTimer = 0f;
        flashTimer = 0f;
        isFlashingColor = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;

        // 1. Kiem tra no
        if (lifeTimer >= explosionDelay)
        {
            Explode();
            return;
        }

        // 2. Xu ly nhap nhay bang toan hoc, khong dung yield return
        if (spriteRenderer != null)
        {
            float percentComplete = lifeTimer / explosionDelay;
            float currentFlashSpeed = Mathf.Lerp(initialFlashSpeed, maxFlashSpeed, percentComplete);
            float flashDuration = 1f / (currentFlashSpeed * 2f);

            flashTimer += Time.deltaTime;
            if (flashTimer >= flashDuration)
            {
                flashTimer = 0f; // reset dong ho nhap nhay
                isFlashingColor = !isFlashingColor; // dao trang thai mau
                spriteRenderer.color = isFlashingColor ? flashColor : originalColor;
            }
        }
    }

    private void Explode()
    {
        if (explosionSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(explosionSound);
        }

        if (explosionVFX != null && SimpleVFXPool.Instance != null)
        {
            // Phat no VFX tu pool
            GameObject fxObj = SimpleVFXPool.Instance.Spawn(explosionVFX.gameObject, transform.position, Quaternion.identity);
            ParticleSystem fx = fxObj.GetComponent<ParticleSystem>();
            fx.Play();

            // Tra VFX ve pool sau 3 giay
            SimpleVFXPool.Instance.Despawn(fxObj, 3f);
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                Vector2 knockback = knockbackDir * knockbackPower;

                enemy.TakeDamage(damage, knockback);
            }
        }

        // Tra cai vo luu dan nay ve kho
        if (SimpleVFXPool.Instance != null)
        {
            SimpleVFXPool.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject); // fallback
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}