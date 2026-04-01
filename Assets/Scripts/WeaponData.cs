using UnityEngine;

[CreateAssetMenu(fileName = "new_weapon", menuName = "game/weapon data")]
public class WeaponData : ScriptableObject
{
    [Header("info")]
    public string weaponName = "gun";

    [Header("shooting mechanics")]
    public bool isAutomatic = false; // true = an giu de say, false = click tung vien
    public float fireRate = 0.2f; // thoi gian delay giua 2 vien đạn (tốc độ bắn)

    [Header("combat stats")]
    public float damage = 1f;
    public float knockbackPower = 15f; // luc day lui quai
    public float parallelSpread = 0.2f; // do lech nong sung (sung giat nhieu hay it)
}