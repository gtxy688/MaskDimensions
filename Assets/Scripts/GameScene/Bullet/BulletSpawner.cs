using System.Collections;
using UnityEngine;

/// <summary>
/// 横向扫射子弹生成器。挂在第 2 关的场景中，进入房间时由 RoomManager 激活。
/// 从屏幕一侧发射横向子弹，穿越整个房间。
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("配置")]
    public BulletStatsSO bulletStats;
    public Bullet bulletPrefab;
    public float fireInterval = 1f;
    public Transform[] spawnPoints;

    private ObjectPool<Bullet> bulletPool;
    private Coroutine fireCoroutine;

    private void Awake()
    {
        if (bulletPrefab != null)
            bulletPool = new ObjectPool<Bullet>(bulletPrefab, transform);
    }

    public void StartFiring()
    {
        if (fireCoroutine != null) 
        {
            StopCoroutine(fireCoroutine);
        }
        fireCoroutine = StartCoroutine(FireRoutine());
    }

    public void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    private IEnumerator FireRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fireInterval);

            foreach (Transform point in spawnPoints)
            {
                Bullet bullet = bulletPool.Get();
                bullet.transform.position = point.position;
                bullet.transform.rotation = point.rotation;
                bullet.Fire(Vector2.right, bulletStats);
            }
        }
    }

    public void ReturnBullet(Bullet bullet)
    {
        bulletPool.Return(bullet);
    }
}
