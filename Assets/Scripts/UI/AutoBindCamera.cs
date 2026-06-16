using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动为 Canvas 绑定当前场景的 Main Camera。
/// 解决 Screen Space - Camera + Render Camera 留空时，
/// 跨场景后 Canvas 找不到相机的问题。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AutoBindCamera : MonoBehaviour
{
    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        TryBindCamera();

        // 场景加载完成后自动重新绑定
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindCamera();
    }

    private void TryBindCamera()
    {
        if (canvas != null && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }
    }
}
