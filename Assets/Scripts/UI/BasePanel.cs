using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class BasePanel : MonoBehaviour
{
    //控制面板透明度组件
    private CanvasGroup canvasGroup;
    //面板淡入淡出的速度
    private float alphaSpeed = 10f;
    

    //隐藏完面板之后 还要处理的事情 比如销毁隐藏后的面板
    private UnityAction hideCallBack = null;

    //当前面板是隐藏还是显示
    public bool isShow = false;

    protected virtual void Awake()
    {
        #region 控制面板整体的透明度
        ////获取面板上挂载的CanavasGroup组件(可以)
        //canvasGroup = GetComponent<CanvasGroup>();
        ////如果面板上没有，添加上
        //if (canvasGroup == null)
        //{
        //    canvasGroup = gameObject.AddComponent<CanvasGroup>();
        //}
        #endregion
    }

    protected virtual void Start()
    {
        Init();
    }

    /// <summary>
    /// 注册控件的方法 所有子面板都要注册一些控件
    /// 写成抽象方法，让每个子类都必须要去实现
    /// </summary>
    public abstract void Init();

    #region 控制面板整体的透明度
    //protected virtual void Update()
    //{
    //    //显示状态 淡入 透明度从0加到1 到1停止变化
    //    if (isShow && canvasGroup.alpha != 1)
    //    {
    //        canvasGroup.alpha += alphaSpeed * Time.deltaTime;
    //        if (canvasGroup.alpha >= 1)
    //        {
    //            canvasGroup.alpha = 1;
    //        }
    //    }
    //    //隐藏状态 淡出 透明度从1减到0 到0停止变化
    //    else if (!isShow && canvasGroup.alpha != 0)
    //    {
    //        canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
    //        if (canvasGroup.alpha <= 0)
    //        {
    //            canvasGroup.alpha = 0;
    //            //面板透明度淡出后 要做的事情
    //            hideCallBack?.Invoke();
    //        }
    //    }
    //}
    #endregion
    
    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void ShowMe()
    {
        //从0渐变为1
        //canvasGroup.alpha = 0;
        isShow = true;
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <param name="callBack">隐藏完毕后要做的事</param>
    public virtual void HideMe(UnityAction callBack)
    {
        //从1渐变为0
        //canvasGroup.alpha = 1;
        isShow = false;

        hideCallBack = callBack;
    }

}
