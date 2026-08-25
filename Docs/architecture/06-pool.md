# 06 对象池（pool）

> 状态：**保留不改**（代码保留，叙事降级）
> 本文件是 AI 写脚本用的功能需求规格。选型论证看 `../decisions.md`（D11）。

## 职责

- 复用子弹/特效等组件对象，避免频繁 Instantiate/Destroy。
- 通过 IPoolable 生命周期回调保证出池/回池状态重置。

## 文件清单

| 文件 | 状态 | 职责 |
|---|---|---|
| `Assets/Scripts/Pool/ObjectPool.cs` | 保留 | 泛型对象池核心 |
| `Assets/Scripts/Pool/IPoolable.cs` | 保留 | 生命周期接口 |

## ObjectPool 现有能力（写脚本时不得破坏）

- `ObjectPool<T>(prefab, parent, prewarmCount, maxCapacity)`：构造即预热。
- `Get()`：出池（空池则实例化），激活对象，触发 `OnSpawn()`。
- `Return(obj)`：回池，触发 `OnDespawn()`；防重复入池校验（HashSet）；超过容量上限直接销毁。
- `Clear()`：清空销毁全部。
- 计数属性：ActiveCount / InactiveCount / TotalCount。

## 核心规则（硬约束）

1. 禁止重写或"优化"现有实现（已稳定）。
2. 禁止新增"为写而写"的池（如把非热点对象也池化）。
3. 若因性能原因要动它：必须先测 Profiler 数据，有数据才能改（否则保持现状）。
4. IPoolable 语义：OnSpawn = 重置可用状态；OnDespawn = 清理（解绑事件/停协程/清引用）。

## 依赖

- 依赖：无。
- 被依赖：07-bullet（BulletSpawner 使用）。

## 验收

见 `../tests/06-pool-test.md`。
