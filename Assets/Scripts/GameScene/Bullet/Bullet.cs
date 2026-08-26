using UnityEngine;

/// <summary>
/// 单个子弹实体（Context 环境类）。
/// 遵循策略模式（Strategy Pattern）与对象池规范（IPoolable），负责驱动弹道运动与生命周期回收。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour, IPoolable
{
    public BulletStatsSO stats;
    private IBulletTrajectoryStrategy trajectoryStrategy;
    private Vector2 initialDirection;
    private float spawnTime;
    private SpriteRenderer spriteRenderer;
    private Transform trackingTarget;
    private bool isFired = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// IPoolable 接口实现：从池中取出时重置状态
    /// </summary>
    public void OnSpawn()
    {
        isFired = false;
        trackingTarget = null;
    }

    /// <summary>
    /// IPoolable 接口实现：回池前清理状态
    /// </summary>
    public void OnDespawn()
    {
        isFired = false;
        trajectoryStrategy = null;
        trackingTarget = null;
    }

    /// <summary>
    /// 发射子弹，注入弹道策略与目标。
    /// </summary>
    public void Fire(Vector2 dir, BulletStatsSO bulletStats, IBulletTrajectoryStrategy customStrategy = null, Transform target = null)
    {
        stats = bulletStats;
        initialDirection = dir.normalized;
        spawnTime = Time.time;
        trackingTarget = target;

        // 如果未传入自定义策略，则根据配置生成默认策略
        trajectoryStrategy = customStrategy ?? stats?.CreateStrategy() ?? new LinearStrategy();

        if (spriteRenderer != null && stats != null)
        {
            spriteRenderer.color = stats.color;
        }

        isFired = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isFired || stats == null) return;

        float elapsedTime = Time.time - spawnTime;

        // 超过生命周期自动回池
        if (elapsedTime > stats.lifetime)
        {
            RecycleSelf();
            return;
        }

        // 通过策略计算即时速度并移动
        if (trajectoryStrategy != null)
        {
            Vector2 velocity = trajectoryStrategy.CalculateVelocity(elapsedTime, initialDirection, stats, transform, trackingTarget);
            transform.Translate(velocity * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 维度门（替代已移除的 IgnoreLayerCollision 维度切换）：子弹自身是 MaskObject（showWhenMaskActive 标记所属世界），
            // 仅当前世界与子弹世界一致时可命中；异世界子弹不可见也不可伤（02-dimension：回调级命中需业务代码显式判定，
            // 射线级走 ICollisionFilter；本脚本其余逻辑保留不动）。
            MaskObject mask = GetComponent<MaskObject>();
            if (mask != null && WorldState.Instance.IsMaskActive != mask.ShowWhenMaskActive)
                return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isDead)
            {
                player.Die();
                RecycleSelf();
            }
        }
    }

    private void RecycleSelf()
    {
        BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
        if (spawner != null)
        {
            spawner.ReturnBullet(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
