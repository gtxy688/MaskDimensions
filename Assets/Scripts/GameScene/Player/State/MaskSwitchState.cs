using UnityEngine;

/// <summary>
/// 面具维度切换状态（0.5秒硬直）
/// </summary>
public class MaskSwitchState : BaseState
{
    private float switchTimer;
    private float switchDuration = 0.5f; // 0.5秒变身硬直

    public MaskSwitchState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        switchTimer = switchDuration;

        // 1. 瞬间停住角色 (X和Y速度清零)
        player.RB.velocity = Vector2.zero;

        //// 可选：为了保证在空中切换时绝对悬停，不受重力影响掉落
        //player.RB.gravityScale = 0f;

        Debug.Log("进入面具切换状态，角色硬直...");
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 2. 开始 0.5 秒倒计时
        switchTimer -= Time.deltaTime;

        if (switchTimer <= 0f)
        {
            // 3. 时间到！触发维度的物理层切换
            player.ToggleDimension();

            // 4. 完美回退：看脚下有没有地，决定回 Idle 还是 Fall
            if (player.IsGrounded)
            {
                player.TransitionTo(PlayerStateId.Idle);
            }
            else
            {
                player.TransitionTo(PlayerStateId.Fall);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        //// 恢复重力
        //player.RB.gravityScale = 1f;
    }
}