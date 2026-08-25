using UnityEngine;

/// <summary>
/// 2D 运动学物理控制器（PlayerPhysicsController2D）。
/// 纯基于射线步进（Raycast-based Kinematic Physics）实现，消除 Unity Dynamic 刚体积分物理在平台跳跃中的：
/// 1. 浮点穿透与卡墙；
/// 2. 斜坡滑步与顿挫；
/// 3. 单向穿透跳板（One-way Platform）逻辑混乱；
/// 4. 移动平台跟随抖动。
/// </summary>
public class PlayerPhysicsController2D : RaycastController2D
{
    [Header("物理与坡度配置")]
    [SerializeField] private float maxClimbAngle = 60f;    // 最大爬坡角度
    [SerializeField] private float maxDescendAngle = 60f;  // 最大下坡角度
    [SerializeField] private LayerMask passThroughMask;    // 单向穿透跳板 Layer

    public CollisionInfo collisions;
    [HideInInspector] public Vector2 playerInput;

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool climbingSlope;
        public bool descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 moveAmountOld;
        public int faceDir; // 1 = 右, -1 = 左
        public bool standingOnPassThrough;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
            standingOnPassThrough = false;
        }
    }

    protected override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    /// <summary>
    /// 驱动角色物理移动主入口。
    /// </summary>
    /// <param name="moveAmount">本帧期望位移量 (Velocity * DeltaTime)</param>
    /// <param name="standingOnPlatform">是否正站立在移动平台上</param>
    public void Move(Vector2 moveAmount, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.moveAmountOld = moveAmount;

        if (moveAmount.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        // 下坡检测与平滑贴地
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        // 水平碰撞检测与爬坡
        if (moveAmount.x != 0)
        {
            HorizontalCollisions(ref moveAmount);
        }

        // 垂直碰撞检测（地面、天花板、单向跳板）
        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);
        }

        // 最终位移应用
        transform.Translate(moveAmount, Space.World);

        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    private void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + SkinWidth;

        for (int i = 0; i < 4; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {
                // 忽略距离为 0 的异常重叠
                if (hit.distance == 0) continue;

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // 爬坡检测
                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }
                    float distanceToSlopeStart = 0f;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - SkinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    moveAmount.x = (hit.distance - SkinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    private void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + SkinWidth;

        for (int i = 0; i < 4; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                // 单向跳板（One-way Platform）支持：向上跳跃时穿透，仅向下落踩在表面时阻断
                if (hit.collider.CompareTag("Through") || ((1 << hit.collider.gameObject.layer) & passThroughMask) != 0)
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue; // 向上跳直接穿过
                    }
                    collisions.standingOnPassThrough = true;
                }

                moveAmount.y = (hit.distance - SkinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    private void ClimbSlope(ref Vector2 moveAmount, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveAmount.y <= climbMoveAmountY)
        {
            moveAmount.y = climbMoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    private void DescendSlope(ref Vector2 moveAmount)
    {
        float directionX = Mathf.Sign(moveAmount.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - SkinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                    {
                        float moveDistance = Mathf.Abs(moveAmount.x);
                        float descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                        moveAmount.y -= descendMoveAmountY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 动态设置碰撞层级掩码（例如在切换维度时更新过滤）
    /// </summary>
    public void SetCollisionMask(LayerMask mask)
    {
        collisionMask = mask;
    }
}
