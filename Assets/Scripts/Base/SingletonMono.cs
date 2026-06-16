using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 继承了 MonoBehaviour 的 单例模式对象
/// 需要我们自己保证它的唯一性
/// </summary>
/// <typeparam name="T"></typeparam>

public class SingletonMono<T> : MonoBehaviour where T: MonoBehaviour
{
    private static T instance;

    public static T Instance => instance;

    protected virtual void Awake()
    {
        // 如果已有实例且不是自己，销毁多余的
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this as T;
        DontDestroyOnLoad(gameObject); // 过场景不销毁
    }

}
