using UnityEngine;

/// <summary>
/// 维度状态持有者。为什么单例：单人游戏合理（沿用 SingletonMono 风格）；
/// 为什么不是裸静态字段：双人/多玩家时可实例化，面试可讲"实例化即可"。
/// 事件签名与 PlayerController 原有静态事件一致（下游依赖，禁止改签名）。
/// </summary>
public class WorldState : SingletonMono<WorldState>
{
    public bool IsMaskActive { get; private set; }

    public static event System.Action<bool> OnMaskStateChanged;
    public static event System.Action<bool> OnMaskPreviewChanged;

    /// <summary>
    /// 翻转维度并广播。由 PlayerController 的切换协程调用（顿帧期间）。
    /// </summary>
    public bool SwitchWorld()
    {
        IsMaskActive = !IsMaskActive;
        OnMaskStateChanged?.Invoke(IsMaskActive);
        return IsMaskActive;
    }

    /// <summary>
    /// 强制设置维度（死亡重生/重置房间时回表世界）。
    /// </summary>
    public void SetWorld(bool isMaskActive)
    {
        if (IsMaskActive == isMaskActive) return;
        IsMaskActive = isMaskActive;
        OnMaskStateChanged?.Invoke(IsMaskActive);
    }

    /// <summary>
    /// 广播预览状态（J 长按虚影）。不改变 IsMaskActive 本体，仅通知订阅者。
    /// 为什么需要此方法：OnMaskPreviewChanged 是静态事件，外部只能注册/退订，
    /// 必须由 WorldState 内部的方法作为唯一广播入口。
    /// </summary>
    public void SetPreview(bool isPreviewing)
    {
        OnMaskPreviewChanged?.Invoke(isPreviewing);
    }
}
