# 关卡系统 + SO + 对象池 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在 GameScene 中实现 3 个关卡房间，由 RoomConfigSO 数据驱动，第 2 关使用 BulletSpawner + ObjectPool 生成横向扫射弹幕。

**架构：**
- Room 作为区域（Tilemap + Trigger + Vcam），RoomManager 控制切换
- 子弹系统用 ObjectPool 池化复用，BulletStatsSO 配置属性
- RoomConfigSO 免除硬编码，改关只需调 .asset 文件

**技术栈：** Unity URP 2D, Cinemachine 2.10.7（已安装）, ScriptableObject, Object Pool

**前提：** GameScene 场景内已布置好 3 个房间的区域（Tilemap、碰撞体、入口触发器位置），这些由用户在 Unity Editor 中手动完成。

---

### 任务 1：创建 RoomConfigSO（关卡数据资产）

**文件：**
- 创建：`Assets/Scripts/SO/RoomConfigSO.cs`

- [ ] **步骤 1：编写 RoomConfigSO**

```csharp
using UnityEngine;

/// <summary>
/// 关卡数据资产。每个房间对应一个 .asset 文件。
/// 位于 Resources/SO/Rooms/ 下，运行时由 RoomManager 读取。
/// </summary>
[CreateAssetMenu(fileName = "NewRoom", menuName = "Config/Room")]
public class RoomConfigSO : ScriptableObject
{
    public string roomName;
    [TextArea] public string description;

    [Header("子弹配置（第 2 关专用）")]
    public BulletStatsSO bulletStats;   // 子弹属性共享引用
    public float fireInterval = 1f;     // 横向扫射发射间隔（秒）

    [Header("BGM")]
    public AudioClip bgmOverride;       // 非空则覆盖当前 BGM
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/SO/RoomConfigSO.cs
git commit -m "feat(so): add RoomConfigSO for level data driven design"
```

---

### 任务 2：创建 BulletStatsSO（子弹属性资产）

**文件：**
- 创建：`Assets/Scripts/SO/BulletStatsSO.cs`

- [ ] **步骤 1：编写 BulletStatsSO**

```csharp
using UnityEngine;

/// <summary>
/// 子弹属性资产。不同子弹类型共享同一份配置引用。
/// 修改一个 .asset 文件即可全局更新对应类型的所有子弹。
/// </summary>
[CreateAssetMenu(fileName = "NewBullet", menuName = "Config/Bullet")]
public class BulletStatsSO : ScriptableObject
{
    public float speed = 5f;         // 子弹飞行速度
    public Color color = Color.white; // 子弹颜色
    public float lifetime = 3f;      // 子弹存活时间（秒），超时自动回池
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/SO/BulletStatsSO.cs
git commit -m "feat(so): add BulletStatsSO for bullet property configuration"
```

---

### 任务 3：创建 ObjectPool（通用对象池）

**文件：**
- 创建：`Assets/Scripts/Pool/ObjectPool.cs`

