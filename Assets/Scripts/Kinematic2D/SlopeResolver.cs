using UnityEngine;

/// <summary>
/// 斜坡判定工具（任务 6，纯静态无状态——斜坡信息由 KinematicBody 每帧从射线命中提取）。
/// 为什么独立成类：斜坡判定（角度换算/放行条件）与位移迭代解耦，可单测、可复述（面试点）。
/// </summary>
public static class SlopeResolver
{
    /// <summary>命中面相对水平面的仰角（0~90 度）。法线不朝上（墙/天花板）返回 90。</summary>
    public static float GetSlopeAngle(RaycastHit2D hit)
    {
        Vector2 n = hit.normal;
        if (n.y <= 0.0001f) return 90f;
        // 法线 (sinθ, cosθ)（θ=坡角，法线朝上偏 x）→ 坡角 = atan2(|n.x|, n.y)
        return Mathf.Atan2(Mathf.Abs(n.x), n.y) * Mathf.Rad2Deg;
    }

    /// <summary>是否为可行走斜坡：法线朝上（n.y>0）且仰角小于最大坡角。超过按墙处理（不进本分支）。</summary>
    public static bool IsSlope(RaycastHit2D hit, float maxSlopeAngle)
    {
        if (hit.collider == null) return false;
        return GetSlopeAngle(hit) < maxSlopeAngle;
    }

    /// <summary>坡面法线的 x 分量（带符号）。与水平位移同号 = 爬坡方向，异号 = 下坡方向。</summary>
    public static float GetSlopeNormalX(RaycastHit2D hit)
    {
        return hit.normal.x;
    }
}