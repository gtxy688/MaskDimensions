using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel :BasePanel
{
    private string githubUrl = "https://github.com/gtxy688";

    public Button btnStart;
    public Button btnQuit;
    public Button btnSetting;
    public Button btnAbout;
    public override void Init()
    {
        btnStart.onClick.AddListener(() =>
        {
            //隐藏开始界面
            UIManager.Instance.HidePanel<BeginPanel>();
            //进入游戏界面
            AsyncOperation ao = SceneManager.LoadSceneAsync("GameScene");
            ao.completed += (obj) =>
            {
                ////关卡初始化
            };

        });

        btnSetting.onClick.AddListener(() =>
        {
            ////显示设置面板
            //UIManager.Instance.ShowPanel<SettingPanel>();
        });

        btnAbout.onClick.AddListener(() =>
        {
            Application.OpenURL(githubUrl);
            Debug.Log("正在跳转至 GitHub: " + githubUrl);
        });

        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

}
