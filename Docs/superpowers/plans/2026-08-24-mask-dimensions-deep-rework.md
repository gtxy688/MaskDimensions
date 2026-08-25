# Mask Dimensions 深度改造实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。
> **本项目工作流（AGENTS.md）：** 不做自动化测试。每个任务 = AI 实现 → 附验收清单（`Docs/tests/XX-xxx-test.md`）→ 用户在 Unity 手动验收 → 验收通过才 commit。AI 不得自行宣称"完成"。

**目标：** 把本项目从"一个月原型"升级为秋招旗舰：自研 2D 运动学物理（Kinematic2D）+ 维度渲染差异化（双 Volume / 2D Light / Renderer Feature），3 关全重做。

**架构：** 玩家物理从 Rigidbody2D 切换到自研 KinematicBody（X/Y 分轴射线迭代 + ICollisionFilter 维度过滤）；维度状态由 WorldState 单例持有，事件签名保持兼容；渲染走 URP Volume weight 过渡 + Light 2D + 自写 Renderer Feature 转场；关卡按"机制必过"原则重做，参数走 RoomConfigSO。

**技术栈：** Unity 2022 LTS + URP 14、C#（无命名空间，沿用现有风格）、ScriptableObject 配置、Cinemachine。

**规格来源：** `Docs/architecture/00-overview.md`（总览）、`Docs/architecture/01~09`（模块）、`Docs/decisions.md`（选型论证）、`Docs/superpowers/specs/2026-08-24-mask-dimensions-deep-rework-design.md`（设计文档）。

---

## 文件结构总览

### 新建（物理框架 `Assets/Scripts/Kinematic2D/`）
| 文件 | 职责 |
|---|---|
| `Kinematic2D/RayConfig.cs` | SO 配置：SkinWidth、射线数量、碰撞 LayerMask |
| `Kinematic2D/ColliderMapping.cs` | BoxCollider2D 几何 → 射线原点/间距（struct） |
| `Kinematic2D/CollisionResult.cs` | 碰撞结果 struct |
| `Kinematic2D/ICollisionFilter.cs` | 碰撞过滤接口 |
| `Kinematic2D/KinematicBody.cs` | 核心 MonoBehaviour：Move() X/Y 分轴求解 |
| `Kinematic2D/SlopeResolver.cs` | 斜坡爬坡/下坡（任务 6） |
| `Kinematic2D/OneWayPlatform.cs` | 单向板穿透（任务 7） |
| `Kinematic2D/MovingPlatform.cs` | 移动平台跟随（任务 8） |

### 新建（维度/渲染）
| 文件 | 职责 |
|---|---|
| `GameScene/SwitchWorld/WorldState.cs` | 维度状态单例（替代静态字段） |
| `GameScene/SwitchWorld/DimensionCollisionFilter.cs` | ICollisionFilter 实现 |
| `Rendering/DimensionVolumeController.cs` | 双 Volume weight 过渡 |
| `Rendering/DimensionTransitionFeature.cs` | Renderer Feature 转场入口（任务 11） |
| `Rendering/DimensionTransitionPass.cs` | ScriptableRenderPass（任务 11） |
| `Rendering/DimensionTransition.shader` | 转场 shader（任务 11） |

### 修改（现有）
| 文件 | 改动 |
|---|---|
| `GameScene/Player/PlayerController.cs` | 移除 Rigidbody2D/OverlapBox/IgnoreLayerCollision/静态字段，接入 KinematicBody + WorldState |
| `GameScene/Player/State/*.cs` | 物理访问改走 KinematicBody（任务 5） |
| `GameScene/SwitchWorld/MaskObject.cs` | 仅加只读属性 `ShowWhenMaskActive`，订阅改 WorldState |
| `GameScene/SwitchWorld/MaskPostProcessingManager.cs` | 任务 9 中被 DimensionVolumeController 替代（删除） |
| `Mgr/AudioManager.cs` | 订阅改 WorldState（任务 3） |
| `SO/RoomConfigSO.cs` | 扩展 dimensionRequirement / sanityDrainMultiplier（任务 12） |

### 删除
| 文件 | 原因 |
|---|---|
| `GameScene/Physics/RaycastController2D.cs` | 教程死代码（任务 1） |
| `GameScene/Physics/PlayerPhysicsController2D.cs` | 教程死代码（任务 1） |
| `com.unity.postprocessing`（Packages/manifest.json） | PPv2 与 URP 冲突（任务 1） |

---

## 任务 1：清理死代码与 PPv2

**文件：**
- 删除：`Assets/Scripts/GameScene/Physics/RaycastController2D.cs`
- 删除：`Assets/Scripts/GameScene/Physics/PlayerPhysicsController2D.cs`
- 修改：`Packages/manifest.json`（移除 `"com.unity.postprocessing": "3.4.0"` 一行）
- 修改：`Packages/packages-lock.json`（移除对应锁定条目）
- 文档：`Docs/architecture/10-legacy-physics.md`（已存在，无需改）

- [ ] **步骤 1：确认死代码无引用**

运行：`rg "RaycastController2D|PlayerPhysicsController2D" Assets/Scripts`（排除 Physics 目录自身）
预期：仅 Physics 目录内互相引用，业务代码无引用

- [ ] **步骤 2：删除两个物理文件**

注意：这两个文件当前是**未跟踪**状态（`git status` 显示 `?? Assets/Scripts/GameScene/Physics/`），`git rm` 对未跟踪文件会报错，用普通文件删除即可。

```bash
Remove-Item "Assets/Scripts/GameScene/Physics/RaycastController2D.cs", "Assets/Scripts/GameScene/Physics/PlayerPhysicsController2D.cs"
```

- [ ] **步骤 3：从 manifest.json 移除 PPv2**

