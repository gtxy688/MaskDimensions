using UnityEngine;
using Cinemachine;

/// <summary>
/// 关卡管理器（单例）。负责房间切换、相机控制、子弹生成器启停。
/// </summary>
public class RoomManager : SingletonMono<RoomManager>
{
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
    /// 玩家到达出口时调用。
    /// </summary>
    public void OnRoomCleared(RoomTrigger nextRoom)
    {
        Debug.Log($"关卡 {currentRoom?.roomConfig?.roomName} 完成！");
        // 等玩家实际走进下一个房间的触发器时自然触发 EnterRoom
    }

    private void ActivateRoom(RoomTrigger trigger)
    {
        // 切换相机：启用当前房间 vcam
        if (trigger.roomVcam != null)
        {
            trigger.roomVcam.gameObject.SetActive(true);
        }

        // 传送玩家到入口
        if (trigger.playerSpawnPos != null && player != null)
        {
            player.transform.position = trigger.playerSpawnPos.position;
        }

        // 激活房间子物体（如 BulletSpawner）
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 切换 BGM
        if (trigger.roomConfig != null && trigger.roomConfig.bgmOverride != null)
        {
            Debug.Log($"切换 BGM 为：{trigger.roomConfig.bgmOverride.name}");
        }

        Debug.Log($"进入房间：{trigger.roomConfig?.roomName}");
    }

    private void DeactivateRoom(RoomTrigger trigger)
    {
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
