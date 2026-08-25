# 04 关卡系统（levels）

> 状态：**重做**（3 关全推倒，Room 框架保留升级）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D9）。

## 职责

- 提供 3 个围绕新物理/维度/视觉设计的关卡，每个机制有"只能靠它才能过"的瞬间。
- 房间流程（进入/退出/相机切换/传送）沿用现有 Room 框架。

## 现有框架（保留部分）

| 文件 | 状态 | 职责 |
|---|---|---|
| `RoomTrigger.cs` | 保留 | 房间入口触发器，玩家进入 → RoomManager.EnterRoom |
| `RoomExit.cs` | 保留 | 通关出口（进下一房间） |
| `TeleportTrigger.cs` | 保留 | 传送触发（死亡/重生动画） |
| `EndGameTrigger.cs` | 保留 | 通关触发 |
| `RoomManager.cs` | 保留 | 单例：房间切换、相机、激活/停用子物体、广播 OnRoomEntered |
| `RoomConfigSO.cs` | **扩展** | 房间参数（见下） |

## 关卡设计（重做目标）

| 关卡 | 主题 | 教学 | 必须秀的技术点 |
|---|---|---|---|
| L1「表里初现」 | 维度切换教学 | 里世界才有的桥 / 表世界才有的墙 | 维度切换 + 切换视觉冲击 |
| L2「斜坡法则」 | 斜坡 + 动量 | 长斜坡冲刺、下坡加速跳远、单向板上下穿 | 斜坡换算、单向板、动量继承 |
| L3「断章」 | 综合 + 理智压力 | 里世界限时通道、连续切换解谜 | 全部能力 + 理智压迫 |

每关结构：教学段（安全试错）→ 应用段（机制组合）→ 挑战段（限时/密集）。

## RoomConfigSO 扩展

现有字段：roomName / description / bulletStats / fireInterval / bgmOverride。
扩展新增（写脚本时保持现有字段兼容）：
- `dimensionRequirement`：该房间是否强制表/里世界（进入时若世界不符则自动切换或禁止进入）
- `sanityDrainMultiplier`：理智消耗倍率（L3 压力用）

## 核心规则（硬约束）

1. 关卡必须满足"机制必过"：每个机制至少一处不用它过不去（验收会逐条检查）。
2. 旧 3 关场景内容作废重建；Room 框架代码保留。
3. 禁止新增 Boss 战 / 收集品 / 成就（YAGNI，范围外）。
4. 房间切换流程不得破坏（ResetForNewRoom 行为：重生点、状态重置、理智回满、摘面具）。
5. 数据驱动：新房间参数进 RoomConfigSO，禁止硬编码在场景脚本里。

## 依赖

- 依赖：05-player-fsm（ResetForNewRoom）、02-dimension（世界状态）、01-kinematic2d（物理能力）、07-bullet（弹幕关）。
- 被依赖：无（顶层玩法组织者）。

## 验收

见 `../tests/04-levels-test.md`。
