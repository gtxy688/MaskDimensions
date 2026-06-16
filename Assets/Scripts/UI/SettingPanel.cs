using UnityEngine;
using UnityEngine.UI;

//设置面板
public class SettingPanel : BasePanel
{
    [Header("UI 控件")]
    public Button btnClose;
    public Button btnQuit;
    public Slider sliderVolume;
    public Slider sliderSound;

    // 定义一个专属的 Key，防止拼写错误
    private const string VolumeKey = "Volume";
    // 定义一个专属的 Key，防止拼写错误
    private const string SoundKey = "Sound";

    // 重写 BasePanel 的初始化方法
    public override void Init()
    {
        // 1.读取音量音效数据
        // 判断本地是否存过这个数据（如果是玩家第一次玩，肯定没有）
        if (PlayerPrefs.HasKey(VolumeKey))
        {
            // 读取保存的音量和音效值，并赋值给滑块
            sliderVolume.value = PlayerPrefs.GetFloat(VolumeKey);
            sliderSound.value = PlayerPrefs.GetFloat(SoundKey);
        }
        else
        {
            // 如果是第一次玩，给一个默认初始值（比如最大音量 1f）
            sliderVolume.value = 1f;
            sliderSound.value = 1f;
        }

        // 2. 绑定关闭按钮事件
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        // 绑定退出按钮事件
        btnQuit.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        // 3. 绑定音量滑动条事件
        sliderVolume.onValueChanged.AddListener((float value) =>
        {
            Debug.Log("当前游戏音量调节为: " + value);
            PlayerPrefs.SetFloat("MasterVolume", value);
        });

        // 绑定音效滑动条事件
        sliderSound.onValueChanged.AddListener((float value) =>
        {
            Debug.Log("当前游戏音效调节为: " + value);
            PlayerPrefs.SetFloat("SoundVolume", value); 
        });

    }
}