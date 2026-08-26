using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 维度独占物体。
/// 根据玩家当前是否戴面具（表/里世界）控制物体的显隐和碰撞。
/// 支持 Tilemap（Tilemap + TilemapRenderer）和 SpriteRenderer 两种渲染类型。
///
/// showWhenMaskActive = true  → 里世界显示（戴面具时）
/// showWhenMaskActive = false → 表世界显示（未戴面具时）
/// </summary>
public class MaskObject : MonoBehaviour
{
    [Tooltip("勾选 = 里世界显示（戴面具时）；不勾选 = 表世界显示（未戴面具时）")]
    [SerializeField] private bool showWhenMaskActive = true;

    /// <summary>当前物体所属维度（供碰撞过滤器查询）。</summary>
    public bool ShowWhenMaskActive => showWhenMaskActive;

    // --- Tilemap 相关（可选） ---
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapRenderer tilemapRenderer;
    private Color originalColor = Color.white;
    private bool hasOriginalColor = false;

    // --- SpriteRenderer（可选） ---
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Tilemap（可选，没有也不报错）
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        if (tilemap != null)
        {
            originalColor = tilemap.color;
            hasOriginalColor = true;
        }

        // SpriteRenderer（可选）
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        SetActiveState(PlayerController.IsMaskActiveGlobally);
    }

    private void OnEnable()
    {
        WorldState.OnMaskStateChanged += HandleMaskStateChanged;
        WorldState.OnMaskPreviewChanged += HandleMaskPreviewChanged;
        // 每次激活（包括对象池复用）都更新一次显隐，与被回收前的状态同步
        SetActiveState(PlayerController.IsMaskActiveGlobally);
    }

    private void OnDisable()
    {
        WorldState.OnMaskStateChanged -= HandleMaskStateChanged;
        WorldState.OnMaskPreviewChanged -= HandleMaskPreviewChanged;
    }

    /// <summary>
    /// 统一设置显隐状态。
    /// </summary>
    private void SetActiveState(bool isMaskActive)
    {
        bool shouldShow = (isMaskActive == showWhenMaskActive);

        if (tilemapRenderer != null)
            tilemapRenderer.enabled = shouldShow;

        if (spriteRenderer != null)
            spriteRenderer.enabled = shouldShow;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        SetActiveState(isMaskActive);
    }

    private void HandleMaskPreviewChanged(bool isPreviewing)
    {
        if (!PlayerController.IsMaskActiveGlobally)
        {
            if (isPreviewing)
            {
                // ===== 进入预览（全屏虚影路径预判） =====

                // 显示Tilemap并调半透明
                if (tilemapRenderer != null)
                    tilemapRenderer.enabled = true;
                if (hasOriginalColor && tilemap != null)
                {
                    Color ghost = originalColor;
                    ghost.a = showWhenMaskActive ? 0.4f : 0.3f;
                    tilemap.color = ghost;
                }

                // 显示SpriteRenderer并调半透明
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                    Color c = spriteRenderer.color;
                    c.a = showWhenMaskActive ? 0.4f : 0.3f;
                    spriteRenderer.color = c;
                }
            }
            else
            {
                // ===== 退出预览 恢复 =====

                // Tilemap恢复
                if (hasOriginalColor && tilemap != null)
                    tilemap.color = originalColor;
                if (tilemapRenderer != null)
                    tilemapRenderer.enabled = !showWhenMaskActive;

                // SpriteRenderer恢复
                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    c.a = 1f;
                    spriteRenderer.color = c;
                    spriteRenderer.enabled = !showWhenMaskActive;
                }
            }
        }
    }
}
