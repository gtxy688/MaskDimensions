using UnityEngine;
using TMPro; // 必须引入 TextMeshPro 命名空间
using System.Collections;

/// <summary>
/// 交互式按键教学 UI。
/// 按照步骤显示按键，等待玩家真实按下对应按键后，才进入下一步。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InteractiveTutorialHUD : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("动画设置")]
    public float fadeDuration = 0.5f; // 淡入淡出所需时间

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        // 游戏开始时确保完全透明
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // 开始执行教学序列
        StartCoroutine(TutorialSequenceRoutine());
    }

    /// <summary>
    /// 核心教学序列：利用 WaitUntil 等待玩家操作
    /// </summary>
    private IEnumerator TutorialSequenceRoutine()
    {
        // === 第一步：移动教学 ===
        tutorialText.text = "[A / D] 移动";
        yield return StartCoroutine(FadeAlpha(1f)); // 等待淡入完成
        // 死死卡在这里，直到玩家按下 A 或 D 键
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D));
        yield return StartCoroutine(FadeAlpha(0f)); // 等待淡出完成

        // === 第二步：跳跃教学 ===
        tutorialText.text = "[Space] 跳跃";
        yield return StartCoroutine(FadeAlpha(1f));
        // 死死卡在这里，直到玩家按下空格键
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        yield return StartCoroutine(FadeAlpha(0f));

        // === 第三步：面具教学 ===
        tutorialText.text = "[J] 佩戴面具";
        yield return StartCoroutine(FadeAlpha(1f));
        // 死死卡在这里，直到玩家按下 J 键
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.J));
        yield return StartCoroutine(FadeAlpha(0f));

        // 教学全部结束！你可以考虑在这里彻底禁用这个 UI 物体节省性能
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 封装好的通用淡入淡出协程
    /// </summary>
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
}