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
| `ComfortToyPlay` | `Exchanges`、`IsRolling`、`Winding01`、`ChildReturnedToy`、`SharedPlayCompleted` | 玩具上弦和滚动；孩子第一次接到主动滚回，第二次接到才抱住 |
| `StagePerformance` | `VoiceAim`、`ExpectedListener`、`AudienceAnswered(int, Vector3)` | 朝向 -1 到 1；预期听众为 0 左 / 1 右 / -1 自由选；回应事件提供实际位置 |
| `StagePerformance` | `ComposedBeats`、`ReprisedBeats`、`FirstBeatLong`、`SecondBeatLong`、`NoteReleased(int,bool)`、`RepriseMistimed` | 自选两拍、重现和温和重试；注意短音也应有声音和回应 |
| `ResearchDraft` | `Observation01`、`EvidenceObserved(HoldTarget)`、`DraftEvidence`、`IsCommitted` | 灯下观察、证据内容显现、换稿和定稿 |
| `ResearchDraft` | `TrialFigure`、`TrialBridge`、`TrialNeedsBridge`、`RequiredBridgeX`、`TrialProgress01` | 实际模型和可搬支撑板；两处缺口要能看清是否被覆盖，不能直接用动画越过缺口 |
| `ResearchDraft` | `TrialState`、`TrialResponse01`、`TrialStateChanged` | `Approaching` / `NeedsCompany` / `NeedsSpace` / `Continuing` / `Arrived`；等陪伴与等空间应有不同身体姿态 |
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

新增研究对象名为 `Research Trial Figure` 和 `Movable Experiment Bridge`，槽分别为 `prop_research_model`、`prop_research_bridge`。模型移动由程序负责；板的根对象由携带系统负责。美术可在子对象上调整造型和动画，别移动交互根节点。对应文件以 Sprite 导入 `Assets/Art/Final` 或 `Assets/Sprites` 后，`JourneyArtImporter` 自动添加 Build 资源引用。先交小人/纸模型和一块板两张静帧即可。

## 不要改变的规则

- 门前需要真实牵手后松开，允许回头，不用恐惧倒计时惩罚停留。
- 孩子被拉太远后必须靠近等待，不能用播放完动画直接跳过陪伴规则。
- 玩具第一回合必须滚回来，让玩家接住再送出；不要第一次接到就播放最终抱玩具状态。
- 舞台第一句是完整呼吸，第二句是朝两侧听众各发一拍，第三句重现自己的方向和长短。走过标记不能自动完成；短音不能要求吸满气。
- 证据定稿前可换；每次换方案需要重新搭桥并试通。模型抵达后结论才会被接收。不要第一次选卡就隐藏另一份证据。
- 支撑板靠近缺口松开会吸附对齐，仍可重新拿起。吸附改变交互根的位置，呈现只需跟随，不要另写一套放置判定。
- 结尾全程保留玩家移动及门前再次牵手。人进门后玩家向右继续才切黑，不能由动画自行结束整个游戏。

程序回归结果：`Logs/JourneyRegressionChecks.txt`；真实输入与物理通关结果：`Logs/JourneyPlaythroughChecks.txt`。
