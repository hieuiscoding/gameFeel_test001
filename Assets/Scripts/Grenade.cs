using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class Grenade : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private SpriteRenderer spriteRenderer; // SpriteRenderer

    [Header("settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float knockbackPower = 20f;
    [SerializeField] private float explosionDelay = 1.5f; // thoi gian tu luc nem den luc no
    [SerializeField] private LayerMask enemyLayer;

    [Header("flashing settings")]
    [SerializeField] private Color flashColor = Color.red; // mau khi nhap nhay (nen dung mau do hoac trang sang)
    [SerializeField] private float initialFlashSpeed = 3f; // toc do nhap nhay ban dau (lan/giay)
    [SerializeField] private float maxFlashSpeed = 15f;    // toc do nhap nhay toi da ngay truoc khi no

    [Header("vfx & sfx")]
    [SerializeField] private ParticleSystem explosionVFX;
    [SerializeField] private AudioClip explosionSound;

    private Rigidbody2D rb;
    private CinemachineImpulseSource impulseSource;
    private Color originalColor;
    private Coroutine flashCoroutine; // luu coroutine de quan ly

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color; // luu lai mau goc
        }
    }

    private void Start()
    {
        // 1. Dem nguoc de no
        Invoke(nameof(Explode), explosionDelay);

        // 2. Bat dau nhap nhay
        if (spriteRenderer != null)
        {
            flashCoroutine = StartCoroutine(FlashSequence());
        }
    }

    // Logic nhap nhay nhanh dan
    private IEnumerator FlashSequence()
    {
        float elapsedTime = 0f;

        while (elapsedTime < explosionDelay)
        {
            // Tinh toan toc do nhap nhay hien tai 
            float percentComplete = elapsedTime / explosionDelay;
            float currentFlashSpeed = Mathf.Lerp(initialFlashSpeed, maxFlashSpeed, percentComplete);

            // Tinh thoi gian bat va tat 
            float flashDuration = 1f / (currentFlashSpeed * 2f);

            // --- BAT MAU ---
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            elapsedTime += flashDuration;

            // Kiem tra lai neu luu dan da no trong luc doi
            if (elapsedTime >= explosionDelay) break;

            //ve mau goc 
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
            elapsedTime += flashDuration;
        }

        // Dam bao tra ve mau goc truoc khi xoa object
        spriteRenderer.color = originalColor;
    }

    private void Explode()
    {
        // Dung Coroutine nhap nhay neu no dang chay
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        if (explosionSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(explosionSound);
        }

        if (explosionVFX != null)
        {
            ParticleSystem fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
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

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}