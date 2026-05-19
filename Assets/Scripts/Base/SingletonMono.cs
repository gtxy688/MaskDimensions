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
        instance = this as T;
    }
	
}
