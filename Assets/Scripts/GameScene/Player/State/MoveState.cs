using UnityEngine;

/// <summary>
/// 具体状态：移动。
/// 负责处理基于输入的物理位移运算与 Transform 翻转。
/// </summary>
public class MoveState : BaseState
{
    public MoveState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Anim.Play("Move");
    }

    public override void LogicUpdate()
    {
        if (!player.IsGrounded && player.RB.velocity.y < 0f)
        {
            player.TransitionTo(PlayerStateId.Fall);
            return;
        }
        // 判定跳跃
        if (player.JumpBufferCounter > 0f && player.CoyoteTimeCounter > 0f)
        {
            player.ConsumeJump(); // 消耗掉跳跃指令
            player.TransitionTo(PlayerStateId.Jump);
            return;
        }

        if (Mathf.Abs(player.MoveInput) <= 0.1f)
        {
            player.TransitionTo(PlayerStateId.Idle);
        }
    }

    public override void PhysicsUpdate()
    {
        player.RB.velocity = new Vector2(player.MoveInput * player.MoveSpeed, player.RB.velocity.y);
        player.UpdateFacingDirection();
    }

}