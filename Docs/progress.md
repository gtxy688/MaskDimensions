# 项目进度跟踪（progress.md）

> 本文件是**项目级进度总览**（用户可见、新对话可直接读取）。
> 子代理执行的详细账本在 `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md`（隐藏目录，同为权威记录）。
> 最后更新：2026-08-26

---

## 一、当前总览

**项目**：Mask Dimensions 深度改造（秋招旗舰项目）
**目标**：自研 2D 运动学物理（Kinematic2D）+ 维度渲染差异化
**分支**：master
**执行模式**：subagent-driven-development（AI 实现 → 任务审查 → 用户在 Unity 手动验收 → 验收通过才 commit）
**当前进度**：任务 4/16 完成 ✅（任务 1、2、3 也已完成）

## 二、任务状态表

| 任务 | 内容 | 状态 | 提交 |
|---|---|---|---|
| 1 | 清理死代码 + 移除 PPv2 | ✅ 完成（已验收） | `4178633` |
| 2 | Kinematic2D 核心框架 | ✅ 完成（已验收） | `f69178c` |
| 3 | 维度系统（WorldState + 过滤器 + 事件迁移） | ✅ 完成（已验收） | `3e605bf`（+chore `04a2d08`） |
| 4 | 玩家接入 KinematicBody（移除 Rigidbody2D 驱动） | ✅ 完成（已验收） | `912d704` |
| 5 | 手感回归（土狼时间/跳缓冲/顿帧） | ⏳ **下一个** | — |
| 6 | 斜坡（SlopeResolver） | 待办 | — |
| 7 | 单向板 | 待办 | — |
| 8 | 移动平台 | 待办 | — |
| 9 | 渲染线1：双 Volume 维度过渡 | 待办 | — |
| 10 | 渲染线2：URP 2D Light | 待办 | — |
| 11 | 渲染线3：自写 Renderer Feature 转场 | 待办 | — |
| 12 | RoomConfigSO 扩展 | 待办 | — |
| 13 | 关卡 L1 重做 | 待办 | — |
| 14 | 关卡 L2 重做 | 待办 | — |
| 15 | 关卡 L3 重做 | 待办 | — |
| 16 | 性能数据 + README + 面试提纲 | 待办 | — |

## 三、已完成任务详情

### 任务 1：清理死代码与 PPv2（`4178633`）

- 删除 `Assets/Scripts/GameScene/Physics/`（RaycastController2D / PlayerPhysicsController2D + meta）
- `Packages/manifest.json`、`packages-lock.json` 移除 `com.unity.postprocessing`（保留 `com.coplaydev.unity-mcp`）
- 用户在 Unity 中清除了 GameScene/TestGameScene 里的 Missing Script 组件引用
- 验收通过

### 任务 2：Kinematic2D 核心框架（`f69178c`）

**交付文件**（`Assets/Scripts/Kinematic2D/`）：
- `RayConfig.cs`：射线配置（SO：skinWidth=0.015 / 4×4 射线 / collisionMask）
- `ColliderMapping.cs`：BoxCollider2D bounds → 射线原点/间距（struct）
- `CollisionResult.cs`：碰撞结果（IsGrounded/四面标志/斜坡字段，struct）
- `ICollisionFilter.cs`：维度过滤接口（无状态，切换无需重建）
- `KinematicBody.cs`：核心（Move 入口 / X-Y 分轴迭代 / RaycastNonAlloc 复用缓冲 / SetFilter 注入）
- `Assets/SO/PlayerRayConfig.asset`：玩家配置资产（mask=648 = OldDimension|Ground|NewDimension）

**验收通过**：用户手动 Play——方块撞墙停、3s 跳起顶头回落、落地、**不穿墙**。

**⚠️ 调试重要教训（面试与后续任务都用得上，详见 `Docs/architecture/01-kinematic2d.md` 硬约束）**：
1. **SyncTransforms 坑（穿墙真根因）**：修改 `transform.position` 后物理缓存 bounds 不更新，射线基于陈旧位置发出（实测偏移 0.35 单位）→ 碰撞失效。**修复**：`Move()` 开头调用 `Physics2D.SyncTransforms()`。后续任何"改位置后立即射线检测"的代码都要注意。
2. **重叠回退公式**：命中后位移 = `(distance - skinWidth) * 方向`（distance < skinWidth 时自动负位移 = 退出重叠）。**不要**写成 `penetration * 方向`（方向反，会把物体往墙里推——本任务实测踩坑）。
3. **出生点不得与地形重叠**：与天花板/墙重叠会钉住物体；出生点应贴地或留 SkinWidth 余量。

