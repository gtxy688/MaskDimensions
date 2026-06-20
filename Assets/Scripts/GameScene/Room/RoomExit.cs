using UnityEngine;

/// <summary>
/// 挂载在每个房间出口处。玩家触碰时通知 RoomManager 解锁下一个房间的入口。
/// </summary>
public class RoomExit : MonoBehaviour
{
    [Header("目标房间（可选）")]
    public RoomTrigger targetRoom;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.OnRoomCleared(targetRoom);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
#endif
}