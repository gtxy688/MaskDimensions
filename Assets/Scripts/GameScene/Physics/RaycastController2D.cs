using UnityEngine;

/// <summary>
/// 2D 射线检测基类（RaycastController2D）。
/// 负责在 BoxCollider2D 边界内收缩 SkinWidth，并均匀计算四周射线的发射原点与间距。
/// 为高精度的运动学物理控制器提供底层的几何与光线投射支持。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController2D : MonoBehaviour
{
    public const float SkinWidth = 0.015f; // 皮肤宽度，防止浮点数导致的穿透与缝隙卡死

    [Header("射线检测配置")]
    [SerializeField] protected LayerMask collisionMask;   // 碰撞层级
    [SerializeField] private int horizontalRayCount = 4;  // 水平射线数量
    [SerializeField] private int verticalRayCount = 4;    // 垂直射线数量

    protected float horizontalRaySpacing;
    protected float verticalRaySpacing;

    protected BoxCollider2D boxCollider;
    protected RaycastOrigins raycastOrigins;

    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    protected virtual void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    protected virtual void Start()
    {
        CalculateRaySpacing();
    }

    /// <summary>
    /// 更新四个角落的射线发射原点。
    /// </summary>
    public void UpdateRaycastOrigins()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        Bounds bounds = boxCollider.bounds;
        bounds.Expand(SkinWidth * -2f); // 内缩 SkinWidth

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    /// <summary>
    /// 计算水平和垂直方向射线之间的均匀间距。
    /// </summary>
    public void CalculateRaySpacing()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        Bounds bounds = boxCollider.bounds;
        bounds.Expand(SkinWidth * -2f);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
}