编辑 `Packages/manifest.json`，删除行：
```json
"com.unity.postprocessing": "3.4.0",
```

- [ ] **步骤 4：在 Unity 中让包管理器刷新**

在 Unity 打开 Package Manager → 等待重新解析；Console 确认无 PPv2 报错。

- [ ] **步骤 5：用户验收 + Commit**

验收清单：`Docs/tests/03-rendering-test.md` 中「无 PPv2 残留」条目。
预期：项目能正常打开，无脚本引用错误（Physics 目录删除后无报错）。

```bash
git add -A
git commit -m "chore: remove legacy physics dead code and PPv2"
```

---

## 任务 2：Kinematic2D 基础框架（核心可移动）

**文件：**
- 创建：`Assets/Scripts/Kinematic2D/RayConfig.cs`
- 创建：`Assets/Scripts/Kinematic2D/ColliderMapping.cs`
- 创建：`Assets/Scripts/Kinematic2D/CollisionResult.cs`
- 创建：`Assets/Scripts/Kinematic2D/ICollisionFilter.cs`
- 创建：`Assets/Scripts/Kinematic2D/KinematicBody.cs`

- [ ] **步骤 1：创建 RayConfig.cs**

```csharp
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
}
```

- [ ] **步骤 2：创建 ColliderMapping.cs**

```csharp
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
```

- [ ] **步骤 3：创建 CollisionResult.cs**

```csharp
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
```

- [ ] **步骤 4：创建 ICollisionFilter.cs**

```csharp
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
```

- [ ] **步骤 5：创建 KinematicBody.cs（核心）**

```csharp
using UnityEngine;

/// <summary>
/// 自研 2D 运动学物理核心。
/// 职责边界：只做"把一段期望位移安全地移动出去"，不碰输入、不碰状态机、不碰渲染。
/// 为什么 X/Y 分轴：碰撞本质是分离轴问题，分轴检测避免对角线穿墙，且逻辑可独立推理。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class KinematicBody : MonoBehaviour
{
    [SerializeField] private RayConfig config;

    private BoxCollider2D boxCollider;
    private ICollisionFilter filter;
    private ColliderMapping mapping;

    public CollisionResult LastResult { get; private set; }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        RefreshMapping();
    }

    /// <summary>
    /// 注入碰撞过滤器（维度切换时由外部调用，换一个过滤器实例或让其内部状态变化）。
    /// </summary>
    public void SetFilter(ICollisionFilter filter)
    {
        this.filter = filter;
    }

    /// <summary>
    /// 每帧位移入口。velocity 由调用方（PlayerController）算好传入。
    /// </summary>
    public void Move(Vector2 velocity)
    {
        Vector2 moveAmount = velocity * Time.deltaTime;
        RefreshMapping();
        CollisionResult result = new CollisionResult();

        // X 轴：水平移动检测与修正
        if (moveAmount.x != 0f)
            moveAmount = MoveHorizontally(moveAmount, ref result);

        // Y 轴：垂直移动检测与修正
        if (moveAmount.y != 0f)
            moveAmount = MoveVertically(moveAmount, ref result);

        transform.Translate(moveAmount, Space.World);
        LastResult = result;
    }

    private void RefreshMapping()
    {
        mapping = ColliderMapping.From(boxCollider, config.skinWidth, config.horizontalRayCount, config.verticalRayCount);
    }

    private Vector2 MoveHorizontally(Vector2 moveAmount, ref CollisionResult result)
    {
        float directionX = Mathf.Sign(moveAmount.x);
        float rayLength = Mathf.Abs(moveAmount.x) + config.skinWidth;

        for (int i = 0; i < config.horizontalRayCount; i++)
        {
            Vector2 rayOrigin = directionX == -1 ? mapping.bottomLeft : mapping.bottomRight;
            rayOrigin += Vector2.up * (mapping.horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, config.collisionMask);

            if (!hit) continue;
            if (hit.distance == 0f) continue;          // 重叠跳过，防抖动
            if (filter != null && !filter.Allow(hit)) continue; // 维度过滤

            moveAmount.x = (hit.distance - config.skinWidth) * directionX;
            rayLength = hit.distance;

            result.HitLeft = directionX == -1;
            result.HitRight = directionX == 1;
        }
        return moveAmount;
    }

    private Vector2 MoveVertically(Vector2 moveAmount, ref CollisionResult result)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + config.skinWidth;

        for (int i = 0; i < config.verticalRayCount; i++)
        {
            // 为什么 + moveAmount.x：垂直射线原点跟随水平修正后的位置，防止贴墙时漏检
            Vector2 rayOrigin = directionY == -1 ? mapping.bottomLeft : mapping.topLeft;
            rayOrigin += Vector2.right * (mapping.verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, config.collisionMask);

            if (!hit) continue;
            if (hit.distance == 0f) continue;
            if (filter != null && !filter.Allow(hit)) continue;

            moveAmount.y = (hit.distance - config.skinWidth) * directionY;
            rayLength = hit.distance;

            result.IsGrounded = directionY == -1;
            result.HitCeiling = directionY == 1;
        }
        return moveAmount;
    }
}
```

- [ ] **步骤 6：创建 RayConfig 资产**

在 Project 窗口 `Assets/SO/` 下：右键 → Create → Kinematic2D/Ray Config，命名 `PlayerRayConfig`，collisionMask 设为地形层（Ground/Platform 等）。

