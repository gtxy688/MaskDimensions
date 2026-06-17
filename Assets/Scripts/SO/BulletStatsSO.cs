using UnityEngine;

/// <summary>
/// 子弹属性资产。不同子弹类型共享同一份配置引用。
/// 修改一个 .asset 文件即可全局更新对应类型的所有子弹。
/// </summary>
[CreateAssetMenu(fileName = "NewBullet", menuName = "Config/Bullet")]
public class BulletStatsSO : ScriptableObject
{
    public float speed = 5f;         // 子弹飞行速度
    public Color color = Color.white; // 子弹颜色
    public float lifetime = 3f;      // 子弹存活时间（秒），超时自动回池
}
