using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public event Action OnTakeDamage;
    public event Action OnDie;
    public event Action OnAnticipate;
    public event Action OnDash;

    public enum EnemyState { chase, anticipate, dash, cooldown }
    public EnemyState currentState = EnemyState.chase;

    // --- SỬ DỤNG SCRIPTABLE OBJECT Ở ĐÂY ---
    [Header("stats configuration")]
    [SerializeField] private EnemyStatsSO stats;

    [Header("sensors (AI)")]
    [SerializeField] private float aggroRange = 15f; // Khoảng cách bắt đầu đuổi
    [SerializeField] private Transform ledgeCheck;   // Đặt 1 cục object rỗng ở mũi chân quái
    [SerializeField] private Transform wallCheck;    // Đặt 1 cục object rỗng ở trước mặt quái
    [SerializeField] private LayerMask groundLayer;

    private float currentHealth;
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
    private float despawnTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalLayer = gameObject.layer;
        corpseLayer = LayerMask.NameToLayer("Corpse");
    }

    void Start()
    {
        // lay chi so tu SO
        if (stats != null) currentHealth = stats.maxHealth;
        else Debug.LogError("Enemy thieu file Stats SO!");

        if (playerRef == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRef = p.transform;
        }
    }

    void Update()
    {
        if (isDead && despawnTimer > 0)
        {
            despawnTimer -= Time.deltaTime;
            if (despawnTimer <= 0) Despawn();
        }
    }

    void FixedUpdate()
    {
        if (isDead || playerRef == null || stats == null) return;

        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        float dx = playerRef.position.x - transform.position.x;
        float dy = playerRef.position.y - transform.position.y; // THÊM: Tính cả khoảng cách trục Y
        float distToPlayerX = Mathf.Abs(dx);
        float distToPlayerY = Mathf.Abs(dy);

        // THÊM: Nếu ở quá xa thì đứng yên, không làm gì cả
        if (Vector2.Distance(transform.position, playerRef.position) > aggroRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // environment checks
        bool isNearLedge = false;
        bool isHittingWall = false;

        if (ledgeCheck != null && wallCheck != null)
        {
            // raycasting xuống để xem có gần mép không 
            isNearLedge = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, 1f, groundLayer);
            // Bắn tia ngang xem có đập mặt vào tường không
            isHittingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * lastMoveDirection, 0.5f, groundLayer);
        }

        float dir = lastMoveDirection;
        if (distToPlayerX > horizontalEpsilon)
        {
            dir = Mathf.Sign(dx);
            if (currentState != EnemyState.dash) lastMoveDirection = dir;
        }

        switch (currentState)
        {
            case EnemyState.chase:
                //stop if hitting wall or near ledge, else keep moving
                if (isHittingWall || isNearLedge)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(dir * stats.moveSpeed, rb.linearVelocity.y);
                }

                if (distToPlayerX <= stats.dashRange && distToPlayerY < 2f)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    currentState = EnemyState.anticipate;
                    stateTimer = stats.anticipateTime;
                    OnAnticipate?.Invoke();
                }
                break;


            case EnemyState.anticipate:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.dash;
                    stateTimer = stats.dashDuration;
                    dashDirection = dir;
                    OnDash?.Invoke();
                }
                break;

            case EnemyState.dash:
                rb.linearVelocity = new Vector2(dashDirection * stats.dashSpeed, rb.linearVelocity.y);
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.cooldown;
                    stateTimer = stats.cooldownTime;
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
    // debug lazer
    private void OnDrawGizmos()
    {
        if (ledgeCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + Vector3.down * 1f);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * lastMoveDirection * 0.5f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
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

        if (currentState != EnemyState.cooldown) currentState = EnemyState.chase;

        OnTakeDamage?.Invoke();

        if (currentHealth <= 0) Die();
    }

    public void ResetEnemy()
    {
        isDead = false;
        if (stats != null) currentHealth = stats.maxHealth;
        currentState = EnemyState.chase;
        stunTimer = 0f;
        despawnTimer = 0f;

        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
        gameObject.layer = originalLayer;
    }

    private void Die()
    {
        isDead = true;
        OnDie?.Invoke();
        gameObject.layer = corpseLayer;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        despawnTimer = 5f;
    }

    private void Despawn()
    {
        if (EnemyPool.Instance != null) EnemyPool.Instance.ReturnToPool(this);
        else Destroy(gameObject);
    }
}