- [ ] **步骤 7：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md` 中「基础移动/跳跃落地/撞墙/顶头/穿墙回归/SkinWidth 防卡缝」条目。
前置：此任务不要求玩家接入（任务 4 才接入），可用临时测试物体验证——在场景放一个带 BoxCollider2D + KinematicBody 的方块，写一个临时脚本调用 `Move`，确认不穿墙。

```bash
git add Assets/Scripts/Kinematic2D Assets/SO
git commit -m "feat(kinematic2d): core kinematic body with X/Y separated raycast collision"
```

---

## 任务 3：WorldState + DimensionCollisionFilter + 事件迁移

**文件：**
- 创建：`Assets/Scripts/GameScene/SwitchWorld/WorldState.cs`
- 创建：`Assets/Scripts/GameScene/SwitchWorld/DimensionCollisionFilter.cs`
- 修改：`Assets/Scripts/GameScene/SwitchWorld/MaskObject.cs`（加只读属性 + 订阅改 WorldState）
- 修改：`Assets/Scripts/Mgr/AudioManager.cs`（订阅改 WorldState）

- [ ] **步骤 1：创建 WorldState.cs**

```csharp
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
}
```

- [ ] **步骤 2：创建 DimensionCollisionFilter.cs**

```csharp
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
```

- [ ] **步骤 3：MaskObject 加只读属性 + 订阅改 WorldState**

在 `MaskObject.cs` 中：
- 私有字段 `showWhenMaskActive` 后新增只读属性：
```csharp
/// <summary>当前物体所属维度（供碰撞过滤器查询）。</summary>
public bool ShowWhenMaskActive => showWhenMaskActive;
```
- OnEnable/OnDisable 中两处订阅改为：
```csharp
WorldState.OnMaskStateChanged += HandleMaskStateChanged;
WorldState.OnMaskPreviewChanged += HandleMaskPreviewChanged;
```
（退订对应改 WorldState。显隐逻辑本身不动——AGENTS.md 约束 6。）

- [ ] **步骤 4：AudioManager 订阅改 WorldState**

`AudioManager.cs` 的 Start/OnDestroy 中：
```csharp
WorldState.OnMaskStateChanged += SwitchBGMDimension;
WorldState.OnMaskStateChanged -= SwitchBGMDimension;
```

- [ ] **步骤 5：场景挂载 WorldState（编辑器操作，用户在 Unity 中执行）**

注意：`WorldState : SingletonMono<WorldState>` 是手动挂载的 MonoBehaviour 单例（不是 SingletonAutoMono），**必须在场景中放置实例**，否则 `WorldState.Instance` 为空、事件迁移后维度切换无响应。

编辑器操作：
1. 打开 `GameScene`，在 Hierarchy 创建空对象，命名 `WorldState`
2. Add Component → 搜索 `WorldState` 挂上
3. 确认场景中只有一个该对象

若跨场景需要常驻：`SingletonMono` 的 Awake 已调 `DontDestroyOnLoad`，但需注意它是"场景首次加载时挂载"——若从 BeginScene 进入 GameScene，应在 GameScene 挂即可（玩家切换逻辑只在 GameScene 用）。

- [ ] **步骤 6：用户验收 + Commit**

验收清单：`Docs/tests/02-dimension-test.md` 中「全局状态已移除」前置确认（`IsMaskActiveGlobally` 尚在 PlayerController，任务 4 删除；本任务验收事件迁移：切换时 UI/音频/显隐仍响应，且 `WorldState.Instance` 非空）。

```bash
git add Assets/Scripts/GameScene/SwitchWorld Assets/Scripts/Mgr
git commit -m "feat(dimension): WorldState singleton + DimensionCollisionFilter + event migration"
```

---

## 任务 4：PlayerController 接入 KinematicBody（移除 Rigidbody2D）

**文件：**
- 修改：`Assets/Scripts/GameScene/Player/PlayerController.cs`（大改）
- 修改：`Assets/Scripts/GameScene/Player/State/JumpState.cs`、`FallState.cs`、`MoveState.cs`、`HitState.cs`、`DieState.cs`、`IdleState.cs`、`MaskSwitchState.cs`

- [ ] **步骤 1：PlayerController 头部改造**

```csharp
// 移除：RequireComponent(typeof(Rigidbody2D)) → 改为 [RequireComponent(typeof(KinematicBody), typeof(BoxCollider2D))]
// 移除：public Rigidbody2D RB { get; private set; }
// 新增：
public KinematicBody KinematicBody { get; private set; }
[SerializeField] private RayConfig rayConfig;   // 拖 PlayerRayConfig
private ICollisionFilter collisionFilter;

