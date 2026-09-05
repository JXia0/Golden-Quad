# 选择与探索版验证记录

2026-09-05，Unity 6000.6.0f1。使用 `Temp/JourneyValidationProject` 隔离副本，未重建或保存正式工作区场景。

- 15 项逻辑/场景检查通过。
- 71 项真实键盘输入与 Unity 物理流程断言通过。
- 完整流程：序章 → 幼儿园 → 舞台 → 研究室 → 告别 → 重玩；另跑研究室替代路线与主动告别。
- Windows x64 构建成功，0 个构建错误。

## 本次覆盖

- 孩子张力等待、取回玩具后重新交回、目标不重复计数。
- 舞台两种站位、自选两种节奏、错误重现可恢复。
- 箱子开门后回收搭路；留下工具前不能走穿机关门。
- 箱子实体垫脚、两次跳跃进入上层档案、读取线索、取回折叠板。
- 折叠板压门后回收搭路；灯可实际携带和放下。
- 缺口和黑暗确实阻止模型；撤掉帮助后退回、恢复帮助后继续（逻辑检查覆盖撤灯退回与玩具替代照明）。
- 开窗让暗区灯失效，关窗恢复；两条不同解法都能产出报告并返回工作台。
- 童年玩具在独立章节测试夹具中按“已带走”状态提供；真实 E 输入上弦，放下后效果继续。完整主线实际执行的是取回、再还给孩子后离开。
- 研究室自主通过的结果传到结尾，年轻人在没有先牵手的情况下主动离开。
- 陪伴版本仍需准备和松手；门前再次牵住会暂停离开，玩家提前走到门后也不能跳过最后一步。
- 重玩清空节奏与实验选择，研究室访问次序保留以切换初始窗况。

## 复现与文件

`Tools > Let Go > Validate Emotional Journey` 运行逻辑检查；`Run Emotional Journey Playthrough Checks` 运行完整流程。批处理入口为 `JourneyPlaythroughChecks.ValidateAndBuildWindowsBatch`；开发中的研究室快速检查使用 `ValidateWorkshopBatch`。

详细结果：`Logs/JourneyRegressionChecks.txt`、`Logs/JourneyPlaythroughChecks.txt`、`Logs/JourneyBuild.txt`。试玩包：`Builds/LetGo-ChoicePlaytest.zip`，解压后运行 `ChoicePlaytest/LetGo.exe`。

已检查 1280×720 的操作画面。新增模型、机关和范围提示保留功能占位，正式呈现见 `GameplayPresentationHandoff.md`。自动检查证明通路和因果可执行；首次试玩的发现过程、趣味及情绪效果仍应由真实玩家检验。
