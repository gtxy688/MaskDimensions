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
        player.SetVelocity(new Vector2(0f, player.Velocity.y));
    }

    public override void LogicUpdate()
    {
        // 判定下落：不在地面且垂直速度为负（处理从边缘滑落的情况）
        if (!player.IsGrounded && player.Velocity.y < 0f)
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

        if (Mathf.Abs(player.MoveInput) > 0.1f)
        {
            player.TransitionTo(PlayerStateId.Move);
        }
    }

    public override void PhysicsUpdate()
    {
        // 站定状态下也必须每固定步产生一次 Move：否则 LastResult 永远陈旧
        // （Move((0,0)) 会跳过全部射线 → IsGrounded 恒 false → 土狼计时器空转 → 站定跳跃失效）。
        // 与 Move/Jump/Fall/Hit 同构：清零水平速度，垂直速度由控制器重力积分维护。
        player.SetVelocity(new Vector2(0f, player.Velocity.y));
    }
}