- [ ] **步骤 1：编写 ObjectPool**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池。泛型 T 必须是 MonoBehaviour，且挂载在预制体上。
/// 使用方式：
///   var pool = new ObjectPool<Bullet>(bulletPrefab, parentTransform);
///   var bullet = pool.Get();
///   pool.Return(bullet);
/// </summary>
public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly Queue<T> pool = new();
    private readonly T prefab;
    private readonly Transform parent;

    public ObjectPool(T prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/Pool/ObjectPool.cs
git commit -m "feat(pool): add generic ObjectPool<T> for bullet recycling"
```

---

### 任务 4：创建 Bullet（子弹行为）

**文件：**
- 创建：`Assets/Scripts/GameScene/Bullet/Bullet.cs`

- [ ] **步骤 1：编写 Bullet.cs**

```csharp
using UnityEngine;

/// <summary>
/// 单个子弹的行为。由 BulletSpawner 从对象池取出并发射。
/// 飞行方向在 Fire() 时设定，到达 lifetime 后自动回池。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour
{
    public BulletStatsSO stats;
    private Vector2 direction;
    private float spawnTime;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 从对象池取出后调用，初始化子弹参数。
    /// </summary>
    public void Fire(Vector2 dir, BulletStatsSO bulletStats)
    {
        stats = bulletStats;
        direction = dir.normalized;
        spawnTime = Time.time;
        gameObject.SetActive(true);

        if (spriteRenderer != null && stats != null)
            spriteRenderer.color = stats.color;
    }

    private void Update()
    {
        if (stats == null) return;

        transform.Translate(direction * (stats.speed * Time.deltaTime));

        if (Time.time - spawnTime > stats.lifetime)
        {
            // 通知 BulletSpawner 回池（通过事件或直接调用）
            BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
            spawner?.ReturnBullet(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 子弹碰到玩家 → 玩家死亡（由 PlayerController.OnCollisionEnter2D 处理 Trap 标签）
            // 但子弹不是 Trap 标签，所以手动触发死亡
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // 触发玩家死亡协程（需要 PlayerController 暴露一个公共死亡方法）
                // 见 PlayerController 修改任务
            }
        }

        // 如果碰到墙壁等边界，提前回池
        if (other.CompareTag("Wall"))
        {
            BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
            spawner?.ReturnBullet(this);
        }
    }
}
```

注意：上面 `OnTriggerEnter2D` 中处理玩家碰撞的部分依赖后续 PlayerController 修改。暂时先不写那段逻辑。

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/GameScene/Bullet/Bullet.cs
git commit -m "feat(bullet): add Bullet behaviour with pool recycling"
```

---

### 任务 5：创建 BulletSpawner（横向扫射生成器）

**文件：**
- 创建：`Assets/Scripts/GameScene/Bullet/BulletSpawner.cs`

- [ ] **步骤 1：编写 BulletSpawner.cs**

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// 横向扫射子弹生成器。挂在第 2 关的场景中，进入房间时由 RoomManager 激活。
/// 从屏幕一侧发射横向子弹，穿越整个房间。
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("配置")]
    public BulletStatsSO bulletStats;
    public Bullet bulletPrefab;
    public float fireInterval = 1f;          // 发射间隔（秒）
    public Transform[] spawnPoints;          // 发射位置（左右两侧各一个）
    public float bulletSpeed = 5f;

    private ObjectPool<Bullet> bulletPool;
    private Coroutine fireCoroutine;

    private void Awake()
    {
        if (bulletPrefab != null)
            bulletPool = new ObjectPool<Bullet>(bulletPrefab, transform);
    }

    /// <summary>
    /// 由 RoomManager 进入房间时调用，开始发射。
    /// </summary>
    public void StartFiring()
    {
        if (fireCoroutine != null) StopCoroutine(fireCoroutine);
        fireCoroutine = StartCoroutine(FireRoutine());
    }

    /// <summary>
    /// 离开房间时调用，停止发射。
    /// </summary>
    public void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    private IEnumerator FireRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fireInterval);

            foreach (Transform point in spawnPoints)
            {
                Bullet bullet = bulletPool.Get();
                bullet.transform.position = point.position;
                bullet.transform.rotation = point.rotation;

                // 根据发射点位置决定方向：左侧点向右，右侧点向左
                float dirX = point.position.x < 0 ? 1f : -1f;
                bullet.Fire(new Vector2(dirX, 0f), bulletStats);
            }
        }
    }

    /// <summary>
    /// 由 Bullet 自身在达到 lifetime 或撞墙时调用。
    /// </summary>
    public void ReturnBullet(Bullet bullet)
    {
        bulletPool.Return(bullet);
    }
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/GameScene/Bullet/BulletSpawner.cs
git commit -m "feat(bullet): add BulletSpawner for horizontal sweep pattern"
```

---

### 任务 6：创建 RoomTrigger（关卡入口触发器）

**文件：**
- 创建：`Assets/Scripts/GameScene/RoomTrigger.cs`

- [ ] **步骤 1：编写 RoomTrigger.cs**

```csharp
using Cinemachine;
using UnityEngine;

