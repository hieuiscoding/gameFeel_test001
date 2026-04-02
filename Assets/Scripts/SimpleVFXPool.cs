using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleVFXPool : MonoBehaviour
{
    public static SimpleVFXPool Instance { get; private set; }

    // tao tu dien luu hang doi cho tung loai prefab
    private Dictionary<GameObject, Queue<GameObject>> poolDict = new Dictionary<GameObject, Queue<GameObject>>();

    // ghi nho clone nay de ra tu prefab nao de tra ve dung cho
    private Dictionary<GameObject, GameObject> spawnMap = new Dictionary<GameObject, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        GameObject obj = null;

        if (poolDict[prefab].Count > 0)
        {
            obj = poolDict[prefab].Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, transform);
            // danh dau ban quyen de sau nay tra ve dung kho
            spawnMap[obj] = prefab;
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    // ban moi chi can truyen dung object can xoa, khong can truyen prefab
    public void Despawn(GameObject obj, float delay = 0f)
    {
        if (delay > 0)
        {
            StartCoroutine(DespawnRoutine(obj, delay));
        }
        else
        {
            ReturnToPool(obj);
        }
    }

    private IEnumerator DespawnRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj);
    }

    private void ReturnToPool(GameObject obj)
    {
        // tim lai xem no thuoc prefab nao roi nhet vao hang doi
        if (spawnMap.TryGetValue(obj, out GameObject prefab))
        {
            obj.SetActive(false);
            poolDict[prefab].Enqueue(obj);
        }
        else
        {
            // fallback cho chac an lo nhet nham
            Destroy(obj);
        }
    }
}