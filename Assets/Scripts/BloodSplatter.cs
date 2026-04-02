using UnityEngine;
using System.Collections.Generic;

public class BloodSplatter : MonoBehaviour
{
    [SerializeField] private GameObject bloodDecalPrefab; // keo prefab hinh vung mau vao day
    [SerializeField] private float decalSizeMin = 0.2f;
    [SerializeField] private float decalSizeMax = 0.6f;

    private ParticleSystem partSystem;
    private List<ParticleCollisionEvent> collisionEvents;

    void Start()
    {
        partSystem = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    // ham nay tu dong chay khi 1 hat particle cham vao object khac
    void OnParticleCollision(GameObject other)
    {
        if (bloodDecalPrefab == null) return;

        // lay danh sach cac hat vua va cham
        int numCollisionEvents = partSystem.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;
            Vector3 normal = collisionEvents[i].normal;

            // tao hinh vung mau tai diem va cham
            GameObject decal = Instantiate(bloodDecalPrefab, pos, Quaternion.identity);

            // xoay vung mau ap sat theo be mat (du la tuong hay san nha)
            decal.transform.up = normal;

            // random to nho cho tu nhien
            float randomSize = Random.Range(decalSizeMin, decalSizeMax);
            decal.transform.localScale = new Vector3(randomSize, randomSize, 1f);

            // gom no lam con cua cai san nha luon cho hierarchy do ban
            decal.transform.SetParent(other.transform);

            // co the xoa dong nay neu muon mau nam vinh vien
            // Destroy(decal, 30f); 
        }
    }
}