using UnityEngine;

/// <summary>
/// 玩家核心数值配置文件 (Scriptable Object)
/// 将角色的物理参数、手感调优参数与底层逻辑代码解耦。
/// 好处：无需修改代码即可在运行中实时调整参数，甚至可以为不同关卡创建多套不同的 Config。
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerConfig", menuName = "Game Settings/Player Configuration")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("=== 基础移动参数 ===")]

    /// <summary>
    /// 角色在水平方向移动的最大速度。
    /// 建议值：5~8，取决于关卡跨度。
    /// </summary>
    public float moveSpeed = 5f;

    [Header("=== 跳跃手感优化 (Game Feel) ===")]

    /// <summary>
    /// 角色起跳瞬间获得的垂直向上初速度，直接决定跳跃的最大高度。
    /// 建议结合 Rigidbody2D 的 Gravity Scale 一起调优。
    /// </summary>
    public float jumpForce = 5f;

    /// <summary>
    /// 跳跃缓冲时间（秒）。
    /// 机制：玩家在落地前提前按下跳跃键，如果在该时间窗口内落地，系统会自动触发下一次起跳。
    /// 作用：解决玩家觉得“按了跳跃却没跳出来”的按键吞噬感。建议值：0.1~0.2秒。
    /// </summary>
    public float jumpBufferTime = 0.15f;

    /// <summary>
    /// 土狼时间 / 边缘容错时间（秒）。
    /// 机制：玩家离开平台边缘进入下落状态后，在该时间窗口内按下跳跃键，依然判定为有效起跳。
    /// 作用：弥补玩家视觉判断与物理碰撞边缘的微小误差，极大降低平台跳跃的挫败感。建议值：0.1~0.2秒。
    /// </summary>
    public float coyoteTime = 0.15f;

    [Header("=== 面具与双重维度 (Sanity System) ===")]

    /// <summary>
    /// 玩家理智值的上限总容量。
    /// </summary>
    public float maxSanity = 100f;

    /// <summary>
    /// 戴上面具进入『灵视界』时，理智值每秒下降的速度。
    /// </summary>
    public float activeSanityCostRate = 20f;

    /// <summary>
    /// 摘下面具回到『现实界』(安全状态)时，理智值每秒自然恢复的速度。
    /// </summary>
    public float sanityRecoverRate = 15f;
}