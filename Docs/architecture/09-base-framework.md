# 09 基础框架（base-framework：Singleton 系）

> 状态：**保留不改**
> 本文件是 AI 写脚本用的功能需求规格。

## 职责

- 提供单例基类，供 Manager（UIManager/AudioManager/RoomManager 等）继承。

## 文件清单

| 文件 | 状态 | 职责 |
|---|---|---|
| `Assets/Scripts/Base/Singleton.cs` | 保留 | 非 MonoBehaviour 单例（UIManager 用） |
| `Assets/Scripts/Base/SingletonMono.cs` | 保留 | MonoBehaviour 单例，DontDestroyOnLoad（Audio/RoomManager 用） |
| `Assets/Scripts/Base/SingletonAutoMono.cs` | 保留 | 自动挂载式 MonoBehaviour 单例（当前无使用方） |
| `Assets/Scripts/Base/BaseManager.cs` | 保留 | 泛型管理器基类（GetInstance）（当前无使用方） |
| `Assets/Scripts/Main.cs` | 保留 | 入口：启动时 ShowPanel\<BeginPanel\> |

## 关键行为（写脚本时不得破坏）

- `Singleton<T>`：惰性创建（`new T()`），属性访问。
- `SingletonMono<T>`：Awake 中防重复（已有实例则销毁自身），DontDestroyOnLoad。
- `SingletonAutoMono<T>`：首次访问自动创建 GameObject 并挂组件。

## 核心规则（硬约束）

1. 禁止重写基类（已稳定）。
2. 新 Manager 继承哪个基类按职责选：需要场景对象用 SingletonMono；纯逻辑用 Singleton。
3. 不使用 SingletonAutoMono/BaseManager 的现有代码保持不动（无使用方，如需清理先问用户）。

## 依赖

- 依赖：UnityEngine。
- 被依赖：08-presentation（UIManager/AudioManager/RoomManager）、04-levels（RoomManager）。

## 验收

见 `../tests/09-base-framework-test.md`。
