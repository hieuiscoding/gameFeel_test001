using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public event Action OnTakeDamage;
    public event Action OnDie;
    public event Action OnAnticipate;

    public enum EnemyState { chase, anticipate, dash, cooldown }
    public EnemyState currentState = EnemyState.chase;

    [Header("stats")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("dash attack")]
    [SerializeField] private float dashRange = 4f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float anticipateTime = 0.3f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float cooldownTime = 1f;

    private float currentHealth;

    // dung static de tim player 1 lan duy nhat cho tat ca quai
    private static Transform playerRef;

    private Rigidbody2D rb;

    private float stunTimer;
    public bool isDead = false;

    private float lastMoveDirection = 1f;
    private const float horizontalEpsilon = 0.05f;

    private float stateTimer;
    private float dashDirection;
    private int originalLayer;
    private int corpseLayer;

    // bien thay the cho invoke()
    private float despawnTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalLayer = gameObject.layer;
        corpseLayer = LayerMask.NameToLayer("Corpse");
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (playerRef == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRef = p.transform;
        }
    }

    void Update()
    {
        // dung update de dem gio huy xac thay vi invoke
        if (isDead)
        {
            if (despawnTimer > 0)
            {
                despawnTimer -= Time.deltaTime;
                if (despawnTimer <= 0)
                {
                    Despawn();
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || playerRef == null) return;

        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        float dx = playerRef.position.x - transform.position.x;
        float distToPlayer = Mathf.Abs(dx);

        float dir = lastMoveDirection;
        if (distToPlayer > horizontalEpsilon)
        {
            dir = Mathf.Sign(dx);
            if (currentState != EnemyState.dash) lastMoveDirection = dir;
        }

        switch (currentState)
        {
            case EnemyState.chase:
                rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                if (distToPlayer <= dashRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    currentState = EnemyState.anticipate;
                    stateTimer = anticipateTime;
                    OnAnticipate?.Invoke();
                }
                break;

            case EnemyState.anticipate:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.dash;
                    stateTimer = dashDuration;
                    dashDirection = dir;
                }
                break;

            case EnemyState.dash:
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.cooldown;
                    stateTimer = cooldownTime;
                }
                break;

            case EnemyState.cooldown:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.chase;
                }
                break;
        }

        float targetScaleX = -lastMoveDirection;
        if (transform.localScale.x != targetScaleX)
        {
            transform.localScale = new Vector3(targetScaleX, 1, 1);
        }
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

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentState = EnemyState.chase;
        stunTimer = 0f;
        despawnTimer = 0f; // reset timer

        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = 0f;
        transform.rotation = Quaternion.identity;
        gameObject.layer = originalLayer;
    }

    private void Die()
    {
        isDead = true;
        OnDie?.Invoke();

        gameObject.layer = corpseLayer;
        rb.linearDamping = 15f;

        despawnTimer = 5f; // bat dau dem gio despawn
    }

    private void Despawn()
    {
        if (EnemyPool.Instance != null)
        {
            EnemyPool.Instance.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}