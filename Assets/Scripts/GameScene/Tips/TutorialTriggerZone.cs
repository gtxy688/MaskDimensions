using UnityEngine;

/// <summary>
/// 教学触发区域。挂载在地刺前的 Trigger Collider 上。
/// 玩家进入时通知 InteractiveTutorialHUD 显示 J 键提示。
/// 自动查找 HUD，无需手动拖拽。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialTriggerZone : MonoBehaviour
{
    [Header("教学提示（可选拖拽，为空时自动查找）")]
    [SerializeField] private InteractiveTutorialHUD tutorialHUD;

    [Header("是否仅触发一次（默认勾选）")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        if (tutorialHUD == null)
            tutorialHUD = FindFirstObjectByType<InteractiveTutorialHUD>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        if (tutorialHUD != null)
            tutorialHUD.ShowJHint();
        else
            Debug.LogWarning("TutorialTriggerZone: 场景中找不到 InteractiveTutorialHUD！");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
#endif
}
