using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池。泛型 T 必须是 MonoBehaviour，且挂载在预制体上。
/// 使用方式：
///   var pool = new ObjectPool<Bullet>(bulletPrefab, parentTransform);
///   var bullet = pool.Get();
///   pool.Return(bullet);
/// </summary>
public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly Queue<T> pool = new();
    private readonly T prefab;
    private readonly Transform parent;

    public ObjectPool(T prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
