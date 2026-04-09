using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerAudioFeedback : MonoBehaviour
{
    [Header("audio clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip damageSound;

    // --- THÊM MẢNG ÂM THANH BƯỚC CHÂN ---
    [Header("footsteps")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepVolume = 0.4f; // Thường tiếng bước chân phải nhỏ hơn tiếng súng

    [Header("settings")]
    [SerializeField] private float defaultVolume = 0.8f;
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
        playerController.OnJump += PlayJumpSound;
        playerController.OnLand += PlayLandSound;
        playerController.OnShoot += PlayShootSound;
        playerController.OnTakeDamage += PlayDamageSound;

        playerController.OnFootstep += PlayFootstepSound; // ĐĂNG KÝ EVENT
    }

    void OnDisable()
    {
        playerController.OnJump -= PlayJumpSound;
        playerController.OnLand -= PlayLandSound;
        playerController.OnShoot -= PlayShootSound;
        playerController.OnTakeDamage -= PlayDamageSound;

        playerController.OnFootstep -= PlayFootstepSound; // HỦY ĐĂNG KÝ
    }

    private void PlaySound(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;

        // Tính năng random pitch đã có sẵn ở đây, cực kỳ tự nhiên!
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        float finalVolume = volume < 0 ? defaultVolume : volume;
        audioSource.PlayOneShot(clip, finalVolume);
    }

    // --- HÀM PHÁT TIẾNG BƯỚC CHÂN ---
    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        // Bốc ngẫu nhiên 1 âm thanh trong mảng
        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        PlaySound(clip, footstepVolume);
    }

    private void PlayJumpSound() => PlaySound(jumpSound);
    private void PlayLandSound() => PlaySound(landSound);
    private void PlayDamageSound() => PlaySound(damageSound);

    private void PlayShootSound()
    {
        WeaponData currentWeapon = playerController.CurrentWeapon;
        if (currentWeapon != null && currentWeapon.shootSound != null)
        {
            PlaySound(currentWeapon.shootSound, currentWeapon.shootVolume);
        }
    }
}