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

    [Header("音量持久化")]
    [SerializeField] private string volumeKey = "Volume";
    [SerializeField] private string soundKey  = "Sound";

    private void Start()
    {
        // 1. 从 PlayerPrefs 读取音量并应用
        ApplyVolumeSettings();

        // 2. 如果在开始界面，播放菜单音乐
        if (bgmSourceMenu != null && menuBGMClip != null)
        {
            bgmSourceMenu.clip = menuBGMClip;
            bgmSourceMenu.Play();
        }

        // 3. 订阅世界切换事件（进入 GameScene 后才会有 PlayerController 触发）
        PlayerController.OnMaskStateChanged += SwitchBGMDimension;
    }

    private void OnDestroy()
    {
        // 取消订阅防泄漏
        PlayerController.OnMaskStateChanged -= SwitchBGMDimension;
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

        // 从 PlayerPrefs 读取音量（由 SettingPanel 设置）
        bgmSourceNormal.volume = PlayerPrefs.GetFloat(volumeKey, 1f);
        bgmSourceVoid.volume   = PlayerPrefs.GetFloat(volumeKey, 1f);

        // 初始状态：表世界播放，里世界静默（但不调用 Play，省性能）
        bgmSourceNormal.Play();
        bgmSourceVoid.Stop(); 
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
    /// 从 PlayerPrefs 重新读取音量并应用到所有 AudioSource。
    /// 供 SettingPanel 在滑动条变化时调用。
    /// </summary>
    public void ApplyVolumeSettings()
    {
        float bgmVol = PlayerPrefs.GetFloat(volumeKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(soundKey, 1f);

        if (bgmSourceMenu   != null) bgmSourceMenu.volume   = bgmVol;
        if (bgmSourceNormal != null) bgmSourceNormal.volume = bgmVol;
        if (bgmSourceVoid   != null) bgmSourceVoid.volume   = bgmVol;
        if (sfxSource       != null) sfxSource.volume       = sfxVol;
    }
}