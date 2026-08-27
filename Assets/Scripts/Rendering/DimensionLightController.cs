using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 双世界 2D 光照控制器（任务 10）。
/// 表世界：Global Light 高亮；里世界：Global Light 压暗（环境仅余微光）+ 玩家持灯（局部 Point Light 跟随玩家）。
/// 与 DimensionVolumeController 同模式：订阅 OnMaskStateChanged，unscaledDeltaTime 平滑过渡（顿帧兼容）。
/// 为什么灯光而不是后处理调亮度：03-rendering 硬约束 5——"后处理调亮度"是假光，真光才谈得上"光照差异化"。
/// </summary>
public class DimensionLightController : MonoBehaviour
{
    [Header("全局光（场景中两个 Global Light 2D，接线顺序：亮光/暗光）")]
    [SerializeField] private Light2D brightGlobalLight; // 表世界主光
    [SerializeField] private Light2D dimGlobalLight;    // 里世界环境光

    [Header("亮度配置")]
    [SerializeField] private float brightIntensity = 1.0f;  // 表世界全局光强度
    [SerializeField] private float dimIntensity = 0.12f;    // 里世界全局光强度（仅存微弱环境）

    [Header("玩家持灯")]
    [SerializeField] private float playerLightIntensity = 1.3f;
    [SerializeField] private Color playerLightColor = new Color(1.0f, 0.92f, 0.8f); // 暖色灯（冷蓝表世界里的暖点，里世界微弱环境中的光源）

    [Header("过渡")]
    [SerializeField] private float transitionDuration = 0.4f; // 与滤镜交叉同长

    private Light2D playerLight;   // 运行时挂到玩家的局部光（无 prefab 编辑，省去子物体配置）
    private Coroutine routine;

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= HandleMaskStateChanged;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void Start()
    {
        // 玩家持灯运行时挂在玩家 GameObject 上（跟随玩家即跟随光源）。
        // 为什么运行时创建而非 prefab 挂子物体：避免 prefab 编辑流程（任务 10 轻量接入），效果等价。
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && playerLight == null)
        {
            playerLight = player.gameObject.AddComponent<Light2D>();
            playerLight.lightType = Light2D.LightType.Point;
            playerLight.intensity = playerLightIntensity;
            playerLight.color = playerLightColor;
            playerLight.enabled = false; // 表世界不开灯
        }

        // 初始状态与 WorldState 对齐（每局从表世界开始）：亮光满、暗光微、灯灭
        if (brightGlobalLight != null) brightGlobalLight.intensity = brightIntensity;
        if (dimGlobalLight != null) dimGlobalLight.intensity = dimIntensity;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(TransitionRoutine(isMaskActive));
    }

    /// <summary>双光强度交叉 + 玩家灯开关，unscaledDeltaTime 与滤镜过渡共 0.4s（timeScale=0 顿帧期间继续）。</summary>
    private IEnumerator TransitionRoutine(bool isMaskActive)
    {
        // 里世界：亮光→微光，暗光→主光（其实就是"谁是主光"互换）；玩家开灯
        float fromBright = brightGlobalLight != null ? brightGlobalLight.intensity : brightIntensity;
        float fromDim = dimGlobalLight != null ? dimGlobalLight.intensity : dimIntensity;
        float toBright = isMaskActive ? dimIntensity : brightIntensity;
        float toDim = isMaskActive ? brightIntensity : dimIntensity;
        if (playerLight != null) playerLight.enabled = isMaskActive;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / transitionDuration);
            if (brightGlobalLight != null) brightGlobalLight.intensity = Mathf.Lerp(fromBright, toBright, k);
            if (dimGlobalLight != null) dimGlobalLight.intensity = Mathf.Lerp(fromDim, toDim, k);
            yield return null;
        }
        if (brightGlobalLight != null) brightGlobalLight.intensity = toBright;
        if (dimGlobalLight != null) dimGlobalLight.intensity = toDim;
        routine = null;
    }
}