using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 维度切换转场播放器：订阅 WorldState 切换事件，用 unscaledDeltaTime 驱动 Renderer Feature 的转场进度。
/// 为什么独立组件而非塞进 PlayerController：玩家逻辑只管输入/物理，渲染演出是纯表现层，单一职责分离（面试可讲分层）。
/// 为什么 unscaledDeltaTime：切换瞬间场景会用 timeScale=0 制造顿帧，转场必须继续播放（断片感），与 DimensionVolumeController 同理由。
/// </summary>
public class DimensionTransitionPlayer : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f;   // 转场总时长（覆盖顿帧 0.15s + 滤镜过渡 0.4s）
    [SerializeField] private bool useTearMode = true; // true=撕裂 false=扫屏

    private Coroutine routine;

    private void Start()
    {
        // 转场 shader 预热：首次绘制会触发 shader 变体编译（首次切换瞬间明显卡顿的主因之一），
        // 进场景即在离屏 RT 上画一次全屏三角形，把编译提前到玩家操作之前。
        // 为什么用独立材质实例：pass 的材质是 RendererData 资产的私有对象，场景组件拿不到；
        // shader 变体编译以 shader 为单位（与材质实例无关），即建即毁即可完成预热。
        WarmUpTransitionShader();
    }

    private void WarmUpTransitionShader()
    {
        Shader shader = Shader.Find("Hidden/DimensionTransition");
        if (shader == null)
        {
            return;
        }
        Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        mat.SetFloat("_Progress", 0.5f); // 让 frag 走撕裂分支，保证完整变体被编译
        mat.SetFloat("_Mode", 1f);

        CommandBuffer cmd = CommandBufferPool.Get("DimensionTransitionWarmUp");
        int tmpId = Shader.PropertyToID("_DimensionTransitionWarmUpRT");
        cmd.GetTemporaryRT(tmpId, 16, 16, 0, FilterMode.Bilinear);
        cmd.SetRenderTarget(tmpId);
        cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3, 1, null);
        cmd.ReleaseTemporaryRT(tmpId);
        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        Destroy(mat);
    }

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= HandleMaskStateChanged;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        // 防残留：任何中断都必须把转场进度清零
        DimensionTransitionFeature.Progress = 0f;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(PlayRoutine(isMaskActive));
    }

    private IEnumerator PlayRoutine(bool isMaskActive)
    {
        DimensionTransitionFeature.Progress = 0f;
        DimensionTransitionFeature.ModeOverride = useTearMode ? 1f : 0f;

        // 进度 0→1：撕裂模式下"从碎裂重组"（新世界在重组中显露），扫屏模式下"光边扫过揭出新世界"
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            DimensionTransitionFeature.Progress = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        // 播完归零（Pass 对 0 跳画，防残留）
        DimensionTransitionFeature.Progress = 0f;
        routine = null;
    }
}