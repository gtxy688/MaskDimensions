using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// 面具维度切换后处理管理器。
/// 监听事件，利用协程在真实时间（Unscaled Time）下进行故障闪现与色调平滑过渡。
/// </summary>
public class MaskPostProcessingManager : MonoBehaviour
{
    [SerializeField] private Volume globalVolume;

    // 缓存我们需要修改的 URP 效果组件
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;

    [Header("故障撕裂效果 (Chromatic Aberration)")]
    public float glitchMaxIntensity = 1f;    // 切换瞬间的屏幕色散撕裂程度
    public float glitchDuration = 0.2f;      // 撕裂消退的持续时间

    [Header("里世界滤镜 (Color Adjustments)")]
    public float normalSaturation = 0f;      // 表世界饱和度
    public float maskActiveSaturation = -40f;// 里世界饱和度（比如降低色彩，变得灰暗压抑）
    public float normalPostExposure = 0f;    // 表世界曝光
    public float maskActivePostExposure = -0.5f; // 里世界稍微变暗
    public float colorTransitionDuration = 0.4f; // 滤镜过渡的时间

    private void Awake()
    {
        if (globalVolume == null)
            globalVolume = GetComponent<Volume>();
    }

    private void OnEnable()
    {
        // 监听玩家的面具切换事件
        PlayerController.OnMaskStateChanged += HandleMaskStateChanged;

        // 从 Volume Profile 中获取后处理组件的引用
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
            globalVolume.profile.TryGet(out colorAdjustments);
        }
    }

    private void OnDisable()
    {
        PlayerController.OnMaskStateChanged -= HandleMaskStateChanged;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        // 每次切换时，停止之前的过渡，开启新的过渡
        StopAllCoroutines();
        StartCoroutine(GlitchEffectRoutine());
        StartCoroutine(ColorTransitionRoutine(isMaskActive));
    }

    /// <summary>
    /// 瞬间色差故障并快速消退
    /// </summary>
    private IEnumerator GlitchEffectRoutine()
    {
        if (chromaticAberration == null) yield break;

        // 瞬间拉满色差
        chromaticAberration.intensity.value = glitchMaxIntensity;
        float timer = 0f;

        while (timer < glitchDuration)
        {
            // 【极其重要】：因为主角脚本里用了 Time.timeScale = 0 制造顿帧，
            // 这里必须使用 Time.unscaledDeltaTime，否则特效会被时间暂停卡住！
            timer += Time.unscaledDeltaTime;

            // 线性插值让色差逐渐恢复到 0
            chromaticAberration.intensity.value = Mathf.Lerp(glitchMaxIntensity, 0f, timer / glitchDuration);
            yield return null;
        }
        chromaticAberration.intensity.value = 0f;
    }

    /// <summary>
    /// 整体画面色调和曝光的平滑过渡
    /// </summary>
    private IEnumerator ColorTransitionRoutine(bool isMaskActive)
    {
        if (colorAdjustments == null) yield break;

        // 获取当前的参数作为起始点（防止玩家狂按切换导致突变）
        float startSat = colorAdjustments.saturation.value;
        float startExp = colorAdjustments.postExposure.value;

        // 确定目标参数
        float targetSat = isMaskActive ? maskActiveSaturation : normalSaturation;
        float targetExp = isMaskActive ? maskActivePostExposure : normalPostExposure;

        float timer = 0f;

        while (timer < colorTransitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / colorTransitionDuration;

            // 平滑改变饱和度和曝光
            colorAdjustments.saturation.value = Mathf.Lerp(startSat, targetSat, t);
            colorAdjustments.postExposure.value = Mathf.Lerp(startExp, targetExp, t);
            yield return null;
        }

        colorAdjustments.saturation.value = targetSat;
        colorAdjustments.postExposure.value = targetExp;
    }
}