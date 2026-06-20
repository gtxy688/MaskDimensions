using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("换装与特效")]
    [SerializeField] private GameObject faceMaskObject; // 挂在脸上的面具子物体
    [SerializeField] private ParticleSystem switchVFXPrefab; // 切换时的粒子特效预制体
    [Header("配置文件")]
    [SerializeField] private PlayerConfigSO config;

    public float MoveSpeed => config.moveSpeed;
    public float JumpForce => config.jumpForce;
    public float MoveInput { get; private set; }

    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    public float CoyoteTimeCounter { get; private set; }
    public float JumpBufferCounter { get; private set; }
    public bool IsGrounded { get; private set; }

    // 记录玩家当前是否戴着面具
    public bool isMaskActive = false;


    // 在 PlayerController 中添加一个公开的静态只读属性,供外界访问玩家是戴着面具
    public static bool IsMaskActiveGlobally { get; private set; }

    [Header("面具与理智系统")]
    //使用可配置的图层名来控制物理忽略
    [SerializeField] private string maskLayerName = "NewDimension";
    [SerializeField] private string oldWorldLayerName = "OldDimension";
    int playerLayer;
    int newLayer;
    int oldLayer;
   
    public float currentSanity = 100f;

    // 定义一个委托事件，用来广播理智值的变化 (当前值, 最大值)
    public static event System.Action<float, float> OnSanityChanged;

    public bool isPreviewing = false; // 是否处于战术定身(透视)状态

    // 按住 J 多久才显示虚影（防止快速点击闪烁）
    private float previewHoldTimer = 0f;
    private bool previewGhostsShown = false;

    [Header("死亡与重生")]
    [SerializeField] private GameObject deathVFXPrefab;   // disappear 预制体
    [SerializeField] private GameObject respawnVFXPrefab; // appear 预制体
    [SerializeField] private Transform respawnPoint;      // 重生点位置（可以是一个空的 GameObject）

    public bool isDead = false;

    // 事件广播
    public static event Action<bool> OnMaskStateChanged;
    public static event Action<bool> OnMaskPreviewChanged;

    //用于显示ui提示的事件
    //理智不足无法切换世界时广播（用于 SanityStatusHints 显示提示#5）
    public static event System.Action OnInsufficientSanity;

    // 理智耗尽强制弹出时广播（用于 ToastMessage 显示）
    public static event System.Action OnSanityForcedRecovery;

    //玩家死亡时广播（用于 SanityStatusHints 清除理智耗尽标记）
    public static event System.Action OnPlayerDied;

    private void Awake()
    {
        // 提前缓存 Layer ID
        playerLayer = LayerMask.NameToLayer("Player");
        newLayer = LayerMask.NameToLayer(maskLayerName);
        oldLayer = LayerMask.NameToLayer(oldWorldLayerName);
        IsMaskActiveGlobally = isMaskActive;
        UpdateLayerCollisions();  // 添加这一行，根据初始面具状态设置碰撞忽略
        StateMachine = new PlayerStateMachine();
        RB = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
        currentSanity = config.maxSanity;
        // 初始化状态字典注册表
        stateTable = new Dictionary<PlayerStateId, BaseState>
        {
           { PlayerStateId.Idle, new IdleState(this, StateMachine) },
            { PlayerStateId.Move, new MoveState(this, StateMachine) },
            { PlayerStateId.Jump, new JumpState(this, StateMachine) },
            { PlayerStateId.Fall, new FallState(this, StateMachine) },
        };
    }

    // 在 PlayerController 中添加一个公开的属性，供状态类查询当前是否允许切换面具
    public bool CanSwitchMask
    {
        get
        {
            // 只有在这些状态下允许切换面具（白名单机制比黑名单更安全）
            return StateMachine.CurrentState is IdleState ||
                   StateMachine.CurrentState is MoveState ||
                   StateMachine.CurrentState is JumpState ||
                   StateMachine.CurrentState is FallState;
        }
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
        if (isDead) return;
        MoveInput = Input.GetAxisRaw("Horizontal");

        //如果处于“战术定身”状态，拦截玩家的所有移动和跳跃输入
        if (isPreviewing)
        {
            MoveInput = 0f;
            // 如果你的跳跃按键是 Space，你也可以在这里把 rb.velocity 的 x 设为 0，防止滑动
        }

        // 处理面具逻辑（透视、切换、理智流逝）
        HandleMaskSystem();

        // 1. 先进行地面检测
        CheckGrounded();

        // 2. 更新跳跃相关的计时器 (新增)
        UpdateJumpTimers();

        // 3. 执行状态逻辑
        StateMachine.CurrentState?.LogicUpdate();

    }

    // 新增：维护两个计时器,优化跳跃体验
    private void UpdateJumpTimers()
    {
        // 土狼时间计时器：在地面时充满，离开地面后开始倒计时
        if (IsGrounded)
        {
            CoyoteTimeCounter = config.coyoteTime;
        }
        else
        {
            CoyoteTimeCounter -= Time.deltaTime;
        }

        // 跳跃缓冲计时器：按下跳跃键时充满，否则倒计时
        if (Input.GetButtonDown("Jump"))
        {
            JumpBufferCounter = config.jumpBufferTime;
        }
        else
        {
            JumpBufferCounter -= Time.deltaTime;
        }
    }

    // 新增：成功跳跃后清空计时器，防止连跳
    public void ConsumeJump()
    {
        CoyoteTimeCounter = 0f;
        JumpBufferCounter = 0f;
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
    /// 异界路面虽然同时在 groundLayer 中，但当前世界禁止的层会被显式排除。
    /// </summary>
    private void CheckGrounded()
    {
        if (groundCheckPoint != null)
        {
            Collider2D hit = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
            if (hit != null)
            {
                // 显式排除当前世界不应踩到的层（与 IgnoreLayerCollision 双重保险）
                int hitLayer = hit.gameObject.layer;
                if (!isMaskActive && hitLayer == newLayer)
                    IsGrounded = false;
                else if (isMaskActive && hitLayer == oldLayer)
                    IsGrounded = false;
                else
                    IsGrounded = true;
            }
            else
            {
                IsGrounded = false;
            }
        }
    }

    /// <summary>
    /// 对外暴露的刷新地面检测接口。
    /// 在变更图层或其他可能影响碰撞查询的操作后可调用以立即更新 IsGrounded。
    /// </summary>
    public void RefreshGrounded()
    {
        CheckGrounded();
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

    /// <summary>
    /// 处理面具系统的所有输入与理智消耗
    /// </summary>
    private void HandleMaskSystem()
    {
        if (!CanSwitchMask)
        {
            if (isPreviewing) CancelPreview();
            return;
        }

        // ---- 按下 J：进入慢动作预览模式 ----
        if (Input.GetKeyDown(KeyCode.J) && !isMaskActive)
        {
            if(currentSanity < config.minSanityToSwitch)
            {
                OnInsufficientSanity?.Invoke();
                return; // 理智不足，无法进入预览
            }
            
            isPreviewing = true;
            previewHoldTimer = 0f;
            previewGhostsShown = false;
            Time.timeScale = config.previewTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            // 不立即显示虚影，等按住一小段时间后才浮现
        }

        // ---- 按住 J 期间：积累按住时长，到达阈值后显示虚影 ----
        if (isPreviewing)
        {
            previewHoldTimer += Time.unscaledDeltaTime;
            if (!previewGhostsShown && previewHoldTimer >= config.previewHoldThreshold)
            {
                previewGhostsShown = true;
                OnMaskPreviewChanged?.Invoke(true);
            }
        }

        // ---- 松开 J：决定是"按住预览后切换"还是"快速点击直接切" ----
        if (Input.GetKeyUp(KeyCode.J))
        {
            if (isPreviewing)
            {
                // 如果之前已经显示了预览虚影，
                // 说明玩家是进入了预览模式，
                // 先取消预览，再切换
                if (previewGhostsShown)
                {
                    CancelPreview();
                    StartCoroutine(ExecuteMaskSwitchWithHitlag());
                }
                else
                {
                    // 直接切换
                    isPreviewing = false;
                    Time.timeScale = 1f;
                    Time.fixedDeltaTime = 0.02f;
                    StartCoroutine(ExecuteMaskSwitchWithHitlag());
                }
            }
            else if (isMaskActive)
            {
                // 离开里世界不需要理智检查（防止被困）
                StartCoroutine(ExecuteMaskSwitchWithHitlag());
            }
        }

        // 理智流逝机制
        if (isMaskActive)
        {
            currentSanity -= config.activeSanityCostRate * Time.deltaTime;

            // 当理智耗尽，强制弹回表世界
            if (currentSanity <= 0)
            {
                currentSanity = 0;
                OnSanityForcedRecovery?.Invoke();
                StartCoroutine(ExecuteMaskSwitchWithHitlag()); // 可以直接复用顿帧切换，营造断片感
            }
        }
        else if (!isPreviewing)
        {
            // 在表世界安全时，恢复理智
            if (currentSanity < config.maxSanity)
            {
                currentSanity += config.sanityRecoverRate * Time.deltaTime;
                currentSanity = Mathf.Clamp(currentSanity, 0, config.maxSanity);
            }
        }

        // 广播当前理智值
        OnSanityChanged?.Invoke(currentSanity, config.maxSanity);
    }

    private void CancelPreview()
    {
        isPreviewing = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        OnMaskPreviewChanged?.Invoke(false);
    }

    /// <summary>
    /// 带顿帧(Hitlag)的无缝切换协程
    /// 使用协程是为了实现等待0.15秒的功能
    /// </summary>
    private IEnumerator ExecuteMaskSwitchWithHitlag()
    {
        // 1. 瞬间实例化并播放粒子特效 (因为是 Unscaled Time，它会继续运动)
        if (switchVFXPrefab != null)
        {
            Instantiate(switchVFXPrefab, transform.position, Quaternion.identity);
        }

        // 2. 瞬间画面定格营造力量感
        Time.timeScale = 0f;

        // 3. 执行底层物理和事件切换
        isMaskActive = !isMaskActive;
        IsMaskActiveGlobally = isMaskActive;
        UpdateLayerCollisions();
        OnMaskStateChanged?.Invoke(isMaskActive);

        // 4. 控制脸上纸娃娃面具的显隐
        if (faceMaskObject != null)
        {
            faceMaskObject.SetActive(isMaskActive);
        }

        // 5.停顿 0.15 秒（不受 Time.timeScale 影响的真实时间）
        yield return new WaitForSecondsRealtime(0.15f);

        // 6. 恢复时间，动量完美继承
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    /// <summary>
    /// 更新物理引擎的图层碰撞忽略
    /// </summary>
    private void UpdateLayerCollisions()
    {
        if (playerLayer == -1) return;

        if (newLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, newLayer, !isMaskActive);

        if (oldLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, oldLayer, isMaskActive);
    }

    /// <summary>
    /// 进入新房间时由 RoomManager 调用。重置状态 + 更新重生点。
    /// </summary>
    public void ResetForNewRoom(Transform spawnPoint)
    {
        // 更新重生点引用（死亡后回到当前房间入口）
        respawnPoint = spawnPoint;

        // 传送玩家
        transform.position = spawnPoint.position;

        // 重置物理
        RB.velocity = Vector2.zero;
        RB.simulated = true;

        // 重置动画和显隐
        Anim.enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        if (faceMaskObject != null) faceMaskObject.SetActive(false);

        // 重置状态
        isDead = false;
        isPreviewing = false;
        currentSanity = config.maxSanity;

        // 摘下面具——只在之前确实戴着时广播事件，避免无谓的顿帧
        bool wasMaskActive = isMaskActive;
        isMaskActive = false;
        IsMaskActiveGlobally = false;
        UpdateLayerCollisions();
        if (wasMaskActive)
            OnMaskStateChanged?.Invoke(false);

        // 重置时间缩放（取消预览残留）
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // 切回 Idle 状态
        TransitionTo(PlayerStateId.Idle);
    }

    /// <summary>
    /// 供外部脚本（子弹、陷阱等）触发玩家死亡。
    /// isDead 检查防止连续触发。
    /// </summary>
    public void Die()
    {
        if (!isDead)
        {
            OnPlayerDied?.Invoke();
            StartCoroutine(DieAndRespawnRoutine());
        }
    }

    // 当角色和任何物体发生物理碰撞时，Unity 会自动调用这个方法
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查撞到的物体是不是贴着 "Trap" 标签
        if (collision.gameObject.CompareTag("Trap"))
        {
            OnPlayerDied?.Invoke();
            StartCoroutine(DieAndRespawnRoutine());
        }
    }

    /// <summary>
    /// 供 TeleportTrigger 等外部脚本调用。用死亡/重生动画将玩家传送到目标位置。
    /// 与 DieAndRespawnRoutine 不同的是不重置理智、面具状态，不切 Idle。
    /// </summary>
    public void TeleportTo(Transform destination)
    {
        if (!isDead)
            StartCoroutine(TeleportRoutine(destination));
    }

    private IEnumerator TeleportRoutine(Transform destination)
    {
        isDead = true;

        // 1. 保存 SpriteRenderer 状态，用 alpha=0 隐藏（比 enabled=false 更可靠）
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        if (faceMaskObject != null)
            faceMaskObject.SetActive(false);
        RB.velocity = Vector2.zero;
        RB.simulated = false;
        Anim.enabled = false;

        // 2. 原位置消散
        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.5f);

        // 3. 传送
        transform.position = destination.position;

        // 4. 目标位置凝聚
        if (respawnVFXPrefab != null)
            Instantiate(respawnVFXPrefab, destination.position, Quaternion.identity);

        yield return new WaitForSeconds(0.4f);

        // 5. 恢复
        sr.color = originalColor;
        RB.simulated = true;
        Anim.enabled = true;
        if (faceMaskObject != null)
            faceMaskObject.SetActive(isMaskActive);

        isDead = false;
    }

    private IEnumerator DieAndRespawnRoutine()
    {
        isDead = true;

        // 1. 禁用玩家的物理、控制和视觉
        RB.velocity = Vector2.zero;
        RB.simulated = false; // 冻结刚体
        Anim.enabled = false; // 停止人物原画动画
        GetComponent<SpriteRenderer>().enabled = false; // 隐藏主角本体
        if (faceMaskObject != null) 
        {
            faceMaskObject.SetActive(false); // 隐藏纸娃娃面具
        }

        // 2. 在当前位置生成死亡消散特效 (disappear)
        if (deathVFXPrefab != null)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        // 播放死亡音效
        AudioManager.Instance.PlayDeathSFX();

        // 3. 等待消散动画播完 (比如 0.5 秒)
        yield return new WaitForSeconds(0.5f);

        // 4. 将玩家瞬间移动到重生点
        transform.position = respawnPoint.position;

        // 5. 在重生点生成重生凝聚特效 (appear)
        if (respawnVFXPrefab != null)
        {
            Instantiate(respawnVFXPrefab, transform.position, Quaternion.identity);
        }

        // 播放复活音效
        AudioManager.Instance.PlayRespawnSFX();

        // 6. 稍微等待凝聚特效快要播完时（比如 0.4 秒），重新显现实体
        yield return new WaitForSeconds(0.4f);

        RB.simulated = true;
        Anim.enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;

        // 重置理智值等状态
        currentSanity = config.maxSanity;
        isMaskActive = false;
        IsMaskActiveGlobally = false;
        UpdateLayerCollisions();
        OnMaskStateChanged?.Invoke(false);

        isDead = false;

        // 强制切回 Idle 状态
        TransitionTo(PlayerStateId.Idle);
    }

}