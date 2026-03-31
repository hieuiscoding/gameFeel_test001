using UnityEngine;
using DG.Tweening;
using System.Collections;

public class FeelTester : MonoBehaviour
{
    [Header("references")]
    [SerializeField] private Transform weaponPivot; // diem cam sung
    [SerializeField] private Camera mainCam;
    [SerializeField] private CanvasGroup bloodOverlay;

    [Header("movement settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lerpSpeed = 10f;

    private Vector3 targetPosition;

    void Start()
    {
        // luu vi tri ban dau
        targetPosition = transform.position;

        // chac chan overlay tat luc moi vao
        if (bloodOverlay != null) bloodOverlay.alpha = 0f;
    }

    void Update()
    {
        HandleMovement();
        HandleActionInputs();
    }

    private void HandleMovement()
    {
        // lay input di chuyen
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // tinh toan vi tri dich den
        Vector3 moveDir = new Vector3(x, y, 0).normalized;
        targetPosition += moveDir * moveSpeed * Time.deltaTime;

        // di chuyen muot ma bang lerp
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
    }

    private void HandleActionInputs()
    {
        // bam space de nhay
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyJumpFeel();
        }

        // bam chuot trai de ban sung
        if (Input.GetMouseButtonDown(0))
        {
            ApplyShootFeel();
        }

        // bam t de mo phong bi quai can
        if (Input.GetKeyDown(KeyCode.T))
        {
            ApplyDamageFeel();
        }
    }

    private void ApplyJumpFeel()
    {
        // xoa tween cu de khong bi giat cuc khi bam lien tuc
        transform.DOKill();

        // nhay len va co gian (squash and stretch)
        transform.DOJump(targetPosition, 1.5f, 1, 0.4f);

        // keo dai truc y, ep truc x
        transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.15f)
            .OnComplete(() => {
                // tiep dat thi phinh ra roi thu ve binh thuong
                transform.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.1f)
                    .OnComplete(() => transform.DOScale(Vector3.one, 0.1f));
            });
    }

    private void ApplyShootFeel()
    {
        // giat sung ve phia sau (recoil)
        if (weaponPivot != null)
        {
            weaponPivot.DOKill();
            // gia su dang ban sang phai, sung giat lui ve muc -0.5 truc x
            weaponPivot.DOLocalMoveX(-0.5f, 0.05f)
                .SetEase(Ease.OutExpo)
                .OnComplete(() => weaponPivot.DOLocalMoveX(0f, 0.2f)); // hoi sung tu tu
        }

        // rung man hinh manh va nhanh
        if (mainCam != null)
        {
            mainCam.DOKill();
            Tweener tweener = mainCam.DOShakePosition(0.15f, 0.2f, 20, 90, true);
        }
    }

    private void ApplyDamageFeel()
    {
        // hit stop tao do nang cho don danh
        StartCoroutine(HitStopRoutine(0.15f));

        // hieu ung mau nhay len roi mo dan
        if (bloodOverlay != null)
        {
            bloodOverlay.DOKill();
            bloodOverlay.alpha = 0.6f;
            bloodOverlay.DOFade(0f, 0.8f).SetEase(Ease.OutQuad);
        }

        // rung camera kieu loang choang (cham va bien do lon)
        if (mainCam != null)
        {
            mainCam.DOKill();
            mainCam.DOShakePosition(0.4f, 0.4f, 10, 90, true);
        }
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // dung thoi gian
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        // tra lai binh thuong
        Time.timeScale = 1f;
    }
}