using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 里世界独占物体（Tilemap 版本）
/// 仅使用 Tilemap.color 控制透视半透明效果，使用 TilemapRenderer.enabled 控制显隐。
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class MaskObject : MonoBehaviour
{
    [Tooltip("勾选代表它是里世界物体(戴面具显示)；不勾选代表它是表世界物体(戴面具隐藏)")]
    [SerializeField] private bool showWhenMaskActive = true;

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private bool hasOriginalColor = false;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();

        if (tilemap != null)
        {
            originalColor = tilemap.color;
            hasOriginalColor = true;
        }
    }

    private void Start()
    {
        // 游戏开始时，根据全局静态变量初始化显隐
        if (tilemapRenderer != null)
            tilemapRenderer.enabled = (PlayerController.IsMaskActiveGlobally == showWhenMaskActive);
    }

    private void OnEnable()
    {
        // 脚本启用时，监听广播
        PlayerController.OnMaskStateChanged += HandleMaskStateChanged;
        PlayerController.OnMaskPreviewChanged += HandleMaskPreviewChanged;
    }

    private void OnDisable()
    {
        PlayerController.OnMaskStateChanged -= HandleMaskStateChanged;
        PlayerController.OnMaskPreviewChanged -= HandleMaskPreviewChanged;
    }

    private void HandleMaskStateChanged(bool isMaskActive)
    {
        // 使用 TilemapRenderer 控制显隐
        if (tilemapRenderer != null)
            tilemapRenderer.enabled = (isMaskActive == showWhenMaskActive);
    }

    private void HandleMaskPreviewChanged(bool isPreviewing)
    {
        // 仅对里世界物体在透视时显示半透明
        if (!showWhenMaskActive) return;

        if (isPreviewing && !PlayerController.IsMaskActiveGlobally)
        {
            if (tilemapRenderer != null) tilemapRenderer.enabled = true;
            if (hasOriginalColor && tilemap != null)
            {
                Color ghost = originalColor;
                ghost.a = 0.4f;
                tilemap.color = ghost;
            }
        }
        else if (!isPreviewing && !PlayerController.IsMaskActiveGlobally)
        {
            // 恢复并隐藏
            if (hasOriginalColor && tilemap != null) tilemap.color = originalColor;
            if (tilemapRenderer != null) tilemapRenderer.enabled = false;
        }
    }
}
