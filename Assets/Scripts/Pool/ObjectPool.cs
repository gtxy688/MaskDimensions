using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 工业级泛型对象池。
/// 特性：
/// 1. 支持 IPoolable 接口，自动在出池/回池时触发 OnSpawn/OnDespawn 状态重置；
/// 2. 支持 Prewarm 预热机制，避免运行时突发实例化造成的帧率抖动（Jank）；
/// 3. 支持 MaxCapacity 容量上限保护，超出容量时自动销毁超出对象，防止内存泄漏；
/// 4. 具备防重复入池校验（Double-Free Detection），确保队列状态安全。
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new();
    private readonly HashSet<T> pooledObjects = new(); // 用于 O(1) 校验重复入池
    private readonly T prefab;
    private readonly Transform parent;
    private readonly int maxCapacity;

    public int ActiveCount { get; private set; }
    public int InactiveCount => pool.Count;
    public int TotalCount => ActiveCount + InactiveCount;

    /// <summary>
    /// 构造泛型对象池。
    /// </summary>
    /// <param name="prefab">原始预制体</param>
    /// <param name="parent">挂载父节点（可选）</param>
    /// <param name="initialPrewarmCount">初始预热数量</param>
    /// <param name="maxCapacity">池内最大缓存数量（超出则直接销毁）</param>
    public ObjectPool(T prefab, Transform parent = null, int initialPrewarmCount = 0, int maxCapacity = 100)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.maxCapacity = maxCapacity;

        if (initialPrewarmCount > 0)
        {
            Prewarm(initialPrewarmCount);
        }
    }

    /// <summary>
    /// 预热对象池，预先生成指定数量的非激活对象。
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count >= maxCapacity) break;

            T obj = CreateNewObject();
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
            pooledObjects.Add(obj);
        }
    }

    /// <summary>
    /// 从池中获取可用对象。
    /// </summary>
    public T Get()
    {
        T obj = null;

        while (pool.Count > 0)
        {
            obj = pool.Dequeue();
            pooledObjects.Remove(obj);

            // 应对外部可能在未归还时被意外 Destroy 的极端情况
            if (obj != null) break;
        }

        if (obj == null)
        {
            obj = CreateNewObject();
        }

        obj.gameObject.SetActive(true);
        ActiveCount++;

        // 触发 IPoolable 生命周期
        if (obj is IPoolable poolable)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    /// <summary>
    /// 将使用完毕的对象归还至池中。
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        // 防御性检查：防止重复归还导致队列污染
        if (pooledObjects.Contains(obj))
        {
            Debug.LogWarning($"[ObjectPool] 试图重复归还已在池中的对象：{obj.name}，已被拦截！");
            return;
        }

        ActiveCount = Mathf.Max(0, ActiveCount - 1);

        // 触发 IPoolable 清理生命周期
        if (obj is IPoolable poolable)
        {
            poolable.OnDespawn();
        }

        // 容量上限保护：超出最大缓存数则直接销毁，避免内存无限膨胀
        if (pool.Count >= maxCapacity)
        {
            Object.Destroy(obj.gameObject);
            return;
        }

        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
        pooledObjects.Add(obj);
    }

    /// <summary>
    /// 清空并销毁池中所有缓存对象。
    /// </summary>
    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
        pooledObjects.Clear();
        ActiveCount = 0;
    }

    private T CreateNewObject()
    {
        T instance = Object.Instantiate(prefab, parent);
        return instance;
    }
}
