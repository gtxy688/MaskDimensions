using UnityEngine;

/// <summary>
/// 具体状态：死亡（Die）。
/// 冻结输入控制，隐藏渲染，并等待重生协程触发完成。
/// </summary>
public class DieState : BaseState
{
    public DieState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        // 死亡进入逻辑：清零速度（自研入口）并冻结事件总线
        // （RB.simulated 方案 B 下仅停用 Unity 回调，不参与求解）
        player.SetVelocity(Vector2.zero);
        if (player.RB != null)
        {
            player.RB.simulated = false;
        }
    }

    public override void Exit()
    {
        if (player.RB != null)
        {
            player.RB.simulated = true;
        }
    }

    public override void LogicUpdate()
    {
        // 死亡期间拦截所有常规状态流转
    }
}
