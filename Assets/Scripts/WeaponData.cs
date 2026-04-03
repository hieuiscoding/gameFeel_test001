using UnityEngine;

[CreateAssetMenu(fileName = "new_weapon", menuName = "game/weapon data")]
public class WeaponData : ScriptableObject
{
    [Header("info")]
    public string weaponName = "gun";
    public Sprite weaponSprite; // hinh anh cua sung

    [Header("shooting mechanics")]
    public bool isAutomatic = false;
    public float fireRate = 0.2f;

    [Header("shotgun settings")]
    public int pelletsCount = 1; // so luong tia dan 
    public float spreadAngle = 0f; // do xoe rẽ quạt cua shotgun 

    [Header("combat stats")]
    public float damage = 1f;
    public float knockbackPower = 15f;
    public float parallelSpread = 0.2f;
    public float playerKickback = 0f; // luc day lui nguoi choi 

    // THÊM PHẦN AUDIO VÀO ĐÂY
    [Header("audio")]
    public AudioClip shootSound; // file am thanh tieng sung
    [Range(0f, 1f)] public float shootVolume = 0.8f; // am luong rieng cho tung loai
}