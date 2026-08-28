# Mask Dimensions — 表里世界维度切换平台跳跃

> 秋招作品集项目 | Unity 2022 LTS + URP | 核心卖点：**自研 2D 运动学物理** + **维度渲染差异化**

## 30 秒玩法介绍

你戴上一张面具，就能从"表世界"潜入"里世界"：表世界明亮安全，里世界压抑危险但藏着另一套地形——表世界的断桥，在里世界是通路；里世界的尖刺，对表世界的你视而不见。

代价是**理智**：停留在里世界会持续掉理智，耗尽会被强制弹回表世界。因此核心循环是——长按 J 预览里世界地形 → 时机成熟瞬间切换（顿帧 + 全屏撕裂转场）→ 在理智耗尽前穿过去 → 回表世界等理智恢复。三个关卡分别围绕**维度教学**（L1 表里初现）、**斜坡与动量**（L2 斜坡法则）、**综合压力**（L3 断章：进房即里世界的限时通道）设计，每个机制都有"不用它就过不去"的瞬间。

## 技术亮点

### 1. 自研 2D 运动学物理（Kinematic2D）

玩家位移不经过 Rigidbody2D——`KinematicBody` 用射线扫描（X/Y 分轴迭代 + `RaycastNonAlloc` 缓冲复用）自己解算碰撞，Rigidbody2D 仅作 Unity 回调的事件总线。在这个地基上实现了：

- **斜坡三步法**：水平射线遇坡面放行 → 爬坡预抬 `tan(θ)·|Δx|` 防嵌坡 → 垂直射线贴坡修正（>60° 按墙处理）；
- **单向板**：向上命中 `OneWayPlatform` 标签一律放行（射线会先后命中底面与顶面，只放行底面会被顶面二次挡住），向下命中正常站立；
- **移动平台跟随**：玩家侧缓存平台位移差，免疫 `FixedUpdate` 执行顺序，重新接住瞬间不叠加（防瞬移）；
- **手感系统**：土狼时间 / 跳缓冲 / 顿帧切换（`timeScale=0` 期间动量完美继承）全部数值走 `PlayerConfigSO`。

### 2. 维度系统（表/里世界）

- 维度状态唯一持有者 `WorldState`（单例 + 静态事件广播），切换 = `SwitchWorld()`，死亡重生/换房 = `SetWorld(false)`；
- **碰撞过滤走射线级 `ICollisionFilter`**（禁用 `Physics2D.IgnoreLayerCollision` 全局开关）：每条射线命中后询问过滤器"该物体在当前维度是否可见"，不可见即忽略。维度切换 = 换过滤器，物理层本身不知道"维度"概念；
- `MaskObject` 驱动双世界物体的显隐（Tilemap / SpriteRenderer 双支持），长按 J 有 GPU Stencil 圆形透视窗预览里世界。

### 3. 渲染三线（URP）

- **双 Volume 维度过渡**：表世界冷色清朗 / 里世界灰暗噪点，切换时 weight 交叉淡化 0.4s（`unscaledDeltaTime`，顿帧期间继续），外加 0.2s 故障闪（ChromaticAberration 尖峰）；
- **URP 2D 光照双世界**：双 Global Light 交叉 + 玩家运行时持灯（里世界开启暖色点光），全场景 Sprite-Lit / Tilemap-Lit 材质；
- **自写转场 Renderer Feature**：`ScriptableRenderPass`（BeforeRenderingPostProcessing）+ 手写 HLSL——扫屏（光边推进）与撕裂（行块错位）双模式，手动全屏三角形链（`SV_VertexID` 合成、显式 SetViewport、同格式组 `CopyTexture`）。

## 架构

模块地图、依赖关系与关键事件流见 [`Docs/architecture/00-overview.md`](Docs/architecture/00-overview.md)；技术选型论证（12 条 X/Y/Z 决策）见 [`Docs/decisions.md`](Docs/decisions.md)；各模块的"AI 写脚本用"功能规格在 `Docs/architecture/`，人工验收清单在 `Docs/tests/`。

```
玩家按 J → 顿帧(timeScale=0) → WorldState.SwitchWorld()
  ├→ ICollisionFilter 换维度（射线级，物理层无感）
  ├→ MaskObject 显隐刷新（Tilemap/SpriteRenderer）
  ├→ 双 Volume weight 交叉 + 2D Light 强度交叉（unscaledDeltaTime）
  └→ Renderer Feature 全屏转场（撕裂/扫屏）
→ 恢复时间，动量继承
```

## 性能数据

> 采集方式：Window → Analysis → Profiler，Play 进入 GameScene（L2_Challenge 弹幕段最佳），Deep Profile 关闭、勾选 Profile，各记录 30s。

| 指标 | 数值 | 采集点 |
|---|---|---|
| 弹幕段 GC Alloc（对象池预热后） | 待实测 | Profiler Memory，30s 弹幕持续 |
| 维度切换瞬时帧耗时 | 待实测 | Profiler CPU，切换瞬间那一帧 |
| KinematicBody 每帧物理开销 | 待实测 | Profiler CPU，`KinematicBody.Move`（4+4 射线 NonAlloc，缓冲零 GC） |
| 对象池 `Get/Return` 单次开销 | 待实测 | Deep Profile 对比 Instantiate 路径 |

分析预期（面试口径，待实测背书）：对象池预热后弹幕路径零 `Instantiate/Destroy`（GC Alloc 应趋近 0）；物理每帧固定 8 次非分配射线查询，无引擎接触求解开销。

## 如何运行

1. Unity Hub 打开项目（**Unity 2022 LTS**，URP 已配置 2D Renderer）；
2. 打开 `Assets/Scenes/GameScene.unity`（完整 3 关流程）或 `BeginScene.unity`（从主菜单进入）；
3. Play。操作：**A/D** 移动，**Space** 跳跃，**J** 点按切换世界 / 长按预览里世界，**ESC** 设置菜单。

## 关卡结构（任务 12-15 重建）

| 房间 | 主题 | 机制必过点 |
|---|---|---|
| L1 表里初现（教学/应用/挑战） | 维度教学 | 表断桥只有里世界有桥；表墙里路交替；尖刺场上三岛连续切换 |
| L2 斜坡法则（教学/应用/挑战） | 斜坡+动量 | 唯一上山路是 45° 坡；坡末起跳跨沟（台顶起跳够不着）；坡末切里保动量落里世界平台；单向板顶穿 |
| L3 断章（教学/应用/挑战） | 综合+理智压力 | 表→里→表连续切换解谜；**进房即里世界**的限时通道（理智 2.5 倍预算 ≈2s，耗尽强制弹回=落尖刺重来） |

关卡由编辑器工具 `Assets/Editor/Tools/LevelRebuildTool.cs` 数据驱动生成（菜单 Tools → Level Rebuild）：矩形填充 + 斜坡线段 + 特殊物列表描述 9 个房间，可整体重建、可审查、可复现。

## 链接

- 试玩视频 / itch.io：待补充
