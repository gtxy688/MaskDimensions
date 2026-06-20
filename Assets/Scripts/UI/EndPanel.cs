using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndPanel :BasePanel{
    public Button btnSure;
    public Button btnClose; 
    public override void Init()
    {
        btnSure.onClick.AddListener(() =>
        {
            //返回标题界面
            UIManager.Instance.HidePanel<EndPanel>();
            //进入游戏界面
            AsyncOperation ao = SceneManager.LoadSceneAsync("BeginScene");
            ao.completed += (obj) =>
            {
                UIManager.Instance.ShowPanel<BeginPanel>();
                AudioManager.Instance.PlayMenuBGM();
            };
        });

        btnClose.onClick.AddListener(() =>
        {
            //关闭界面
            UIManager.Instance.HidePanel<EndPanel>();
            AsyncOperation ao = SceneManager.LoadSceneAsync("BeginScene");
            ao.completed += (obj) =>
            {
                UIManager.Instance.ShowPanel<BeginPanel>();
                AudioManager.Instance.PlayMenuBGM();
            };
        });
    }
    
}
