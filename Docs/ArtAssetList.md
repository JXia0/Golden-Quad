# 《放开我的手》最终美术资产清单

> 当前更新：七场景流程已恢复，六张现有蒙太奇图已接入两个可操作 interlude。无需重画这些背景；拼纸暂用笔记图，可补一张单页稿纸。当前交接见 [恢复与引导交接](RestoredInterludesAndResearchGuide.md)，下方部分任务卡说明属于早期版本。

## 交付方法

1. 透明资产交付 PNG，背景可交付 PNG 或高质量 JPG。
2. 文件名必须与下表完全一致；不要把文字、角色或交互道具画进背景。
3. 当前制作中的素材可直接放入 `Assets/Sprites`；定稿素材放入 `Assets/Art/Final`，并会覆盖同名的 Sprites 素材。之后执行 `Tools > Let Go > Apply Final Art By Filename`。
4. 工具会自动写入五个正式场景。背景按占位区域铺满，角色和道具保持宽高比并缩放到灰盒范围内。
5. 三位主角的 Animator Controller 可命名为 `char_child_controller.controller`、`char_teen_controller.controller`、`char_adult_controller.controller`。参数至少提供 `Speed`（Float）。
6. 背景精确画布见 `Docs/BackgroundCanvasSpecs.md`。

## P0：角色

| 文件名 | 最低交付 | 用途 |
|---|---|---|
| `char_child` | Idle 4 帧、Walk 6 帧 | 幼儿园与长廊幼儿阶段 |
| `char_teen` | Idle 4 帧、Walk 6 帧、手按胸口呼吸 4 帧、舞台动作 3 帧 | 舞台、年轻汇报者与长廊少年阶段 |
| `char_adult` | Idle 4 帧、Walk 6 帧 | 序章、研究与长廊成人阶段 |
| `char_parent` | 牵手/等待姿势各 1 张 | 幼儿园、后台、长廊 |
| `char_crying_child` | 哭泣与被牵住姿势各 1 张 | 幼儿园 |
| `char_teacher` | 站立 1 张 | 幼儿园接收区 |
| `char_mentor` | 站立 1 张 | 研究空间入口 |

所有角色侧视、默认面朝右。三位主角使用相同画布尺寸与脚底 Pivot，代码会自动水平翻转。

## P0：背景与长廊

| 文件名 | 数量 | 用途 |
|---|---:|---|
| `bg_office_hallway` | 1 | 序章会议室走廊 |
| `bg_kindergarten_hall` | 1 | 巨大尺度的幼儿园走廊 |
| `bg_stage_auditorium` | 1 | 舞台与黑暗观众席 |
| `bg_research_room` | 1 | 从现实空间过渡到抽象研究空间 |
| `memory_kindergarten_set` | 1 | 最终长廊第一段 |
| `memory_stage_set` | 1 | 最终长廊第二段 |
| `memory_research_set` | 1 | 最终长廊第三段 |

## P0：交互道具

| 文件名 | 用途/需要区分的状态 |
|---|---|
| `prop_meeting_door` | 序章门、研究完成出口 |
| `prop_office_clock` | 序章墙面时钟，表现汇报前等待的时间压力 |
| `prop_office_doorsigns` | 会议室门牌 |
| `prop_office_waysign` | 指向会议室的走廊标牌 |
| `prop_kindergarten_door` | 放手门槛与教室出口 |
| `prop_named_chair` | 放大的幼儿园椅子 |
| `deco_blocks` | 放大的积木障碍 |
| `prop_stage_curtain_l` | 左侧舞台翼幕，交付 3:1 透明画布 |
| `prop_stage_curtain_r` | 右侧舞台翼幕，交付 3:1 透明画布；下台交互区位于其前方 |
| `prop_stage_marker` | 呼吸点与两条站位路线，可由 Unity 颜色区分 |
| `prop_toy` | 幼儿园地面环境叙事道具 |
| `prop_research_desk` | 研究空间工作桌 |
| `prop_research_report` | 序章中成人主角抱着的研究报告 |
| `node_question` | 可抓取的问题卡与长廊回声 |
| `node_evidence_photo` | 照片记录证据，轮廓必须与数据证据明显不同 |
| `node_evidence_data` | 数据图表证据 |
| `node_conclusion` | 边缘略不稳定的结论卡 |
| `prop_unknown_door` | 年轻汇报者面前的最终门 |

## P0：视觉效果

| 文件名 | 用途 |
|---|---|
| `audience_eyes` | 舞台恐惧状态，可只交付 2–3 个变体到同一 Sprite Sheet |
| `fx_shadow_blob` | 幼儿园陌生影子与研究怀疑影子 |
| `fx_stage_spotlight` | 三束舞台光与长廊回声，透明渐变 |

牵手光线、胸口光、淡入淡出和物件插槽已由 Unity 生成，不需要额外美术。

## 当前 `Assets/Sprites` 接入状态

- 已接入四张主背景、幼儿园回忆背景、左右舞台幕布、观众眼睛、两张门、办公室时钟与指示牌、命名椅、研究桌、报告、玩具、问题卡、两种证据、结论卡与阴影人物。
- 儿童 Idle 4 帧、Walk 6 帧和牵手/放手 4 帧会自动重新切片，并生成 `Assets/Art/Generated/char_child_controller.controller`。
- 少年 Idle 4 帧、Walk 6 帧和手按胸口呼吸 4 帧会自动重新切片，并生成 `Assets/Art/Generated/char_teen_controller.controller`；呼吸动画由现有按住 `E` 稳定机制直接驱动。
- `char_child_递玩具 3.png` 暂不绑定。当前核心玩法已经改为牵住哭泣的孩子并带到老师身边，使用递玩具动画会与玩家动作不一致。
- `prop_stage_curtain_left/right.png` 与 `prop_stage_curtain_l/r.png` 内容重复；场景使用较短文件名的 `l/r` 版本。

## P1：有时间再加

- 舞台普通观众剪影 3–4 个变体。
- `char_teen_舞台动作 3.png`：横向三帧，依次为站定看向观众、手向前打开开始表达、收势重新站稳；画布和脚底 Pivot 与少年其他动画一致。
- `prop_stage_chair.png`、`prop_stage_microphone.png`、`prop_stage_box.png`：分别交付三张独立透明 PNG，不拼图；内容固定为一把木椅、一个落地麦克风和一个矮台/脚踏箱。
- 幼儿园墙画、挂旗、书包柜等纯装饰层。
- 研究桌面杂物与报告边缘注释。
- `fx_fear_vignette.png`：一张独立的 1920×1080 透明 PNG，四周深蓝黑、中间透明；Unity 通过全屏 UI Image 改变透明度。浮尘粒子由 Unity 制作。

P1 不应遮挡手部光线、舞台站位、资料卡或门把手。
