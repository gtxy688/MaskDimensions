using UnityEngine;
using System; 

/// <summary>
/// 关卡管理器（单例）。负责房间切换、相机控制、子弹生成器启停。
/// </summary>
public class RoomManager : SingletonMono<RoomManager>
{
    // 房间切换时广播（参数 = 房间名字），供 ToastMessage ui显示使用
    public static event Action<string> OnRoomEntered;

    private RoomTrigger currentRoom;
    private GameObject player;

    protected override void Awake()
    {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>
    /// 玩家进入房间时调用。
    /// </summary>
    public void EnterRoom(RoomTrigger trigger)
    {
        if (currentRoom == trigger) return; // 已在该房间中

        // 1. 退出上一个房间（如果有）
        if (currentRoom != null)
        {
            DeactivateRoom(currentRoom);
        }

        // 2. 进入新房间
        currentRoom = trigger;
        ActivateRoom(trigger);
    }

    /// <summary>
    /// 玩家到达出口时调用。直接切换到下一个房间。
    /// </summary>
    public void OnRoomCleared(RoomTrigger nextRoom)
    {
        EnterRoom(nextRoom);
    }

    private void ActivateRoom(RoomTrigger trigger)
    {
        // 切换相机：启用当前房间 vcam
        if (trigger.roomVcam != null)
        {
            trigger.roomVcam.gameObject.SetActive(true);
        }

        // 传送玩家并重置状态
        if (trigger.playerSpawnPos != null && player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.ResetForNewRoom(trigger.playerSpawnPos);
        }

        // 激活房间子物体（如 BulletSpawner）
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(true);
        }
        // 新增：广播房间名给 UI
        OnRoomEntered?.Invoke(trigger.roomConfig?.roomName ?? "未知房间");
    }

    private void DeactivateRoom(RoomTrigger trigger)
    {
        // 禁用房间相机
        if (trigger.roomVcam != null)
        {
            trigger.roomVcam.gameObject.SetActive(false);
        }

        // 停用房间子物体（如 BulletSpawner）
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
