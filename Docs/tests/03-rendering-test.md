# 03 渲染系统验收清单（rendering-test）

> 验收方式：用户在 Unity 中手动逐条验证。AI 交付代码时必须附本清单。

## 前置

- Unity 打开 GameScene，进入 Play Mode
- 场景已配置两个 Volume（表世界 Profile / 里世界 Profile）
- URP 2D Renderer 已启用 Light 2D；PPv2 已从 manifest 移除

## 逐条验证

- [ ] **表世界视觉**：冷色调、清朗、正常曝光，无明显噪点/扫描线
- [ ] **里世界视觉**：低饱和、偏暗、有噪点/扫描线（视觉上明显"另一个世界"）
- [ ] **切换过渡**：按 J 切换时，两个 Volume 的 weight 平滑交叉（约 0.4s），无跳变
- [ ] **顿帧期间过渡继续**：切换瞬间 timeScale=0 的 0.15s 内，视觉过渡仍在进行（unscaledDeltaTime 生效）
- [ ] **转场效果**：切换时自写 Renderer Feature 转场（扫屏/撕裂）正常播放一次，不残留
- [ ] **2D 光照差异**：表世界明亮，里世界昏暗；里世界局部光源（如玩家持灯）照亮周围
- [ ] **光照遮挡**：光被墙/障碍物遮挡产生明暗层次（不是整体变暗的假光）
- [ ] **切换后光照同步**：回到表世界光照恢复，无延迟/闪烁
- [ ] **性能**：切换瞬间无卡顿（Profiler 抽查帧耗时，无明显 spike）
- [ ] **无 PPv2 残留**：Packages/manifest.json 无 com.unity.postprocessing（AI 自查项）

## 通过标准

- 全部勾选 → 验收通过，AI 方可 commit
