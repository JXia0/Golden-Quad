# Golden-Quad · 放开我的手

Unity 6.6 / 2D 叙事游戏。按住维持连接，松开把东西、声音和人留在它们该去的地方。

当前 GGJ 版本的方向、玩法与美术优先级见 [情绪玩法版本](Docs/GGJEmotionalPlayable.md)。

- A / D、方向键：行走；Space / W / ↑：跳跃。
- 按住 E：牵手、携带、呼吸；松开 E：放手、放下、发声。
- 玩具：按住 E 上弦，用 A / D 朝向，松开滚过去；孩子会先滚回来，接住后再滚给他。
- 舞台：按住 E 时 A / D 控制发声朝向，两侧听众各接一拍；最后重现自己选的方向和长短。
- 研究：搬动同一块支撑板跨过两处缺口，试通选中的方案后才能定稿；结尾可以在门前再牵一次。
- R：回到当前检查点；结尾 Enter：重玩。
- 从 `Assets/Scenes/00_Prologue.unity` 开始，也可独立运行任一正式章节。

新版章节在 Play Mode 中接入，保留美术手动排版。不要为了启用新版玩法执行 Rebuild All Game Scenes。

检查菜单：`Tools > Let Go > Validate Emotional Journey` 和 `Run Emotional Journey Playthrough Checks`。测试日志写入 `Logs`。
