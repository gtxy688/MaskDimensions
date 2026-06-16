# 双重维度 BGM 与音效集成设计

## 概述

在现有双重维度世界（表世界/里世界）基础上，补全音频系统的集成接线，实现：
- 世界切换时 BGM 自动切换（暂停/恢复）
- 角色死亡和复活时播放音效

## 改动范围

### 1. AudioManager 增强

**文件：** `Assets/Scripts/Mgr/AudioManager.cs`

新增字段：

```csharp
[Header("BGM 音乐片段")]
[SerializeField] private AudioClip normalBGMClip;  // 表世界 BGM
[SerializeField] private AudioClip voidBGMClip;    // 里世界 BGM

[Header("音效片段")]
[SerializeField] private AudioClip deathSFX;       // 死亡音效
[SerializeField] private AudioClip respawnSFX;      // 复活音效
```

新增生命周期逻辑：

- `Start()`: 如果已拖入 Clip，调用 `PlayDualBGM(normalBGMClip, voidBGMClip)` 初始化；
  订阅 `PlayerController.OnMaskStateChanged` 事件，接上 `SwitchBGMDimension()`
- `OnDestroy()`: 取消订阅事件，防止内存泄漏

新增快捷方法：

- `PlayDeathSFX()` → 调用 `PlaySFX(deathSFX)`
- `PlayRespawnSFX()` → 调用 `PlaySFX(respawnSFX)`

**数据流：**

```
玩家按 J → PlayerController.ExecuteMaskSwitchWithHitlag()
         → 广播 OnMaskStateChanged
         → AudioManager.SwitchBGMDimension(isVoid)
         → bgmSourceNormal.Pause() / UnPause()
         → bgmSourceVoid.Pause() / UnPause()
```

### 2. PlayerController 音效集成

**文件：** `Assets/Scripts/GameScene/Player/PlayerController.cs`

在 `DieAndRespawnRoutine()` 方法中新增两处调用：

| 位置 | 代码 | 时机 |
|------|------|------|
| 实例化死亡消散特效后 | `AudioManager.Instance.PlayDeathSFX()` | 角色消失同时 |
| 实例化重生凝聚特效后 | `AudioManager.Instance.PlayRespawnSFX()` | 角色显形前 |

### 3. 场景配置

在 **GameScene** 中的 AudioManager 预制体（或挂载点）上：
- 将 normalBGMClip / voidBGMClip / deathSFX / respawnSFX 四个音频资源拖入对应槽位

### 3. 音量持久化读取

**问题：** `SettingPanel` 仅将音量写入 `PlayerPrefs`（键 `"MasterVolume"` / `"SoundVolume"`），但从未真正应用到音频输出。

**方案：** AudioManager 在 `Start()` 中读取 PlayerPrefs，并直接设置 AudioSource.volume。

在 PlayDualBGM 中，将原来的硬编码 `volume = 1f` 改为从 PlayerPrefs 读取：

```csharp
bgmSourceNormal.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
bgmSourceVoid.volume   = PlayerPrefs.GetFloat("MasterVolume", 1f);
```

新增公共方法，供 SettingPanel 在滑动条变化时调用：

```csharp
public void ApplyVolumeSettings()
{
    float bgmVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
    float sfxVol = PlayerPrefs.GetFloat("SoundVolume", 1f);

    bgmSourceNormal.volume = bgmVol;
    bgmSourceVoid.volume   = bgmVol;
    sfxSource.volume       = sfxVol;
}
```

**在 SettingPanel 中：** 滑动条回调末尾追加调用 `AudioManager.Instance.ApplyVolumeSettings()` 使音量实时生效。

## 不变的部分

- 不创建新的管理器或事件系统
- 不修改现有的 `SwitchBGMDimension` / `PlaySFX` 方法体
- 不修改 `MaskPostProcessingManager`、`SanityUIManager` 等已有监听者