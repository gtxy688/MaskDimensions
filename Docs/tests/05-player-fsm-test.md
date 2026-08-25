# 05 玩家状态机验收清单（player-fsm-test）

> 验收方式：用户在 Unity 中手动逐条验证。AI 交付代码时必须附本清单。

## 前置

- Unity 打开 GameScene，进入 Play Mode
- 玩家已接入 Kinematic2D（无 Rigidbody2D 组件）
- 状态机接入新物理后整体回归

## 逐条验证

- [ ] **状态流转**：Idle ↔ Move ↔ Jump ↔ Fall 切换正常，动画匹配
- [ ] **受击状态**：Hit 状态进入（击退生效）、退出正常；Hit 期间禁止切面具
- [ ] **死亡状态**：Die 进入 → 死亡特效 → 重生点复活 → 回 Idle，理智/面具重置
- [ ] **传送**：TeleportTrigger 触发消散/凝聚动画，传送后状态正常
- [ ] **切面具白名单**：Idle/Move/Jump/Fall 中可切换；Hit/Die 中不可切换
- [ ] **输入手感**：土狼时间 0.15s / 跳缓冲 0.15s 手感与改造前一致（或更好）
- [ ] **时间系统回归**：顿帧（0.15s 停顿）正常；预览慢放（timeScale 0.05）正常；后处理/粒子用 unscaledDeltaTime 不受影响
- [ ] **状态无残留**：死亡重生后无"上一状态残留"（如仍在下落/仍戴面具）
- [ ] **事件广播**：切换/死亡/理智事件均广播且订阅方响应（UI/音频/渲染）
- [ ] **无 Rigidbody2D 依赖**：PlayerController 及所有 State 无 RB.velocity 引用（AI 自查项）

## 通过标准

- 全部勾选 → 验收通过，AI 方可 commit
