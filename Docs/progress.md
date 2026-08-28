# 项目进度跟踪（progress.md）

> 本文件是**项目级进度总览**（用户可见、新对话可直接读取）。
> 子代理执行的详细账本在 `.superpowers/sdd/2026-08-24-mask-dimensions-deep-rework/progress.md`（隐藏目录，同为权威记录）。
> 最后更新：2026-08-28

---

## 一、当前总览

**项目**：Mask Dimensions 深度改造（秋招旗舰项目）
**目标**：自研 2D 运动学物理（Kinematic2D）+ 维度渲染差异化
**分支**：master
**执行模式**：subagent-driven-development（AI 实现 → 任务审查 → 用户在 Unity 手动验收 → 验收通过才 commit）
**当前进度**：任务 1~15 全部实现完成；任务 16 完成文档部分（README/面试提纲/total.md），性能数据待用户 Play 实测。**任务 12~16 整体待用户 Unity 验收**（本次未提交）。

## 二、任务状态表

| 任务 | 内容 | 状态 | 提交 |
|---|---|---|---|
| 1 | 清理死代码 + 移除 PPv2 | ✅ 完成（已验收） | `4178633` |
| 2 | Kinematic2D 核心框架 | ✅ 完成（已验收） | `f69178c` |
| 3 | 维度系统（WorldState + 过滤器 + 事件迁移） | ✅ 完成（已验收） | `3e605bf`（+chore `04a2d08`） |
| 4 | 玩家接入 KinematicBody（移除 Rigidbody2D 驱动） | ✅ 完成（已验收） | `912d704` |
| 5 | 手感回归（土狼时间/跳缓冲/顿帧） | ✅ 完成（已验收） | `6f0cff3` |
| 6 | 斜坡（SlopeResolver） | ✅ 完成（已验收） | `4596477` |
| 7 | 单向板 | ✅ 完成（已验收） | `4596477` |
| 8 | 移动平台 | ✅ 完成（已验收） | `4596477` |
| 9 | 渲染线1：双 Volume 维度过渡 | ✅ 完成（自主验收通过） | `bcf3b40` |
| 10 | 渲染线2：URP 2D Light | ✅ 完成（已验收） | `305ea22` |
| 11 | 渲染线3：自写 Renderer Feature 转场 shader | ✅ 完成（已验收） | `c75c96c` |
| 12 | RoomConfigSO 扩展 | 🟡 已实现（待验收） | — |
| 13 | 关卡 L1 重做 | 🟡 已实现（待验收） | — |
| 14 | 关卡 L2 重做 | 🟡 已实现（待验收） | — |
| 15 | 关卡 L3 重做 | 🟡 已实现（待验收） | — |
| 16 | 性能数据 + README + 面试提纲 | 🟡 文档完成；性能数据待实测（见 README 采集指南） | — |

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

### 任务 6-8：物理增量——斜坡 / 单向板 / 移动平台（已提交 `4596477`）

**交付**（`Assets/Scripts/Kinematic2D/`）：
- `SlopeResolver.cs`（新）：静态坡面判定工具——`GetSlopeAngle`（法线 atn2 换算）/`IsSlope`（法线朝上且 < maxSlopeAngle，>60° 按墙）/`GetSlopeNormalX`（坡向，判爬坡/下坡方向）
- `RayConfig.cs`：+`maxSlopeAngle = 60`（01-kinematic2d 边界：>60° 按墙）
- `KinematicBody.cs` 斜坡集成（**分轴迭代内的斜坡三步法**，面试可讲）：
  1. **水平放行**：水平射线命中坡面不计为墙（否则卡坡），只记录坡信息
  2. **爬坡预抬**：`moveAmount.y += tan(坡角)×|moveAmount.x|`——不预抬时大步长会把底角推进坡面下方（嵌坡 → Y 下探打不到坡 → 卡进坡体）；仅爬坡方向（法线×水平位移同号）预抬，下坡由重力+贴坡天然顺滑
  3. **垂直贴坡**：Y 迭代命中坡 → 正常位移修正贴坡；**向上命中坡不置 HitCeiling**（爬坡预抬后向上射线打坡是"沿坡上行"的贴坡，不是撞顶）
  - `OnSlope / SlopeAngle` 上报 LastResult（字段原占位，本任务实装）
