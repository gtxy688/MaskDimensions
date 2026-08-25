# 项目概述

表里世界维度切换 | 2D 平台跳跃 | 自研运动学物理 + 渲染差异化
> 引擎：Unity 2022 LTS + URP | 用途：秋招作品集

AI 辅助开发，**用户负责测试与验收（按模块）**。AI 每完成一个模块，交付「验收清单」供用户逐条验证，验收通过才算完成。

# 架构约束

1. **玩家物理走自研运动学控制器（Kinematic2D）**：禁止用 Rigidbody2D 驱动玩家。物理层只做"位移求解"，不碰输入。
2. **维度碰撞过滤走 ICollisionFilter（射线级）**：禁止 `Physics2D.IgnoreLayerCollision`。维度切换 = 换过滤器。
3. **地面检测读 CollisionResult.IsGrounded**：禁止 `Physics2D.OverlapBox` + 手工层排除。
4. **手感数值全部走 PlayerConfigSO**：土狼时间/跳缓冲/速度等不硬编码在 .cs 里。
5. **维度状态由 WorldState 持有**：禁止裸静态字段承载状态（如 `IsMaskActiveGlobally`）。静态事件可保留（单人游戏合理），订阅必须配对退订（OnEnable/OnDisable）。
6. **MaskObject 显隐逻辑保留不动**：OnEnable/OnDisable 订阅刷新 + Renderer.enabled 切换是已验证的正确方案，禁止重写。
7. **后处理统一 URP Volume**：禁止再引入 PPv2（com.unity.postprocessing 必须移除）。
8. **玩家状态机保留不重做**：字典注册表方案可用；禁止扩成 HFSM（那是只狼项目的主场，本项目差异化在物理与渲染）。
9. **对象池保留但不再作为简历吹点**：改动需有真实性能数据支撑，禁止"为写而写"的新增。

# 代码规范

## 命名
- 公开成员 PascalCase，私有 camelCase（Unity 习惯，不用 m_ 前缀）
- 新物理框架接口命名 `<能力>Filter/Body`（ICollisionFilter/KinematicBody）
- 状态类命名沿用现有风格（IdleState/MoveState/JumpState/FallState/HitState/DieState）

## 文件组织
- 物理框架：`Assets/Scripts/Kinematic2D/`
- 玩家：`Assets/Scripts/GameScene/Player/`（状态在 `State/` 子目录）
- 维度：`Assets/Scripts/GameScene/SwitchWorld/`
- 渲染：`Assets/Scripts/Rendering/`
- 子弹：`Assets/Scripts/GameScene/Bullet/`（策略在 `Strategy/` 子目录）
- 配置：`Assets/Scripts/SO/`
- 管理器：`Assets/Scripts/Mgr/`
- 池：`Assets/Scripts/Pool/`
- 命名空间不强制，保持现有风格（无命名空间）

## 数据结构
- 配置数据用 ScriptableObject（PlayerConfigSO/RoomConfigSO/BulletStatsSO），带 `[CreateAssetMenu]`
- 碰撞结果/命令数据用 struct（值类型）
- 物理类是纯 C# 类（非 MonoBehaviour），挂载点是 MonoBehaviour（KinematicBody 挂玩家 GameObject 上）

## 注释
- 中文注释，解释"为什么"而非"是什么"
- 占位资源名（如未接的动画/特效）注明「占位，XX 阶段接入」

# 工作流（AI 实现 → 用户验收）

1. 收到任务 → 按「文档加载指引」读对应架构文档 + 验收文档，只读需要的章节
2. 实现代码 → 不自己宣称"完成"
3. 在交付说明中附「验收清单」：操作步骤 + 预期结果（供用户在 Unity 中逐条验证）
4. 用户验收通过 → 提交；验收不通过 → 修正后重新交付

# 文档访问纪律（硬规则）

1. 收到任务 → 在下方加载指引表定位对应架构文档，**只读该文件**，禁止通读 `Docs/` 下所有文档
2. 禁止跨模块引用其他文档（改物理就读 01-kinematic2d，不读 04-levels）
3. **动手前先声明**：先说出"本任务要读哪份文档"，再开始读取，让用户确认找对了文档
4. 文档与代码冲突 → **停下报告，不自行选边**（如：架构文档说射线过滤而代码是 IgnoreLayerCollision）
5. 遇到缺失的文档或字段 → 停下询问用户，不臆造

# 文档加载指引

架构文档在 `Docs/architecture/`（AI 写脚本用：功能需求规格），验收清单在 `Docs/tests/`（用户验收用），技术选型论证在 `Docs/decisions.md`（用户查阅，AI 不当规范用）。AI 只读与本任务相关的文件。

| 任务类型 | 读这个 |
|---------|--------|
| 物理/移动/碰撞/斜坡/单向板/移动平台 | `Docs/architecture/01-kinematic2d.md` |
| 维度切换/面具/理智/WorldState/MaskObject | `Docs/architecture/02-dimension.md` |
| 后处理/2D 光照/转场 shader/Renderer Feature | `Docs/architecture/03-rendering.md` |
| 关卡/房间/传送/数据配置 | `Docs/architecture/04-levels.md` |
| 玩家状态/手感参数/PlayerController | `Docs/architecture/05-player-fsm.md` |
| 对象池/回收/IPoolable | `Docs/architecture/06-pool.md` |
| 子弹/弹幕/策略模式 | `Docs/architecture/07-bullet.md` |
| UI/音频/Tips 提示 | `Docs/architecture/08-presentation.md` |
| Singleton/Manager 基类 | `Docs/architecture/09-base-framework.md` |
| 项目总览/模块依赖/索引 | `Docs/architecture/00-overview.md` |
| 用户决策记录 | `Docs/total.md`（用户视角，AI 不当规范用） |

> 总原则：先读 `Docs/architecture/00-overview.md` 定位模块，再读对应架构文档，最后对照 `Docs/tests/XX-xxx-test.md` 验收。

# 测试要求

本项目不做自动化测试。验证方式：AI 交付时附「验收清单」（`Docs/tests/XX-xxx-test.md`），用户在 Unity 中手动逐条验收。
