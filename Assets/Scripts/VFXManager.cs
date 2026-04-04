using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    // tao tu dien de luu nhieu loai pool (pool mau, pool no, pool bui...)
    private Dictionary<ParticleSystem, IObjectPool<ParticleSystem>> pools = new Dictionary<ParticleSystem, IObjectPool<ParticleSystem>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // api chinh de goi vfx
    public ParticleSystem PlayVFX(ParticleSystem prefab, Vector3 position, Quaternion rotation, float scaleMultiplier = 1f)
    {
        if (prefab == null) return null;

        // neu chua co pool cho loai vfx nay thi tu dong tao moi
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: (ps) => ps.gameObject.SetActive(true),
                actionOnRelease: (ps) =>
                {
                    ps.gameObject.SetActive(false);
                    ps.transform.SetParent(transform); // don dep cho gon hierarchy
                },
                actionOnDestroy: (ps) => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        // lay vfx tu kho ra
        ParticleSystem vfx = pools[prefab].Get();

        // set vi tri, goc xoay va do lon
        vfx.transform.position = position;
        vfx.transform.rotation = rotation;
        vfx.transform.localScale = prefab.transform.localScale * scaleMultiplier;

        vfx.Play();

        // tinh toan thoi gian song cua vfx de thu hoi
        float duration = vfx.main.duration + vfx.main.startLifetime.constantMax;

        // dung dotween delay roi tra ve kho
        DOVirtual.DelayedCall(duration, () => {
            // check an toan vi co the chuyen scene lam object bi huy
            if (vfx != null && vfx.gameObject.activeInHierarchy)
            {
                pools[prefab].Release(vfx);
            }
        }).SetLink(vfx.gameObject);

        return vfx;
    }
}