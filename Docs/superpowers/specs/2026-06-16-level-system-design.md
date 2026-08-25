# Level System + SO + Object Pool 设计方案（最终版）

## 概述

在单场景（GameScene）内实现 3 个关卡房间的系统。每个房间由 ScriptableObject 驱动配置，面向数据而非面向场景进行关卡调整。

---

## 一、架构总览

```
GameScene（单一场景）
├── Room_1（教学关）    — 表世界地刺 → 切里世界通行
├── Room_2（子弹关）    — 表世界横向扫弹，里世界通行
└── Room_3（跑酷关）    — 表世界无路，里世界连续跑酷

每个房间：
  RoomTrigger（入口触发器）
  CinemachineVirtualCamera（房间相机）
  Tilemap（地形，分表/里两层）
  ExitPoint（出口传送门）
```

---

## 二、RoomConfigSO（关卡数据资产）

所有 SO 文件放在 `Resources/SO/Rooms/` 下。

```csharp
// Assets/Scripts/SO/RoomConfigSO.cs

[CreateAssetMenu(fileName = "NewRoom", menuName = "Config/Room")]
public class RoomConfigSO : ScriptableObject
{
    public string roomName;
    [TextArea] public string description;

    [Header("子弹配置（第 2 关专用）")]
    public BulletStatsSO bulletStats;   // 子弹属性共享引用
    public float fireInterval = 1f;     // 横向扫射发射间隔（秒）

    [Header("BGM")]
    public AudioClip bgmOverride;
}
```

---

## 三、BulletStatsSO（子弹属性资产）

```csharp
// Assets/Scripts/SO/BulletStatsSO.cs

[CreateAssetMenu(fileName = "NewBullet", menuName = "Config/Bullet")]
public class BulletStatsSO : ScriptableObject
{
    public float speed = 5f;       // 子弹飞行速度
    public Color color = Color.white; // 子弹颜色
    public float lifetime = 3f;    // 子弹存活时间（秒）
}
```

---

## 四、RoomTrigger（关卡入口脚本）

```csharp
// Assets/Scripts/GameScene/RoomTrigger.cs

public class RoomTrigger : MonoBehaviour
{
    public RoomConfigSO roomConfig;
    public CinemachineVirtualCamera roomVcam;
    public Transform playerSpawnPos;    // 进入时玩家位置
    public GameObject[] activateOnEnter; // 进入时激活的物体（如第 2 关的子弹生成器）

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RoomManager.Instance.EnterRoom(this);
        }
    }
}
```

---

## 五、RoomManager（关卡管理器）

```csharp
// Assets/Scripts/Mgr/RoomManager.cs

public class RoomManager : SingletonMono<RoomManager>
{
    public void EnterRoom(RoomTrigger trigger)
    {
        // 1. 切 Cinemachine vcam
        // 2. 激活 trigger.activateOnEnter 中的物体
        // 3. 如果 roomConfig 有 bgmOverride，切 BGM
        // 4. 将玩家 spawn 到入口
    }
}
```

---

## 六、ObjectPool（通用对象池）

供第 2 关子弹回收复用。

```csharp
// Assets/Scripts/Pool/ObjectPool.cs

public class ObjectPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new();
    private T prefab;
    private Transform parent;

    public T Get()
    {
        if (pool.Count > 0)
            return pool.Dequeue();
        return Object.Instantiate(prefab, parent);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

---

## 七、关卡一览

| 关卡 | 主题 | 核心体验 | 理智消耗 |
|------|------|---------|---------|
| 1 教学关 | 地刺教学 | 表世界有地刺 → 切里世界通行；认识切换和理智 | 不消耗 |
| 2 子弹关 | 横向扫弹 | 表世界每 1s 一波横向扫弹，里世界安全但烧理智；逼玩家动态切换 | 正常消耗 |
| 3 跑酷关 | 预览 + 极限跑酷 | 表世界无路，里世界连续平台 + 地刺；必须长按 J 预览规划路线再一次性通过 | 高消耗 |

---

## 八、文件清单

| 文件 | 类型 | 说明 |
|------|------|------|
| `Assets/Scripts/SO/RoomConfigSO.cs` | 新增 | 关卡数据资产 |
| `Assets/Scripts/SO/BulletStatsSO.cs` | 新增 | 子弹属性资产 |
| `Assets/Scripts/Mgr/RoomManager.cs` | 新增 | 关卡管理器 |
| `Assets/Scripts/GameScene/RoomTrigger.cs` | 新增 | 房间入口触发器 |
| `Assets/Scripts/GameScene/Bullet/Bullet.cs` | 新增 | 子弹行为 |
| `Assets/Scripts/GameScene/Bullet/BulletSpawner.cs` | 新增 | 横向扫射生成器 |
| `Assets/Scripts/GameScene/RoomExit.cs` | 新增 | 出口传送门 |
| `Assets/Scripts/Pool/ObjectPool.cs` | 新增 | 通用对象池 |
| `Resources/SO/Rooms/` | 新增 | 存放 3 个 .asset 文件 |