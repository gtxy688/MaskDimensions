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
        
    }
}
