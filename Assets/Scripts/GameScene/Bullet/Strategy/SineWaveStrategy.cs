using UnityEngine;

/// <summary>
/// 正弦波摇摆弹道策略。
/// 在前进方向的基础上，叠加垂直方向的周期性正弦摆动。
/// </summary>
public class SineWaveStrategy : IBulletTrajectoryStrategy
{
    private readonly float frequency; // 摆动频率
    private readonly float amplitude; // 摆动幅度

    public SineWaveStrategy(float frequency = 5f, float amplitude = 3f)
    {
        this.frequency = frequency;
        this.amplitude = amplitude;
    }

    public Vector2 CalculateVelocity(float elapsedTime, Vector2 baseDirection, BulletStatsSO stats, Transform bulletTransform, Transform target = null)
    {
        // 计算与前进方向垂直的法线向量
        Vector2 perpendicularDir = new Vector2(-baseDirection.y, baseDirection.x);

        // 正弦导数作为垂直速度分量: v = A * w * cos(w * t)
        float waveSpeed = amplitude * frequency * Mathf.Cos(frequency * elapsedTime);

        return (baseDirection * stats.speed) + (perpendicularDir * waveSpeed);
    }
}
