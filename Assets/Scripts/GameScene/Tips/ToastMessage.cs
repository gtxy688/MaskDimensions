using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 通用浮动通知。挂载到 Canvas 下的一个子物体上。
/// 自动监听房间切换、理智耗尽等事件并在屏幕上显示文本。
///
/// 使用方式：在 Canvas 下建一个空物体 → 加 TextMeshPro + CanvasGroup + 本脚本
/// 也可通过 ToastMessage.Show("自定义消息") 在任意地方调用。
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(TextMeshProUGUI))]
public class ToastMessage : MonoBehaviour
{
    [Header("自动引用")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI toastText;

    [Header("显示参数")]
    public float displayDuration = 2.5f;
    public float fadeDuration = 0.5f;

    private static ToastMessage _instance;
    private Coroutine currentRoutine;

    private void Awake()
    {
        _instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (toastText == null) toastText = GetComponent<TextMeshProUGUI>();
        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        RoomManager.OnRoomEntered += OnRoomEntered;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomEntered -= OnRoomEntered;
    }

    // ===================================================================
    // 事件处理
    // ===================================================================
    private void OnRoomEntered(string roomName)
    {
        Show(roomName);
    }

    // ===================================================================
    // 公开静态方法：任何地方都可以直接调用
    // ===================================================================
    public static void Show(string message)
    {
        if (_instance != null)
            _instance.ShowInternal(message);
        else
            Debug.LogWarning("ToastMessage: 场景中未找到实例，无法显示：" + message);
    }

    private void ShowInternal(string message)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ToastRoutine(message));
    }

    // ===================================================================
    // 淡入 → 停留 → 淡出
    // ===================================================================
    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;

        // 淡入
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 停留
        yield return new WaitForSeconds(displayDuration);

        // 淡出
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
