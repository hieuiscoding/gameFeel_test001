using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;

// bat buoc co rigidbody2d de xu ly vat ly cho platformer
[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerFeelTester : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot; // diem cam sung
    [SerializeField] private CanvasGroup bloodOverlay; // ui man hinh do
    [SerializeField] private Transform groundCheck; // diem duoi chan
    [SerializeField] private LayerMask groundLayer; // layer cua mat dat
    [SerializeField] private CinemachineImpulseSource impulseSource; // nguon phat rung camera

    [Header("movement settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float acceleration = 10f; // gia toc di chuyen

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // chac chan man hinh mau tat luc moi vao game
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        HandleActionInputs();
    }

    // code lien quan den addforce bat buoc de o fixedupdate
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

        // ve hinh tron nho duoi chan de check xem co dang dung tren dat khong
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        // neu frame truoc tren khong, frame nay cham dat -> chay hieu ung lún (squash)
        if (!wasGrounded && isGrounded)
        {
            ApplyLandFeel();
        }
    }

    private void HandleMovement()
    {
        // dung noi suy de tinh toan luc day, giup nhan vat truot nhe khi nha phim
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * acceleration;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // quay mat nhan vat va sung sang trai/phai
        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), transform.localScale.y, 1);
        }
    }

    private void HandleActionInputs()
    {
        // bam space de nhay (chi khi dang cham dat)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            ApplyJumpFeel();
        }

        // click chuot trai de ban sung
        if (Input.GetMouseButtonDown(0))
        {
            ApplyShootFeel();
        }

        // bam t de test bi quai can (mat mau)
        if (Input.GetKeyDown(KeyCode.T))
        {
            ApplyDamageFeel();
        }
    }

    private void ApplyJumpFeel()
    {
        // day nhan vat bay len
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // xoa tween cu de tranh loi khi spam phim
        transform.DOKill();

        // lay huong quay mat hien tai de scale khong bi lat nguoc
        float faceDir = Mathf.Sign(transform.localScale.x);

        // keo thuon nguoi ra (stretch) roi tra ve binh thuong
        transform.DOScale(new Vector3(0.6f * faceDir, 1.4f, 1f), 0.15f)
            .OnComplete(() => transform.DOScale(new Vector3(1f * faceDir, 1f, 1f), 0.1f));
    }

    private void ApplyLandFeel()
    {
        transform.DOKill();
        float faceDir = Mathf.Sign(transform.localScale.x);

        // lún nguoi xuong (squash) roi tra ve binh thuong
        transform.DOScale(new Vector3(1.3f * faceDir, 0.7f, 1f), 0.1f)
            .OnComplete(() => transform.DOScale(new Vector3(1f * faceDir, 1f, 1f), 0.1f));
    }

    private void ApplyShootFeel()
    {
        // giat sung ve phia sau
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f));
        }

        // cinemachine phat song rung nhe
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(0.2f);
        }
    }

    private void ApplyDamageFeel()
    {
        // dung thoi gian 0.06 giay (khoang 3-4 frame)
        StartCoroutine(HitStopRoutine(0.06f));

        // man hinh nhay do len roi mo dan
        if (bloodOverlay != null)
        {
            bloodOverlay.DOKill();
            bloodOverlay.alpha = 0.6f;
            // dung setupdate de ui van chay muot trong luc hit stop
            bloodOverlay.DOFade(0f, 0.8f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        // cinemachine phat song rung cuc manh
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(1.0f);
        }
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // dong bang thoi gian game
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        // mo khoa thoi gian
        Time.timeScale = 1f;
    }
}