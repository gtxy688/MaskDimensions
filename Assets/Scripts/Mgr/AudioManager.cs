using UnityEngine;

public class AudioManager : SingletonMono<AudioManager>
{
    [Header("音频播放器 (拖入对应的 AudioSource)")]
    public AudioSource bgmSourceNormal; // 表世界 BGM (挂载 BGM Mixer，开启 Loop)
    public AudioSource bgmSourceVoid;   // 里世界 BGM (挂载 BGM Mixer，开启 Loop)
    public AudioSource sfxSource;       // 音效 (挂载 SFX Mixer，关闭 Loop)

    [Header("菜单音乐")]
    public AudioSource bgmSourceMenu;   // 开始界面音乐 (挂载 BGM Mixer，开启 Loop)
    [SerializeField] private AudioClip menuBGMClip; // 菜单 BGM

    [Header("BGM 音乐片段 (拖入)")]
    [SerializeField] private AudioClip normalBGMClip;  // 表世界 BGM
    [SerializeField] private AudioClip voidBGMClip;    // 里世界 BGM

    [Header("音效片段 (拖入)")]
    [SerializeField] private AudioClip deathSFX;       // 死亡音效
    [SerializeField] private AudioClip respawnSFX;      // 复活音效
    [SerializeField] private AudioClip endGameSFX;      // 通关音效

    [Header("音量持久化")]
    [SerializeField] private string volumeKey = "Volume";
    [SerializeField] private string soundKey  = "Sound";

    // 基准音量：捕获 Inspector 里 AudioSource.volume 的初始值（BGM 母带偏响时直接在源上调小），
    // 最终音量 = 基准 × 玩家设置滑条（PlayerPrefs）。为什么必须捕获：ApplyVolumeSettings 会把
    // PlayerPrefs 值（默认 1）写回源音量，Inspector 配置一运行就被顶掉。
    private float baseMenu, baseNormal, baseVoid, baseSfx = 1f;
    private bool baseCaptured;

    /// <summary>为什么懒捕获：PlayDualBGM 可能先于本类 Start 执行（跨物体 Start 顺序未定义），
    /// 此时字段还是默认 0，直接套公式会静音；捕获本身在"写音量之前"做，读到的必然是 Inspector 值。</summary>
    private void EnsureBaseCaptured()
    {
        if (baseCaptured) return;
        baseMenu   = bgmSourceMenu   != null ? bgmSourceMenu.volume   : 1f;
        baseNormal = bgmSourceNormal != null ? bgmSourceNormal.volume : 1f;
        baseVoid   = bgmSourceVoid   != null ? bgmSourceVoid.volume   : 1f;
        baseSfx    = sfxSource       != null ? sfxSource.volume       : 1f;
        baseCaptured = true;
    }

    private void Start()
    {
        // 1. 从 PlayerPrefs 读取音量并应用（最终音量 = 基准 × 滑条）
        ApplyVolumeSettings();

        // 2. 如果在开始界面，播放菜单音乐
        if (bgmSourceMenu != null && menuBGMClip != null)
        {
            bgmSourceMenu.clip = menuBGMClip;
            bgmSourceMenu.Play();
        }

        // 3. 订阅世界切换事件（进入 GameScene 后才会有 WorldState 触发）
        WorldState.OnMaskStateChanged += SwitchBGMDimension;
    }

    private void OnDestroy()
    {
        // 取消订阅防泄漏
        WorldState.OnMaskStateChanged -= SwitchBGMDimension;
    }

    /// <summary>
    /// 进入 GameScene 时调用，停止菜单音乐，启动双 BGM
    /// </summary>
    public void PlayDualBGMFromClips()
    {
        // 停止菜单音乐
        StopMenuBGM();

        if (normalBGMClip == null || voidBGMClip == null)
        {
            Debug.LogWarning("BGM Clip 未设置，请拖入 normalBGMClip 和 voidBGMClip");
            return;
        }
        PlayDualBGM(normalBGMClip, voidBGMClip);
    }

    /// <summary>
    /// 停止菜单音乐
    /// </summary>
    public void StopMenuBGM()
    {
        if (bgmSourceMenu != null)
            bgmSourceMenu.Stop();
    }

