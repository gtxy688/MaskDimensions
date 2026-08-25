# 项目进度跟踪（progress.md）

> 本文件是**项目级进度总览**（用户可见、新对话可直接读取）。
> 子代理执行的详细账本在 `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md`（隐藏目录，同为权威记录）。
> 最后更新：2026-08-25

---

## 一、当前总览

**项目**：Mask Dimensions 深度改造（秋招旗舰项目）
**目标**：自研 2D 运动学物理（Kinematic2D）+ 维度渲染差异化
**分支**：master
**执行模式**：subagent-driven-development（AI 实现 → 任务审查 → 用户在 Unity 手动验收 → 验收通过才 commit）
**当前进度**：任务 2/16 完成 ✅（任务 1 也已完成）

## 二、任务状态表

| 任务 | 内容 | 状态 | 提交 |
|---|---|---|---|
| 1 | 清理死代码 + 移除 PPv2 | ✅ 完成（已验收） | `4178633` |
| 2 | Kinematic2D 核心框架 | ✅ 完成（已验收） | `f69178c` |
| 3 | 维度系统（WorldState + 过滤器 + 事件迁移） | ⏳ **下一个** | — |
| 4 | 玩家接入 KinematicBody（移除 Rigidbody2D） | 待办 | — |
| 5 | 手感回归（土狼时间/跳缓冲/顿帧） | 待办 | — |
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

## 四、给新对话的交接说明

> 从 **任务 3（维度系统）** 开始继续执行。以下信息必须传给新对话。

### 新对话必读文件（按顺序）
1. `Docs/superpowers/plans/2026-08-24-mask-dimensions-deep-rework.md` —— 16 任务实施计划全文
2. `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md` —— 子代理执行账本（含任务状态、调试教训、产物位置）
3. `Docs/architecture/00-overview.md` → 定位模块 → `Docs/architecture/02-dimension.md`（任务 3 的架构文档）
4. `Docs/tests/02-dimension-test.md` —— 任务 3 验收清单

### 任务 3 要点（已确认的修正）
- 创建 `WorldState.cs`（SingletonMono）+ `DimensionCollisionFilter.cs`（ICollisionFilter 实现）
- 修改 `MaskObject.cs`（加只读属性 `ShowWhenMaskActive` + 订阅改 WorldState）
- 修改 `AudioManager.cs`（订阅改 WorldState）
- **场景挂载**：WorldState 是 SingletonMono **需要手动在场景放对象挂载**（简报步骤 5，编辑器操作）
- 事件签名 `OnMaskStateChanged(bool)` 等**禁止变更**（多下游依赖）

### 执行须知
- **工作流**：AI 实现 → 附验收清单 → 用户在 Unity 手动验收 → 验收通过才 commit（禁止 AI 自行提交）
- commit 用中文规范（type 英文 + scope/description 中文）
- **MCP 坑（如果要用来调试）**：8092 桥是 ARPG 项目的（连本项目不稳）；本项目自己的桥是 8091（未启动）。MCP 适合配置检查/静态查询；Play 模式实时观察不可靠，**最终验收以用户手动 Play 为准**
- 删任何脚本前，检查场景 GUID 引用（任务 1 的教训：会留 Missing Script）
- 测试脚手架（PhysTest 等）验收后删除、不提交

## 五、待办

- [ ] 任务 3~16 按计划执行（新对话接手）
- [ ] 每完成一任务更新本文件和 `.superpowers/sdd/.../progress.md`
- [ ] 全 16 任务完成后：最终代码审查 + finishing-a-development-branch