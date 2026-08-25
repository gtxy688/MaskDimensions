using System.Collections;
using UnityEngine;

/// <summary>
/// 扫射子弹生成器。挂在关卡场景中，进入房间时由 RoomManager 激活。
/// 利用泛型对象池管理子弹生命周期，并向子弹分发策略。
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("配置")]
    public BulletStatsSO bulletStats;
    public RoomConfigSO roomConfig;
    public Bullet bulletPrefab;
    public Transform[] spawnPoints;
    [SerializeField] private int prewarmCount = 15;
    [SerializeField] private int maxPoolCapacity = 50;

    private ObjectPool<Bullet> bulletPool;
    private Coroutine fireCoroutine;
    private Transform playerTransform;

    private void Awake()
    {
        if (bulletPrefab != null)
        {
            bulletPool = new ObjectPool<Bullet>(bulletPrefab, transform, prewarmCount, maxPoolCapacity);
        }
    }

    private void OnEnable()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        StartFiring();
    }

    private void OnDisable()
    {
        StopFiring();
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
        float interval = roomConfig != null ? roomConfig.fireInterval : 1f;
        BulletStatsSO stats = bulletStats ?? (roomConfig != null ? roomConfig.bulletStats : null);

        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (spawnPoints == null || spawnPoints.Length == 0 || stats == null || bulletPool == null)
            {
                continue;
            }

            foreach (Transform point in spawnPoints)
            {
                if (point == null) continue;

                Bullet bullet = bulletPool.Get();
                bullet.transform.position = point.position;
                bullet.transform.rotation = point.rotation;
                bullet.Fire(Vector2.left, stats, null, playerTransform);
            }
        }
    }

    public void ReturnBullet(Bullet bullet)
    {
        bulletPool?.Return(bullet);
    }
}
