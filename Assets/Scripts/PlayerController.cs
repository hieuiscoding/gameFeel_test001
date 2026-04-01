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
    [SerializeField] private Transform shootPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("weapons")]
    [SerializeField] private WeaponData[] weapons; // danh sach sung dang mang

    private int currentWeaponIndex = 0; // vi tri sung dang cam
    private float nextFireTime = 0f; // thoi diem vien dan tiep theo duoc phep ban

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    public Vector2 Velocity => rb.linearVelocity;
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

        if (horizontalInput != 0)
        {
            faceDir = Mathf.Sign(horizontalInput);
            transform.rotation = Quaternion.Euler(0, faceDir == 1 ? 0 : 180, 0);
        }
    }

    private void HandleActionInputs()
    {
        // nhay
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            OnJump?.Invoke();
        }

        HandleWeaponSwitch();
        HandleShooting();

        if (Input.GetKeyDown(KeyCode.T))
        {
            OnTakeDamage?.Invoke();
        }
    }

    private void HandleWeaponSwitch()
    {
        if (weapons == null || weapons.Length == 0) return;

        // quet cac phim tu 1 den 9 de doi sung
        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                currentWeaponIndex = i;
                // co the them event OnWeaponSwitch o day de phat am thanh len dan
            }
        }
    }

    private void HandleShooting()
    {
        if (weapons == null || weapons.Length == 0 || shootPoint == null) return;

        WeaponData currentWeapon = weapons[currentWeaponIndex];

        // neu súng auto thi dung GetMouseButton (giu de ban), neu khong thi GetMouseButtonDown (click tung vien)
        bool isTryingToShoot = currentWeapon.isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        // kiem tra xem da het thoi gian cooldown cua sung chua
        if (isTryingToShoot && Time.time >= nextFireTime)
        {
            // set cooldown cho vien tiep theo
            nextFireTime = Time.time + currentWeapon.fireRate;
            ExecuteShoot(currentWeapon);
        }
    }

    private void ExecuteShoot(WeaponData weapon)
    {
        Vector2 shootDir = Vector2.right * faceDir;

        // lay chi so tản mát từ data súng
        Vector3 randomOffset = transform.up * UnityEngine.Random.Range(-weapon.parallelSpread, weapon.parallelSpread);
        LastShootStart = shootPoint.position + randomOffset;

        RaycastHit2D hit = Physics2D.Raycast(LastShootStart, shootDir, 15f, enemyLayer);

        if (hit.collider != null)
        {
            LastShootEnd = hit.point;

            EnemyController enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // lay chi so sat thuong va day lui tu data súng
                Vector2 knockback = shootDir * weapon.knockbackPower;
                enemy.TakeDamage(weapon.damage, knockback);
            }
        }
        else
        {
            LastShootEnd = LastShootStart + (Vector3)(shootDir * 15f);
        }

        OnShoot?.Invoke();
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