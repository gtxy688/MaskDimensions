using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SanityUIManager : MonoBehaviour
{
    [SerializeField] private Slider sanitySlider;
    [SerializeField] private TextMeshProUGUI sanityValueText; // 新增：显示 "86 / 100"

    private void Awake()
    {
        if (sanitySlider == null)
            sanitySlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        // 监听玩家理智值变化事件
        PlayerController.OnSanityChanged += UpdateSanityUI;
    }

    private void OnDisable()
    {
        // 移除监听，防止内存泄漏
        PlayerController.OnSanityChanged -= UpdateSanityUI;
    }

    private void UpdateSanityUI(float current, float max)
    {
        if (sanitySlider != null && max > 0)
        {
            // 计算百分比并更新 Slider
            sanitySlider.value = current / max;
        }

        // 新增：更新数值文本
        if (sanityValueText != null)
        {
            sanityValueText.text = $"{current:F0} / {max:F0}";
        }
    }
}