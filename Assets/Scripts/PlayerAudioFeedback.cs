using UnityEngine;

// ep buoc phai co controller va audiosource de chay
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerAudioFeedback : MonoBehaviour
{
    [Header("audio clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip damageSound;

    [Header("settings")]
    [SerializeField] private float volume = 0.8f;
    // thay doi cao do ngau nhien de am thanh khong bi robot
    [SerializeField] private float pitchVariation = 0.15f;

    private PlayerController playerController;
    private AudioSource audioSource;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        // dang ky y het ben hieu ung hinh anh
        playerController.OnJump += PlayJumpSound;
        playerController.OnLand += PlayLandSound;
        playerController.OnShoot += PlayShootSound;
        playerController.OnTakeDamage += PlayDamageSound;
    }

    void OnDisable()
    {

        playerController.OnJump -= PlayJumpSound;
        playerController.OnLand -= PlayLandSound;
        playerController.OnShoot -= PlayShootSound;
        playerController.OnTakeDamage -= PlayDamageSound;
    }

    // ham xu ly chung cho moi loai am thanh
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        // random pitch tu 0.85 den 1.15
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        // dung playoneshot de cac tieng khong bi cat ngang nhau neu ban qua nhanh
        audioSource.PlayOneShot(clip, volume);
    }

    // cac ham goi am thanh cu the
    private void PlayJumpSound() => PlaySound(jumpSound);
    private void PlayLandSound() => PlaySound(landSound);
    private void PlayShootSound() => PlaySound(shootSound);
    private void PlayDamageSound() => PlaySound(damageSound);
}