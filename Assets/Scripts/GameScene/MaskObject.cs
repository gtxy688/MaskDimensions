using UnityEngine;

/// <summary>
/// 里世界独占物体
/// 作为监听者，监听玩家面具状态的切换事件。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MaskObject : MonoBehaviour
{
    [Tooltip("可选：手动指定 Renderer，默认使用本对象上的 Renderer（例如 TilemapRenderer）")]
    [SerializeField] private Renderer targetRenderer;
    [Header("显示设置")]
    [Tooltip("如果为 true，则在玩家戴上面具时显示；为 false 则在未戴面具时显示（用于旧世界物体）")]
    [SerializeField] private bool showWhenMaskActive = true;

    private void Awake()
    {
        // 如果 Inspector 没手动指定 Renderer，尝试从当前对象获取一个
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        // 根据当前玩家的面具状态初始化可见性：
        // - 如果能找到 PlayerController，则以 player.isMaskActive 决定显隐
        // - 否则做保守默认：旧世界物体（showWhenMaskActive==false）可见，新世界物体不可见
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            targetRenderer.enabled = (player.isMaskActive == showWhenMaskActive);
        }
        else
        {
            targetRenderer.enabled = !showWhenMaskActive;
        }
    }

    private void OnEnable()
    {
        // 脚本启用时，监听大喇叭频道的广播
        PlayerController.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        // 物体销毁或禁用时，取消监听，防止内存泄漏！
        PlayerController.OnMaskStateChanged -= HandleMaskStateChanged;
    }

    /// <summary>
    /// 收到玩家“面具状态改变”的广播后执行
    /// </summary>
    private void HandleMaskStateChanged(bool isMaskActive)
    {
        // 这里只控制渲染的显示和隐藏，绝不使用 gameObject.SetActive
        // 物理碰撞层面的开关，已经由 PlayerController 统一指挥 Box2D 物理引擎完成了
        // 支持两类物体：showWhenMaskActive==true 的物体在戴面具时显示；为 false 的物体在未戴面具时显示
        if (targetRenderer != null)
        {
            targetRenderer.enabled = (isMaskActive == showWhenMaskActive);
        }

        // TODO: 以后如果要加高级效果，可以直接加在这里，比如：
        // if(isMaskActive) 播放出现粒子特效() / 播放渐显动画();
    }
}