    /// <summary>
    /// 回到开始界面时调用，停掉游戏 BGM，重新播放菜单音乐
    /// </summary>
    public void PlayMenuBGM()
    {
        // 停掉游戏 BGM
        if (bgmSourceNormal != null) bgmSourceNormal.Stop();
        if (bgmSourceVoid != null)   bgmSourceVoid.Stop();

        // 播放菜单音乐
        if (bgmSourceMenu != null && menuBGMClip != null)
        {
            bgmSourceMenu.clip = menuBGMClip;
            bgmSourceMenu.Play();
        }
    }

    /// <summary>
    /// 游戏场景启动时调用，载入两首音乐
    /// </summary>
    public void PlayDualBGM(AudioClip normalClip, AudioClip voidClip)
    {
        // 装载音乐片段
        bgmSourceNormal.clip = normalClip;
        bgmSourceVoid.clip = voidClip;

        // 音量统一走 ApplyVolumeSettings（基准 × 玩家滑条），不在此处直读 PlayerPrefs
        ApplyVolumeSettings();

        // 初始状态：表世界播放，里世界静默但不调用 Play（省性能）
        bgmSourceNormal.Play();
        // 里世界 BGM 预热：首次 Play 会触发 AudioClip 解码与音频管线初始化（首次切换卡顿嫌疑之一），
        // 进场景即静音预载一次并暂停——Pause 后 isPlaying=false，首次切入仍走 Play() 从头播放，行为不变。
        float voidVol = bgmSourceVoid.volume;
        bgmSourceVoid.volume = 0f;
        bgmSourceVoid.Play();
        bgmSourceVoid.volume = voidVol;
        bgmSourceVoid.Pause(); 
    }

    /// <summary>
    /// 切换维度时调用，瞬间的硬切（暂停一个，播放/恢复另一个）
    /// </summary>
    public void SwitchBGMDimension(bool isVoid)
    {
        if (isVoid)
        {
            // 戴上面具进入里世界：暂停表世界，激活里世界
            bgmSourceNormal.Pause();
            
            if (!bgmSourceVoid.isPlaying)
                bgmSourceVoid.Play(); // 第一次进里世界，从头播放
            else
                bgmSourceVoid.UnPause(); // 之后再进，从上次暂停的地方继续
        }
        else
        {
            // 摘下面具回到表世界：暂停里世界，恢复表世界
            bgmSourceVoid.Pause();
            bgmSourceNormal.UnPause();
        }
    }

    /// <summary>
    /// 播放短促音效（跳跃、碎裂、UI点击等）
    /// </summary>
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null) return;
        // PlayOneShot 允许多个音效叠加，不会互相切断
        sfxSource.PlayOneShot(sfxClip);
    }

    /// <summary>
    /// 播放死亡音效
    /// </summary>
    public void PlayDeathSFX() => PlaySFX(deathSFX);

    /// <summary>
    /// 播放复活音效
    /// </summary>
    public void PlayRespawnSFX() => PlaySFX(respawnSFX);

    /// <summary>
    /// 播放通关音效
    /// </summary>
    public void PlayEndGameSFX() => PlaySFX(endGameSFX);

    /// <summary>
    /// 从 PlayerPrefs 重新读取音量并应用到所有 AudioSource。
    /// 供 SettingPanel 在滑动条变化时调用。
    /// 最终音量 = Inspector 基准音量（Start 捕获）× 玩家滑条（PlayerPrefs，默认 1）。
    /// </summary>
    public void ApplyVolumeSettings()
    {
        EnsureBaseCaptured();
        float bgmSlider = PlayerPrefs.GetFloat(volumeKey, 1f);
        float sfxSlider = PlayerPrefs.GetFloat(soundKey, 1f);

        if (bgmSourceMenu   != null) bgmSourceMenu.volume   = baseMenu   * bgmSlider;
        if (bgmSourceNormal != null) bgmSourceNormal.volume = baseNormal * bgmSlider;
        if (bgmSourceVoid   != null) bgmSourceVoid.volume   = baseVoid   * bgmSlider;
        if (sfxSource       != null) sfxSource.volume       = baseSfx    * sfxSlider;
    }
}