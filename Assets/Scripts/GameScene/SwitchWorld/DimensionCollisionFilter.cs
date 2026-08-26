using UnityEngine;

/// <summary>
/// 维度碰撞过滤器：命中物的维度标记与当前世界不一致 → 忽略该命中。
/// 无状态：每次查询读 WorldState 当前值，切换维度无需重建实例。
/// </summary>
public class DimensionCollisionFilter : ICollisionFilter
{
    public bool Allow(RaycastHit2D hit)
    {
        MaskObject maskObject = hit.collider.GetComponent<MaskObject>();
        if (maskObject == null) return true; // 普通地形恒放行

        bool isMaskActive = WorldState.Instance.IsMaskActive;
        return maskObject.ShowWhenMaskActive == isMaskActive;
    }
}