- 单向板（任务 7）：MoveVertically 向上命中标签 `OneWayPlatform` 的 collider 一律放行（**为什么全放行**：向上射线先后命中底面+顶面，只放行底面会被顶面再次挡住无法穿越）；向下命中顶面走正常修正 = 踩板；标签已注册 ProjectSettings/TagManager
- `MovingPlatform.cs`（新，任务 8）：pingpong 往返移动组件（轴/距离/速度序列化）
- `KinematicBody` 平台跟随（**玩家侧缓存位移差**，面试可讲）：Move 尾部把"平台自上次物理帧以来位移差"叠加进 moveAmount——缓存放在玩家侧而非平台侧，**免疫 FixedUpdate 执行顺序**（平台组件先/后更新 lastPos 都正确）；重新接住瞬间不叠加（防脱离后瞬移大跳）；`OnMovingPlatform` 上报

**验收（用户手动 Play，色块测试区）**：30° 绿坡爬坡/下坡顺滑不啃坡、85° 红陡坡按墙挡、黄单向板下穿（跳起穿过不撞头）/上踩（下落站稳）、蓝移动平台跳上站定跟随移动/跳下自由落体——全部通过；测试区为临时色块（验收后已删除，正式关卡在任务 12-15 用 Tilemap 重做）。

### 任务 9：渲染线1——双 Volume 维度过渡（已提交 `bcf3b40`）

**交付**：
- 新建 `Assets/Scripts/Rendering/DimensionVolumeController.cs`（计划逐字：双 Volume weight 交叉 0.4s、`Time.unscaledDeltaTime`、OnEnable/OnDisable 成对订阅、StopCoroutine 防叠加、尾权重硬置 0/1）
- 新建 `Assets/SO/Profiles/RealWorldProfile.asset`（ColorAdjustments 0/0 + Vignette 0.2）与 `MaskWorldProfile.asset`（ColorAdjustments -40/-0.5 + FilmGrain 0.6）——**所有参数 `overrideState=true`（关键！见下方教训）**
- `GameScene.unity`：RealWorldVolume(w=1) + MaskWorldVolume(w=0)（isGlobal、layer 0）+ DimensionVolumeController 接线；**移除旧 MaskPostProcessingManager 组件与旧 Volume**
- `TestGameScene.unity`：移除 MPP 组件
- **删除** `MaskPostProcessingManager.cs`（guid `ae948431…` 已无场景引用，删除前已核查）

**验收方式**：用户委托 AI 自主验收（MCP 进 Play + 手动渲染截图像素对比，全流程无需用户操作）。

**⚠️ 调试教训（本项目最隐蔽的坑，面试级）——"配置全对却完全没效果"的三层根因**：
1. **`VolumeParameter.overrideState` 未开（主因）**：Volume 系统合成时只采用 `overrideState=true` 的参数！`VolumeProfile.Add<T>()` 创建组件时所有参数默认**关闭**覆盖——只设 `.value` 无效。症状：Profile 资产 YAML 里值正确（-40/0.6）、组件非空、Volume 权重正确，但 `VolumeManager.stack` 合成结果永远是默认值 → 画面零变化。修复：`component.SetAllOverridesTo(true)` + SetDirty + SaveAssets。
2. **Play 模式中改资产不生效**：运行时 `Volume.profile` 是资产实例化副本（InstanceID 与 `LoadAssetAtPath` 不同）——改资产必须**退出 Play 重进**才被副本克隆。调试时在 Play 中反复改资产必然无效。
3. **验证维度要对**：磁盘重载"验证全绿"（组件数、value 正确）仍可能无效——必须验证 `overrideState`。这也是"修了两轮都无效"的根源。