/// <summary>
/// 挂载在每个房间入口的 Trigger Collider 上。
/// 玩家进入时通知 RoomManager 切换房间。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomTrigger : MonoBehaviour
{
    [Header("房间配置")]
    public RoomConfigSO roomConfig;

    [Header("相机")]
    public CinemachineVirtualCamera roomVcam;   // 该房间的专属相机

    [Header("玩家生成位置")]
    public Transform playerSpawnPos;             // 进入时玩家传送至此

    [Header("子物体控制")]
    public GameObject[] activateOnEnter;         // 进入时激活（如第 2 关的 BulletSpawner 父物体）

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.EnterRoom(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 在场景视图中绘制触发区域
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
#endif
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/GameScene/RoomTrigger.cs
git commit -m "feat(room): add RoomTrigger for room entry detection"
```

---

### 任务 7：创建 RoomExit（出口传送门）

**文件：**
- 创建：`Assets/Scripts/GameScene/RoomExit.cs`

- [ ] **步骤 1：编写 RoomExit.cs**

```csharp
using UnityEngine;

/// <summary>
/// 挂载在每个房间出口处。玩家触碰时通知 RoomManager 解锁下一个房间的入口。
/// </summary>
public class RoomExit : MonoBehaviour
{
    [Header("目标房间（可选）")]
    public RoomTrigger targetRoom;  // 指向下一个房间的入口触发器

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.OnRoomCleared(targetRoom);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
#endif
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/GameScene/RoomExit.cs
git commit -m "feat(room): add RoomExit for level completion detection"
```

---

### 任务 8：创建 RoomManager（关卡管理器）

**文件：**
- 创建：`Assets/Scripts/Mgr/RoomManager.cs`

- [ ] **步骤 1：编写 RoomManager.cs**

```csharp
using UnityEngine;
using Cinemachine;

/// <summary>
/// 关卡管理器（单例）。负责房间切换、相机控制、子弹生成器启停。
/// </summary>
public class RoomManager : SingletonMono<RoomManager>
{
    private RoomTrigger currentRoom;
    private GameObject player;

    protected override void Awake()
    {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>
    /// 玩家进入房间时调用。
    /// </summary>
    public void EnterRoom(RoomTrigger trigger)
    {
        if (currentRoom == trigger) return; // 已在该房间中

        // 1. 退出上一个房间（如果有）
        if (currentRoom != null)
        {
            DeactivateRoom(currentRoom);
        }

        // 2. 进入新房间
        currentRoom = trigger;
        ActivateRoom(trigger);
    }

    /// <summary>
    /// 玩家到达出口时调用。
    /// </summary>
    public void OnRoomCleared(RoomTrigger nextRoom)
    {
        Debug.Log($"关卡 {currentRoom?.roomConfig?.roomName} 完成！");

        // 如果指定了下一个房间，不做特殊操作，
        // 等玩家实际走进触发器时自然触发 EnterRoom
    }

    private void ActivateRoom(RoomTrigger trigger)
    {
        // 切换相机
        if (trigger.roomVcam != null)
        {
            trigger.roomVcam.gameObject.SetActive(true);
            // 禁用上一个房间的相机（由 Cinemachine 自动管理优先级，或手动控制）
        }

        // 传送玩家到入口
        if (trigger.playerSpawnPos != null && player != null)
        {
            player.transform.position = trigger.playerSpawnPos.position;
        }

        // 激活房间子物体（如 BulletSpawner）
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(true);
            // BulletSpawner 自身在 OnEnable 中调用 StartFiring()
        }

        // 切换 BGM
        if (trigger.roomConfig != null && trigger.roomConfig.bgmOverride != null)
        {
            // 注：AudioManager 当前支持 dual BGM（normal/void 切换），
            // 如果 roomConfig.bgmOverride 不为空，可以扩展 AudioManager 支持临时 BGM 覆盖
            Debug.Log($"切换 BGM 为：{trigger.roomConfig.bgmOverride.name}");
        }

        Debug.Log($"进入房间：{trigger.roomConfig?.roomName}");
    }

    private void DeactivateRoom(RoomTrigger trigger)
    {
        // 禁用房间子物体（如 BulletSpawner → 自动停止射击）
        foreach (GameObject obj in trigger.activateOnEnter)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
```

- [ ] **步骤 2：Commit**

```bash
git add Assets/Scripts/Mgr/RoomManager.cs
git commit -m "feat(room): add RoomManager singleton for room transitions"
```

---

### 任务 9：修改 PlayerController（暴露死亡触发接口）

**文件：**
- 修改：`Assets/Scripts/GameScene/Player/PlayerController.cs`

子弹需要能触发玩家死亡。目前 `OnCollisionEnter2D` 只检查 `Trap` 标签，但子弹是 `Trigger` 碰撞，且标签不同。需要：

- [ ] **步骤 1：在 PlayerController 中暴露公共死亡方法**

在 `DieAndRespawnRoutine()` 所在的区域上方添加：

```csharp
/// <summary>
/// 供外部脚本（子弹、陷阱等）触发玩家死亡。
/// isDead 检查防止连续触发。
/// </summary>
public void Die()
{
    if (!isDead)
        StartCoroutine(DieAndRespawnRoutine());
}
```

放在 `OnCollisionEnter2D` 之前。

- [ ] **步骤 2：更新子弹的子弹碰撞逻辑**

在 Bullet.cs 中，将 `OnTriggerEnter2D` 的玩家碰撞处理改为调用：

```csharp
PlayerController player = other.GetComponent<PlayerController>();
if (player != null)
{
    player.Die();
    BulletSpawner spawner = GetComponentInParent<BulletSpawner>();
    spawner?.ReturnBullet(this);
}
```

- [ ] **步骤 3：Commit**

```bash
git add Assets/Scripts/GameScene/Player/PlayerController.cs Assets/Scripts/GameScene/Bullet/Bullet.cs
git commit -m "feat(player): expose Die() method for bullet collision trigger"
```

---

### 任务 10：创建 3 个 RoomConfigSO 资产文件

**文件：**
- 创建：`Assets/Resources/SO/Rooms/Room_Tutorial.asset`
- 创建：`Assets/Resources/SO/Rooms/Room_Bullet.asset`
- 创建：`Assets/Resources/SO/Rooms/Room_Parkour.asset`

这些需要 Unity Editor 中手动创建，因为 .asset 是二进制序列化文件，不能直接在文本编辑器中编写。

- [ ] **步骤 1：在 Unity Editor 中创建 3 个 .asset**

操作方法：
1. 在 Project 窗口中导航到 `Assets/Resources/SO/Rooms/`
2. 右键 → Create → Config/Room（由 RoomConfigSO 的 CreateAssetMenu 提供）
3. 命名为 `Room_Tutorial`
4. 重复创建 `Room_Bullet` 和 `Room_Parkour`

- [ ] **步骤 2：配置三个房间的参数**

Room_Tutorial：
| 字段 | 值 |
|------|----|
| roomName | "第一关：新手教学" |
| description | "利用面具穿越两个世界抵达出口" |
| bulletStats | (空) |
| fireInterval | 1 |
| bgmOverride | (空，沿用场景 BGM) |

Room_Bullet：
| 字段 | 值 |
|------|----|
| roomName | "第二关：弹幕穿梭" |
| description | "表世界的子弹逼迫你不断切换世界" |
| bulletStats | (新建或指向一个 BulletStatsSO) |
| fireInterval | 1 |
| bgmOverride | (可选) |

Room_Parkour：
| 字段 | 值 |
|------|----|
| roomName | "第三关：虚空跑酷" |
| description | "表世界无路，依靠预览规划里世界路线" |
| bulletStats | (空) |
| fireInterval | 1 |
| bgmOverride | (可选) |

- [ ] **步骤 3：创建初始子弹配置 .asset**

1. 在 `Assets/Resources/SO/Rooms/` 下右键 → Create → Config/Bullet
2. 命名为 `Bullet_Default`
3. 设置 speed=5, color=红色或橙色 (如 #FF4444), lifetime=3

- [ ] **步骤 4：Commit**

```bash
git add Assets/Resources/SO/Rooms/
git commit -m "feat(so): add 3 RoomConfigSO assets and default bullet config"
```

---

### 任务 11：创建 BulletStatsSO 资产

**文件：**
- 创建：`Assets/Resources/SO/Rooms/Bullet_Default.asset`

（已在任务 10 步骤 3 中覆盖）

- [ ] **步骤 1：Commit（如果任务 10 未包含）**

```bash
git add Assets/Resources/SO/Rooms/Bullet_Default.asset
git commit -m "feat(so): add default bullet stats asset"
```

---

### 任务 12（可选）：DOTween UI 集成

**前提：** 需要先在 Package Manager 中安装 DOTween（或从 Asset Store 导入）。

**文件：**
- 修改：`Assets/Scripts/UI/BasePanel.cs`

- [ ] **步骤 1（可选）：在 BasePanel 中集成 DOTween 淡入淡出**

```csharp
using DG.Tweening;

public abstract class BasePanel : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public bool isShow = false;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {
        Init();
    }

    public abstract void Init();

    public virtual void ShowMe()
    {
        isShow = true;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
    }

    public virtual void HideMe()
    {
        isShow = false;
        canvasGroup.DOFade(0f, 0.2f).SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
```

注意：此修改会改变 BasePanel 的行为（从当前的无动画直接显示变为 DOTween 动画）。需要确认 DOTween 已导入。

- [ ] **步骤 2（可选）：Commit**

```bash
git add Assets/Scripts/UI/BasePanel.cs
git commit -m "feat(ui): integrate DOTween fade animation in BasePanel"
```