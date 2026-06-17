using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键替换场景中所有 TextMeshProUGUI 的字体。
/// 菜单：Tools → 替换 TMP 字体
/// </summary>
public class ReplaceTMPFont : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/替换 TMP 字体")]
    private static void Open()
    {
        GetWindow<ReplaceTMPFont>("替换 TMP 字体");
    }

    private void OnGUI()
    {
        GUILayout.Label("选择要统一替换的目标字体：", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(targetFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);

        if (GUILayout.Button("替换当前场景所有 TMP 文本", GUILayout.Height(40)))
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择目标字体！", "确定");
                return;
            }

            int count = ReplaceAllInScene();
            EditorUtility.DisplayDialog("完成", $"已替换 {count} 个 TMP 文本组件的字体。", "确定");
        }
    }

    private int ReplaceAllInScene()
    {
        int count = 0;

        // 处理当前场景所有 GameObject（包括隐藏的）
        var allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var text in allTexts)
        {
            if (text.font != targetFont)
            {
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        // 也处理非 UI 的 TextMeshPro（可能在 3D 空间用）
        var allTMP = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
        foreach (var text in allTMP)
        {
            if (text.font != targetFont)
            {
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        return count;
    }
}
