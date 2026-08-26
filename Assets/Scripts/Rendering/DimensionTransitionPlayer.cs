using System.Collections;
using UnityEngine;

/// <summary>
/// 维度切换转场播放器：订阅 WorldState 切换事件，用 unscaledDeltaTime 驱动 Renderer Feature 的转场进度。
/// 为什么独立组件而非塞进 PlayerController：玩家逻辑只管输入/物理，渲染演出是纯表现层，单一职责分离（面试可讲分层）。
/// 为什么 unscaledDeltaTime：切换瞬间场景会用 timeScale=0 制造顿帧，转场必须继续播放（断片感），与 DimensionVolumeController 同理由。
/// </summary>
public class DimensionTransitionPlayer : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f;   // 转场总时长（覆盖顿帧 0.15s + 滤镜过渡 0.4s）
    [SerializeField] private bool useTearMode = true; // true=撕裂 false=扫屏

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
        // 防残留：任何中断都必须把转场进度清零
        DimensionTransitionFeature.Progress = 0f;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(PlayRoutine(isMaskActive));
    }

    private IEnumerator PlayRoutine(bool isMaskActive)
    {
        DimensionTransitionFeature.Progress = 0f;
        DimensionTransitionFeature.ModeOverride = useTearMode ? 1f : 0f;

        // 进度 0→1：撕裂模式下"从碎裂重组"（新世界在重组中显露），扫屏模式下"光边扫过揭出新世界"
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            DimensionTransitionFeature.Progress = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        // 播完归零（Pass 对 0 跳画，防残留）
        DimensionTransitionFeature.Progress = 0f;
        routine = null;
    }
}