using UnityEngine;

/// <summary>
/// 直线弹道策略（默认）。
/// </summary>
public class LinearStrategy : IBulletTrajectoryStrategy
{
    public Vector2 CalculateVelocity(float elapsedTime, Vector2 baseDirection, BulletStatsSO stats, Transform bulletTransform, Transform target = null)
    {
        return baseDirection * stats.speed;
    }
}
