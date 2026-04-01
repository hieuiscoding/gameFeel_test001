using UnityEngine;
using System;

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

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(dir, 1, 1);
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
        isDead = true; // danh dau la da chet
        OnDie?.Invoke();

        // nga lan quay ra dat (xoay 90 do ra dang sau)
        transform.rotation = Quaternion.Euler(0, 0, -90f * Mathf.Sign(transform.localScale.x));

        // doi layer sang default de nguoi choi co the di xuyen qua xac (khong bi ket)
        gameObject.layer = LayerMask.NameToLayer("Default");

        // cho phep nguoi choi hanh ha cai xac trong 5 giay roi moi xoa
        Destroy(gameObject, 5f);
    }
}