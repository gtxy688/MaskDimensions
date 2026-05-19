using UnityEngine;

/// <summary>
/// 具体状态：待机。
/// 负责清零水平位移速度，并检测向移动状态的转换条件。
/// </summary>
public class IdleState : BaseState
{
    public IdleState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Anim.Play("Idle"); // 单向驱动视图表现
        player.RB.velocity = new Vector2(0f, player.RB.velocity.y);
    }

    public override void LogicUpdate()
    {
        // 判定下落：不在地面且垂直速度为负（处理从边缘滑落的情况）
        if (!player.IsGrounded && player.RB.velocity.y < 0f)
        {
            player.TransitionTo(PlayerStateId.Fall);
            return;
        }

        // 判定跳跃
        if (Input.GetButtonDown("Jump") && player.IsGrounded)
        {
            player.TransitionTo(PlayerStateId.Jump);
            return;
        }

        if (Mathf.Abs(player.MoveInput) > 0.1f)
        {
            player.TransitionTo(PlayerStateId.Move);
        }
    }
}