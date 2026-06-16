using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 单例模式，方便全网随时调用
    public static AudioManager Instance;

    [Header("音频播放器")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    private void Awake()
    {
        // 经典的单例与跨场景保活逻辑
        if (Instance == null)
        {
            Instance = this;
            // 保证切换场景时，BGM不会突然中断重头播放
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmClip == null) return;
        
        // 如果正在放这首歌，就不管它（防止重复触发导致音乐重头开始）
        if (bgmSource.clip == bgmClip) return; 

        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    /// <summary>
    /// 播放短促音效
    /// </summary>
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null) return;
        
        // 绝对不要用 sfxSource.Play()！
        // PlayOneShot 允许多个音效在同一个 AudioSource 上叠加播放，不会互相切断！
        sfxSource.PlayOneShot(sfxClip); 
    }
}