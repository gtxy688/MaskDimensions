using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : BasePanel
{

    public Button btnSetting;

    /// <summary>暂停菜单开关广播（订阅方：PlayerController 屏蔽输入）。为什么事件驱动：表现层状态变化通知业务层收口接口，
    /// 避免业务侧每帧查 UI 状态（AGENTS.md 表现层"禁止每帧轮询"精神的反向适用）。</summary>
    public static event System.Action<bool> OnPauseStateChanged;

    /// <summary>暂停状态广播入口：event 只能在定义类内 Invoke，供 SettingPanel（OnDestroy 恢复）等外部触发。</summary>
    public static void BroadcastPauseState(bool paused)
    {
        OnPauseStateChanged?.Invoke(paused);
    }

    public override void Init()
    {
        btnSetting.onClick.AddListener(() =>
        {
            OpenPauseMenu();
        });
    }
    void Update()
    {
        // ESC 开关设置菜单：键盘等价于 btnSetting（演示/游玩时免鼠标点击），已打开时再按 ESC 关闭
        // 关闭走 HidePanel → SettingPanel.OnDestroy 统一恢复（按钮/ESC/返回标题所有路径一致）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.Instance.GetPanel<SettingPanel>() != null)
                UIManager.Instance.HidePanel<SettingPanel>();
            else
                OpenPauseMenu();
        }
    }

    /// <summary>打开暂停菜单：timeScale=0 冻结世界（Update 的 deltaTime=0、FixedUpdate/物理停摆），
    /// 并广播通知玩家屏蔽输入。BGM/转场协程（unscaledDeltaTime）不受影响。</summary>
    private void OpenPauseMenu()
    {
        UIManager.Instance.ShowPanel<SettingPanel>();
        Time.timeScale = 0f;
        OnPauseStateChanged?.Invoke(true);
    }
}
