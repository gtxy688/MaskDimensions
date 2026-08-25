# 10 旧物理控制器归档（legacy-physics）

> 状态：**已删除（归档）**
> 本文件仅记录历史，不指导任何新代码。为什么删：见 `../decisions.md`（D2）。

## 历史

以下文件曾是项目中的死代码（从未被任何脚本引用），在深度改造中删除：

| 文件（已删） | 曾经的角色 |
|---|---|
| `Assets/Scripts/GameScene/Physics/RaycastController2D.cs` | 射线检测基类（SkinWidth 内缩 + 射线原点/间距计算） |
| `Assets/Scripts/GameScene/Physics/PlayerPhysicsController2D.cs` | 运动学控制器（X/Y 分轴检测、斜坡爬坡/下坡、单向板） |

## 为什么归档（而非复用）

1. 它们是经典教程结构（Sebastien Lague 风格），面试叙事上"自研"不成立。
2. 新框架 Kinematic2D（见 01-kinematic2d.md）从零重写，API 为维度过滤器设计。
3. git 历史保留全部代码（`git log` / 早期 commit 可找回），不怕丢。

## 对 AI 的提示

- 若在旧 commit 或他人代码中看到这两个类，**不要**引用或移植其实现。
- 若需要斜坡/单向板算法参考，自己推导或查阅 Kinematic2D 实现（M3 阶段），不要翻教程原版。
