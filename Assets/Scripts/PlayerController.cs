using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnShoot; // phat am thanh, flash
    public event Action OnTakeDamage;
    public event Action<Vector3, Vector3> OnDrawTracer; // ve tung tia dan rieng le
    public event Action<Sprite> OnWeaponSwitched; // doi hinh anh sung

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

    private int currentWeaponIndex = 0;
    private float nextFireTime = 0f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    public Vector2 Velocity => rb.linearVelocity;
    public event Action OnThrowGrenade; // them event de feedback lang nghe

    [Header("grenade settings")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float throwUpwardForce = 5f;
    [SerializeField] private float grenadeCooldown = 1f;

    private float nextGrenadeTime = 0f;
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
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            OnJump?.Invoke();
        }

        HandleWeaponSwitch();
        HandleShooting();
        // check chuot phai (1) de nem luu dan
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
            // neu bam so va sung do co ton tai
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
        Vector3 baseDir = Vector3.right * faceDir;

        // giat lui nguoi choi (kickback) cho cac loai sung hang nang
        if (weapon.playerKickback > 0f)
        {
            // ham nhe toc do chay (truc x) de an luc giat, nhung BAT BUOC GIU NGUYEN toc do roi
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);

            // chi day lui thuan tuy theo chieu ngang
            rb.AddForce(new Vector2(-faceDir, 0f) * weapon.playerKickback, ForceMode2D.Impulse);
        }


        for (int i = 0; i < weapon.pelletsCount; i++)
        {
            // tinh goc xoe ngau nhien
            float angleOffset = 0f;
            if (weapon.pelletsCount > 1)
            {
                angleOffset = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle);
            }

            // xoay huong ban
            Vector3 shootDir = Quaternion.Euler(0, 0, angleOffset) * baseDir;

            // lech nong sung song song 
            Vector3 randomOffset = transform.up * UnityEngine.Random.Range(-weapon.parallelSpread, weapon.parallelSpread);
            Vector3 startPos = shootPoint.position + randomOffset;
            Vector3 endPos;

            RaycastHit2D hit = Physics2D.Raycast(startPos, shootDir, 15f, enemyLayer);

            if (hit.collider != null)
            {
                endPos = hit.point;

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    Vector2 knockback = shootDir * weapon.knockbackPower;
                    enemy.TakeDamage(weapon.damage, knockback);
                }
            }
            else
            {
                endPos = startPos + (shootDir * 15f);
            }

            // goi ve tia dan hien tai
            OnDrawTracer?.Invoke(startPos, endPos);
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
            // nem theo huong mat cua nhan vat + hoi check len tren tao duong vong cung
            Vector2 force = new Vector2(faceDir * throwForce, throwUpwardForce);
            rbGrenade.AddForce(force, ForceMode2D.Impulse);

            // tao do xoay cho luu dan bay tu nhien
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