# 03 渲染系统（rendering）

> 状态：**新写**（替换现有 MaskPostProcessingManager 的单 Volume lerp 方案）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D5~D8）。

## 职责

- 表/里世界**视觉差异化**：两个世界两套滤镜，切换时平滑过渡。
- 2D 光照差异化：表世界明亮，里世界昏暗 + 局部光源。
- 切换转场：自写 Renderer Feature（扫屏/撕裂），配合顿帧播放。
- 移除 PPv2（com.unity.postprocessing 从 manifest 删除）。

## 目标文件（`Assets/Scripts/Rendering/`）

| 文件 | 职责 |
|---|---|
| `DimensionVolumeController.cs` | 驱动两个 Volume 的 weight 过渡（unscaledDeltaTime） |
| `DimensionTransitionFeature.cs` | Renderer Feature 入口（切换转场） |
| `DimensionTransitionPass.cs` | ScriptableRenderPass 实现（全屏 Pass，用 CommandBuffer 画转场） |
| `DimensionTransition.shader` | 转场效果 shader（扫屏展开/撕裂，参数可调） |
| （场景配置） | 两个 Volume Profile：RealWorld（冷色清朗）/ MaskWorld（噪点/扫描线/低饱和） |

## 关键设计

### Volume 双系统（替换 MaskPostProcessingManager 的 lerp 方案）

- 场景放两个 Volume（isGlobal），各自引用独立 Profile：
  - 表世界 Profile：正常饱和度、正常曝光、轻微暗角。
  - 里世界 Profile：低饱和度（灰暗）、降曝光、噪点 + 扫描线（Film Grain / 自写效果）。
- `DimensionVolumeController` 订阅 `OnMaskStateChanged`，在切换协程里用 `Time.unscaledDeltaTime` 做两个 Volume 的 weight 交叉淡化（0→1 / 1→0），时长约 0.4s。
- 顿帧期间（timeScale=0）过渡必须继续——**必须用 unscaledDeltaTime**（现有代码已有此经验，保持）。

### 2D 光照

- 场景启用 URP 2D Renderer 的 Light 2D 功能。
- 表世界：Global Light 2D 高亮度；里世界：Global Light 2D 低亮度 + 局部 Point Light（玩家持灯挂在玩家子物体上）。
- 光照切换与维度切换同步（同一事件驱动，或在 Volume weight 过渡时同步）。

### 切换转场（Renderer Feature，M4 阶段）

- `DimensionTransitionFeature`：检测切换请求 → 在渲染流程插入 `DimensionTransitionPass`。
- Pass：全屏四边形 + 转场 shader；进度参数由 `DimensionVolumeController` 或独立协程驱动（unscaledDeltaTime）。
- 转场视觉：扫屏展开 / 水平撕裂，参数（进度、方向、宽度）可调。

## 生命周期

1. Awake：VolumeController 缓存两个 Volume 与 profile 组件引用。
2. OnEnable：订阅 `OnMaskStateChanged`（切换触发过渡 + 转场）。
3. OnDisable：退订（防泄漏）。
4. 过渡协程：记录当前 weight → 用 unscaledDeltaTime 插值 → 到达目标后停。

## 核心规则（硬约束）

1. **禁止 PPv2**：com.unity.postprocessing 必须从 Packages/manifest.json 移除；现有 MaskPostProcessingManager 若引用 PPv2 组件则改/删。
2. 过渡必须用 `Time.unscaledDeltaTime`（顿帧期间播放，timeScale=0）。
3. 事件签名沿用 `OnMaskStateChanged(bool)`，不得改动。
4. Volume 过渡与转场 Feature 同时启动，时机对齐（都在切换协程的同一时刻触发）。
5. 光照方案优先用 URP 2D Light，禁止"后处理调亮度"当光照（假光）。

## 依赖

- 依赖：02-dimension（订阅维度事件）、URP（Volume / Light 2D / Renderer Feature）。
- 被依赖：无（纯表现层，通过事件接入）。
- 不依赖：玩家逻辑、物理。

## 验收

见 `../tests/03-rendering-test.md`。
