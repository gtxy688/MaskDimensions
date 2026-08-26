using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 维度切换转场 Renderer Feature：在渲染队列插入全屏转场 Pass。
/// 为什么静态 progress：本组件挂在 RendererData 资产上（非场景对象），场景里无法直接引用；
/// 单人游戏里用静态桥与"静态事件"同理（AGENTS.md 允许单人场景保留静态），由 DimensionTransitionPlayer 写入。
/// 为什么 AfterRenderingPostProcessing：转场是"全局演出层"，应盖在两个世界的滤镜之上、不被噪点/暗化污染。
/// </summary>
public class DimensionTransitionFeature : ScriptableRendererFeature
{
    /// <summary>转场播放进度（0=未播放/已结束，1=播完）——由场景侧播放器写入。</summary>
    public static float Progress;

    /// <summary>模式强制覆盖（-1=用序列化 mode，>=0=播放器写入），播放器切换撕裂/扫屏用。</summary>
    public static float ModeOverride = -1f;

    [SerializeField] private float mode = 1f;            // 0=扫屏 1=撕裂
    [SerializeField] private Vector2 direction = Vector2.down; // 扫屏方向
    [SerializeField] private float width = 0.15f;        // 扫屏光带宽度
    [SerializeField] private float blockCount = 10f;     // 撕裂块数
    [SerializeField] private float amplitude = 0.12f;    // 撕裂错位幅度
    [SerializeField] private float darken = 0.85f;       // 扫过区压暗

    private DimensionTransitionPass pass;

    public override void Create()
    {
        pass = new DimensionTransitionPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null)
        {
            return;
        }
        pass.Setup(Progress, ModeOverride >= 0f ? ModeOverride : mode, direction, width, blockCount, amplitude, darken);
        pass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (pass != null)
        {
            pass.DisposeRT();
            pass = null;
        }
    }
}