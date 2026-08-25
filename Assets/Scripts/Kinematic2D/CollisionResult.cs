using UnityEngine;

/// <summary>
/// 碰撞结果：物理层每帧解算后的碰撞事实，供上层（状态机/地面检测）读取。
/// struct（值类型）：避免每帧堆分配。
/// </summary>
public struct CollisionResult
{
    public bool IsGrounded;      // 脚下有可站立面（含斜坡/单向板/移动平台）
    public bool HitCeiling;
    public bool HitLeft;
    public bool HitRight;
    public bool OnSlope;
    public float SlopeAngle;
    public bool OnMovingPlatform;
}
