using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;

// bat buoc co rigidbody2d
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerFeelTester : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot; // diem cam sung
    [SerializeField] private CanvasGroup bloodOverlay; // ui man hinh do
    [SerializeField] private Transform groundCheck; // diem duoi chan
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("movement settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float acceleration = 10f;

    [Header("platformer juice (game feel)")]
    [SerializeField] private float fallMultiplier = 2.5f; // roi nhanh hon khi dang xuong
    [SerializeField] private float lowJumpMultiplier = 2f; // nhay thap neu nha phim space som
    [SerializeField] private float coyoteTime = 0.1f; // thoi gian an gian khi rot khoi mep
    [SerializeField] private float jumpBufferTime = 0.1f; // thoi gian luu phim nhay

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    // bien ho tro game feel
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float faceDir = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
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

        // lún nguoi khi tiep dat
        if (!wasGrounded && isGrounded)
        {
            ApplyLandFeel();
        }
    }

    private void UpdateTimers()
    {
        // 1. xu ly coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 2. xu ly jump buffer
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * acceleration;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // quay mat nhan vat dung cach an toan de khong loi dotween
        if (horizontalInput != 0)
        {
            faceDir = Mathf.Sign(horizontalInput);
            // dung euler de quay truc y 180 do thay vi lat scale x
            transform.rotation = Quaternion.Euler(0, faceDir == 1 ? 0 : 180, 0);
        }
    }

    private void HandleActionInputs()
    {
        // cho phep nhay neu con coyote time va da bam jump buffer
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            ApplyJumpFeel();
            // reset counter de khong bi nhay double
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ApplyShootFeel();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ApplyDamageFeel();
        }
    }

    private void ApplySmartGravity()
    {
        // neu dang roi xuong -> roi nhanh hon (tao do nang cho nhan vat)
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // neu dang bay len nhung khong giu phim space -> roi xuong som (nhay thap)
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    private void ApplyJumpFeel()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        transform.DOKill();

        // khong can nhan faceDir nua vi da dung rotation de quay mat
        transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.15f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }

    private void ApplyLandFeel()
    {
        transform.DOKill();

        transform.DOScale(new Vector3(1.3f, 0.7f, 1f), 0.1f)
            .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
    }

    private void ApplyShootFeel()
    {
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f));
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(0.2f);
        }
    }

    private void ApplyDamageFeel()
    {
        StartCoroutine(HitStopRoutine(0.06f));

        if (bloodOverlay != null)
        {
            bloodOverlay.DOKill();
            bloodOverlay.alpha = 0.6f;
            bloodOverlay.DOFade(0f, 0.8f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(1.0f);
        }
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}