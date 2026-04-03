using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnShoot; // phat am thanh, flash
    public event Action OnTakeDamage;
    // Thêm thông tin vào event: startPos, endPos, damage, knockback
    public event Action<Vector3, Vector3, float, float> OnDrawTracer;
    public event Action<Sprite> OnWeaponSwitched; // doi hinh anh sung
    public event Action OnThrowGrenade; // them event de feedback lang nghe

    [Header("auto aim settings")]
    [SerializeField] private float targetRange = 10f; // tầm quét quái
    [SerializeField] private Transform weaponPivot; // cái pivot bọc ngoài cái súng để xoay
    private Transform currentTarget;

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
    [SerializeField] private WeaponData[] weapons;

    [Header("grenade settings")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float throwUpwardForce = 5f;
    [SerializeField] private float grenadeCooldown = 1f;

    private int currentWeaponIndex = 0;
    private float nextFireTime = 0f;
    private float nextGrenadeTime = 0f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    public Vector2 Velocity => rb.linearVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // set vu khi mac dinh luc moi vao game
        if (weapons != null && weapons.Length > 0 && weapons[0] != null)
        {
            OnWeaponSwitched?.Invoke(weapons[0].weaponSprite);
        }
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        UpdateTimers();
        FindNearestTarget();

        // FIX LỖI: Bắt buộc phải xoay súng TRƯỚC khi xử lý nút bấm bắn!
        RotateWeapon();

        HandleActionInputs();
        ApplySmartGravity();
    }

    private void FindNearestTarget()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, targetRange, enemyLayer);
        float closestDist = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = enemy.transform;
            }
        }
        currentTarget = closestEnemy;
    }

    private void RotateWeapon()
    {
        if (weaponPivot == null) return;

        Vector3 targetDir;
        if (currentTarget != null) targetDir = (currentTarget.position - weaponPivot.position).normalized;
        else targetDir = Vector3.right * faceDir;

        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        if (angle > 90 || angle < -90)
        {
            faceDir = -1f;
            transform.localScale = new Vector3(-1, 1, 1);
            angle += 180f;
        }
        else
        {
            faceDir = 1f;
            transform.localScale = new Vector3(1, 1, 1);
        }

        weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
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

        HandleWeaponSwitch();
        HandleShooting();

        if (Input.GetMouseButtonDown(1) && Time.time >= nextGrenadeTime)
        {
            nextGrenadeTime = Time.time + grenadeCooldown;
            ExecuteThrowGrenade();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            OnTakeDamage?.Invoke();
        }
    }

    private void HandleWeaponSwitch()
    {
        if (weapons == null || weapons.Length == 0) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && currentWeaponIndex != i && weapons[i] != null)
            {
                currentWeaponIndex = i;
                OnWeaponSwitched?.Invoke(weapons[i].weaponSprite);
            }
        }
    }

    private void HandleShooting()
    {
        if (weapons == null || weapons.Length == 0 || shootPoint == null) return;

        WeaponData currentWeapon = weapons[currentWeaponIndex];
        bool isTryingToShoot = currentWeapon.isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (isTryingToShoot && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + currentWeapon.fireRate;
            ExecuteShoot(currentWeapon);
        }
    }

    private void ExecuteShoot(WeaponData weapon)
    {
        // FIX LỖI CAO CẤP: Dùng toán học tính thẳng đường đạn vào quái, bỏ qua hierarchy
        Vector3 shootDirBase;
        if (currentTarget != null)
        {
            shootDirBase = (currentTarget.position - shootPoint.position).normalized;
        }
        else
        {
            shootDirBase = Vector3.right * faceDir;
        }

        // Ép xoay nòng súng để Muzzle Flash và Light luôn phụt ra chuẩn hướng đạn
        float exactAngle = Mathf.Atan2(shootDirBase.y, shootDirBase.x) * Mathf.Rad2Deg;
        shootPoint.rotation = Quaternion.Euler(0, 0, exactAngle);

        if (weapon.playerKickback > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
            float kickDir = -Mathf.Sign(shootDirBase.x);
            rb.AddForce(new Vector2(kickDir, 0f) * weapon.playerKickback, ForceMode2D.Impulse);
        }

        for (int i = 0; i < weapon.pelletsCount; i++)
        {
            float angleOffset = 0f;
            if (weapon.pelletsCount > 1)
            {
                angleOffset = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle);
            }

            Vector3 shootDir = Quaternion.Euler(0, 0, angleOffset) * shootDirBase;
            Vector3 randomOffset = transform.up * UnityEngine.Random.Range(-weapon.parallelSpread, weapon.parallelSpread);
            Vector3 startPos = shootPoint.position + randomOffset;
            Vector3 endPos;

            RaycastHit2D hit = Physics2D.Raycast(startPos, shootDir, 15f, enemyLayer);

            if (hit.collider != null)
            {
                endPos = hit.point;
                // ĐÃ XÓA GỌI TAKE_DAMAGE Ở ĐÂY ĐỂ TRÁNH TRỪ MÁU 2 LẦN
            }
            else
            {
                endPos = startPos + (shootDir * 15f);
            }

            // Giao phó toàn bộ việc tính toán sát thương cho Tracer bên Feedback
            OnDrawTracer?.Invoke(startPos, endPos, weapon.damage, weapon.knockbackPower);
        }

        OnShoot?.Invoke();
    }

    private void ExecuteThrowGrenade()
    {
        if (grenadePrefab == null || shootPoint == null) return;

        GameObject grenade = Instantiate(grenadePrefab, shootPoint.position, Quaternion.identity);
        Rigidbody2D rbGrenade = grenade.GetComponent<Rigidbody2D>();

        if (rbGrenade != null)
        {
            // Ném lựu đạn theo hướng mục tiêu luôn cho ngầu
            Vector2 aimDir = (currentTarget != null) ? (currentTarget.position - shootPoint.position).normalized : Vector2.right * faceDir;

            Vector2 force = new Vector2(aimDir.x * throwForce, aimDir.y * throwForce + throwUpwardForce);
            rbGrenade.AddForce(force, ForceMode2D.Impulse);

            rbGrenade.AddTorque(-faceDir * 15f, ForceMode2D.Impulse);
        }

        OnThrowGrenade?.Invoke();
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