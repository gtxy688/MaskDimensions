using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 双 Volume 维度过渡：表世界/里世界各一个 Volume，切换时 weight 交叉淡化。
/// 为什么 unscaledDeltaTime：顿帧期间 timeScale=0，必须用真实时间驱动过渡（断片感）。
/// glitchVolume：切换瞬间色差故障（Chromatic Aberration 拉满 → 0.2s 消退），同为 unscaledDeltaTime 驱动。
/// </summary>
public class DimensionVolumeController : MonoBehaviour
{
    [SerializeField] private Volume realWorldVolume;  // 表世界 Profile
    [SerializeField] private Volume maskWorldVolume;  // 里世界 Profile
    [SerializeField] private Volume glitchVolume;     // 切换瞬间故障 Volume（可选，只含 ChromaticAberration）
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private float glitchDuration = 0.2f;

    private Coroutine transitionRoutine;

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= HandleMaskStateChanged;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(isMaskActive));
    }

    private IEnumerator TransitionRoutine(bool isMaskActive)
    {
        float timer = 0f;
        float glitchTimer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime;
            glitchTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            realWorldVolume.weight = isMaskActive ? 1f - t : t;
            maskWorldVolume.weight = isMaskActive ? t : 1f - t;

            // 故障闪：瞬间拉满 → glitchDuration 内线性消退到 0（顿帧期间照常播放）
            if (glitchVolume != null)
            {
                glitchVolume.weight = glitchTimer < glitchDuration
                    ? Mathf.Clamp01(1f - glitchTimer / glitchDuration)
                    : 0f;
            }
            yield return null;
        }
        realWorldVolume.weight = isMaskActive ? 0f : 1f;
        maskWorldVolume.weight = isMaskActive ? 1f : 0f;
        if (glitchVolume != null) glitchVolume.weight = 0f;
    }
}
