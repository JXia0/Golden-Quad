# 程序与呈现的交接

> 舞台与研究的最新规则覆盖下面旧说明：见 [表演与学习交接](PerformanceAndLearning.md)。第三个舞台目标改为主动谢幕；实验可以召回学习，报告提交前可改结果。父母协作仍待重做，不将已有自动带路视为完成。

> 最新：父母牵手现可实际带路，研究室按需显示操作反馈；控制与呈现接口见 [父母带路与研究室减负](ParentSupportAndResearchFocus.md)。

> 新增：两个 interlude 已恢复，研究室目标和物件引导已接入；新状态、已用素材及替换注意事项见 [恢复与引导交接](RestoredInterludesAndResearchGuide.md)。

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
| `ComfortToyPlay` | `Exchanges`、`IsRolling`、`Winding01`、`ChildReturnedToy`、`SharedPlayCompleted` | 玩具上弦和滚动；孩子第一次接到主动滚回，第二次接到才抱住 |
| `StagePerformance` | `VoiceAim`、`ExpectedListener`、`AudienceAnswered(int, Vector3)` | 朝向 -1 到 1；预期听众为 0 左 / 1 右 / -1 自由选；回应事件提供实际位置 |
| `StagePerformance` | `ComposedBeats`、`ReprisedBeats`、`FirstBeatLong`、`SecondBeatLong`、`NoteReleased(int,bool)`、`RepriseMistimed` | 自选两拍、重现和温和重试；注意短音也应有声音和回应 |
| `ReleaseEndingGoal` | `IsDeparting`、`HasEnteredDoor`、`RecipientDeparted`、`RecipientEnteredDoor` | 年轻人离开和进入门内 |
| `ReleaseEndingGoal` | `IsReadyToLeave`、`IsHeldBack`、`TimesReheld`、`OnwardPositionX`、`ReadyToLeave`、`RecipientReheld` | 主动迈步被手留住，以及离开途中再次牵回；最后一步的目标可能前移，避免玩家提前走到门后就自动结束 |
| `ReleaseEndingGoal` | `ReadinessBeat`、`RecipientBreath(int,bool)` | 沿用玩家在舞台上留下的两拍，参数 0 / 1 与是否长音；用呼吸或短音呼应即可 |
| `ReleaseEndingGoal` | `HideRecipientOnEntry` | 默认为 true 的临时消失实现；若接入进门动画，可设为 false 自行处理显示 |
| `EmotionalEnvironment` | `Resolution01` | 章节进度带来的平静程度；原有环境组件读取它 |

现有回调使用 C# `Action`，在对象初始化完成后订阅。章节组件由 `StorySceneDirector.Start` 安装；呈现脚本可在下一帧接入，或使用 `GetComponent` 等待对应组件出现。

## 灰盒标记可替换

`JourneyVisuals` 只创建辅助光圈、波形和研究路径示意，并提供新增书包的贴图引用。它们是功能验证用的默认标记，不是最终效果标准。

接入正式呈现后调用 `JourneyVisuals.SetGuidesVisible(false)`，会关闭该组件创建的辅助标记，不隐藏角色、书包或原有场景物件，也不停止交互逻辑。

新增瞄准线、听众接收点、支撑板缺口和观察进度属于必要操作反馈，由 `GameplayLine` 创建；不会跟随装饰标记一起关闭。**正式替代反馈接入后**可调用 `SetGameplayCuesVisible(false)`。不要在替代品尚未接入时关闭这些操作反馈，否则玩家无法瞄准或判断搭桥位置。

新增书包对象名为 `Bag from home`，挂在当前 `Story Scene Director` 下。它带 `HoldTarget` 和 `ArtSlot`，槽名为 `prop_child_backpack`；其他场景的既有美术由原来的流程管理。


## 不要改变的规则