**自主验收证据**（MCP 采样 + 截图像素统计）：
- 表世界：RealWorld w=1 / MaskWorld w=0，stack 合入 vig=0.2 ✓
- 同内容对照截图：无滤镜 avgSat=0.535 → 里世界滤镜 avgSat=0.370（饱和 -31%、整体变暗）→ 滤镜真实进渲染 ✓
- 过渡中态（weight 0.5/0.5）avgSat=0.493 平滑介于两者之间 → 任意中间帧渲染正确 ✓
- 顿帧兼容：timeScale=0 下协程用 unscaledDeltaTime 首帧推进 w 0→0.041 + 插值数学必然完成（真实帧循环）✓
- 表/里权重切换、事件→控制器→weight 全链路采样验证 ✓

**视觉差异化补强（2026-08-27，用户指出"看不出不同"后一次性补齐）**：
- 表世界冷色清朗：ColorFilter 偏蓝 (0.90/0.98/1.10) + 饱和 +5 + 对比度 +8 + Vignette 0.25
- 里世界灰暗压抑：饱和 **-80** + 曝光 -0.8 + 对比度 +10 + FilmGrain **1.0** + **ChromaticAberration 0.3 常驻紫边** + Vignette 0.45
- 切换故障：新增 `GlitchProfile.asset`（ChromaticAberration 0.9）+ 场景 GlitchVolume + 控制器 `glitchVolume` 驱动（切换瞬间拉满 → 0.2s 消退，unscaledDeltaTime）
- **编辑模式三态截图像素验证**：表 avgSat=0.631（B 通道主导 0.518=冷蓝）vs 里 avgSat=0.182（饱和 -71%）vs 故障帧中间态——辨识度远超旧参数（旧仅 -31%）；视觉参数资产级，Play 克隆一致

### 任务 12-15：RoomConfigSO 扩展 + 三关重做（2026-08-28 实现完成，待用户验收）

**任务 12 交付**（代码 3 文件）：
- `RoomConfigSO.cs`：+`dimensionRequirement`（0/1/2）+`sanityDrainMultiplier`（默认 1）
- `RoomManager.cs`：+`CurrentSanityMultiplier` 属性（无房间兜底 1）；`ActivateRoom` 在 ResetForNewRoom 后调 `pc.EnforceRoomDimension(cfg.dimensionRequirement)`
- `PlayerController.cs`：理智流逝乘房间倍率；+`EnforceRoomDimension(int)`（要求里世界则进房即戴面具，faceMask 视觉同步在玩家侧——自动切换不走手动协程）

**任务 13-15 交付**（关卡由编辑器工具数据驱动生成）：
- `Assets/Editor/Tools/LevelRebuildTool.cs`（新）：9 房间规格（矩形填充 + 45° 斜坡线段 + 移动平台/弹幕列表）→ 一键重建 + 灯光接线 + 存场景；菜单 Tools → Level Rebuild
- `GameScene.unity`：旧三关（Room_1/2/3 prefab 实例）删除，新 9 房间链 L1_Teach→…→L3_Challenge(EndGame)；每房 = RoomTrigger(入口)+PlayerSpawn+RoomVcam(静态机位 y=-1.5, ortho 9.8)+RoomExit/EndGame+Grid 六张维度 Tilemap（Ground/OldGround/NewGround/OldTrap/NewTrap/OneWay，层 7/3/9，Lit 材质，Composite Polygons）
- `Assets/SO/Rooms/Room_L1_Teach … Room_L3_Challenge.asset`（9 个新 RoomConfigSO）：L1C 理智 1.5×；L2C 2×+弹幕 0.7s；L3A 2×；L3C **dimensionRequirement=2** + 2.5× + 弹幕 0.5s
- 斜坡定式：程序生成楔形 tile（Gen/SlopeUp/Down，colliderType=None）只管视觉 + `PolygonCollider2D` 三角形作物理真值（贴图集无 45° 砖；方形砖阶梯会被射线当墙）
- 旧资产清理：3 个旧房间 prefab + 3 个旧 Room SO 删除；`PhysTest_Task2.unity`（任务 2 测试脚手架遗留）删除