// Awake 中：
KinematicBody = GetComponent<KinematicBody>();
collisionFilter = new DimensionCollisionFilter();
KinematicBody.SetFilter(collisionFilter);
```

- [ ] **步骤 2：替换物理驱动**

`FixedUpdate` 改为在 `Update` 中调用（或保留 FixedUpdate 但调用 `KinematicBody.Move`）：
```csharp
private void FixedUpdate()
{
    StateMachine.CurrentState?.PhysicsUpdate();
}
```
状态类中所有 `player.RB.velocity = ...` 改为 `player.SetVelocity(...)`，在 PlayerController 新增：
```csharp
/// <summary>物理入口：状态机把期望速度交给 KinematicBody。为什么统一入口：物理层只认速度，不认"跳跃/移动"语义。</summary>
public void SetVelocity(Vector2 velocity)
{
    KinematicBody.Move(velocity);
}
```

- [ ] **步骤 3：删除 OverlapBox 地面检测与双重排除**

删除 `CheckGrounded()` 中的 OverlapBox 逻辑，改为：
```csharp
private void CheckGrounded()
{
    IsGrounded = KinematicBody.LastResult.IsGrounded;
}
```
`IsTouchingWall` 改为读 `KinematicBody.LastResult.HitLeft/HitRight`。

- [ ] **步骤 4：删除 IgnoreLayerCollision 与静态维度字段**

- 删除 `UpdateLayerCollisions()`（整个方法）及 Awake 中对它的调用
- 删除 `public static bool IsMaskActiveGlobally`，所有读取处改为 `WorldState.Instance.IsMaskActive`
- 删除 `isMaskActive` 字段，改为读 WorldState；`CanSwitchMask` 白名单逻辑保留
- 切换协程 `ExecuteMaskSwitchWithHitlag` 中：`isMaskActive = !isMaskActive; IsMaskActiveGlobally = ...; UpdateLayerCollisions();` 改为：
```csharp
WorldState.Instance.SwitchWorld(); // 内部翻转 + 广播
```
- 死亡重生/ResetForNewRoom 中 `isMaskActive = false; IsMaskActiveGlobally = false; UpdateLayerCollisions();` 改为：
```csharp
WorldState.Instance.SetWorld(false);
```

- [ ] **步骤 5：清理静态事件归属**

`PlayerController` 上的 `OnMaskStateChanged` / `OnMaskPreviewChanged` 静态事件删除（已迁移到 WorldState，任务 3 完成）。保留理智相关事件（OnSanityChanged 等，仍属 PlayerController）。

- [ ] **步骤 6：各状态类适配**

逐个检查 `State/` 下 7 个状态类，把：
- `player.RB.velocity` → `player.SetVelocity(...)`
- `player.RB.simulated` → 删除（KinematicBody 无此概念；死亡时改用 KinematicBody 开关或碰撞器开关）
- 跳跃初速度：`SetVelocity(new Vector2(vel.x, player.JumpForce))`（JumpForce 从 PlayerConfigSO 读取，不变）

- [ ] **步骤 7：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md`（基础移动全套）+ `Docs/tests/05-player-fsm-test.md`（状态流转/死亡重生/无 Rigidbody2D 依赖）。

```bash
git add Assets/Scripts/GameScene/Player
git commit -m "feat(player): migrate player from Rigidbody2D to KinematicBody"
```

---

## 任务 5：手感回归与边界（土狼时间/跳缓冲/顿帧）

**文件：**
- 修改：`Assets/Scripts/GameScene/Player/PlayerController.cs`（计时器部分微调）
- 修改：`Assets/Scripts/GameScene/Player/PlayerConfigSO.cs`（如有需要）

- [ ] **步骤 1：验证计时器逻辑与新物理兼容**

`UpdateJumpTimers()` 的 CoyoteTime/JumpBuffer 逻辑**不改动**（数值走 PlayerConfigSO）；确认 `IsGrounded` 现在来自 `LastResult` 后，计时器行为一致。

- [ ] **步骤 2：验证顿帧与预览**

`ExecuteMaskSwitchWithHitlag` 的 `Time.timeScale = 0f` + `WaitForSecondsRealtime(0.15f)` 不变；确认 KinematicBody.Move 使用 `Time.deltaTime`，顿帧期间 deltaTime≈0 → 玩家自然冻结（正确行为）。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md`（土狼时间/跳缓冲/顿帧兼容）+ `Docs/tests/05-player-fsm-test.md`（输入手感/时间系统回归）。

```bash
git commit -am "feat(player): verify game feel timers with kinematic physics"
```

---

## 任务 6：斜坡增量（SlopeResolver）

**文件：**
- 创建：`Assets/Scripts/Kinematic2D/SlopeResolver.cs`
- 修改：`Assets/Scripts/Kinematic2D/KinematicBody.cs`

- [ ] **步骤 1：创建 SlopeResolver.cs**

```csharp
using UnityEngine;

/// <summary>
/// 斜坡换算：把水平位移分解为沿坡面的 x/y 分量。
/// 为什么独立类：斜坡数学是纯几何，可独立推理；KinematicBody 只调用结果。
/// 硬规则：爬坡角 > maxClimbAngle 时按墙处理（不做爬坡）。
/// </summary>
public static class SlopeResolver
{
    public const float MaxClimbAngle = 60f;
    public const float MaxDescendAngle = 60f;

