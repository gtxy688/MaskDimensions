using UnityEngine;

/// <summary>
/// 碰撞过滤器：决定一次射线命中是否参与碰撞。
/// 为什么用接口：维度切换 = 换过滤器，物理层自身不知道"维度"概念（解耦）。
/// 过滤器必须无状态：每次查询读当前世界状态，切换时无需重建实例。
/// </summary>
public interface ICollisionFilter
{
    bool Allow(RaycastHit2D hit);
}
