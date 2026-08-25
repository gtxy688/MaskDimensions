# 09 基础框架验收清单（base-framework-test）

> 验收方式：用户在 Unity 中手动逐条验证 + AI 自查。AI 交付代码时必须附本清单。

## 逐条验证（运行回归）

- [ ] **UIManager 单例**：全流程中仅一个 Canvas/UIManager 实例（Hierarchy 检查）
- [ ] **AudioManager 单例**：跨场景不销毁，重复加载场景不产生第二个实例
- [ ] **RoomManager 单例**：房间切换正常，单例引用唯一
- [ ] **入口**：Main.Start 正确显示 BeginPanel，无空引用报错
- [ ] **跨场景**：BeginScene → GameScene → 回 BeginScene，管理器引用不失效

## 逐条验证（AI 自查，代码审查）

- [ ] SingletonMono 子类均通过 Instance 访问，无 `new` 直接构造
- [ ] 无重复挂载单例组件导致的"销毁自身"日志刷屏
- [ ] SingletonAutoMono / BaseManager 无使用方（保持现状，如需清理已询问用户）

## 通过标准

- 全部勾选 → 验收通过，AI 方可 commit
