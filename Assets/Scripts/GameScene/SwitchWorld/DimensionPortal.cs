using System.Collections;
using UnityEngine;

/// <summary>
/// 局部维度视窗（Dimension Portal / Lens）控制器。
/// 基于 GPU Stencil 模板测试，在玩家长按 J 键进行战术定身/透视时，
/// 在角色周围展开一个平滑缩放的圆形 Mask 视窗，透视里世界的隐藏平台与怪物。
/// </summary>
public class DimensionPortal : MonoBehaviour
{
    [Header("组件与目标绑定")]
    [SerializeField] private Transform portalVisual;     // 挂载了 DimensionPortalMask 材质的圆形 Mesh/Sprite
    [SerializeField] private Transform followTarget;     // 跟随目标（通常为 Player）
    [SerializeField] private ParticleSystem rimParticles;// 视窗边缘光效/粒子（可选）

    [Header("视窗参数")]
    [SerializeField] private float maxPortalRadius = 4.5f;   // 战术透视时的最大半径
    [SerializeField] private float expandSpeed = 8f;        // 展开速度
    [SerializeField] private float collapseSpeed = 12f;     // 收缩速度

    private Vector3 targetScale = Vector3.zero;
    private bool isExpanding = false;

    private void Awake()
    {
        if (portalVisual != null)
        {
            portalVisual.localScale = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        PlayerController.OnMaskPreviewChanged += HandleMaskPreviewChanged;
        PlayerController.OnMaskStateChanged += HandleMaskStateChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnMaskPreviewChanged -= HandleMaskPreviewChanged;
        PlayerController.OnMaskStateChanged -= HandleMaskStateChanged;
    }

    private void LateUpdate()
    {
        // 保持视窗跟随玩家中心
        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }

        // 平滑缩放视窗 Mesh
        if (portalVisual != null)
        {
            float speed = isExpanding ? expandSpeed : collapseSpeed;
            portalVisual.localScale = Vector3.Lerp(
                portalVisual.localScale, 
                targetScale, 
                Time.unscaledDeltaTime * speed
            );
        }
    }

    private void HandleMaskPreviewChanged(bool isPreviewing)
    {
        isExpanding = isPreviewing;
        targetScale = isPreviewing ? Vector3.one * maxPortalRadius : Vector3.zero;

        if (rimParticles != null)
        {
            if (isPreviewing) rimParticles.Play();
            else rimParticles.Stop();
        }
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        // 发生全局维度切换时，快速收拢视窗
        isExpanding = false;
        targetScale = Vector3.zero;
        if (portalVisual != null)
        {
            portalVisual.localScale = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, maxPortalRadius);
    }
}
