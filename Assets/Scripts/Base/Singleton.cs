using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 不继承Mono的单例模式基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> where T:class,new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if(instance == null)
                instance = new T();
            return instance;
        }
    }

    protected virtual void Awake()
    {
        instance = this as T;
    }
}