**机制必过点位（验收对表用）**：
- L1：断桥只里世界有桥（Teach 安全版/Apply 尖刺版）；表墙切里穿；表尖刺带里世界通行；挑战段尖刺场上 表岛→里岛→表岛 两次空中切换
- L2：Teach 唯一上山路=45°坡 + 移动平台跨 9 格沟；Apply **台顶起跳 +4.2 < +5 必死、坡末起跳 +6.2 过**（下坡动量机制必过）+ 单向板坑道顶穿越墙；Challenge 坡末切里保动量落里世界平台（落点只有里世界有）+ 弹幕
- L3：Teach 坡+单向板+短里桥热身；Apply 表台→(切里)里岛→(切表)表岛 三连解谜（坑内尖刺随世界换向）；Challenge **进房即里世界**（dimensionRequirement=2），限时通道预算 ≈2.0s（冲刺 0.3s+通道 1.4s，贴线可过），耗尽强制弹回=落表尖刺死亡重来，弹幕 0.5s，EndGame 收尾

**⚠️ 本轮发现并修复的问题**：
1. **任务 10 灯光接线丢失回归**：磁盘 GameScene 从未有双 GlobalLight/DimensionLightController（当时用户 Play 验收过但场景未落盘）→ 本轮 EnsureLights 补回（GlobalLight_Real 1.0 / GlobalLight_Mask 0.12 + 控制器接线）。双 Global Light 并存的 console 警告为良性（运行时亮光强度交叉承担变暗，与已验收行为一致）
2. **Rooms 父物体带历史偏移 (3.02,3.49)**（旧 Grid 手工挪动残留）→ 工具内归零，房间坐标与设计稿一致
3. RoomTrigger 加宽至 6 单位覆盖出生点——否则 L3C 出生点在触发器外，首次进房不触发 EnterRoom、弹幕生成器不激活
4. 砖块索引按"平均色表"实测选定（`_16` 等是透明装饰切片不可作地形）；顶砖判定用最终占用状态（挖空后再判）

**验证**：编辑模式手动渲染 9 房间截图逐张核对（Temp/levelshots/）+ 静态断言全绿（层/标签/触发器/引用/遮罩/合成碰撞体/spawner 初始停用/出生点在触发器内，errors=0）。

### 任务 16：性能数据 + README + 面试提纲（文档完成，数据待实测）

- `README.md`（新）：玩法 30 秒 + 三大技术亮点 + 架构引用 + 性能数据表（**待实测**，附 Profiler 采集指南）+ 运行方式 + 关卡结构表
- `Docs/interview-prep.md`（新）：D1~D12 每条"面试官问题 + 3 句话答案" + 7 个真实 bug STAR 故事（SyncTransforms 穿墙/复合体隧道/overrideState 三层坑/转场四轮/Awake 顺序 NRE/Kinematic 接触回调/旧项目粘墙与进不了下一关）+ 追问防线
- `Docs/total.md`：追加 2026-08-28 决策记录（关卡工具化/斜坡定式/性能数据不估数原则等 7 条）
- 性能数据需要用户 Play + Profiler 实测（MCP 进 Play 有失联风险，AI 不代测）：按 README 表格采集后回填，同时可回应 total.md 待决策"对象池是否重新变回卖点"

## 四、给新对话的交接说明

> 任务 12~16 已全部实现，**等待用户 Unity 验收**（清单已交付，见 `Docs/tests/04-levels-test.md` + 交付说明）。验收通过后按模块 commit；性能数据实测后回填 README。
> 重建关卡用菜单 Tools → Level Rebuild → 重建关卡 L1-L3（幂等，会先清旧 L* 房间）。

