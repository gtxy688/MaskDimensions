using UnityEngine;

/// <summary>
/// 传送触发器。挂载在 Trigger Collider 上。
/// 玩家触碰后调用 PlayerController.TeleportTo() 播放消散 → 传送 → 凝聚动画。
/// 使用方式：在场景中放置一个 GameObject → 挂载 Collider2D(设为 Trigger) → 挂载本脚本 →
/// 将目标位置的 Transform 拖入 Destination。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TeleportTrigger : MonoBehaviour
{
    [Header("传送目标")]
    [SerializeField] private Transform destination;

    [Header("是否仅触发一次")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (destination == null)
            Debug.LogError($"[TeleportTrigger] {gameObject.name}: Destination 未设置！");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || player.isDead) return;

        hasTriggered = true;
        player.TeleportTo(destination);
    }
}