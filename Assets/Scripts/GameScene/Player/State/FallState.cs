using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 下落状态。
/// 处理空中的下坠过程，并在接触地面碰撞体后重置为基础状态。
/// </summary>
public class FallState : BaseState
{
    public FallState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Anim.Play("Fall");
    }

    public override void LogicUpdate()
    {
        if (player.JumpBufferCounter > 0f && player.CoyoteTimeCounter > 0f)
        {
            player.ConsumeJump();
            player.TransitionTo(PlayerStateId.Jump);
            return;
        }
        // 落地检测：触发物理阻挡且地面检测盒返回 True
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
    }

    public override void PhysicsUpdate()
    {
        float targetXVel = player.MoveInput * player.MoveSpeed;
        if (player.MoveInput != 0 && player.IsTouchingWall(player.MoveInput))
            targetXVel = 0f;

        player.RB.velocity = new Vector2(targetXVel, player.RB.velocity.y);
        player.UpdateFacingDirection();
    }
}