using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

public class UIManager:Singleton<UIManager>
{
    public UIManager()
    {
        //得到场景中的Canvas对象
        GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("UI/Canvas"));
        canvasTrans = canvas.transform;
        //过场景不移除该对象 保证这个游戏过程中 只有一个canvas对象
        GameObject.DontDestroyOnLoad(canvas);
    }

    //存储场景中显示的面板 
    //要隐藏面板时 直接通过名字获取对应面板 进行隐藏
    private Dictionary<string, BasePanel> panelDic = new Dictionary<string, BasePanel>();

    //场景中的面板对象
    private Transform canvasTrans;

    /// <summary>
    /// 显示面板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T ShowPanel<T>() where T : BasePanel
    {
        //此处 必须要保证 泛型T的类型名 和 面板预设体的名字一致 才能准确找到要进行控制的面板
        string panelName = typeof(T).Name;

        //判断 字典中 是否存在该面板(是否显示)
        //存在 直接返回
        if (panelDic.ContainsKey(panelName))
            return panelDic[panelName] as T;
        //不存在 就生成一个面板对象
        GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));
        panelObj.transform.SetParent(canvasTrans, false);

        //处理面板显示逻辑 并且保存在字典中
        //获取面板上挂载的Panel脚本
        T panel = panelObj.GetComponent<T>();
        //存储到字典中
        panelDic.Add(panelName, panel);
        //调用自己的显示逻辑
        panel.ShowMe();

        return panel;
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <typeparam name="T">面板类名</typeparam>
    /// <param name="isFade">是否淡出完毕后才删除面板 默认是true</param>
    public void HidePanel<T>(bool isFade = false) where T : BasePanel
    {
        //根据泛型得名字
        string panelName = typeof(T).Name;
        //判断需要隐藏的面板 是否在字典中(是否显示)
        if (panelDic.ContainsKey(panelName))
        {
            if (isFade)
            {
                //让面板淡出完毕后再删除
                panelDic[panelName].HideMe(() =>
                {
                    //删除要隐藏的面板对象
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    //删除字典中的记录
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                //不淡出,直接删除

                //删除要隐藏的面板对象
                GameObject.Destroy(panelDic[panelName].gameObject);
                //删除字典中的记录
                panelDic.Remove(panelName);
            }
        }
    }

    /// <summary>
    /// 获取面板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = typeof(T).Name;
        //字典中有面板 则返回对应面板
        if (panelDic.ContainsKey(panelName))
            return panelDic[panelName] as T;
        //字典中无面板 返回空
        return null;
    }

}
