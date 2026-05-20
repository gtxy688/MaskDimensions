using UnityEngine;

/// <summary>
/// 面具维度切换状态（0.5秒硬直，变身期间不可做其他动作）
/// </summary>
public class MaskSwitchState : BaseState
{
    private float switchTimer;
    private float switchDuration = 0.5f; // 0.5秒变身硬直时间

    public MaskSwitchState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        switchTimer = 0f;

        // 1. 调用 Controller 中的核心方法，触发物理忽略和视觉广播
        //    并立即刷新地面检测结果（切换图层后碰撞检测结果可能变化）
        player.ToggleMaskDimension();
        player.RefreshGrounded();

        // 2. 仅在地面时停止玩家水平移动（空中变身不应瞬间清除横向动量）
        if (player.IsGrounded)
        {
            player.RB.velocity = new Vector2(0f, player.RB.velocity.y);
        }

        // 3. (可选) 如果有变身动画，在这里播放
        if (player.Anim != null)
        {
            player.Anim.Play("MaskSwitch");
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switchTimer += Time.deltaTime;

        // 硬直时间结束，退出变身状态并选择合适后续状态
        if (switchTimer >= switchDuration)
        {
            // 以最新的地面检测结果为准（可能在变身期间发生变化）
            player.RefreshGrounded();

            if (player.IsGrounded)
            {
                if (Mathf.Abs(player.MoveInput) > 0.1f)
                {
                    player.TransitionTo(PlayerStateId.Move);
                }
                else
                {
                    player.TransitionTo(PlayerStateId.Idle);
                }
            }
            else
            {
                player.TransitionTo(PlayerStateId.Fall);
            }
        }
    }
}