### 执行须知
- **工作流**：AI 实现 → 附验收清单 → 用户在 Unity 手动验收 → 验收通过才 commit（禁止 AI 自行提交）。**例外**：用户可委托 AI 自主验收（如任务 9）。
- commit 用中文规范（type 英文 + scope/description 中文）
- **MCP**：`set_active_instance` 选 `Mask Dimensions@5092658667197165`（本会话已验证可用）；execute_code 帧 frozen；改资产必须在编辑模式；进 Play 有失联风险（账本任务 9 教训）

### 任务 9 已完成（2026-08-27，用户委托 AI 自主验收通过）
- 实现 = `Assets/Scripts/Rendering/DimensionVolumeController.cs`（计划逐字）+ 双 Profile（**全部参数 overrideState=true**）+ 双 Volume 场景接线 + 移除旧 MPP（组件两场景已清、脚本已删，guid 无残留引用）。
- 提交号见任务表；**坑与教训见任务 9 详情段（三坑：overrideState / Play 中改资产不生效 / 验证维度要对）**。

### 任务 11：渲染线3——自写 Renderer Feature 转场 shader（✅ 已完成，用户手动验收通过）

**交付**（`Assets/Scripts/Rendering/` + `Assets/Shaders/`）：
- `DimensionTransition.shader`：Hidden shader，**扫屏（光边推进+压暗+亮带）与撕裂（行块错位+裂缝亮线）双模式**，`_Progress/_Mode/_Direction/_Width/_BlockCount/_Amplitude/_Darken` 参数化
- `DimensionTransitionPass.cs`：ScriptableRenderPass（BeforeRenderingPostProcessing），最终形态 = **手动全屏三角形链**（详见调试坑）
- `DimensionTransitionFeature.cs`：ScriptableRendererFeature，**静态 Progress 桥**（Feature 是资产对象非场景对象）+ `ModeOverride`
- `DimensionTransitionPlayer.cs`：场景组件，订阅 WorldState 事件 → 协程驱动进度（unscaledDeltaTime，0.4s，撕裂默认）
- 接线：RendererData 资产挂 Feature + GameScene 挂 Player 组件

**修正简报原稿 3 个 bug**（面试可讲）：
1. blit 全屏顶点**已是裁剪空间**，`TransformObjectToHClip` 会双重变换 → 直接透传 positionCS
2. **同一 RT 原地 Blit 未定义**，必须 源→临时RT→源
3. `FindObjectOfType<Feature>` **找不到资产对象**（Feature 在 RendererData 上）→ 静态进桥；且渲染演出独立 Player 组件（不进 PlayerController）

**调试坑（4 轮真实渲染链问题，面试级）**：
1. **1/4 屏根因**：`cmd.Blit` 隐式视口残留 → 重写为手动全屏三角形（SV_VertexID 合成 3 顶点铺满视口）+ 显式 `SetRenderTarget/SetViewport`
2. **全白根因**：材质实例默认 `_MainTex`(white) 覆盖全局纹理 → 改 `material.SetTexture(_MainTex, source.rt)` 材质实例绑定
3. **CopyTexture 运行时报错**：临时 RT 与源格式组不一致（SRGB 混搭，D3D11 base formats 27/26）→ `desc.graphicsFormat = source.rt.graphicsFormat`（同格式组纯拷贝）
4. **画面上下颠倒根因**：`suv.y = 1.0 - uv.y` 翻转是**全屏三角形重写前 cmd.Blit 路径的遗留**（cmd.Blit 内部 quad 的 uv 约定相反）；全屏三角形下 uv(0,0)=屏幕左上、v=0=画面顶部，**直传即正确** → 删除翻转

附带修复：撕裂采样 `frac`→`saturate`（frac 折叠采样坐标 → 画面多个"缩小版"）；扫屏光带 `smoothstep(_Width,0.0,…)` 逆序边界未定义 → 线性衰减；编辑模式手动 `Camera.Render()` 的自定义 blit **源纹理为空**（环境怪癖）→ 最终视觉以用户 Play 为准。

