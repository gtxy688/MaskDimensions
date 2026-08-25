using UnityEngine;

/// <summary>
/// 具体状态：受击硬直（Hit）。
/// 处理受击时的停顿、击退（Knockback）衰减与硬直时间结束后的状态回归。
/// </summary>
public class HitState : BaseState
{
    private float hitTimer = 0f;
    private const float HitStunDuration = 0.2f;

    public HitState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        hitTimer = 0f;
        if (player.Anim != null)
        {
            player.Anim.Play("Hit");
        }
    }

    public override void LogicUpdate()
    {
        hitTimer += Time.deltaTime;

        if (hitTimer >= HitStunDuration)
        {
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

    public override void PhysicsUpdate()
    {
        // 硬直期间水平速度阻尼衰减
        if (player.RB != null)
        {
            player.RB.velocity = new Vector2(
                Mathf.MoveTowards(player.RB.velocity.x, 0f, 15f * Time.fixedDeltaTime),
                player.RB.velocity.y
            );
        }
    }
}
