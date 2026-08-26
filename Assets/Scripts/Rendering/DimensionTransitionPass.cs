using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 维度切换全屏转场 Pass。
/// 为什么源→临时RT→源两次 Blit：同一 RenderTarget 原地读写是未定义行为（读取发生在采样时，写入发生在光栅化时，顺序不可控），
/// 必须经由临时 RT 中转；这也是本任务简报原稿的修正点之一。
/// </summary>
public class DimensionTransitionPass : ScriptableRenderPass
{
    private const string k_CmdName = "DimensionTransition";
    private static readonly int s_ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int s_ModeId = Shader.PropertyToID("_Mode");
    private static readonly int s_DirectionId = Shader.PropertyToID("_Direction");
    private static readonly int s_WidthId = Shader.PropertyToID("_Width");
    private static readonly int s_BlockCountId = Shader.PropertyToID("_BlockCount");
    private static readonly int s_AmplitudeId = Shader.PropertyToID("_Amplitude");
    private static readonly int s_DarkenId = Shader.PropertyToID("_Darken");

    private Material material;

    // 每帧由 Feature.Setup 写入
    private float progress;
    private float mode;
    private Vector2 direction;
    private float width;
    private float blockCount;
    private float amplitude;
    private float darken;

    public DimensionTransitionPass()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        Shader shader = Shader.Find("Hidden/DimensionTransition");
        if (shader != null)
        {
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
    }

    public void Setup(float progress, float mode, Vector2 direction, float width, float blockCount, float amplitude, float darken)
    {
        this.progress = progress;
        this.mode = mode;
        this.direction = direction;
        this.width = width;
        this.blockCount = blockCount;
        this.amplitude = amplitude;
        this.darken = darken;
    }

    public void DisposeRT()
    {
        if (material != null)
        {
            CoreUtils.Destroy(material);
            material = null;
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 播放范围外零开销（也防残留：Feature 静态进度复位为 0 后不再画）
        if (material == null || progress <= 0.0001f || progress >= 0.9999f)
        {
            return;
        }

        material.SetFloat(s_ProgressId, progress);
        material.SetFloat(s_ModeId, mode);
        material.SetVector(s_DirectionId, direction);
        material.SetFloat(s_WidthId, width);
        material.SetFloat(s_BlockCountId, blockCount);
        material.SetFloat(s_AmplitudeId, amplitude);
        material.SetFloat(s_DarkenId, darken);

        CommandBuffer cmd = CommandBufferPool.Get(k_CmdName);
        using (new ProfilingScope(cmd, new ProfilingSampler(k_CmdName)))
        {
            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            // 老式 blit 链：源 → 临时 RT（shader 扫屏/撕裂）→ 拷回源。
            // 为什么临时 RT：同一目标原地读写未定义；GetTemporaryRT 由核心 API 托管生命周期。
            int tmpId = Shader.PropertyToID("_DimensionTransitionTmp");
            cmd.GetTemporaryRT(tmpId, desc, FilterMode.Bilinear);
            cmd.Blit(source, tmpId, material, 0);
            cmd.Blit(tmpId, source);
            cmd.ReleaseTemporaryRT(tmpId);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}