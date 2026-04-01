using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public event Action OnTakeDamage;
    public event Action OnDie;

    [Header("stats")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float moveSpeed = 3f;

    private float currentHealth;
    private Transform player;
    private Rigidbody2D rb;

    private float stunTimer;
    private bool isDead = false; // dung bien nay de check xem thanh cai xac chua

    // remember last horizontal direction to avoid getting stuck when player.x ~= enemy.x
    private float lastMoveDirection = 1f;
    // threshold to consider player horizontally aligned
    private const float horizontalEpsilon = 0.05f;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        // neu da chet thi dung suy nghi, de mac cho vat ly gravity, knockback xu ly
        if (isDead || player == null) return;

        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        float dx = player.position.x - transform.position.x;
        float dir;
        if (Mathf.Abs(dx) > horizontalEpsilon)
        {
            dir = Mathf.Sign(dx);
            lastMoveDirection = dir;
        }
        else
        {
            // player roughly aligned on X — keep moving in last known direction rather than stopping
            dir = lastMoveDirection;
        }

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(Mathf.Sign(dir), 1, 1);
    }

    public void TakeDamage(float damage, Vector2 knockbackForce)
    {
        // 1. an dan la phai nhan luc day lui, bat ke song hay chet
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        // 2. neu da chet roi thi chi xit mau thoi, khong tru mau nua
        if (isDead)
        {
            OnTakeDamage?.Invoke(); // van phat tin hieu bi thuong de xit mau
            return;
        }

        // 3. neu con song thi tru mau va lam choang
        currentHealth -= damage;
        stunTimer = 0.2f;

        OnTakeDamage?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        OnDie?.Invoke();

        // nga lan quay ra dat 
        transform.rotation = Quaternion.Euler(0, 0, -90f * Mathf.Sign(transform.localScale.x));

        // doi layer sang Corpse de khong va cham voi Player va Enemy khac
        gameObject.layer = LayerMask.NameToLayer("Corpse");

        Destroy(gameObject, 5f);
    }
}