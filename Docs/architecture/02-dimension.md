# 02 维度系统（dimension）

> 状态：**重构**（碰撞过滤重做；MaskObject 显隐保留）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D3/D4）。

## 职责

- 维护"当前世界"状态（表/里）。
- 控制每个维度独占物体的显隐（MaskObject，**保留现有实现**）。
- 为玩家物理提供维度过滤器（ICollisionFilter），实现"里世界的墙在表世界不存在"。

## 目标文件

| 文件 | 状态 | 职责 |
|---|---|---|
| `Assets/Scripts/GameScene/SwitchWorld/WorldState.cs` | **新写** | 维度状态持有者，替代静态字段 `IsMaskActiveGlobally` |
| `Assets/Scripts/GameScene/SwitchWorld/DimensionCollisionFilter.cs` | **新写** | 实现 ICollisionFilter：命中物维度 ≠ 当前世界 → 放行变忽略 |
| `Assets/Scripts/GameScene/SwitchWorld/MaskObject.cs` | **保留不改** | 维度独占物体的显隐与预览虚影 |
| `Assets/Scripts/GameScene/SwitchWorld/DimensionPortal.cs` | 保留 | 维度传送门（如有，维持现状） |
| `Assets/Scripts/GameScene/SwitchWorld/MaskPostProcessingManager.cs` | **改造** | 切换到 Volume weight 过渡（见 03-rendering） |

## 关键设计

### WorldState（新）

- 单例（沿用 SingletonMono 风格），持有 `bool IsMaskActive`。
- 维度切换入口：`SwitchWorld()`——翻转状态 → 广播事件 → 返回新状态。
- 事件：`OnMaskStateChanged(bool)`、`OnMaskPreviewChanged(bool)` 保持现有签名（下游 MaskObject/AudioManager/渲染都已订阅，**不能改签名**）。

### DimensionCollisionFilter（新）

- 实现 `ICollisionFilter.Allow(hit)`：从命中物体上读取维度标记，与当前世界比对，一致才返回 true。
- 维度标记来源：命中物体是 MaskObject 时读其 `showWhenMaskActive`；非 MaskObject（普通地形）恒放行。
- 注入时机：玩家 Awake 时注入一次；切换时只改 WorldState，过滤器每次查询时读最新状态（**无状态过滤器**，避免切换时需要重建实例）。

### MaskObject（保留，禁止重写）

现有实现要点（写脚本时不得破坏）：
- `showWhenMaskActive` 决定属于哪个世界。
- OnEnable 订阅 + 立即刷新一次显隐（解决对象池复用后状态残留）。
- OnDisable 退订（防泄漏）。
- 支持 Tilemap + SpriteRenderer 两种渲染；预览模式（J 长按）显示半透明虚影。
- 显隐通过 Renderer.enabled 控制（不是 SetActive 物体本身）。

## 生命周期

1. 玩家 Awake：创建 WorldState 引用 + 注入 DimensionCollisionFilter。
2. 切换（PlayerController 协程内）：顿帧 → WorldState.SwitchWorld() → 广播事件 → MaskObject/Audio/渲染响应 → 恢复时间。
3. 预览（J 按住）：不改 WorldState，仅广播 OnMaskPreviewChanged。

## 核心规则（硬约束）

1. 禁止 `Physics2D.IgnoreLayerCollision`（所有调用点移除）。
2. 禁止静态字段承载维度状态（`IsMaskActiveGlobally` 删除，读 WorldState）。
3. MaskObject 的 OnEnable/OnDisable 订阅模式是已验证方案，**禁止改写**。
4. 事件签名 `OnMaskStateChanged(bool)` 等**禁止变更**（多个下游依赖）。
5. 过滤器必须无状态（每次查询读 WorldState 当前值），禁止切换时重建。
6. 地面检测禁止 OverlapBox + 手工层排除（改读 KinematicBody.LastResult）。

## 依赖

- 依赖：01-kinematic2d（ICollisionFilter 接口）。
- 被依赖：05-player-fsm（切换入口）、03-rendering（订阅）、08-presentation（AudioManager 订阅）。
- 不依赖：渲染、UI、音频（通过事件解耦）。

## 验收

见 `../tests/02-dimension-test.md`。
