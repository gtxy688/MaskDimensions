# 00 项目总览（overview）

> 本文件是项目地图：先读它定位模块，再读对应架构文档。

## 项目是什么

2D 平台跳跃游戏，核心机制 = 表/里世界（面具）维度切换。玩家在表世界（安全、明亮）与里世界（危险、压抑、限时）之间切换前进，附带理智资源限制与弹幕关卡。

用途：秋招作品集。差异化卖点：**自研 2D 运动学物理** + **维度渲染差异化**（与作者的只狼复刻项目错位互补）。

## 技术栈

- Unity 2022 LTS + URP（已启用）
- 玩家物理：自研 Kinematic2D（**改造中**，替换 Rigidbody2D）
- 维度碰撞：ICollisionFilter 射线级过滤（**改造中**，替换 IgnoreLayerCollision）
- 渲染：URP Volume 后处理 + 2D Light + Renderer Feature（**改造中**，移除 PPv2）
- 数据驱动：ScriptableObject（PlayerConfigSO / RoomConfigSO / BulletStatsSO）
- 其他：泛型对象池、玩家状态机（字典注册表）、策略模式（子弹弹道）

## 模块地图与依赖

```
09-base-framework（Singleton/SingletonMono 基类）
   └── 被 08-presentation 的 Mgr 使用（UIManager/AudioManager/RoomManager）
08-presentation（UI / 音频 / Tips 提示）
   ├── UI 面板：Begin/Game/Setting/End
   ├── AudioManager：双 BGM 随维度切换 + SFX + 音量持久化
   └── Tips：理智提示 / 教程 HUD / Toast
07-bullet（子弹 + 策略模式 + 对象池回收）
   └── 依赖 06-pool、SO
06-pool（ObjectPool<T> / IPoolable）
   └── 被 07-bullet 使用
05-player-fsm（PlayerController + 状态机）
   ├── 依赖 01-kinematic2d（改造后：物理由 KinematicBody 提供）
   ├── 依赖 02-dimension（世界状态/维度切换）
   └── 依赖 PlayerConfigSO
04-levels（RoomTrigger/RoomExit/RoomManager，关卡流程）
   ├── 依赖 05-player-fsm（ResetForNewRoom）
   └── 依赖 RoomConfigSO
03-rendering（Volume 后处理 / 2D Light / Renderer Feature）
   └── 依赖 02-dimension（订阅维度切换事件）
02-dimension（WorldState / MaskObject / ICollisionFilter）
   ├── 被 05 使用（维度切换入口）
   └── 订阅关系：MaskObject/渲染/音频都订阅维度事件
01-kinematic2d（KinematicBody 自研物理）
   └── 被 05 使用；过滤器由 02 注入
```

## 关键事件流（维度切换瞬间）

玩家按 J → PlayerController 状态机 → 顿帧（timeScale=0）→ WorldState 维度翻转 → 注入新过滤器 → MaskObject 显隐刷新 → Volume weight 过渡（unscaledDeltaTime）→ 转场 Feature 播放 → 恢复时间。

## 文档索引

| 文档 | 内容 |
|---|---|
| `01-kinematic2d.md` | 自研运动学物理框架（新） |
| `02-dimension.md` | 维度系统：WorldState / MaskObject / ICollisionFilter（新+旧） |
| `03-rendering.md` | 渲染三线：Volume / 2D Light / Renderer Feature（新） |
| `04-levels.md` | 关卡重做 + Room 系统（重做） |
| `05-player-fsm.md` | 玩家状态机 + PlayerController（旧·保留） |
| `06-pool.md` | 对象池（旧·保留） |
| `07-bullet.md` | 子弹系统（旧·保留） |
| `08-presentation.md` | UI / 音频 / Tips（旧·保留） |
| `09-base-framework.md` | Singleton 基类（旧·保留） |
| `10-legacy-physics.md` | 已删除的旧物理控制器归档 |
| `../decisions.md` | 技术选型论证（用户查阅） |
| `../total.md` | 用户决策记录（用户视角） |
| `../tests/*.md` | 各模块验收清单 |

## 硬约束速查（详细见 AGENTS.md）

1. 禁止 Rigidbody2D 驱动玩家；禁止 IgnoreLayerCollision；禁止 OverlapBox 地面检测
2. 手感数值全走 SO；维度状态走 WorldState；静态事件必须配对退订
3. MaskObject 显隐逻辑禁止重写；状态机禁止扩成 HFSM；后处理禁止引入 PPv2
