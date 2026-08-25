/// <summary>
/// 可池化对象生命周期接口。
/// 实现该接口的组件在从对象池取出（OnSpawn）和归还（OnDespawn）时会收到回调，以重置内部状态。
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 当对象从池中取出被激活时调用。用于重置计时器、速度、状态等。
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// 当对象即将归还至池中时调用。用于清理特效、解绑事件、停止协程等。
    /// </summary>
    void OnDespawn();
}