    /// <summary>
    /// 计算爬坡位移。返回 true 表示本帧处于爬坡状态。
    /// </summary>
    public static bool ResolveClimb(Vector2 moveAmount, float slopeAngle, float skinWidth,
        out Vector2 resolved, out float resolvedAngle)
    {
        resolved = moveAmount;
        resolvedAngle = 0f;

        if (slopeAngle <= 0f || slopeAngle > MaxClimbAngle) return false;

        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        // 仅当期望位移不足以"越过"坡面时爬坡，否则按正常水平移动（可跳离坡顶）
        if (moveAmount.y <= climbY)
        {
            resolved.y = climbY;
            resolved.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            resolvedAngle = slopeAngle;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 计算下坡位移：沿坡面下滑，贴地不腾空。
    /// </summary>
    public static bool ResolveDescend(Vector2 moveAmount, float slopeAngle, float skinWidth,
        out Vector2 resolved)
    {
        resolved = moveAmount;
        if (slopeAngle <= 0f || slopeAngle > MaxDescendAngle) return false;

        float moveDistance = Mathf.Abs(moveAmount.x);
        float descendY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        resolved.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
        resolved.y -= descendY;
        return true;
    }
}
```

- [ ] **步骤 2：KinematicBody 接入斜坡检测**

在 `MoveHorizontally` 中，命中后取法线角度：
```csharp
float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
if (SlopeResolver.ResolveClimb(moveAmount, slopeAngle, config.skinWidth, out Vector2 climbAmount, out float resolvedAngle))
{
    moveAmount = climbAmount;
    result.OnSlope = true;
    result.SlopeAngle = resolvedAngle;
    result.IsGrounded = true;
    continue;
}
```
在 `MoveVertically` 前增加下坡检测：当 `moveAmount.y < 0` 时，向下发射一根长射线，若命中坡面则调用 `ResolveDescend` 修正。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md`「增量模块」斜坡/下坡条目。

```bash
git add Assets/Scripts/Kinematic2D
git commit -m "feat(kinematic2d): slope climb/descend resolver"
```

---

## 任务 7：单向板增量（OneWayPlatform）

**文件：**
- 创建：`Assets/Scripts/Kinematic2D/OneWayPlatform.cs`
- 修改：`Assets/Scripts/Kinematic2D/KinematicBody.cs`

- [ ] **步骤 1：创建 OneWayPlatform.cs**

```csharp
using UnityEngine;

/// <summary>
/// 单向板标记组件：挂在地形/平台碰撞体上。
/// 规则：向上跳跃穿透；下落踩中才阻断（standingOnPassThrough）。
/// 为什么用组件标记而非 Tag：组件查询比字符串 Tag 更类型安全、更快。
/// </summary>
public class OneWayPlatform : MonoBehaviour { }
```

- [ ] **步骤 2：KinematicBody 垂直检测接入单向板规则**

在 `MoveVertically` 中，命中后检查：
```csharp
if (hit.collider.GetComponent<OneWayPlatform>() != null)
{
    if (directionY == 1) continue;             // 向上跳直接穿过
    result.OnMovingPlatform = true;            // 复用字段？不——新增字段说明见下
}
```
说明：单向板踩中时 `IsGrounded` 置 true 已有逻辑覆盖（directionY == -1 时）。若需要区分"踩在单向板上"（用于下穿：按住下+跳时清除站立），在 CollisionResult 增加 `public bool StandingOnOneWay;`，在命中单向板且 directionY == -1 时置位。

- [ ] **步骤 3：下穿支持（可选，按验收清单决定）**

若验收要求"按住下+跳下穿"：在 `PlayerController` 的 JumpState 中，当输入含下方向且 `LastResult.StandingOnOneWay` 时，跳过本次跳跃并让速度向下（走正常下落即可穿过，因为单向板只在向下时被射线检测——需在 MoveVertically 中对 directionY == -1 且 StandingOnOneWay 且玩家主动下穿时跳过命中）。

实现：
```csharp
// KinematicBody 新增：下穿请求标记
public bool DropThroughRequested { get; set; }
// MoveVertically 中：
if (hit.collider.GetComponent<OneWayPlatform>() != null)
{
    if (directionY == 1) continue;
    if (DropThroughRequested) { DropThroughRequested = false; continue; }
    result.StandingOnOneWay = true;
}
```

- [ ] **步骤 4：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md`「增量模块」单向板条目。

```bash
git add Assets/Scripts/Kinematic2D
git commit -m "feat(kinematic2d): one-way platform support"
```

---

## 任务 8：移动平台增量（MovingPlatform）

**文件：**
- 创建：`Assets/Scripts/Kinematic2D/MovingPlatform.cs`
- 修改：`Assets/Scripts/Kinematic2D/KinematicBody.cs`

- [ ] **步骤 1：创建 MovingPlatform.cs**

```csharp
using UnityEngine;

/// <summary>
/// 移动平台：在两点间往返移动。玩家站立时随平台移动。
/// 实现：平台自身每帧计算位移；KinematicBody 检测到"站立在平台"时把平台位移叠加进玩家位移。
/// 为什么方向标记：平台上的玩家应随平台一起动，否则会被平台"甩下"。
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;

    public Vector2 CurrentDelta { get; private set; }

    private Vector2 target;

    private void Start()
    {
        transform.position = pointA.position;
        target = pointB.position;
    }

    private void Update()
    {
        Vector2 before = transform.position;
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
        CurrentDelta = (Vector2)transform.position - before;

        if (Vector2.Distance(transform.position, target) < 0.01f)
            target = target == (Vector2)pointA.position ? pointB.position : pointA.position;
    }
}
```

- [ ] **步骤 2：KinematicBody 跟随平台**

```csharp
// CollisionResult 增加：public Transform StandingPlatform;
// MoveVertically 中，命中且 directionY == -1 时：
//   result.StandingPlatform = hit.transform; result.OnMovingPlatform = true;
// Move() 末尾应用位移后：
if (LastResult.StandingPlatform != null)
{
    var platform = LastResult.StandingPlatform.GetComponent<MovingPlatform>();
    if (platform != null)
        transform.Translate(platform.CurrentDelta, Space.World);
}
```

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/01-kinematic2d-test.md`「增量模块」移动平台条目。

```bash
git add Assets/Scripts/Kinematic2D
git commit -m "feat(kinematic2d): moving platform follow support"
```

---

## 任务 9：渲染线 1——双 Volume 维度过渡

**文件：**
- 创建：`Assets/Scripts/Rendering/DimensionVolumeController.cs`
- 删除：`Assets/Scripts/GameScene/SwitchWorld/MaskPostProcessingManager.cs`
- 场景：`Assets/Scenes/GameScene.unity`（挂 Volume 组件 + 配 Profile）

- [ ] **步骤 1：创建 DimensionVolumeController.cs**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 双 Volume 维度过渡：表世界/里世界各一个 Volume，切换时 weight 交叉淡化。
/// 为什么 unscaledDeltaTime：顿帧期间 timeScale=0，必须用真实时间驱动过渡（断片感）。
/// </summary>
public class DimensionVolumeController : MonoBehaviour
{
    [SerializeField] private Volume realWorldVolume;  // 表世界 Profile
    [SerializeField] private Volume maskWorldVolume;  // 里世界 Profile
    [SerializeField] private float transitionDuration = 0.4f;

