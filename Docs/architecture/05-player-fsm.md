# 05 玩家状态机与控制器（player-fsm）

> 状态：**保留 + 改造接入**（状态机保留，底层物理接入 Kinematic2D）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D12）。

## 职责

- PlayerController：输入轮询、状态机驱动、面具（维度）逻辑、死亡/重生/传送。
- 状态机：Idle/Move/Jump/Fall/Hit/Die 状态生命周期管理。

## 文件清单

| 文件 | 状态 | 职责 |
|---|---|---|
| `PlayerController.cs` | **改造** | 输入 + 状态机 + 面具逻辑（底层换 Kinematic2D） |
| `PlayerStateMachine.cs` | 保留 | 状态机核心：Initialize/ChangeState |
| `State/BaseState.cs` | 保留 | 状态基类：Enter/Exit/LogicUpdate/PhysicsUpdate |
| `State/Idle/Move/Jump/Fall/Hit/DieState.cs` | 保留（改接入点） | 各状态逻辑 |
| `State/MaskSwitchState.cs` | 保留 | 切换状态（如需） |
| `PlayerConfigSO.cs` | 保留 | 手感参数（土狼时间/跳缓冲/理智/预览） |

## PlayerController 改造点（硬约束）

1. **移除 Rigidbody2D 驱动**：删除 RB.velocity 赋值，改为调用 `KinematicBody.Move(velocity)`。RequireComponent(Rigidbody2D) 移除。
2. **地面检测**：删 `Physics2D.OverlapBox` 地面检测与"双重世界排除"，读 `KinematicBody.LastResult.IsGrounded`。
3. **维度状态**：`isMaskActive` / `IsMaskActiveGlobally` 静态字段 → 读 WorldState。
4. **维度切换协程保留**：顿帧（timeScale=0 + WaitForSecondsRealtime）逻辑不变；事件广播不变。
5. **手感计时器保留**：CoyoteTime/JumpBuffer 更新逻辑不变（数值来自 PlayerConfigSO）。
6. 状态类对物理的访问改走 KinematicBody（如 JumpState 设置初速度、FallState 读取下落速度）。

## 保留不动（硬约束）

- 状态机生命周期方法（Enter/Exit/LogicUpdate/PhysicsUpdate）签名。
- 事件：OnMaskStateChanged / OnMaskPreviewChanged / OnSanityChanged / OnInsufficientSanity / OnSanityForcedRecovery / OnPlayerDied（签名禁止变更）。
- 死亡/重生/传送协程流程（DieAndRespawnRoutine / TeleportRoutine / ResetForNewRoom）。
- 状态类命名与切换白名单（CanSwitchMask）。
- 禁止把状态机扩成 HFSM（只狼项目主场）。

## 依赖

- 依赖：01-kinematic2d（物理）、02-dimension（WorldState/过滤器）、PlayerConfigSO。
- 被依赖：02-dimension（事件源头）、08-presentation（Audio 订阅其事件）、04-levels（ResetForNewRoom）。

## 验收

见 `../tests/05-player-fsm-test.md`。
