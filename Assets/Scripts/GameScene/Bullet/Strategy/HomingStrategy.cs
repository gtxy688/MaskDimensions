using UnityEngine;

/// <summary>
/// 追踪目标弹道策略。
/// 子弹在飞行过程中会平滑向目标（如玩家）转向。
/// </summary>
public class HomingStrategy : IBulletTrajectoryStrategy
{
    private readonly float rotateSpeed; // 追踪转向角速度（度/秒）

    public HomingStrategy(float rotateSpeed = 120f)
    {
        this.rotateSpeed = rotateSpeed;
    }

    public Vector2 CalculateVelocity(float elapsedTime, Vector2 baseDirection, BulletStatsSO stats, Transform bulletTransform, Transform target = null)
    {
        if (target == null)
        {
            return bulletTransform.right * stats.speed;
        }

        Vector2 currentDir = bulletTransform.right;
        Vector2 targetDir = ((Vector2)target.position - (Vector2)bulletTransform.position).normalized;

        // 计算当前朝向向目标朝向平滑旋转
        float maxDegrees = rotateSpeed * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(currentDir, targetDir, maxDegrees * Mathf.Deg2Rad, 0f);

        // 更新子弹旋转以匹配飞行方向
        float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        bulletTransform.rotation = Quaternion.Euler(0, 0, angle);

        return (Vector2)newDir * stats.speed;
    }
}