    private Coroutine transitionRoutine;

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= HandleMaskStateChanged;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(isMaskActive));
    }

    private IEnumerator TransitionRoutine(bool isMaskActive)
    {
        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            realWorldVolume.weight = isMaskActive ? 1f - t : t;
            maskWorldVolume.weight = isMaskActive ? t : 1f - t;
            yield return null;
        }
        realWorldVolume.weight = isMaskActive ? 0f : 1f;
        maskWorldVolume.weight = isMaskActive ? 1f : 0f;
    }
}
```

- [ ] **步骤 2：场景配置两个 Volume + Profile**

在 GameScene：
- 创建 `Volume`（isGlobal=true）→ 新建 Profile `RealWorldProfile`：Color Adjustments（饱和度 0、曝光 0）、Vignette（轻微）
- 创建第二个 `Volume`（isGlobal=true）→ 新建 Profile `MaskWorldProfile`：Color Adjustments（饱和度 -40、曝光 -0.5）、Film Grain（噪点）、自写扫描线（如无，先用 Grain 占位）
- 拖给 `DimensionVolumeController` 的两个引用

- [ ] **步骤 3：删除 MaskPostProcessingManager**

```bash
git rm Assets/Scripts/GameScene/SwitchWorld/MaskPostProcessingManager.cs
```
并从场景中移除该组件与旧全局 Volume（若有）。

- [ ] **步骤 4：用户验收 + Commit**

验收清单：`Docs/tests/03-rendering-test.md`（表/里视觉、切换过渡、顿帧期间过渡继续、无 PPv2 残留）。

```bash
git add Assets/Scripts/Rendering Assets/Scenes/GameScene.unity
git commit -m "feat(rendering): dual-volume dimension transition replacing MaskPostProcessingManager"
```

---

## 任务 10：渲染线 2——URP 2D Light 双世界光照

**文件：**
- 修改：URP Renderer 资产（启用 Light 2D：Renderer Features 添加 Render Objects 或使用 2D Renderer 自带光照）
- 场景：`Assets/Scenes/GameScene.unity`（Global Light ×2、玩家持灯 Point Light）
- 修改：`Assets/Scripts/GameScene/Player/PlayerController.cs`（持灯显隐随维度）

- [ ] **步骤 1：确认 URP 2D Renderer**

Project Settings → Graphics → 确认当前使用 URP 2D Renderer（`UniversalRendererData` 中 `Renderer Features` 可用）；若当前是默认 3D Renderer，改为 2D Renderer（URP Asset → Renderer List）。

- [ ] **步骤 2：场景布光**

- 表世界：`Global Light 2D`（Intensity 1，冷白）
- 里世界：第二个 `Global Light 2D`（Intensity 0.15，暗蓝）→ 切换时二者 intensity 交叉（复用 DimensionVolumeController 的切换事件或独立 LightController）
- 玩家子物体挂 `Point Light 2D`（暖黄，作为"持灯"），表世界关闭/调弱、里世界开启

- [ ] **步骤 3：创建 DimensionLightController.cs**

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 双世界光照切换：表/里各一个 Global Light 2D，随维度事件交叉淡化。
/// 为什么真实光照而非调亮度：光有遮挡与层次，是关卡信息传达手段（危险/安全）。
/// </summary>
public class DimensionLightController : MonoBehaviour
{
    [SerializeField] private Light2D realWorldLight;
    [SerializeField] private Light2D maskWorldLight;
    [SerializeField] private Light2D playerHeldLight;
    [SerializeField] private float transitionDuration = 0.4f;

    private void OnEnable() => WorldState.OnMaskStateChanged += Handle;
    private void OnDisable() => WorldState.OnMaskStateChanged -= Handle;

    private void Handle(bool isMaskActive)
    {
        StopAllCoroutines();
        StartCoroutine(Transition(isMaskActive));
    }

    private System.Collections.IEnumerator Transition(bool isMaskActive)
    {
        float t = 0f;
        float startReal = realWorldLight.intensity;
        float startMask = maskWorldLight.intensity;
        float targetReal = isMaskActive ? 0.15f : 1f;
        float targetMask = isMaskActive ? 1f : 0.15f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime; // 顿帧期间继续
            float k = Mathf.Clamp01(t / transitionDuration);
            realWorldLight.intensity = Mathf.Lerp(startReal, targetReal, k);
            maskWorldLight.intensity = Mathf.Lerp(startMask, targetMask, k);
            yield return null;
        }
        if (playerHeldLight != null) playerHeldLight.enabled = isMaskActive;
    }
}
```

- [ ] **步骤 4：用户验收 + Commit**

验收清单：`Docs/tests/03-rendering-test.md`（2D 光照差异、遮挡层次、切换后同步）。

```bash
git add Assets/Scripts/Rendering Assets/Scenes/GameScene.unity
git commit -m "feat(rendering): dual-world URP 2D lighting"
```

---

## 任务 11：渲染线 3——自写 Renderer Feature 扫屏转场

**文件：**
- 创建：`Assets/Scripts/Rendering/DimensionTransitionFeature.cs`
- 创建：`Assets/Scripts/Rendering/DimensionTransitionPass.cs`
- 创建：`Assets/Shaders/DimensionTransition.shader`

- [ ] **步骤 1：创建转场 shader**

```hlsl
// Assets/Shaders/DimensionTransition.shader
Shader "Hidden/DimensionTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Direction ("Direction", Vector) = (1,0,0,0)
        _Width ("Width", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Progress; float4 _Direction; float _Width;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // 扫屏：沿方向推进一条边，未过部分显示原画面，过线部分混合暗色
                float edge = dot(input.uv - 0.5, _Direction.xy) + _Progress;
                float band = smoothstep(_Width, 0.0, abs(edge)) * 0.8; // 边缘光带
                col.rgb = lerp(col.rgb, col.rgb * 0.2, step(0.0, edge)); // 已扫过区变暗
                col.rgb += band;
                return col;
            }
            ENDHLSL
        }
    }
}
```

