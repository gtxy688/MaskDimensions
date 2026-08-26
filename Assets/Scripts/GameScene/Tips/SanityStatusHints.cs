using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 理智值 UI 下方的文字提示：
///   #3 第一次进入异界  — "在异界会持续消耗理智"
///   #7 理智耗尽强制回  — "理智耗尽，强制回到普通世界"
///   #4 理智耗尽回到后  — "回到普通世界后理智会逐渐回升"
///   #5 理智不足以切换  — "理智不足，无法进入异界"
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SanityStatusHints : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("动画参数")]
    public float fadeDuration = 0.4f;
    public float displayDuration = 2f;

    // 内部状态
    private bool hasShownVoidHint = false;      // #3 只触发一次
    private bool hasShownRecoveryHint = false;   // #4 只触发一次
    private bool sanityWasDepleted = false;      // 标记理智曾耗尽

    private Coroutine currentHintRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (hintText == null) hintText = GetComponent<TextMeshProUGUI>();
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += OnMaskStateChanged;
        PlayerController.OnSanityChanged += OnSanityChanged;
        PlayerController.OnInsufficientSanity += OnInsufficientSanity;
        PlayerController.OnSanityForcedRecovery += OnSanityForcedRecovery;
        PlayerController.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= OnMaskStateChanged;
        PlayerController.OnSanityChanged -= OnSanityChanged;
        PlayerController.OnInsufficientSanity -= OnInsufficientSanity;
        PlayerController.OnSanityForcedRecovery -= OnSanityForcedRecovery;
        PlayerController.OnPlayerDied -= OnPlayerDied;
    }

    // 玩家死亡时清除理智耗尽标记，防止复活后误触恢复提示(#4)
    private void OnPlayerDied()
    {
        sanityWasDepleted = false;
    }

    // 理智耗尽强制返回时，在 OnMaskStateChanged(false) 之前标记
    // 确保恢复提示(#4)能正确触发
    private void OnSanityForcedRecovery()
    {
        sanityWasDepleted = true;
    }

    // #3 第一次进入异界 → "在异界会持续消耗理智"
    // #4 理智耗尽强制回到 → "理智耗尽，强制回到普通世界\n回到普通世界后理智会逐渐回升"
    private void OnMaskStateChanged(bool isMaskActive)
    {
        if (isMaskActive && !hasShownVoidHint)
        {
            hasShownVoidHint = true;
            ShowHint("在异界会持续消耗理智");
        }
        else if (!isMaskActive && sanityWasDepleted && !hasShownRecoveryHint)
        {
            hasShownRecoveryHint = true;
            sanityWasDepleted = false;
            ShowHint("理智耗尽，强制回到普通世界\n回到普通世界后理智会逐渐回升");
        }
    }

    // 跟踪理智是否曾经耗尽（为 #4 准备）
    private void OnSanityChanged(float current, float max)
    {
        if (current <= 0f)
            sanityWasDepleted = true;
    }


    // #5：理智不足以切换世界时提示
    private void OnInsufficientSanity()
    {
        ShowHint("理智不足30，无法预览和进入异界");
    }

    // 通用提示
    private void ShowHint(string text)
    {
        if (currentHintRoutine != null)
            StopCoroutine(currentHintRoutine);
        currentHintRoutine = StartCoroutine(HintRoutine(text));
    }

    private IEnumerator HintRoutine(string text)
    {
        hintText.text = text;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
