# 双重维度 BGM 与音效 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 补全双世界 BGM 自动切换、死亡/复活音效播放、PlayerPrefs 音量持久化

**架构：** AudioManager 订阅 `PlayerController.OnMaskStateChanged` 事件自动切换 BGM；在 `DieAndRespawnRoutine` 中调用音效方法；通过 PlayerPrefs 读写音量值。

**技术栈：** Unity (C#)、PlayerPrefs、AudioSource

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `Assets/Scripts/Mgr/AudioManager.cs` | 修改 | 新增资源字段、事件订阅、音量持久化、SFX 快捷方法 |
| `Assets/Scripts/GameScene/Player/PlayerController.cs` | 修改 | 死亡/复活流程中调用音效 |
| `Assets/Scripts/UI/SettingPanel.cs` | 修改 | 音量滑动条变化时实时应用音量 |

---

### 任务 1：AudioManager — 新增字段与初始化

**文件：**
- 修改：`Assets/Scripts/Mgr/AudioManager.cs:1-26`

- [ ] **步骤 1：写入 AudioManager 新字段和初始化逻辑**

将以下内容追加到类开头（现有字段的 `Header` 区之后）：

```csharp
[Header("BGM 音乐片段 (拖入)")]
[SerializeField] private AudioClip normalBGMClip;  // 表世界 BGM
[SerializeField] private AudioClip voidBGMClip;    // 里世界 BGM

[Header("音效片段 (拖入)")]
[SerializeField] private AudioClip deathSFX;       // 死亡音效
[SerializeField] private AudioClip respawnSFX;      // 复活音效

[Header("音量持久化")]
[SerializeField] private string masterVolumeKey = "MasterVolume";
[SerializeField] private string soundVolumeKey   = "SoundVolume";

private void Start()
{
    // 1. 从 PlayerPrefs 读取音量并应用
    ApplyVolumeSettings();

    // 2. 初始化双 BGM（如果已拖入 Clip）
    if (normalBGMClip != null && voidBGMClip != null)
        PlayDualBGM(normalBGMClip, voidBGMClip);

    // 3. 订阅世界切换事件
    PlayerController.OnMaskStateChanged += SwitchBGMDimension;
}

private void OnDestroy()
{
    // 取消订阅防泄漏
    PlayerController.OnMaskStateChanged -= SwitchBGMDimension;
}
```

- [ ] **步骤 2：验证编译通过**

在 Unity 中等待编译完成，确保 Console 无红色报错。

- [ ] **步骤 3：Commit**

```bash
git add Assets/Scripts/Mgr/AudioManager.cs
git commit -m "feat(audio): add audio clip fields, Start/OnDestroy lifecycle"
```

---

### 任务 2：AudioManager — 修改 PlayDualBGM 使用 PlayerPrefs 音量

**文件：**
- 修改：`Assets/Scripts/Mgr/AudioManager.cs:13-26`

- [ ] **步骤 1：修改 PlayDualBGM 中的硬编码 volume**

将原来 `volume = 1f` 的两行替换为：

```csharp
// 从 PlayerPrefs 读取音量（由 SettingPanel 设置）
bgmSourceNormal.volume = PlayerPrefs.GetFloat(masterVolumeKey, 1f);
bgmSourceVoid.volume   = PlayerPrefs.GetFloat(masterVolumeKey, 1f);
```

- [ ] **步骤 2：添加 ApplyVolumeSettings 方法**

在类中新增方法（放在 `PlaySFX` 方法后面）：

```csharp
/// <summary>
/// 从 PlayerPrefs 重新读取音量并应用到所有 AudioSource。
/// 供 SettingPanel 在滑动条变化时调用。
/// </summary>
public void ApplyVolumeSettings()
{
    float bgmVol = PlayerPrefs.GetFloat(masterVolumeKey, 1f);
    float sfxVol = PlayerPrefs.GetFloat(soundVolumeKey, 1f);

    if (bgmSourceNormal != null) bgmSourceNormal.volume = bgmVol;
    if (bgmSourceVoid   != null) bgmSourceVoid.volume   = bgmVol;
    if (sfxSource       != null) sfxSource.volume       = sfxVol;
}
```

- [ ] **步骤 3：添加 PlayDeathSFX / PlayRespawnSFX 方法**

在 `ApplyVolumeSettings` 后面：

```csharp
public void PlayDeathSFX()  => PlaySFX(deathSFX);
public void PlayRespawnSFX() => PlaySFX(respawnSFX);
```

- [ ] **步骤 4：验证编译通过**

在 Unity 中等待编译完成，确保 Console 无红色报错。

- [ ] **步骤 5：Commit**

```bash
git add Assets/Scripts/Mgr/AudioManager.cs
git commit -m "feat(audio): read volume from PlayerPrefs, add SFX helpers"
```

---

### 任务 3：PlayerController — 集成死亡/复活音效

**文件：**
- 修改：`Assets/Scripts/GameScene/Player/PlayerController.cs:375-422`

- [ ] **步骤 1：在 DieAndRespawnRoutine 中加入音效调用**

找到 `DieAndRespawnRoutine()` 方法，在 `deathVFXPrefab` 实例化之后（第 389 行附近）添加死亡音效：

```csharp
// 2. 死亡消散特效
if (deathVFXPrefab != null)
    Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

// ★ 播放死亡音效
AudioManager.Instance.PlayDeathSFX();
```

在 `respawnVFXPrefab` 实例化之后（第 401 行附近）添加复活音效：

```csharp
// 5. 重生凝聚特效
if (respawnVFXPrefab != null)
    Instantiate(respawnVFXPrefab, transform.position, Quaternion.identity);

// ★ 播放复活音效
AudioManager.Instance.PlayRespawnSFX();
```

- [ ] **步骤 2：验证编译通过**

在 Unity 中等待编译完成，确保 Console 无红色报错。

- [ ] **步骤 3：Commit**

```bash
git add Assets/Scripts/GameScene/Player/PlayerController.cs
git commit -m "feat(audio): play death/respawn SFX in DieAndRespawnRoutine"
```

---

### 任务 4：SettingPanel — 滑动条实时应用音量

**文件：**
- 修改：`Assets/Scripts/UI/SettingPanel.cs:49-60`

- [ ] **步骤 1：在滑动条回调中添加 AudioManager 调用**

找到 `sliderVolume.onValueChanged` 和 `sliderSound.onValueChanged` 的回调，在 `PlayerPrefs.SetFloat` 之后追加：

```csharp
// 3. 绑定音量滑动条事件
sliderVolume.onValueChanged.AddListener((float value) =>
{
    Debug.Log("当前游戏音量调节为: " + value);
    PlayerPrefs.SetFloat("MasterVolume", value);
    // ★ 实时应用音量
    AudioManager.Instance.ApplyVolumeSettings();
});

// 绑定音效滑动条事件
sliderSound.onValueChanged.AddListener((float value) =>
{
    Debug.Log("当前游戏音效调节为: " + value);
    PlayerPrefs.SetFloat("SoundVolume", value);
    // ★ 实时应用音量
    AudioManager.Instance.ApplyVolumeSettings();
});
```

- [ ] **步骤 2：验证编译通过**

在 Unity 中等待编译完成，确保 Console 无红色报错。

- [ ] **步骤 3：Commit**

```bash
git add Assets/Scripts/UI/SettingPanel.cs
git commit -m "fix(setting): apply volume changes to AudioManager in real-time"
```

---

### 任务 5：场景配置 — 拖入音频资源

**说明：** 此任务需要在 Unity Editor 中手动操作。

- [ ] **步骤 1：在 GameScene 中找到 AudioManager 对象**

AudioManager 是 `SingletonAutoMono`，第一次访问 `AudioManager.Instance` 时自动创建。进入 GameScene，在 Hierarchy 中应存在名为 `AudioManager` 的 GameObject。

- [ ] **步骤 2：拖入 BGM 音频资源**

选中 AudioManager 对象，在 Inspector 中找到：
- `Normal BGM Clip` → 拖入表世界 BGM 音频文件
- `Void BGM Clip` → 拖入里世界 BGM 音频文件

- [ ] **步骤 3：拖入音效资源**

- `Death SFX` → 拖入死亡音效
- `Respawn SFX` → 拖入复活音效

- [ ] **步骤 4：验证功能**

运行游戏，进入 GameScene：
1. 表世界 BGM 应自动播放
2. 按 J 切换到里世界 → 表世界 BGM 暂停，里世界 BGM 开始播放
3. 摘面具返回表世界 → 里世界 BGM 暂停，表世界 BGM 从暂停处恢复
4. 角色触碰 Trap 死亡 → 播放死亡音效 → 重生 → 播放复活音效
5. 打开设置面板调节音量滑块 → 音量实时变化

- [ ] **步骤 5：Commit**

```bash
git add -A
git commit -m "config(audio): assign BGM and SFX clips in scene"
```