- [ ] **步骤 2：创建 Renderer Feature**

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 转场 Renderer Feature：在渲染流程插入一个全屏 Pass。
/// 为什么自写：展示 ScriptableRenderPass / CommandBuffer 功底，且能精确对齐顿帧时机。
/// </summary>
public class DimensionTransitionFeature : ScriptableRendererFeature
{
    [Range(0f, 1f)] public float progress;
    public Vector2 direction = Vector2.right;
    [Range(0f, 1f)] public float width = 0.15f;

    private DimensionTransitionPass pass;

    public override void Create()
    {
        pass = new DimensionTransitionPass();
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.ConfigureInput(ScriptableRenderPassInput.Color);
        pass.Setup(progress, direction, width);
        renderer.EnqueuePass(pass);
    }

    /// <summary>供切换协程驱动进度（unscaledDeltaTime）。</summary>
    public void SetProgress(float value) => progress = Mathf.Clamp01(value);
}
```

- [ ] **步骤 3：创建 ScriptableRenderPass**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 全屏转场 Pass：Blit 全屏四边形，shader 负责扫屏效果。
/// </summary>
public class DimensionTransitionPass : ScriptableRenderPass
{
    private Material material;
    private RTHandle tempRT;

    public DimensionTransitionPass()
    {
        material = new Material(Shader.Find("Hidden/DimensionTransition"));
    }

    public void Setup(float progress, Vector2 direction, float width)
    {
        if (material == null) return;
        material.SetFloat("_Progress", progress);
        material.SetVector("_Direction", direction);
        material.SetFloat("_Width", width);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null) return;
        CommandBuffer cmd = CommandBufferPool.Get("DimensionTransition");

        RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraTarget, cameraTarget, material); // 就地全屏处理

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
```

- [ ] **步骤 4：接入切换协程**

在 `PlayerController.ExecuteMaskSwitchWithHitlag` 中（顿帧期间）驱动转场：
```csharp
// 需要拿到 Feature 引用（场景中挂 DimensionTransitionFeature 的相机/物体）
DimensionTransitionFeature feature = FindObjectOfType<DimensionTransitionFeature>();
if (feature != null)
{
    float t = 0f;
    while (t < 0.15f)
    {
        t += Time.unscaledDeltaTime;
        feature.SetProgress(t / 0.15f);
        yield return null;
    }
    feature.SetProgress(0f);
}
```

- [ ] **步骤 5：用户验收 + Commit**

验收清单：`Docs/tests/03-rendering-test.md`（转场效果正常播放一次、不残留、性能）。

```bash
git add Assets/Scripts/Rendering Assets/Shaders Assets/Scripts/GameScene/Player
git commit -m "feat(rendering): custom renderer feature transition"
```

---

## 任务 12：RoomConfigSO 扩展

**文件：**
- 修改：`Assets/Scripts/SO/RoomConfigSO.cs`
- 修改：`Assets/Scripts/GameScene/Room/RoomTrigger.cs`（如需要维度强制）

- [ ] **步骤 1：扩展 RoomConfigSO**

```csharp
[CreateAssetMenu(fileName = "NewRoom", menuName = "Config/Room")]
public class RoomConfigSO : ScriptableObject
{
    public string roomName;
    [TextArea] public string description;

    [Header("子弹配置")]
    public BulletStatsSO bulletStats;
    public float fireInterval = 1f;

    [Header("BGM")]
    public AudioClip bgmOverride;

    [Header("维度要求（关卡重做新增）")]
    [Tooltip("0=不限制，1=必须表世界，2=必须里世界；进入时若不满足则自动切换")]
    public int dimensionRequirement = 0;

    [Tooltip("理智消耗倍率（L3 压力用，默认 1）")]
    public float sanityDrainMultiplier = 1f;
}
```

- [ ] **步骤 2：PlayerController 应用理智倍率**

`sanity 流逝` 处：`currentSanity -= config.activeSanityCostRate * Time.deltaTime;` 改为读取当前房间倍率（RoomManager 持有 currentRoom → roomConfig.sanityDrainMultiplier）。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/04-levels-test.md`（房间参数来自 SO、扩展字段生效）。

```bash
git add Assets/Scripts/SO Assets/Scripts/GameScene
git commit -m "feat(levels): extend RoomConfigSO with dimension requirement and sanity multiplier"
```

---

## 任务 13：关卡 L1「表里初现」重做

**文件：**
- 修改：`Assets/Scenes/GameScene.unity`（或新场景，按现状决定）
- 创建：`Assets/SO/Rooms/Room_L1_*.asset`（RoomConfigSO 资产）

- [ ] **步骤 1：搭建 L1 结构**

设计：教学段（表世界安全区 + 第一个里世界桥）→ 应用段（表墙/里路的交替）→ 挑战段（小段连续切换）。
- 用 Tilemap 分两层：`OldDimension` 层（表世界地形）、`NewDimension` 层（里世界地形），分别挂 MaskObject（showWhenMaskActive 对应）
- 关键点 1（机制必过）：表世界一段断桥，只有切里世界才有桥 → 不切换过不去
- 关键点 2：里世界限时门（理智消耗区），回表世界等理智恢复再进

- [ ] **步骤 2：配置房间数据**

创建 `RoomConfigSO` 资产：L1_教学段（dimensionRequirement=0）、L1_挑战段（sanityDrainMultiplier=1.5）。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/04-levels-test.md`（L1 完整通关、机制必过检查维度切换项、房间框架回归）。

