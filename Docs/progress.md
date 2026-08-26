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
**当前进度**：任务 5/16 完成 ✅（任务 1~4 也已完成）

## 二、任务状态表

| 任务 | 内容 | 状态 | 提交 |
|---|---|---|---|
| 1 | 清理死代码 + 移除 PPv2 | ✅ 完成（已验收） | `4178633` |
| 2 | Kinematic2D 核心框架 | ✅ 完成（已验收） | `f69178c` |
| 3 | 维度系统（WorldState + 过滤器 + 事件迁移） | ✅ 完成（已验收） | `3e605bf`（+chore `04a2d08`） |
| 4 | 玩家接入 KinematicBody（移除 Rigidbody2D 驱动） | ✅ 完成（已验收） | `912d704` |
| 5 | 手感回归（土狼时间/跳缓冲/顿帧） | ✅ 完成（已验收） | `6f0cff3` |
| 6 | 斜坡（SlopeResolver） | ⏳ **下一个（晚上做）** | — |
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

### 任务 5：手感回归（提交见下节，代码 1 处 + 文档维护）

**交付**：
- `PlayerController.cs` Start 末尾 +4 行：`WorldState.Instance.SetWorld(false);`（缺陷 7 修复：DontDestroyOnLoad 单例跨局残留 → 每局必从表世界开始；首次等值静默零广播）
- 计时器（土狼/跳缓冲）、顿帧、预览**零改动**（字节级验证）；混合时间步裁定**不改**（顿帧冻结依赖 Move 的 Time.deltaTime）
- 文档维护：`01-kinematic2d.md` 补硬约束 #9（复合体内侧隧道 + 上探防护）；`05-player-fsm.md` 事件归属更新为 WorldState + 方案 B 注记

**验收通过**：土狼/跳缓冲/顿帧兼容、手感与改造前一致（数值基线 gravity 9.81 / maxFall 20 未调）、菜单重进必为表世界、无回归。

**⚠️ 顿帧语义订正（审查者核实）**：timeScale=0 时 Unity 固定步停摆（FixedUpdate 不运行），顿帧期间 Velocity/LastResult 原样保留——"动量完美继承"字面成立（子代理报告曾误述"FixedUpdate 仍运行累积 1.5m/s"，已驳回）。

## 四、给新对话的交接说明

> 从 **任务 6（斜坡 SlopeResolver）** 开始继续执行（用户计划晚上做）。以下信息必须传给新对话。

### 新对话必读文件（按顺序）
1. `Docs/superpowers/plans/2026-08-24-mask-dimensions-deep-rework.md` —— 16 任务实施计划全文（任务 6 段：步骤 1 SlopeResolver.cs 完整代码、步骤 2 KinematicBody 接入、验收引用）
2. `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md` —— 子代理执行账本（任务状态、调试教训、全部裁定、产物位置）
3. `Docs/architecture/01-kinematic2d.md` —— 任务 6 架构文档（重点：增量 SlopeResolver、8 条硬约束 + #9 复合体防护）
4. `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/task-6-brief.md` —— 任务 6 简报（需先读再按需补控制者裁定）
5. `Docs/tests/01-kinematic2d-test.md` 「增量模块」斜坡/下坡条目 + `Docs/architecture/06-pool.md` 不需要 —— 任务 6 验收清单

### 任务 6 要点（预判裁定，供参考）
- **计划有 SlopeResolver 完整代码**（步骤 1 逐字实现）；KinematicBody 接入见步骤 2（爬坡取 hit.normal 角度 + `ResolveClimb`；下坡在 `moveAmount.y<0` 时向下长射线）。
- **⚠️ 前序遗留风险（任务 2 审查 M2，必须处理）**：KinematicBody 水平/垂直射线命中后**不 break（last-wins）**——平墙/平地无影响，但**斜面会取后命中覆盖前命中**（非最近者优先），与斜坡解析冲突。任务 6 简报需裁定：改为"最近有效命中优先"（best-distance，与 NonAlloc 现有逻辑可统一）后再叠加斜坡解析，或斜坡专用分支。
- **复合体上探防护已实装（MoveVertically）**：斜坡任务改动时不得破坏该分支；注意"脚底上探命中 → 判贴地"与斜坡贴面（下坡）的交互——下坡时上探可能命中坡面，需验证不误判。
- 无视差风险：`PlayerRayConfig` 掩码 648（Ground+双维度层）已覆盖斜坡瓦片；斜坡瓦片若用 MaskObject 需 `ShowWhenMaskActive` 匹配当前世界（过滤器已内置）。
- 若关卡暂无斜坡几何（L2 才重做），验收可用临时测试场景/临时方块验证（同任务 2 模式，脚手架不提交）。

### 执行须知
- **工作流**：AI 实现 → 附验收清单 → 用户在 Unity 手动验收 → 验收通过才 commit（禁止 AI 自行提交）
- commit 用中文规范（type 英文 + scope/description 中文）
- **MCP（8092 桥已实测可用）**：`set_active_instance` 选 `Mask Dimensions@5092658667197165`（hash 前缀 `5092`）；Play 模式 execute_code 间歇性可用；**最终验收以用户手动 Play 为准**
- 删任何脚本前，检查场景 GUID 引用（任务 1 的教训：会留 Missing Script）
- 测试脚手架（PhysTest 等）验收后删除、不提交；`.mcp-schema.json` 为 MCP 产物，不提交
- 生成审查包必须用 UTF-8 编码（任务 2 的 I3 教训：默认编码会乱码）

## 五、待办

- [x] 任务 3（维度系统，`3e605bf`）
- [x] 任务 4（玩家接入 KinematicBody，`912d704`）
- [x] 任务 5（手感回归，任务 5 提交）
- [ ] 任务 6~16 按计划执行（新对话接手，任务 6 晚上做）
- [ ] 每完成一任务更新本文件和 `.superpowers/sdd/.../progress.md`
- [ ] 全 16 任务完成后：最终代码审查 + finishing-a-development-branch