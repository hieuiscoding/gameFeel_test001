using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public event Action OnTakeDamage;
    public event Action OnDie;
    public event Action OnAnticipate; // bao hieu luc bat dau gong de feedback chay hieu ung

    public enum EnemyState { chase, anticipate, dash, cooldown }
    public EnemyState currentState = EnemyState.chase;

    [Header("stats")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("dash attack")]
    [SerializeField] private float dashRange = 4f; // khoang cach bat dau lao
    [SerializeField] private float dashSpeed = 12f; // toc do lao
    [SerializeField] private float anticipateTime = 0.3f; // thoi gian gong (dung yen)
    [SerializeField] private float dashDuration = 0.25f; // thoi gian bay tren khong
    [SerializeField] private float cooldownTime = 1f; // thoi gian tho sau khi vồ hụt

    private float currentHealth;
    private Transform player;
    private Rigidbody2D rb;

    private float stunTimer;
    private bool isDead = false;

    private float lastMoveDirection = 1f;
    private const float horizontalEpsilon = 0.05f;

    // cac bien cho state machine
    private float stateTimer;
    private float dashDirection;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;

        // bi ban trung la huy moi trang thai, bi choang
        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        float dx = player.position.x - transform.position.x;
        float distToPlayer = Mathf.Abs(dx);

        // xac dinh huong quay mat
        float dir = lastMoveDirection;
        if (distToPlayer > horizontalEpsilon)
        {
            dir = Mathf.Sign(dx);
            if (currentState != EnemyState.dash) lastMoveDirection = dir; // khong quay mat luc dang bay
        }

        // may trang thai (state machine)
        switch (currentState)
        {
            case EnemyState.chase:
                rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

                // neu du gan thi bat dau gong
                if (distToPlayer <= dashRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // dung lai
                    currentState = EnemyState.anticipate;
                    stateTimer = anticipateTime;
                    OnAnticipate?.Invoke(); // bao cho hieu ung biet
                }
                break;

            case EnemyState.anticipate:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // dung im rinh moi
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    // bat dau phong
                    currentState = EnemyState.dash;
                    stateTimer = dashDuration;
                    dashDirection = dir; // khoa huong bay
                }
                break;

            case EnemyState.dash:
                // bay voi toc do cao
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    // bay xong thi dung lai tho doc
                    currentState = EnemyState.cooldown;
                    stateTimer = cooldownTime;
                }
                break;

            case EnemyState.cooldown:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // dung yen tho
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.chase;
                }
                break;
        }

        // quay hinh anh quai
        transform.localScale = new Vector3(lastMoveDirection, 1, 1);
    }

    public void TakeDamage(float damage, Vector2 knockbackForce)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        if (isDead)
        {
            OnTakeDamage?.Invoke();
            return;
        }

        currentHealth -= damage;
        stunTimer = 0.2f;

        // neu dang gong hoac dang vồ ma an dan thi reset ve chase
        if (currentState != EnemyState.cooldown)
        {
            currentState = EnemyState.chase;
        }

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

        transform.rotation = Quaternion.Euler(0, 0, -90f * Mathf.Sign(transform.localScale.x));
        gameObject.layer = LayerMask.NameToLayer("Corpse");
        Destroy(gameObject, 5f);
    }
}