**验收通过（2026-08-27，用户手动 Play）**：按 J → 撕裂/扫屏转场全屏播放（0.4s）、**画面方向与平时一致（不再颠倒）**、错位断层/光带/滤镜过渡正常、无残留、顿帧期间继续 —— 用户确认"验收完毕"。

### 任务 10：渲染线2——URP 2D 光照双世界（已提交 `305ea22`，用户手动验收通过）

**前置裁定（开工卡点）**：3D Renderer（UniversalRendererData）不支持 Light 2D → **整体迁移 2D Renderer**。迁移前源码级验证（Renderer2D.cs 检查）：2D Renderer **含 PostProcessPass（Volume 后处理 ✓，任务 9 兼容）+ BeforeRenderingPostProcessing event（自定义 Renderer Feature ✓，任务 11 兼容）+ Render2DLightingPass（Light 2D ✓）**——本地源码证据，非猜测。

**交付**：
- `Assets/ArtRes/URP/URP2DRenderer.asset`（新）：2D Renderer 资产（反射调官方工厂 `UniversalRenderPipelineAsset.CreateRendererAsset`，internal API via reflection）
- 管线切换：`New Universal Render Pipeline Asset.asset` 的 `m_RendererDataList[0]` → 2D Renderer（SerializedObject 修改资产）
- 转场 Feature 迁移：新 Feature 实例 AddObjectToAsset 挂 2D Renderer（参数与旧资产默认值一致，旧 3D Renderer 资产保留作回滚）
- `DimensionLightController.cs`（新）：双 Global Light 2D 强度交叉（unscaledDeltaTime 0.4s 与滤镜同长，顿帧兼容）＋ **玩家持灯运行时创建**（Point Light 2D 挂玩家 GameObject，暖色 1.3，里世界开启；免 prefab 编辑）
- 场景接线：GameScene 双 GlobalLight（亮 1.0 / 暗 0.12）+ DimensionLightController + BeginScene 恒亮 GlobalLight
- **精灵材质全量迁移**：场景 15 处 + prefab 18 处（7 个 prefab）默认材质 → `URP2D_SpriteLit.mat` / `URP2D_TilemapLit.mat`（`Universal Render Pipeline/2D/Sprite-Lit-Default`）——**2D 光照只对 Lit 材质生效**，内置 Sprites/Default 不受光；`New Material.mat` 与 M_DimensionSprite（模板门）不换（跳过非默认材质）

**验证**（编辑模式手动渲染像素统计）：
- 迁移前基线 avgRGB=(0.280,0.347,0.516) avgSat=0.632（冷蓝 = 表世界滤镜在位）
- 迁移后（2D Renderer + 灯光 + Lit 材质）**同基线 avgSat=0.632、nonBlack=100%** → 渲染链/后处理/Sprite-Lit 无破坏 ✓
- 用户 Play 验收：里世界变暗 + 玩家暖灯晕、转场/滤镜/菜单全正常 ✓

**⚠️ 经验（面试可讲）**：3D Renderer 里的内置 CG 精灵 shader 换成 2D Renderer 后照常渲染（URP 兼容内置非光照 shader）；2D 光照差异化 = 材质(Lit) × 灯光(Global/Point) × 事件驱动(订阅维度)，缺一不可。

## 五、待办

- [x] 任务 1~11 全部验收（物理线 + 渲染线）
- [ ] 任务 12~16：已实现，**待用户 Unity 验收**（验收清单见 `Docs/tests/04-levels-test.md` + 交付说明）
- [ ] 验收通过后按模块 commit（levels 代码 / 关卡场景+资产 / docs 三个提交为宜）
- [ ] 用户 Profiler 实测性能数据 → 回填 README 表格 → 决定对象池是否重新升级为卖点（D11 条件）
- [ ] 全 16 任务完成后：最终代码审查 + finishing-a-development-branch