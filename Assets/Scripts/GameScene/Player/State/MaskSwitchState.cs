//using UnityEngine;

///// <summary>
///// 面具维度切换状态（2秒延迟）
///// </summary>
//public class MaskSwitchState : BaseState
//{
//    private float switchTimer;
//    private float switchDuration = 2f; // 2秒变身延迟

//    public MaskSwitchState(PlayerController player, PlayerStateMachine stateMachine)
//        : base(player, stateMachine)
//    {
//    }

//    public override void Enter()
//    {
//        base.Enter();
//        switchTimer = 0f;

//        // 1. 调用 Controller 中的核心方法，触发物理过滤与事件广播
//        //    （任务 4 后维度翻转/广播统一走 WorldState；RefreshGrounded 已删除，
//        //      IsGrounded 改为每帧读 KinematicBody.LastResult，切换后自动反映新世界）
//        WorldState.Instance.SwitchWorld();

//        // 2. 仅在地面时停止玩家水平移动（空中变身不应瞬间清除横向动量）
//        if (player.IsGrounded)
//        {
//            player.SetVelocity(new Vector2(0f, player.Velocity.y));
//        }

//        // 3. (可选) 如果有变身动画，在这里播放
//        //if (player.Anim != null)
//        //{
//        //    player.Anim.Play("MaskSwitch");
//        //}
//    }

//    public override void LogicUpdate()
//    {
//        base.LogicUpdate();

//        switchTimer += Time.deltaTime;

//        // 硬直时间结束，退出变身状态并选择合适后续状态
//        if (switchTimer >= switchDuration)
//        {
//            // 以最新的地面检测结果为准（IsGrounded 每帧由 KinematicBody.LastResult 驱动，
//            //  切换后自动反映新世界，无需手动刷新）
//            if (player.IsGrounded)
//                if (Mathf.Abs(player.MoveInput) > 0.1f)
//                {
//                    player.TransitionTo(PlayerStateId.Move);
//                }
//                else
//                {
//                    player.TransitionTo(PlayerStateId.Idle);
//                }
//            }
//            else
//            {
//                player.TransitionTo(PlayerStateId.Fall);
//            }
//        }
//    }
//}