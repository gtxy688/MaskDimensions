using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 状态标识符枚举。
/// 用于在状态字典中作为键值，实现状态间的解耦切换。
/// </summary>
public enum PlayerStateId
{
    Idle,
    Move,
    Jump,
    Fall,
    MaskSwitch
}

/// <summary>
/// 玩家主控制器（Context 环境类）。
/// 负责维护组件依赖、轮询输入数据，并驱动状态机运行。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }

    // 状态映射字典，缓存所有实例化的具体状态
    private Dictionary<PlayerStateId, BaseState> stateTable;

    public Rigidbody2D RB { get; private set; }
    public Animator Anim { get; private set; }
    
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 10f;
    public float MoveSpeed => moveSpeed;

    public float MoveInput { get; private set; }

    [Header("物理与碰撞参数")]
    [SerializeField] private float jumpForce = 10f;
    public float JumpForce => jumpForce;

    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;
    
    public bool IsGrounded { get; private set; }


    [Header("维度图层设置")]
    public GameObject objectsToAppear;    // 戴面具后【出现】的物体 (比如隐藏桥梁)
    public GameObject objectsToDisappear; // 戴面具后【消失】的物体 (比如挡路的石门)
    public bool isMaskActive = false; // 记录当前是否戴着面具

    private void Awake()
    {
        StateMachine = new PlayerStateMachine();
        RB = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();

        // 初始化状态字典注册表
        stateTable = new Dictionary<PlayerStateId, BaseState>
        {
           { PlayerStateId.Idle, new IdleState(this, StateMachine) },
            { PlayerStateId.Move, new MoveState(this, StateMachine) },
            { PlayerStateId.Jump, new JumpState(this, StateMachine) },
            { PlayerStateId.Fall, new FallState(this, StateMachine) },
            { PlayerStateId.MaskSwitch, new MaskSwitchState(this, StateMachine) }
        };
    }

    private void Start()
    {
        //直接从字典里把 Idle 取出来，喂给状态机的“初始化”接口
        StateMachine.Initialize(stateTable[PlayerStateId.Idle]);
    }
    private void FixedUpdate()
    {
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    private void Update()
    {
        MoveInput = Input.GetAxisRaw("Horizontal");

        //一直检测是否接触地面，供状态使用
        CheckGrounded();

        // 🎭 [新增] 监听按键 J，且确保当前不在切换状态中，防止狂按
        if (Input.GetKeyDown(KeyCode.J) && !(StateMachine.CurrentState is MaskSwitchState))
        {
            TransitionTo(PlayerStateId.MaskSwitch);
        }

        StateMachine.CurrentState?.LogicUpdate();

    }

    /// <summary>
    /// 执行基于标识符的状态转换。
    /// 保护私有数据,让其他State只需要存储id就能切换状态
    /// </summary>
    /// <param name="id">目标状态枚举</param>
    public void TransitionTo(PlayerStateId id)
    {
        if (stateTable.TryGetValue(id, out BaseState targetState))
        {
            StateMachine.ChangeState(targetState);
        }
        else
        {
            Debug.LogError($"[PlayerController] 状态转换失败：未注册的标识符 {id}");
        }
    }

    /// <summary>
    /// 更新角色的 Transform 缩放以匹配输入方向。
    /// 提取至控制器层面以复用代码。
    /// </summary>
    public void UpdateFacingDirection()
    {
        if (MoveInput != 0)
        {
            float facingDirection = Mathf.Sign(MoveInput);
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }
    }

    /// <summary>
    /// 基于物理引擎的 OverlapBox 碰撞检测。
    /// 用于判定角色是否与地面层发生交叠。
    /// </summary>
    private void CheckGrounded()
    {
        if (groundCheckPoint != null)
        {
            IsGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
        }
    }

    //绘制一个红色的框，供调试
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
    // 🎭 [新增] 供 MaskSwitchState 调用的终极维度开关
    public void ToggleDimension()
    {
        isMaskActive = !isMaskActive;

        // 隐藏的桥梁出现
        if (objectsToAppear != null) objectsToAppear.SetActive(isMaskActive);

        // 挡路的石门消失
        if (objectsToDisappear != null) objectsToDisappear.SetActive(!isMaskActive);

        Debug.Log($"维度切换完毕！当前面具状态：{isMaskActive}");
    }

    // 当角色和任何物体发生物理碰撞时，Unity 会自动调用这个方法
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查撞到的物体是不是贴着 "Trap" 标签
        if (collision.gameObject.CompareTag("Trap"))
        {
            Debug.Log("啊！踩到地刺了！扣血或重新开始！");

            // 这里可以写你的扣血逻辑，或者直接让角色回到出生点
            // Die(); 
        }
    }
}