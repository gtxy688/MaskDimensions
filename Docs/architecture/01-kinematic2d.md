# 01 自研运动学物理框架（kinematic2d）

> 状态：**新写**（从零重写，不沿用旧代码）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D1/D2）。

## 职责

- 为玩家提供**确定性**的 2D 位移求解：输入速度 → 检测碰撞 → 修正位移 → 应用位移。
- 不碰输入、不碰状态机、不碰渲染——只做"把一段期望位移安全地移动出去"。
- 维度过滤通过注入的过滤器实现（见 02-dimension），物理层自身不知道"维度"概念。

## 目标文件（`Assets/Scripts/Kinematic2D/`）

| 文件 | 职责 |
|---|---|
| `KinematicBody.cs` | 核心 MonoBehaviour。每帧 `Move(velocity)` 求解位移 |
| `ICollisionFilter.cs` | 接口：射线命中后是否放行 |
| `CollisionResult.cs` | struct：IsGrounded / 四向碰撞标志 / 命中信息 |
| `ColliderMapping.cs` | 由 BoxCollider2D 几何计算射线原点与间距（SkinWidth 内缩） |
| `RayConfig.cs` | 配置：SkinWidth、射线数量、碰撞 LayerMask |
| `SlopeResolver.cs`（增量） | 斜坡爬坡/下坡换算（M3 阶段加） |
| `OneWayPlatform.cs`（增量） | 单向板穿透规则（M3 阶段加） |
| `MovingPlatform.cs`（增量） | 移动平台跟随（M3 阶段加） |

## 关键接口（签名级，实现自由）

```
class KinematicBody : MonoBehaviour
    void Move(Vector2 velocity)          // 每帧入口：内部按 X/Y 分轴迭代
    void SetFilter(ICollisionFilter filter)  // 注入维度过滤器
    CollisionResult LastResult { get; }      // 上一帧碰撞结果（供地面检测）

struct CollisionResult
    bool IsGrounded        // 脚下有可站立面（含斜坡/单向板/移动平台）
    bool HitCeiling
    bool HitLeft / HitRight
    bool OnSlope            // 是否处于爬坡/下坡
    float SlopeAngle
    bool OnMovingPlatform   // 增量阶段

interface ICollisionFilter
    bool Allow(RaycastHit2D hit)   // 返回 true = 该命中参与碰撞
```

## 生命周期

- Awake：缓存 Collider2D，计算射线间距。
- FixedUpdate（或 Update，随设计定）：由 PlayerController 调用 `Move(velocity)`。
- 每帧顺序：更新射线原点 → 重置碰撞结果 → **X 轴迭代**（水平射线检测 → 修正 x 位移 → 碰撞标志）→ **Y 轴迭代**（垂直射线检测 → 修正 y 位移 → 碰撞标志）→ 应用位移 → 记录 LastResult。

## 核心规则（硬约束）

1. **X/Y 分轴迭代**：先水平后垂直，每轴独立检测与修正。禁止一次性斜向位移。
2. **SkinWidth = 0.015f**：射线原点从碰撞盒内缩 SkinWidth；修正位移时保留 SkinWidth 间隙，防浮点穿透/卡缝。
3. **命中距离的处理（重叠回退，穿墙根因修复）**：命中后位移 = `(distance - SkinWidth) * 方向`。distance < SkinWidth（含 0，已重叠）时该公式**自动产生负位移 = 退出重叠**。禁止"跳过 distance==0 的命中"或"penetration * 方向"（方向反，会把物体往墙里推——本任务实际踩过的坑）。
4. **每条射线都过 `ICollisionFilter.Allow()`**：过滤器放行才参与碰撞。
5. **Move() 开头必须 `Physics2D.SyncTransforms()`**：外部代码/本帧内修改 transform.position 后，物理缓存的碰撞体 bounds 不会立即更新；未同步时射线基于陈旧位置发出（实测偏移 0.35 单位）→ 碰撞失效 → 穿墙（本项目实测根因，已修复）。
6. **物理层不读输入**：Move 的 velocity 由调用方（PlayerController）算好传入。
7. **不产生 GC 压力**：射线数组/临时数据避免每帧分配（面试点）。
8. **场景出生位置不得与地形重叠**：出生时与墙/天花板重叠会导致首帧检测混乱；物体出生点应贴地或留出 SkinWidth 余量。

## 依赖

- 依赖：BoxCollider2D（RequireComponent）、Physics2D.Raycast。
- 被依赖：05-player-fsm（PlayerController 调用 Move）、02-dimension（注入过滤器）。
- 不依赖：Rigidbody2D（禁止添加）、其他业务系统。

## 边界与错误处理

- 斜坡（M3）：爬坡角 > 60° 按墙；下坡需检测坡面连续性。
- 单向板（M3）：向上跳跃穿透，下落踩中阻断（由标签/掩码识别）。
- 移动平台（M3）：站在平台上时随平台移动（速度叠加）。

## 验收

见 `../tests/01-kinematic2d-test.md`。
