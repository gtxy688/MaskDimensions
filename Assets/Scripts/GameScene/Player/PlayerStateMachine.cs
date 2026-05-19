/// <summary>
/// 状态机核心逻辑类。
/// 负责维护当前状态实例的引用，并执行状态切换时的生命周期方法。
/// </summary>
public class PlayerStateMachine
{
    public BaseState CurrentState { get; private set; }

    /// <summary>
    /// 初始化状态机。
    /// </summary>
    /// <param name="startingState">初始启动状态</param>
    public void Initialize(BaseState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    /// <summary>
    /// 执行状态转换。
    /// 触发当前状态的退出逻辑，并调用新状态的进入逻辑。
    /// </summary>
    /// <param name="newState">目标状态实例</param>
    public void ChangeState(BaseState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}