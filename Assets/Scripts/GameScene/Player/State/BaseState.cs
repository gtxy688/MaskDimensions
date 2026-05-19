using UnityEngine;

/// <summary>
/// 玩家状态抽象基类。
/// 定义标准的状态生命周期接口，供具体状态类实现。
/// </summary>
public abstract class BaseState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;

    /// <summary>
    /// 构造函数，通过依赖注入接收上下文引用。
    /// </summary>
    public BaseState(PlayerController player, PlayerStateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    /// <summary>
    /// 生命周期：状态进入时调用。
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// 生命周期：状态退出时调用。
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// 生命周期：物理帧更新，用于处理刚体力学运算。
    /// </summary>
    public virtual void PhysicsUpdate() { }

    /// <summary>
    /// 生命周期：逻辑帧更新，用于处理输入检测与状态流转。
    /// </summary>
    public virtual void LogicUpdate() { }

}