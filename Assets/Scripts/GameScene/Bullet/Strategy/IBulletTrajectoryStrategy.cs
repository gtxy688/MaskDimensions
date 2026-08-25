using UnityEngine;

/// <summary>
/// 弹道运动计算策略接口（Strategy Pattern）。
/// 将不同的弹道运动算法与子弹实体（Bullet）解耦，使得新增弹道（如正弦波、追踪、折线）无需修改子弹核心类。
/// </summary>
public interface IBulletTrajectoryStrategy
{
    /// <summary>
    /// 根据经过的时间与当前环境计算子弹的即时速度向量。
    /// </summary>
    /// <param name="elapsedTime">子弹已存活时间（秒）</param>
    /// <param name="baseDirection">发射时的初始基准方向</param>
    /// <param name="stats">子弹的基础配置参数</param>
    /// <param name="bulletTransform">子弹当前的 Transform</param>
    /// <param name="target">追踪目标（可选）</param>
    /// <returns>计算出的即时速度向量 (Vector2)</returns>
    Vector2 CalculateVelocity(float elapsedTime, Vector2 baseDirection, BulletStatsSO stats, Transform bulletTransform, Transform target = null);
}
