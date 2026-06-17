using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 交互式按键教学 UI。
/// 所有提示通过 StartSoloHint() 统一调度，同一时间只有一个协程在控制 alpha，
/// 从根源上杜绝多个协程争夺 canvasGroup.alpha 导致的闪烁。
///
/// 提示序列：
///   1. [A/D] 移动 → 等待输入 → [Space] 跳跃 → 等待输入
///   2. 长按[J] 预览异界 → 等待输入或超时（由 TutorialTriggerZone 触发）
///   3. 按[J] 回到普通世界 → 等待输入或离开异界或超时（进入异界后自动触发）
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InteractiveTutorialHUD : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("动画设置")]
    public float fadeDuration = 0.5f;

    [Header("J 提示设置")]
    public float jHintTimeout = 8f;

    private bool movementTutorialDone = false;
    private bool jHintDone = false;
    private bool returnHintShown = false;
    private bool returnHintCompleted = false;

    /// <summary>当前活跃的提示协程。新提示启动前会被 Stop。</summary>
    private Coroutine currentHint;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        StartSoloHint(MovementTutorialSequence());
    }

    private void OnEnable()
    {
        PlayerController.OnMaskStateChanged += OnMaskStateChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnMaskStateChanged -= OnMaskStateChanged;
    }

    // ===================================================================
    // 核心：单协程门控。杀掉上一个提示，再启动新的。
    // ===================================================================
    private void StartSoloHint(IEnumerator routine)
    {
        if (currentHint != null)
            StopCoroutine(currentHint);
        currentHint = StartCoroutine(routine);
    }

    // ===================================================================
    // 外部事件 → 通过单门控启动提示
    // ===================================================================
    public void ShowJHint()
    {
        if (jHintDone) return;
        jHintDone = true;
        StartSoloHint(JHintRoutine());
    }

    private void OnMaskStateChanged(bool isMaskActive)
    {
        if (isMaskActive && !returnHintShown)
        {
            returnHintShown = true;
            movementTutorialDone = true; // 已不需再等 Space 教学
            StartSoloHint(ReturnHintRoutine());
        }
    }

    // ===================================================================
    // 提示 1：移动 / 跳跃教学
    // ===================================================================
    private IEnumerator MovementTutorialSequence()
    {
        // --- A/D ---
        tutorialText.text = "[A / D] 移动";
        yield return FadeAlpha(1f);
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D));
        yield return FadeAlpha(0f);

        // 可能已被外部 Stop，检查一下
        if (!this.IsAlive()) yield break;

        // --- Space ---
        tutorialText.text = "[Space] 跳跃";
        yield return FadeAlpha(1f);
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        canvasGroup.alpha = 0f; // 按过立即隐藏

        movementTutorialDone = true;
        TryDisable();
    }

    // ===================================================================
    // 提示 2：J 键预览 / 切换
    // ===================================================================
    private IEnumerator JHintRoutine()
    {
        tutorialText.text = "长按[J] 预览异界\n松开 [J] 进入异界";
        yield return FadeAlpha(1f);

        float timer = 0f;
        while (timer < jHintTimeout)
        {
            if (Input.GetKeyDown(KeyCode.J)) break;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return FadeAlpha(0f);
        TryDisable();
    }

    // ===================================================================
    // 提示 3：按 J 回到普通世界
    // ===================================================================
    private IEnumerator ReturnHintRoutine()
    {
        yield return null; // 等一帧

        tutorialText.text = "按[J] 回到普通世界";
        yield return FadeAlpha(1f);

        float timer = 0f;
        while (timer < 8f)
        {
            if (Input.GetKeyDown(KeyCode.J)) break;
            if (!PlayerController.IsMaskActiveGlobally) break;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return FadeAlpha(0f);
        returnHintCompleted = true;
    }

    // ===================================================================
    // 通用
    // ===================================================================
    private void TryDisable()
    {
        if (movementTutorialDone && jHintDone && returnHintCompleted)
            gameObject.SetActive(false);
    }

    private IEnumerator FadeAlpha(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    /// <summary>检查 MonoBehaviour 是否还活着（未被 Destroy 且 enabled）。</summary>
    private bool IsAlive() => this != null && isActiveAndEnabled;
}