- 门前需要真实牵手后松开，允许回头，不用恐惧倒计时惩罚停留。
- 孩子被拉太远后必须靠近等待，不能用播放完动画直接跳过陪伴规则。
- 玩具第一回合必须滚回来，让玩家接住再送出；不要第一次接到就播放最终抱玩具状态。
- 舞台开场是完整呼吸，之后自由选择两拍的听众与长短，可连续回应同侧，也可 Q 改写。重现自己的方向和长短或 F 请观众接唱后，仍需 F 主动谢幕。走过标记不能自动完成；短音不能要求吸满气。
- 结尾全程保留玩家移动及门前再次牵手。人进门后玩家向右继续才切黑，不能由动画自行结束整个游戏。

程序回归结果：`Logs/JourneyRegressionChecks.txt`；真实输入与物理通关结果：`Logs/JourneyPlaythroughChecks.txt`。

## 当前研究室已改为空间探索（替代 ResearchDraft）

旧卡片/资料板任务在运行时停用，不再制作对应投递动画。当前规则以 `MeaningfulChoicesPlayable.md` 为准。

| 根节点 | 坐标 | 槽名/用途 |
|---|---|---|
| Workshop Crate | 4,-2.1 | prop_stage_box；实体 1.1×1.3 箱，可踩、搬、压机关、搭落脚点 |
| Archive Shelf / Archive Notebook | -2,-0.35 / -3,0.3 | workshop-platform / prop_research_book；可选上层探索 |
| Folding Plank | -2,0.8 | prop_research_bridge；可搬运并反复放置 |
| Pressure Plate / Workshop Shutter | 6.5,-2.68 / 9,-0.75 | workshop-plate / workshop-shutter；地面机关与实体门 |
| Inside Shutter Latch | 10.5,-1.5 | workshop-latch；永久开启返程 |
| Workshop Learner | 12,-2.25 | prop_research_model；移动由程序控制，只在子物件做姿态 |
| Portable Lamp | 12.6,-2.2 | prop_researchlight；光的有效半径 3.6，漏风暗区会失效 |
| The Toy You Kept | -5,-2.3 | prop_toy；仅取回童年玩具的玩家拥有，松手后发声 12 秒 |
| Draft Window Handle | 21,-1.5 | workshop-window；风源与可操作窗 |
| Your Working Report | 22,-2.1 | prop_research_report；抵达后才出现 |
| Return Workbench | 1,-1.9 | workshop-desk；带报告回来完成探索闭环 |

已有槽从 Assets/Sprites 或 Assets/Art/Final 自动引用。其余槽允许正式 Sprite 覆盖；目前有功能占位。不要为了填槽移动碰撞/交互根节点。

| 组件 | 公开状态/回调 | 用途 |
|---|---|---|
| ResearchExpedition | Crate / Plank / Lamp / Toy / Learner / Report | 获取实际可交互根节点，Toy 可为空 |
| ResearchExpedition | GateOpen / GateLatched | 压力门开合、门闩常开 |
| ResearchExpedition | State / LearnerChanged | Waiting / Walking / NeedsBridge / NeedsComfort / Returning / Arrived；停在缺口与害怕退回须区分 |
| ResearchExpedition | WindActive / LampWorking / WindowChanged | 关窗后恢复光；不要只播放风特效而把灯继续显示为有效 |
| ResearchExpedition | ShortcutOpen / ShortcutOpened / ReportReturned | 显示回程入口、报告与桌上成果 |
| ResearchExpedition | CluesFound | 可选调查，无收集配额 |
| JourneyChoices | TookChildhoodToy / CrossingTool / TestedResearchModel / LearnedIndependentDeparture | 实际记录的资源和解法；不要自行猜测分支 |
| ReleaseEndingGoal | InitiatesOwnDeparture | true 时靠近就主动离开；false 时仍等待陪伴。两条都允许再次牵住 |

必要反馈：机关的通电方向、箱子的可踩顶部、缺口宽度、灯范围/熄灭、窗户状态、模型退回和恢复、回程入口。保留 GameplayLine 到替代反馈完成。结尾不可由动画直接切黑，依然等待玩家最后一步。

玩具声音接口：ResearchExpedition.ToyWinding01、ToySoundRemaining、ToyWound；默认保留声音范围圈，正式发条/旋律接这三个值。
