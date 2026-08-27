using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : BasePanel
{

    public Button btnSetting;
    
    public override void Init()
    {
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
    }
    void Update()
    {
        // ESC 开关设置菜单：键盘等价于 btnSetting（演示/游玩时免鼠标点击），已打开时再按 ESC 关闭
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.Instance.GetPanel<SettingPanel>() != null)
                UIManager.Instance.HidePanel<SettingPanel>();
            else
                UIManager.Instance.ShowPanel<SettingPanel>();
        }
    }
}
