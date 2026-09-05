# 程序与呈现的交接

作者分工确认：本任务负责产品把控、核心玩法和程序，包括情绪主线、范围取舍、节奏、整体验收。美术、灯光、镜头、动画、音效风格与正式效果由美术和 5.6 sol 执行。

程序没有重建或保存正式场景，没有修改美术排版。此前新增的舞台灯光漂移、透明度覆盖、幕布位移、空椅子/围巾/台灯自动放置，以及灰盒色块隐藏已撤回。

## 可以直接接入的状态和事件

| 对象/组件 | 接口 | 呈现可使用的时机 |
|---|---|---|
| `HandConnection` | `CurrentTarget`、`IsSelfAnchoring`、`SelfChargeNormalized`、`Stability01` | 牵手、呼吸及稳定程度；携带资料不等于完全稳定 |
| `HandConnection` | `LastReleasedTarget`、`LastReleaseTime`、`LastFullSelfReleaseTime` | 真实松手与完整呼吸释放；R 取消不会产生这些新事件 |
| `HoldTarget` | `IsHeld`、`IsPlaced`、`IsWaiting`、`Reassurance01`、`HeldDuration` | 孩子缩回去、等候、站稳；携带与放置 |
| `HoldTarget` | `Released` 回调 | 一次主动松手 |
| `HoldSocket` | `Placed` 回调、`Occupant`、`Completed`、`Locked` | 接收物件、换稿、定稿；换稿只算一次任务进度 |
| `StagePerformance` | `SpokenPhrases`、`PhraseSpoken(int, Transform)` | 三次发声；参数 1–3 和实际站位，可据此开灯、表演、开幕、播放声音 |
| `ResearchDraft` | `Observation01`、`EvidenceObserved(HoldTarget)`、`DraftEvidence`、`IsCommitted` | 灯下观察、证据内容显现、换稿和定稿 |
| `ReleaseEndingGoal` | `IsDeparting`、`HasEnteredDoor`、`RecipientDeparted`、`RecipientEnteredDoor` | 年轻人离开和进入门内 |
| `ReleaseEndingGoal` | `HideRecipientOnEntry` | 默认为 true 的临时消失实现；若接入进门动画，可设为 false 自行处理显示 |
| `EmotionalEnvironment` | `Resolution01` | 章节进度带来的平静程度；原有环境组件读取它 |

现有回调使用 C# `Action`，在对象初始化完成后订阅。章节组件由 `StorySceneDirector.Start` 安装；呈现脚本可在下一帧接入，或使用 `GetComponent` 等待对应组件出现。

## 灰盒标记可替换

`JourneyVisuals` 只创建辅助光圈、波形和研究路径示意，并提供新增书包的贴图引用。它们是功能验证用的默认标记，不是最终效果标准。

接入正式呈现后调用 `JourneyVisuals.SetGuidesVisible(false)`，会关闭该组件创建的辅助标记，不隐藏角色、书包或原有场景物件，也不停止交互逻辑。

新增书包对象名为 `Bag from home`，挂在当前 `Story Scene Director` 下。它带 `HoldTarget` 和 `ArtSlot`，槽名为 `prop_child_backpack`；其他场景的既有美术由原来的流程管理。

## 不要改变的规则

- 门前需要真实牵手后松开，允许回头，不用恐惧倒计时惩罚停留。
- 孩子被拉太远后必须靠近等待，不能用播放完动画直接跳过陪伴规则。
- 舞台站位只有完整吸气后松开才完成；走过标记不能自动完成。高处碰撞平台为单向平台，玩家跳跃力已调到能到达。
- 证据定稿前可换；结论提交才锁住。不要在第一次播放投递效果后隐藏另一份证据。
- 结尾先让年轻人走进门，再恢复玩家移动；玩家向右继续才切黑，不能由动画自行结束整个游戏。

程序回归结果：`Logs/JourneyRegressionChecks.txt`；真实输入与物理通关结果：`Logs/JourneyPlaythroughChecks.txt`。
