using UnityEngine;

/// <summary>
/// 由 BoxCollider2D 的 Bounds 内缩 SkinWidth 后计算四个角的射线原点与均匀间距。
/// struct（值类型）：每帧重建无 GC 压力。
/// </summary>
public struct ColliderMapping
{
    public Vector2 topLeft, topRight, bottomLeft, bottomRight;
    public float horizontalRaySpacing;
    public float verticalRaySpacing;

    public static ColliderMapping From(BoxCollider2D collider, float skinWidth, int hRayCount, int vRayCount)
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2f);

        ColliderMapping m = new ColliderMapping
        {
            bottomLeft = new Vector2(bounds.min.x, bounds.min.y),
            bottomRight = new Vector2(bounds.max.x, bounds.min.y),
            topLeft = new Vector2(bounds.min.x, bounds.max.y),
            topRight = new Vector2(bounds.max.x, bounds.max.y),
            // 为什么 -1：首尾各一根射线，中间均分
            horizontalRaySpacing = bounds.size.y / Mathf.Max(1, hRayCount - 1),
            verticalRaySpacing = bounds.size.x / Mathf.Max(1, vRayCount - 1)
        };
        return m;
    }
}
