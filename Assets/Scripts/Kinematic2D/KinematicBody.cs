using UnityEngine;

/// <summary>
/// 自研 2D 运动学物理核心。
/// 职责边界：只做"把一段期望位移安全地移动出去"，不碰输入、不碰状态机、不碰渲染。
/// 为什么 X/Y 分轴：碰撞本质是分离轴问题，分轴检测避免对角线穿墙，且逻辑可独立推理。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class KinematicBody : MonoBehaviour
{
    [SerializeField] private RayConfig config;

    private BoxCollider2D boxCollider;
    private ICollisionFilter filter;
    private ColliderMapping mapping;
    // 多命中缓冲区：本项目 m_QueriesStartInColliders=1（射线起点在自身盒内会先自命中），
    // 且维度过滤器拒绝最近命中后可能还有允许的命中（叠层几何）。单发 Raycast 只能拿第一个
    // 命中，两种场景都覆盖不到；NonAlloc 取全部命中再自行筛选。数组每帧复用，无 GC。
    private RaycastHit2D[] hitBuffer;

    public CollisionResult LastResult { get; private set; }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        // 防御性校验（I1）：若 collisionMask 包含自身所在层，m_QueriesStartInColliders=1 下
        // 射线会在原点处先自命中（distance==0 被跳过），该物体对墙/地/天花板全部探测失效，
        // 且不会报任何错——"配置违规即静默失效"。这条不变量仅靠资产配置维持太脆弱，
        // 违规必须在 Awake 立即显式报错，而不是等玩家穿墙了才暴露。
        if ((config.collisionMask.value & (1 << gameObject.layer)) != 0)
        {
            Debug.LogError(
                "KinematicBody '" + name + "'：collisionMask 不能包含自身所在层（此物体 layer=" + gameObject.layer + "）。" +
                "Queries Start In Colliders 开启时射线会先自命中（distance==0 被跳过），导致该物体所有碰撞探测静默失效（穿墙/无地面）。" +
                "请从 collisionMask 中移除自身层。", this);
        }

        // 为什么 max(8, 水平/垂直射线数)：单条射线在叠层几何下命中数可能多于一条，
        // 缓冲区必须装得下任意一次查询的全部命中；8 为最低余量，防止满写丢命中。
        hitBuffer = new RaycastHit2D[Mathf.Max(Mathf.Max(8, config.horizontalRayCount), config.verticalRayCount)];
        RefreshMapping();
    }

    /// <summary>
    /// 注入碰撞过滤器（维度切换时由外部调用，换一个过滤器实例或让其内部状态变化）。
    /// </summary>
    public void SetFilter(ICollisionFilter filter)
    {
        this.filter = filter;
    }

    /// <summary>
    /// 每帧位移入口。velocity 由调用方（PlayerController）算好传入。
    /// </summary>
    public void Move(Vector2 velocity)
    {
        // 为什么先 SyncTransforms：Physics2D 的碰撞体 bounds 可能滞后于 Transform（例如
        // 外部代码在同一帧内修改 transform.position 后立即进行射线查询）。若不同步，
        // 射线原点会基于陈旧的物理位置 → 碰撞检测错位 → 穿墙（本项目实际踩过的坑）。
        Physics2D.SyncTransforms();

        Vector2 moveAmount = velocity * Time.deltaTime;
        RefreshMapping();
        CollisionResult result = new CollisionResult();

        // X 轴：水平移动检测与修正
        if (moveAmount.x != 0f)
            moveAmount = MoveHorizontally(moveAmount, ref result);

        // Y 轴：垂直移动检测与修正
        if (moveAmount.y != 0f)
            moveAmount = MoveVertically(moveAmount, ref result);

        transform.Translate(moveAmount, Space.World);
        LastResult = result;
    }

    private void RefreshMapping()
    {
        mapping = ColliderMapping.From(boxCollider, config.skinWidth, config.horizontalRayCount, config.verticalRayCount);
    }

    private Vector2 MoveHorizontally(Vector2 moveAmount, ref CollisionResult result)
    {
        float directionX = Mathf.Sign(moveAmount.x);
        float rayLength = Mathf.Abs(moveAmount.x) + config.skinWidth;

        for (int i = 0; i < config.horizontalRayCount; i++)
        {
            Vector2 rayOrigin = directionX == -1 ? mapping.bottomLeft : mapping.bottomRight;
            rayOrigin += Vector2.up * (mapping.horizontalRaySpacing * i);

            // 为什么 NonAlloc：最近命中可能被维度过滤器拒绝（如里世界墙在表世界不可见），
            // 单发 Raycast 取不到被拒命中之后的下一面墙 → 两世界地形叠在一起时撞错位置或穿模。
            // 这里取全部命中，跳过自身盒/重叠/被拒者后，取最近的有效命中。
            int count = Physics2D.RaycastNonAlloc(rayOrigin, Vector2.right * directionX, hitBuffer, rayLength, config.collisionMask);

            RaycastHit2D best = default(RaycastHit2D);
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int j = 0; j < count; j++)
            {
                if (hitBuffer[j].collider == boxCollider) continue;          // 自身盒自命中（queriesStartInColliders），跳过
                if (filter != null && !filter.Allow(hitBuffer[j])) continue; // 维度过滤：被拒后继续找下一个最近的
                if (hitBuffer[j].distance < bestDistance)                    // 取最近的有效命中
                {
                    best = hitBuffer[j];
                    bestDistance = hitBuffer[j].distance;
                    found = true;
                }
            }

            if (found)
            {
                // 重叠/贴墙统一处理：避免旧实现的"distance==0 跳过"导致重叠时无法回退（穿墙根因）。
                // 公式 (distance - skinWidth) * directionX 本身自洽：
                //   distance > skinWidth → 正位移，正常前进贴墙；
                //   distance < skinWidth（含 0，已嵌入墙）→ 负位移，自动沿反方向退出重叠。
                // 千万不要改成"penetration * directionX"——那会把物体往墙里推（此前修复的错误）。
                moveAmount.x = (best.distance - config.skinWidth) * directionX;

                rayLength = best.distance < rayLength ? best.distance : rayLength;

                result.HitLeft = directionX == -1;
                result.HitRight = directionX == 1;
            }
        }
        return moveAmount;
    }

    private Vector2 MoveVertically(Vector2 moveAmount, ref CollisionResult result)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + config.skinWidth;

        for (int i = 0; i < config.verticalRayCount; i++)
        {
            // 为什么 + moveAmount.x：垂直射线原点跟随水平修正后的位置，防止贴墙时漏检
            Vector2 rayOrigin = directionY == -1 ? mapping.bottomLeft : mapping.topLeft;
            rayOrigin += Vector2.right * (mapping.verticalRaySpacing * i + moveAmount.x);

            // 同 MoveHorizontally：被 filter 拒绝的命中之后可能还有允许的碰撞体（维度叠墙），
            // 必须 NonAlloc 拿全部命中再筛选，否则会穿过本应阻挡的墙。
            int count = Physics2D.RaycastNonAlloc(rayOrigin, Vector2.up * directionY, hitBuffer, rayLength, config.collisionMask);

            RaycastHit2D best = default(RaycastHit2D);
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int j = 0; j < count; j++)
            {
                if (hitBuffer[j].collider == boxCollider) continue;          // 自身盒自命中，跳过
                if (filter != null && !filter.Allow(hitBuffer[j])) continue; // 维度过滤：被拒后继续找下一个最近的
                if (hitBuffer[j].distance < bestDistance)                    // 取最近的有效命中
                {
                    best = hitBuffer[j];
                    bestDistance = hitBuffer[j].distance;
                    found = true;
                }
            }

            if (found)
            {
                // 与 MoveHorizontally 同根因同公式：不跳过 distance==0 的命中，
                // 用 (distance - skinWidth) * directionY 自洽处理重叠（自动回退）。
                moveAmount.y = (best.distance - config.skinWidth) * directionY;

                rayLength = best.distance < rayLength ? best.distance : rayLength;

                result.IsGrounded = directionY == -1;
                result.HitCeiling = directionY == 1;
            }
        }
        return moveAmount;
    }
}