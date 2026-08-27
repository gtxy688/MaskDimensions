using UnityEngine;

/// <summary>
/// 移动平台（任务 8）：简单往返移动，供 KinematicBody 检测"脚下是移动平台"并跟随。
/// 为什么位置在 Update 更新：平台是纯表现/关卡物体，渲染帧级移动即可；玩家跟随的位移差
/// 由 KinematicBody 用自己的"上次平台位置"缓存计算（不依赖本组件的回调顺序，见 KinematicBody.Move）。
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 moveAxis = Vector2.right; // 移动轴（归一向量）
    [SerializeField] private float moveDistance = 3f;          // 往返总距离
    [SerializeField] private float speed = 2f;                 // 单位/秒

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // PingPong：0↔1 平滑往返（基于缩放时间；顿帧时平台也停——与游戏世界一致）
        float t = Mathf.PingPong(Time.time * speed, moveDistance) / moveDistance;
        transform.position = startPos + (Vector3)(moveAxis.normalized * (t * moveDistance));
    }
}