```bash
git add Assets/Scenes/GameScene.unity Assets/SO/Rooms
git commit -m "feat(levels): rebuild L1 dimension tutorial level"
```

---

## 任务 14：关卡 L2「斜坡法则」重做

**文件：**
- 修改：`Assets/Scenes/GameScene.unity`
- 创建：`Assets/SO/Rooms/Room_L2_*.asset`

- [ ] **步骤 1：搭建 L2 结构**

设计：教学段（缓坡冲刺跳跃）→ 应用段（下坡加速长跳 + 单向板上下穿）→ 挑战段（斜坡 + 弹幕组合）。
- 关键点 1（机制必过）：一段长跨度必须靠**下坡加速**才能跳过
- 关键点 2：必须从单向板**下穿**才能到达的区域
- 关键点 3：斜坡冲刺 + 切里世界保留动量（切换瞬间不掉速——物理层天然支持，需关卡验证）

- [ ] **步骤 2：配置房间数据**

L2 各段：fireInterval 递减制造压力；挑战段 sanityDrainMultiplier=2。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/04-levels-test.md`（L2 完整通关、机制必过检查斜坡/单向板/动量继承项）。

```bash
git add Assets/Scenes/GameScene.unity Assets/SO/Rooms
git commit -m "feat(levels): rebuild L2 slope & momentum level"
```

---

## 任务 15：关卡 L3「断章」重做

**文件：**
- 修改：`Assets/Scenes/GameScene.unity`
- 创建：`Assets/SO/Rooms/Room_L3_*.asset`

- [ ] **步骤 1：搭建 L3 结构**

设计：教学段（综合热身）→ 应用段（连续切换解谜：里世界结构 + 表世界结构交替依赖）→ 挑战段（里世界限时通道 + 弹幕密集）。
- 关键点 1（机制必过）：一段只能短暂停留的里世界通道（理智耗尽强制弹回 = 重来）
- 关键点 2：连续切换谜题（先表后里再表，中间依赖动量）
- 关键点 3：EndGameTrigger 收尾

- [ ] **步骤 2：配置房间数据**

L3 各段：sanityDrainMultiplier=2.5（挑战段），弹幕 interval 最短。

- [ ] **步骤 3：用户验收 + Commit**

验收清单：`Docs/tests/04-levels-test.md`（L3 完整通关、理智压迫实际惩罚、通关流程）。

```bash
git add Assets/Scenes/GameScene.unity Assets/SO/Rooms
git commit -m "feat(levels): rebuild L3 final level with sanity pressure"
```

---

## 任务 16：性能数据 + README + 面试提纲（M6 弹药）

**文件：**
- 创建：`README.md`（根目录，项目主页）
- 创建：`Docs/interview-prep.md`（面试提纲）
- 修改：`Docs/total.md`（记录最终决策）

- [ ] **步骤 1：采集性能数据**

在 Unity Profiler 中记录 3 组数字：
1. 对象池 GC：弹幕段持续 30 秒的 GC Alloc（对比改造前如果有记录）
2. 维度切换瞬时耗时：切换瞬间帧耗时（Profiler CPU 视图）
3. 物理开销：移动/跳跃时 KinematicBody 射线检测耗时（vs 旧 Rigidbody2D，如可对比）
把数字写进 README 与 `Docs/interview-prep.md`。

- [ ] **步骤 2：写 README.md**

结构：玩法 30 秒介绍（表里世界 + 理智 + 维度切换）→ 技术亮点（自研物理/维度过滤/渲染三线，各 2-3 句）→ 架构图（引用 00-overview）→ 性能数据表 → 如何运行（Unity 版本、打开场景）→ 链接（itch.io/视频，如有）。

- [ ] **步骤 3：写 Docs/interview-prep.md**

按 `Docs/decisions.md` 的 12 条决策，每条写：面试官可能的问题 + 3 句话标准答案（X/Y/为什么压缩版）。另附 git log 中的真实 bug 故事（粘墙、相机异常、进不了下一关）作为 STAR 素材。

- [ ] **步骤 4：用户验收 + Commit**

验收：README 打开无断链；interview-prep 每条答案你本人能复述。

```bash
git add README.md Docs/interview-prep.md Docs/total.md
git commit -m "docs: add README, performance data, interview prep"
```

---

## 自检记录

**规格覆盖度：**
- 01-kinematic2d（物理框架）→ 任务 2/6/7/8 ✅
- 02-dimension（WorldState/过滤器/MaskObject）→ 任务 3/4 ✅
- 03-rendering（Volume/2D Light/Feature + 移除 PPv2）→ 任务 1/9/10/11 ✅
- 04-levels（3 关重做 + SO 扩展）→ 任务 12/13/14/15 ✅
- 05-player-fsm（PlayerController 接入）→ 任务 4/5 ✅
- 06-pool（保留）→ 无任务（不改动），由任务 16 采集数据 ✅
- 07-bullet（保留）→ 无任务（不改动）✅
- 08-presentation（保留；AudioManager 订阅迁移）→ 任务 3 ✅
- 09-base-framework（保留）→ 无任务（不改动）✅
- 10-legacy-physics（删除归档）→ 任务 1 ✅
- 设计文档 M1~M6 里程碑 → 任务 2-5=M1/M2，6-8=M3，9-11=M4，12-15=M5，16=M6 ✅

**占位符扫描：** 无 TODO/待定；每个任务有具体代码、验收清单引用与 commit 命令 ✅

**类型一致性：** `CollisionResult` 字段在任务 2 定义后，任务 6/7/8 只做增量扩展（StandingOnOneWay/StandingPlatform）；`WorldState.SwitchWorld/SetWorld` 在任务 3 定义，任务 4 使用；`KinematicBody.SetFilter/Move/LastResult` 签名全程一致 ✅
