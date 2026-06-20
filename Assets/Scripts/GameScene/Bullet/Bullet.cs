using UnityEngine;

/// <summary>
/// 单个子弹的行为。由 BulletSpawner 从对象池取出并发射。
/// 飞行方向在 Fire() 时设定，到达 lifetime 后自动回池。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour
{
    public BulletStatsSO stats;
    private Vector2 direction;
    private float spawnTime;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 从对象池取出后调用，初始化子弹参数。
    /// </summary>
    public void Fire(Vector2 dir, BulletStatsSO bulletStats)
    {
        stats = bulletStats;
        direction = dir.normalized;
        spawnTime = Time.time;
        gameObject.SetActive(true);

        if (spriteRenderer != null && stats != null)
            spriteRenderer.color = stats.color;
    }

    private void Update()
    {
        if (stats == null) 
        {
            return;
        }

        transform.Translate(direction * (stats.speed * Time.deltaTime));

        // 超过生命周期后回池
        if (Time.time - spawnTime > stats.lifetime)
        {
            BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
            spawner?.ReturnBullet(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Die();
                BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
                spawner?.ReturnBullet(this);
            }
        }
    }
}
