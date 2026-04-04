using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnShoot;
    public event Action OnTakeDamage;
    public event Action<Vector3, Vector3, float, float> OnDrawTracer;
    public event Action<Sprite> OnWeaponSwitched;
    public event Action OnThrowGrenade;
    public event Action OnRoll;

    [Header("auto aim settings")]
    [SerializeField] private float targetRange = 10f;
    [SerializeField] private Transform weaponPivot;
    private Transform currentTarget;

    // --- BỘ LỌC VÀ DANH SÁCH TÌM KIẾM TỐI ƯU ---
    private ContactFilter2D enemyFilter;
    private List<Collider2D> enemyColliders = new List<Collider2D>(20);

    [Header("movement settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float acceleration = 10f;

    [Header("roll/dodge settings")]
    [SerializeField] private float rollDistance = 6f;
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float rollCooldown = 1f;

    public bool isInvincible = false;

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
    private float nextRollTime = 0f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isRolling = false;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    public Vector2 Velocity => rb.linearVelocity;
    public float CurrentInput => horizontalInput;

    private int enemyLayerIdx;
    private int playerLayerIdx;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // cache layer 1 lan duy nhat de toi uu 
        enemyLayerIdx = LayerMask.NameToLayer("Enemy");
        playerLayerIdx = gameObject.layer;

        enemyFilter.useLayerMask = true;
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useTriggers = false;

        if (weapons != null && weapons.Length > 0 && weapons[0] != null)
        {
            OnWeaponSwitched?.Invoke(weapons[0].weaponSprite);
        }
    }

    void Update()
    {
        CheckGrounded();
        UpdateTimers();
        FindNearestTarget();

        if (!isRolling)
        {
            GetInput();
            RotateWeapon();
            HandleActionInputs();
        }

        ApplySmartGravity();
    }

    // --- BIẾN ĐẾM GIỜ MỚI, TỐI ƯU HƠN ---
    private float nextScanTime = 0f;

    private void FindNearestTarget()
    {
        // ep quet lai ngay lap tuc neu muc tieu hien tai da chet hoac bi cat vao pool
        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                nextScanTime = 0f;
            }
            else
            {
                EnemyController currentEc = currentTarget.GetComponent<EnemyController>();
                if (currentEc != null && currentEc.isDead) nextScanTime = 0f;
            }
        }

        // --- SO SÁNH TRỰC TIẾP VỚI TIME.TIME ---
        if (Time.time < nextScanTime) return; // chua toi luc quet thi bo qua
        nextScanTime = Time.time + 0.15f;

        // doi 2 bien nay xuong day de tiet kiem bo nho tam thoi
        float closestSqDist = Mathf.Infinity;
        Transform closestEnemy = null;

        int count = Physics2D.OverlapCircle(transform.position, targetRange, enemyFilter, enemyColliders);
        for (int i = 0; i < count; i++)
        {
            Collider2D enemy = enemyColliders[i];

            if (!enemy.gameObject.activeInHierarchy) continue;

            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec != null && ec.isDead) continue;

            float sqDist = (transform.position - enemy.transform.position).sqrMagnitude;

            if (sqDist < closestSqDist)
            {
                closestSqDist = sqDist;
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
        if (!isRolling) HandleMovement();
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
        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            && Time.time >= nextRollTime)
        {
            ExecuteRoll();
            return;
        }

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

    private void ExecuteRoll()
    {
        isRolling = true;
        isInvincible = true;
        nextRollTime = Time.time + rollCooldown;

        // su dung truc tiep bien int da luu, bo truy van string
        if (enemyLayerIdx != -1) Physics2D.IgnoreLayerCollision(playerLayerIdx, enemyLayerIdx, true);

        float rollDir;
        if (Mathf.Abs(horizontalInput) > 0.1f) rollDir = Mathf.Sign(horizontalInput);
        else rollDir = -faceDir;

        float startSpeed = (rollDistance / rollDuration) * 1.5f;

        DOVirtual.Float(startSpeed, 0f, rollDuration, v => {
            if (isRolling) rb.linearVelocity = new Vector2(v * rollDir, rb.linearVelocity.y);
        }).SetEase(Ease.OutCubic).OnComplete(() => {
            isRolling = false;
            isInvincible = false;

            // phuc hoi va cham su dung bien cache
            if (enemyLayerIdx != -1) Physics2D.IgnoreLayerCollision(playerLayerIdx, enemyLayerIdx, false);
        });

        OnRoll?.Invoke();
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
        Vector3 shootDirBase;
        if (currentTarget != null) shootDirBase = (currentTarget.position - shootPoint.position).normalized;
        else shootDirBase = Vector3.right * faceDir;

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
            if (weapon.pelletsCount > 1) angleOffset = UnityEngine.Random.Range(-weapon.spreadAngle, weapon.spreadAngle);

            Vector3 shootDir = Quaternion.Euler(0, 0, angleOffset) * shootDirBase;
            Vector3 randomOffset = transform.up * UnityEngine.Random.Range(-weapon.parallelSpread, weapon.parallelSpread);
            Vector3 startPos = shootPoint.position + randomOffset;
            Vector3 endPos;

            RaycastHit2D hit = Physics2D.Raycast(startPos, shootDir, 15f, enemyLayer);

            if (hit.collider != null) endPos = hit.point;
            else endPos = startPos + (shootDir * 15f);

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
            Vector2 aimDir = (currentTarget != null) ? (currentTarget.position - shootPoint.position).normalized : Vector2.right * faceDir;
            Vector2 force = new Vector2(aimDir.x * throwForce, aimDir.y * throwForce + throwUpwardForce);
            rbGrenade.AddForce(force, ForceMode2D.Impulse);
            rbGrenade.AddTorque(-faceDir * 15f, ForceMode2D.Impulse);
        }

        OnThrowGrenade?.Invoke();
    }

    private void ApplySmartGravity()
    {
        if (rb.linearVelocity.y < 0) rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space)) rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
    }
}