### 任务 3：维度系统（`3e605bf` + chore `04a2d08`）

**交付文件**：
- 新建 `Assets/Scripts/GameScene/SwitchWorld/WorldState.cs`：维度状态唯一持有者（SingletonMono，`IsMaskActive` + 静态事件 `OnMaskStateChanged/OnMaskPreviewChanged` 签名不变 + `SwitchWorld()/SetWorld()/SetPreview()`）
- 新建 `Assets/Scripts/GameScene/SwitchWorld/DimensionCollisionFilter.cs`：首个 ICollisionFilter 实现（无状态，读 WorldState 当前值）
- 修改 `MaskObject.cs`（加只读属性 `ShowWhenMaskActive` + 订阅迁 WorldState，显隐逻辑零改动）、`AudioManager.cs`（订阅迁移）、`PlayerController.cs`（补充 B 最小 5 处路由胶水）
- `GameScene.unity`：挂载 WorldState 对象（用户步骤 5 编辑器操作，随提交）
- `04a2d08`：清理 BeginPanel 残留 Missing Script GlobalVolume 组件（用户 Unity 会话产物）

**两处计划缺口裁定**（已写入 `task-3-brief.md` 控制者补充需求 A/B）：
- A：WorldState 增 `SetPreview(bool)` 预览广播唯一入口（静态事件外部无法 Invoke，否则虚影失效）
- B：PlayerController 最小 5 处路由胶水（切换走 `SwitchWorld()`、预览走 `SetPreview`、重生/换房走 `SetWorld(false)`）——保证"切换时 UI/音频/显隐仍响应"，未迁移的 4 个订阅方继续走 PlayerController 自身事件

**验收通过**：表世界基线、切换显隐/BGM、预览虚影、强制回表世界（死亡重生/换房）、UI 提示照常、Console 无报错。维度碰撞过滤本任务不生效（任务 4 接线），属预期。

**⚠️ 执行注意**：WorldState 是 SingletonMono **需要手动在场景挂载**（无挂载即 NRE——那是提醒信号）；PlayerController 的 `IsMaskActiveGlobally` 与静态事件在任务 4 才删除；事件签名禁止变更。

### 任务 4：玩家接入 KinematicBody（`912d704`，30 文件）

**交付**（方案 B 已由用户拍板）：
- Player 保留 Kinematic Rigidbody2D 作**事件总线**（Awake 强制 Kinematic/零重力/`useFullKinematicContacts`），位移/重力/碰撞 100% 走自研 KinematicBody；`Velocity` + `SetVelocity` + FixedUpdate 重力积分；`DimensionCollisionFilter` 注入；地面/墙检测改读 `LastResult`；删除 `IgnoreLayerCollision`/`IsMaskActiveGlobally`/静态维度事件；理智事件保留；7 状态类适配（IdleState 补 PhysicsUpdate）
- 事件迁移收尾：MaskObject/InteractiveTutorialHUD/SanityStatusHints/DimensionPortal/MaskPostProcessingManager 订阅迁 WorldState
- PlayerConfigSO 增 `gravity=9.81`/`maxFallSpeed=20`；Player.prefab 加 KinematicBody 组件（guid `1e04ee79…`，config=PlayerRayConfig `cfa678e8…`）
- 配置资产 `Assets/Resources/SO/` → `Assets/SO/` **保 GUID 搬家**（git 识别为 100% rename，引用不断）

**验收调试（MCP 8092 接入实测，3 个回归全部修复）**：
1. **穿越地面**：TotalGround 是 CompositeCollider2D（Outlines）——起点在轮廓内侧时 qStart 不生效，向下射线隧道到 y=−6 底部边框。修复：MoveVertically 脚底上探 + FixedUpdate 落地钳制 `Velocity.y=0`
2. **陷阱不致死**：陷阱 RB=Kinematic，玩家改 Kinematic 后 Kinematic↔Kinematic 默认无接触回调。修复：`RB.useFullKinematicContacts=true`
3. **异世界子弹仍命中**：移除 IgnoreLayerCollision 后 Trigger 回调失去维度门。修复：`Bullet.OnTriggerEnter2D` + 陷阱 `HandleTrapContact` 加维度门（异世界隐形物不生效）

