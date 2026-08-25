# 08 表现层（presentation：UI / 音频 / Tips）

> 状态：**保留不改**
> 本文件是 AI 写脚本用的功能需求规格。

## 职责

- UI 面板管理（UIManager + BasePanel 体系）。
- 音频（AudioManager：双 BGM 随维度切换 + SFX + 音量持久化）。
- 提示系统（理智提示 / 教程 HUD / Toast 消息）。

## 文件清单

| 文件 | 状态 | 职责 |
|---|---|---|
| `Mgr/UIManager.cs` | 保留 | 面板管理器（Resources 加载 + 字典缓存） |
| `Mgr/AudioManager.cs` | 保留 | 音频：双 BGM 切换、SFX、音量 PlayerPrefs |
| `UI/BasePanel.cs` | 保留 | 面板基类（ShowMe/HideMe 生命周期） |
| `UI/BeginPanel.cs` | 保留 | 开始面板 |
| `UI/GamePanel.cs` | 保留 | 游戏内面板 |
| `UI/SettingPanel.cs` | 保留 | 设置面板（音量滑条） |
| `UI/EndPanel.cs` | 保留 | 结束面板 |
| `UI/AutoBindCamera.cs` | 保留 | Canvas 相机绑定 |
| `GameScene/Tips/SanityUIManager.cs` | 保留 | 理智条 UI |
| `GameScene/Tips/ToastMessage.cs` | 保留 | Toast 消息 |
| `GameScene/Tips/TutorialTriggerZone.cs` | 保留 | 教程触发区 |
| `GameScene/Tips/SanityStatusHints.cs` | 保留 | 理智状态提示 |
| `GameScene/Tips/InteractiveTutorialHUD.cs` | 保留 | 交互教程 HUD |
| `GameScene/Effects/DestroyAfterAnimation.cs` | 保留 | 特效播完销毁 |

## 关键行为（写脚本时不得破坏）

- UIManager：`ShowPanel<T>()` 按类型名找 Resources/UI/ 预制体，实例化进 Canvas，字典缓存；`HidePanel<T>(isFade)` 支持淡出后销毁。
- AudioManager：双 BGM 源（表/里）随 `OnMaskStateChanged` 切换（Pause/UnPause）；音量存 PlayerPrefs（Volume/Sound 键）；订阅/退订配对。
- 面板与提示全部通过事件/管理器接入，禁止每帧轮询。

## 核心规则（硬约束）

1. 禁止重写表现层（不是本项目差异化方向）。
2. 改 UI/音频时**保持现有事件签名与 PlayerPrefs 键名**（Volume/Sound）。
3. 订阅事件必须配对退订（OnEnable/OnDisable 或 OnDestroy），防泄漏。
4. 表现层一律事件驱动，禁止每帧轮询业务状态。

## 依赖

- 依赖：09-base-framework（Singleton 基类）、02-dimension（维度事件）、05-player-fsm（玩家事件）。
- 被依赖：无（纯表现，被场景/入口调用）。

## 验收

见 `../tests/08-presentation-test.md`。
