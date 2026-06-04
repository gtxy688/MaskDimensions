using UnityEngine;
using UnityEngine.UI;

public class SanityUIManager : MonoBehaviour
{
    [SerializeField] private Slider sanitySlider;

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
    }
}