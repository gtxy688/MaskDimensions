using UnityEngine;

/// <summary>
/// 射线检测配置。为什么用 SO：不同角色/不同关卡可复用不同配置，无需改代码。
/// </summary>
[CreateAssetMenu(fileName = "NewRayConfig", menuName = "Kinematic2D/Ray Config")]
public class RayConfig : ScriptableObject
{
    [Tooltip("皮肤宽度：射线原点从碰撞盒内缩该值，防止浮点穿透与卡缝")]
    public float skinWidth = 0.015f;

    [Tooltip("水平射线数量（沿垂直方向分布）")]
    public int horizontalRayCount = 4;

    [Tooltip("垂直射线数量（沿水平方向分布）")]
    public int verticalRayCount = 4;

    [Tooltip("碰撞检测层（地面/墙/平台）")]
    public LayerMask collisionMask;

    [Tooltip("最大可行走坡角（度）：超过视为墙，不做爬坡换算（01-kinematic2d 边界：>60° 按墙）")]
    public float maxSlopeAngle = 60f;
}
