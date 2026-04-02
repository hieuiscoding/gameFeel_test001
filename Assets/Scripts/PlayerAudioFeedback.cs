using UnityEngine;

// BO require AudioSource đi, dung chung Pool cua AudioManager
[RequireComponent(typeof(PlayerController))]
public class PlayerAudioFeedback : MonoBehaviour
{
    [Header("audio clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip damageSound;

    [Header("settings")]
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchVariation = 0.15f;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
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

    // Ham phat am thanh duoc don gian hoa, day het rui ro sang AudioManager ganh
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null) return;

        // Dong goi thong so giong het y ong muon
        var opts = new AudioManager.SFXPlayOptions
        {
            is2D = true,
            volume = this.volume,
            pitch = 1f,
            pitchVariance = this.pitchVariation,
            allowStealWhenBusy = true
            // bo minIntervalPerClip hoac set thap de cho phep nhay/ban lien tuc
        };

        AudioManager.Instance.PlaySFX(clip, opts);
    }

    private void PlayJumpSound() => PlaySound(jumpSound);
    private void PlayLandSound() => PlaySound(landSound);
    private void PlayShootSound() => PlaySound(shootSound);
    private void PlayDamageSound() => PlaySound(damageSound);
}