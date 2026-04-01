using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnShoot;
    public event Action OnTakeDamage;

    [Header("movement settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float acceleration = 10f;

    [Header("platformer logic")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("references")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform shootPoint; // diem bat dau cua tia dan 
    [SerializeField] private LayerMask enemyLayer; // mask de tia dan chi trung quai

    [Header("gun accuracy")]
    [SerializeField] private float knockbackPower = 15f; // luc day lui quai khi ban
    [SerializeField] private float parallelSpread = 0.2f; // do lech len/xuong cua nong sung

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    // cho phep doc van toc tu ben ngoai (cho vfx)
    public Vector2 Velocity => rb.linearVelocity;

    // luu lai diem dau, diem cuoi cho script feedback doc de ve vet dan
    public Vector3 LastShootStart { get; private set; }
    public Vector3 LastShootEnd { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        UpdateTimers();
        HandleActionInputs();
        ApplySmartGravity();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    private void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (!wasGrounded && isGrounded)
        {
            OnLand?.Invoke();
        }
    }

    private void UpdateTimers()
    {
        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;
    }

    private void HandleMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * acceleration;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // quay mat
        if (horizontalInput != 0)
        {
            faceDir = Mathf.Sign(horizontalInput);
            transform.rotation = Quaternion.Euler(0, faceDir == 1 ? 0 : 180, 0);
        }
    }

    private void HandleActionInputs()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            OnJump?.Invoke();
        }

        if (Input.GetMouseButtonDown(0))
        {
            // ban tia raycast xuyen toi 15 don vi
            if (shootPoint != null)
            {
                Vector2 shootDir = Vector2.right * faceDir;

                // tao do lech len/xuong ngau nhien cho nong sung
                Vector3 randomOffset = transform.up * UnityEngine.Random.Range(-parallelSpread, parallelSpread);
                LastShootStart = shootPoint.position + randomOffset;

                RaycastHit2D hit = Physics2D.Raycast(LastShootStart, shootDir, 15f, enemyLayer);

                if (hit.collider != null)
                {
                    LastShootEnd = hit.point; // luu toa do dung tren nguoi quai

                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        // day lui quai 
                        Vector2 knockback = shootDir * knockbackPower;
                        enemy.TakeDamage(1f, knockback);
                    }
                }
                else
                {
                    LastShootEnd = LastShootStart + (Vector3)(shootDir * 15f); // bay het tam
                }
            }

            // goi onshoot sau khi da tinh toan xong diem dau / diem cuoi de ben feedback chay
            OnShoot?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            OnTakeDamage?.Invoke();
        }
    }

    private void ApplySmartGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }
}