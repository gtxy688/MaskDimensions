using UnityEngine;

/// <summary>
/// 起跳状态。
/// 处理垂直初速度的赋予，并在到达最高点后转换至下落状态。
/// </summary>
public class JumpState : BaseState
{
    public JumpState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.Anim.Play("Jump");
        player.RB.velocity = new Vector2(player.RB.velocity.x, player.JumpForce);
    }

    public override void LogicUpdate()
    {
        // 当垂直速度衰减至负值时，物理引擎接管自由落体，逻辑层切入下落状态
        if (player.RB.velocity.y < 0f)
        {
            player.TransitionTo(PlayerStateId.Fall);
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