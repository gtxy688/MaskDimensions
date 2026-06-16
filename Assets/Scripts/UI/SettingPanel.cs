using UnityEngine;
using UnityEngine.UI;

//设置面板
public class SettingPanel : BasePanel
{
    [Header("UI 控件")]
    public Button btnClose;
    public Button btnClose2;
    public Button btnEnsure;
    public Slider sliderVolume;
    public Slider sliderSound;
    public Toggle toggleVolume; // 音量开关
    public Toggle toggleSound;  // 音效开关

    // 定义一个专属的 Key，防止拼写错误
    private const string VolumeKey = "Volume";
    private const string SoundKey = "Sound";
    private const string VolumeToggleKey = "VolumeToggle";
    private const string SoundToggleKey = "SoundToggle";

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

        // 读取并应用开关状态
        bool volOn = PlayerPrefs.GetInt(VolumeToggleKey, 1) == 1;
        bool sndOn = PlayerPrefs.GetInt(SoundToggleKey, 1) == 1;
        toggleVolume.isOn = volOn;
        toggleSound.isOn = sndOn;
        sliderVolume.interactable = volOn;
        sliderSound.interactable = sndOn;

        // 2. 绑定关闭按钮事件
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        // 绑定退出按钮事件
        btnClose2.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        btnEnsure.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        // 3. 绑定音量滑动条事件
        sliderVolume.onValueChanged.AddListener((float value) =>
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            // ★ 实时应用音量
            AudioManager.Instance.ApplyVolumeSettings();
        });

        // 绑定音效滑动条事件
        sliderSound.onValueChanged.AddListener((float value) =>
        {
            PlayerPrefs.SetFloat("SoundVolume", value);
            // ★ 实时应用音量
            AudioManager.Instance.ApplyVolumeSettings();
        });

        // 4. 绑定开关事件
        toggleVolume.onValueChanged.AddListener((bool isOn) =>
        {
            PlayerPrefs.SetInt(VolumeToggleKey, isOn ? 1 : 0);
            sliderVolume.interactable = isOn;
            // 关闭时音量归零，开启时恢复之前的值
            if (!isOn)
            {
                PlayerPrefs.SetFloat("MasterVolume", 0f);
                sliderVolume.value = 0f;
            }
            else
            {
                sliderVolume.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
            }
            AudioManager.Instance.ApplyVolumeSettings();
        });

        toggleSound.onValueChanged.AddListener((bool isOn) =>
        {
            PlayerPrefs.SetInt(SoundToggleKey, isOn ? 1 : 0);
            sliderSound.interactable = isOn;
            if (!isOn)
            {
                PlayerPrefs.SetFloat("SoundVolume", 0f);
                sliderSound.value = 0f;
            }
            else
            {
                sliderSound.value = PlayerPrefs.GetFloat(SoundKey, 1f);
            }
            AudioManager.Instance.ApplyVolumeSettings();
        });

    }
}