**⚠️ 调试教训（面试可用，详见 01-kinematic2d.md 硬约束）**：Composite Collider 2D（Outlines 几何）的内侧起点不被 `queriesStartInColliders=true` 视为命中——自研射线控制器遇复合碰撞体必须做"贴地表上探"防隧道；Kinematic↔Kinematic 接触需 FullKinematicContacts。

## 四、给新对话的交接说明

> 从 **任务 5（手感回归）** 开始继续执行。以下信息必须传给新对话。

### 新对话必读文件（按顺序）
1. `Docs/superpowers/plans/2026-08-24-mask-dimensions-deep-rework.md` —— 16 任务实施计划全文
2. `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md` —— 子代理执行账本（含任务状态、调试教训、全部裁定、产物位置）
3. `Docs/architecture/01-kinematic2d.md` + `Docs/architecture/05-player-fsm.md` —— 任务 5 相关架构文档
4. `Docs/tests/01-kinematic2d-test.md`（土狼/跳缓冲/顿帧条目）+ `Docs/tests/05-player-fsm-test.md`（输入手感/时间系统回归）—— 任务 5 验收清单

### 任务 5 要点（已确认的裁定）
- **计划本体**：验证计时器（CoyoteTime/JumpBuffer）与新物理兼容（不改逻辑，数值走 PlayerConfigSO）；验证顿帧/预览兼容（KinematicBody.Move 用 `Time.deltaTime` → timeScale=0 时自然冻结 ✓ 正确行为）
- **遗留事项一并处理**：
  - 缺陷7（账本）：WorldState 是 DontDestroyOnLoad 单例，菜单→GameScene 重进可能残留上一局维度 → 玩家 Start 时 `WorldState.Instance.SetWorld(false)` 同步（初始必为表世界）
  - 审查 Minor：`Velocity.y` 落地不归零 → **已在任务 4 实装**（FixedUpdate 落地钳制）；混合时间步（Update 时序 SetVelocity 用 frame deltaTime）→ **裁定不改**（顿帧需要 deltaTime 冻结，保持现状并记录理由）；BOM 噪音、MaskSwitchState 注释括号 → 可不处理
- 若用户反馈跳跃/下落手感与改造前有感知差异：调 `PlayerConfigSO` 的 gravity/maxFallSpeed（基线 9.81/20，资产在 `Assets/SO/PlayerSO.asset`）
- 文档维护（控制器做，不进实现子代理）：05-player-fsm.md 第 34 行事件归属改为 WorldState（任务 3 后文档滞后）；01-kinematic2d.md 补"复合碰撞体内侧起点隧道"硬约束（任务 4 调试教训）

### 执行须知
- **工作流**：AI 实现 → 附验收清单 → 用户在 Unity 手动验收 → 验收通过才 commit（禁止 AI 自行提交）
- commit 用中文规范（type 英文 + scope/description 中文）
- **MCP（8092 桥本会话已实测可用）**：桥 8092 可同时连多个 Unity 实例，需 `set_active_instance` 选 `Mask Dimensions@5092658667197165`（hash 前缀 `5092` 亦可）；Play 模式 execute_code 间歇性可用；**最终验收以用户手动 Play 为准**
- 删任何脚本前，检查场景 GUID 引用（任务 1 的教训：会留 Missing Script）
- 测试脚手架（PhysTest 等）验收后删除、不提交；`.mcp-schema.json` 为 MCP 产物，不提交
- 生成审查包必须用 UTF-8 编码（任务 2 的 I3 教训：默认编码会乱码）

## 五、待办

- [x] 任务 3（维度系统，`3e605bf`）
- [x] 任务 4（玩家接入 KinematicBody，`912d704`）
- [ ] 任务 5~16 按计划执行（新对话接手）
- [ ] 每完成一任务更新本文件和 `.superpowers/sdd/.../progress.md`
- [ ] 全 16 任务完成后：最终代码审查 + finishing-a-development-branch