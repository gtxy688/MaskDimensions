using UnityEngine;

/// <summary>
/// 通关触发器。挂载在 Trigger Collider 上，玩家触碰时冻结玩家并显示 EndPanel。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EndGameTrigger : MonoBehaviour
{
    [Header("是否仅触发一次")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;

        // 冻结玩家输入
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            player.isDead = true;

        UIManager.Instance.ShowPanel<EndPanel>();
    }
}