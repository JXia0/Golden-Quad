# Golden-Quad · 放开我的手

Unity 6.6 / 2D 叙事游戏。按住维持连接，松开把东西、声音和人留在它们该去的地方。

当前玩法与选择后果见 [可改变后续的玩法版本](Docs/MeaningfulChoicesPlayable.md)。

- A / D、方向键：行走；Space / W / ↑：跳跃。
- 按住 E：牵手、携带、呼吸；松开 E：放手、放下、发声。
- 玩具：按住 E 上弦，用 A / D 朝向，松开滚过去；孩子会先滚回来，接住后再滚给他。
- 舞台：按住 E 时 A / D 控制发声朝向，两侧听众各接一拍；最后重现自己选的方向和长短。
- F：操作机关、窗户、线索和门；带着玩具离开幼儿园会改变研究室可用的工具。
- 研究：探索上层档案、复用箱子或板开门搭路，以灯、玩具或牵手帮助模型；成功后带报告走捷径回工作台。
- 结尾：研究室中陪伴或放手的实际做法决定对方等待你还是主动离开，门前仍可重新牵手。
- R：回到当前检查点；结尾 Enter：重玩。
- 从 `Assets/Scenes/00_Prologue.unity` 开始，也可独立运行任一正式章节。

新版章节在 Play Mode 中接入，保留美术手动排版。不要为了启用新版玩法执行 Rebuild All Game Scenes。

检查菜单：`Tools > Let Go > Validate Emotional Journey` 和 `Run Emotional Journey Playthrough Checks`。测试日志写入 `Logs`。
