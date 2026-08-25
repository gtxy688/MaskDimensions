# 07 子弹系统（bullet）

> 状态：**保留不改**
> 本文件是 AI 写脚本用的功能需求规格。

## 职责

- 扫射弹幕：BulletSpawner 按房间配置间隔发射子弹。
- 策略模式弹道：直线 / 正弦 / 追踪，由 BulletStatsSO 配置驱动。
- 对象池回收：子弹生命周期结束或命中玩家后回池。

## 文件清单

| 文件 | 状态 | 职责 |
|---|---|---|
| `Bullet.cs` | 保留 | 子弹实体：驱动策略、生命周期、命中判定 |
| `BulletSpawner.cs` | 保留 | 扫射生成器：池管理、发射协程 |
| `Strategy/IBulletTrajectoryStrategy.cs` | 保留 | 弹道策略接口 |
| `Strategy/LinearStrategy.cs` | 保留 | 直线弹道 |
| `Strategy/SineWaveStrategy.cs` | 保留 | 正弦弹道 |
| `Strategy/HomingStrategy.cs` | 保留 | 追踪弹道 |
| `SO/BulletStatsSO.cs` | 保留 | 子弹属性 + 策略工厂（CreateStrategy） |

## 关键行为（写脚本时不得破坏）

- `Bullet.Fire(dir, stats, strategy?, target?)`：注入弹道策略（默认按 stats 工厂创建），激活并开始飞行。
- 生命周期超时（stats.lifetime）自动回池；命中 Player 标签 → 玩家 Die() + 回池。
- BulletSpawner：Awake 建池（prewarm 15 / cap 50）→ OnEnable 开始扫射 → OnDisable 停止。
- 每发子弹按 spawnPoints 依次发射，初始方向左（或随 spawn point 旋转）。

## 核心规则（硬约束）

1. 禁止重写现有子弹系统（已稳定且不是本项目差异化方向）。
2. 新增弹道类型：加策略类 + BulletStatsSO 枚举 + 工厂分支（三处），保持模式一致。
3. 子弹与维度系统交互（如需）：里世界子弹是否可见/有效——**遇到此需求停下询问用户**，不要自行设计（当前无此需求）。

## 依赖

- 依赖：06-pool（对象池）、SO（BulletStatsSO/RoomConfigSO）、05-player-fsm（命中玩家）。
- 被依赖：04-levels（房间激活扫射器）。

## 验收

见 `../tests/07-bullet-test.md`。
