using Cinemachine;
using UnityEngine;

/// <summary>
/// 挂载在每个房间入口的 Trigger Collider 上。
/// 玩家进入时通知 RoomManager 切换房间。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomTrigger : MonoBehaviour
{
    [Header("房间配置")]
    public RoomConfigSO roomConfig;

    [Header("相机")]
    public CinemachineVirtualCamera roomVcam;

    [Header("玩家生成位置")]
    public Transform playerSpawnPos;

    [Header("子物体控制")]
    public GameObject[] activateOnEnter;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.EnterRoom(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
#endif
}
