using UnityEngine;

public enum BulletTrajectoryType
{
    Linear,
    SineWave,
    Homing
}

/// <summary>
/// 子弹属性资产。不同子弹类型共享同一份配置引用。
/// 结合策略模式，通过配置枚举动态创建对应的弹道计算策略实例。
/// </summary>
[CreateAssetMenu(fileName = "NewBullet", menuName = "Config/Bullet")]
public class BulletStatsSO : ScriptableObject
{
    [Header("基础参数")]
    public float speed = 5f;         // 子弹飞行速度
    public Color color = Color.white; // 子弹颜色
    public float lifetime = 3f;      // 子弹存活时间（秒），超时自动回池

    [Header("弹道策略配置")]
    public BulletTrajectoryType trajectoryType = BulletTrajectoryType.Linear;
    [Tooltip("正弦波频率 (Hz)")]
    public float sineFrequency = 6f;
    [Tooltip("正弦波振幅")]
    public float sineAmplitude = 2f;
    [Tooltip("追踪转向角速度 (度/秒)")]
    public float homingRotateSpeed = 150f;

    /// <summary>
    /// 工厂方法：根据当前配置创建对应的弹道策略实例。
    /// </summary>
    public IBulletTrajectoryStrategy CreateStrategy()
    {
        return trajectoryType switch
        {
            BulletTrajectoryType.SineWave => new SineWaveStrategy(sineFrequency, sineAmplitude),
            BulletTrajectoryType.Homing => new HomingStrategy(homingRotateSpeed),
            _ => new LinearStrategy()
        };
    }
}
