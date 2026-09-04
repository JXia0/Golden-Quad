# 《放开我的手》美术资产清单

## 交付原则

- 优先完成标记为 **P0** 的资产；P0 足以完成正式版本。
- 所有角色统一侧视角，面朝右。代码会通过水平翻转支持向左移动。
- 推荐角色单帧高度 256 px，环境模块以 256 px 或 512 px 网格制作。
- 分层导出 PNG，透明背景；背景可使用整张 PNG。
- 文件名使用下面列出的英文名称，便于直接替换 Unity 灰盒。

## 角色与动画

| 优先级 | 文件名 | 数量 | 必需动画/状态 |
|---|---|---:|---|
| P0 | `char_child` | 1 套 | Idle 4 帧、Walk 6 帧、牵手 1 帧、放手 3 帧、递玩具 3 帧 |
| P0 | `char_teen` | 1 套 | Idle 4 帧、Walk 6 帧、手按胸口/呼吸 4 帧、舞台动作 3 帧 |
| P0 | `char_adult` | 1 套 | Idle 4 帧、Walk 6 帧、持报告 1 帧、伸手 3 帧、按胸口 3 帧、推门 4 帧 |
| P0 | `char_parent` | 1 套 | Idle 4 帧、牵手 1 帧、门口等待 1 帧、手放肩膀 1 帧 |
| P0 | `char_mentor` | 1 套 | Idle 4 帧、递资料 3 帧 |
| P0 | `char_teacher` | 1 张 | 站立剪影 |
| P0 | `char_crying_child` | 1 套 | 哭泣 4 帧、接过玩具 3 帧 |
| P1 | `crowd_silhouettes` | 4 个变体 | 幼儿园孩子、舞台观众、会议观众 |

## 通用视觉资产

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `fx_hand_light_line` | 1 | 牵手时连接父母与孩子的柔和光线，可用代码绘制 |
| P0 | `fx_inner_light` | 3 | 幼儿、少年、成人三档胸口光晕 |
| P0 | `fx_door_white_light` | 1 | 最终门缝白光 |
| P0 | `fx_shadow_blob` | 3 个变体 | 幼儿园和研究场景中的抽象怀疑影子 |
| P0 | `ui_interact_e` | 1 | E 键互动图标 |
| P0 | `ui_courage_glow` | 1 | 勇气状态的柔光表现；不需要传统血条 |
| P0 | `ui_fade_texture` | 1 | 黑色全屏转场，可由 Unity UI 直接生成 |
| P1 | `fx_dust` | 1 | 舞台和记忆长廊的浮尘粒子 |
| P1 | `fx_heartbeat_vignette` | 1 | 勇气降低时的暗角纹理 |

## 场景一：会议室走廊

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `bg_office_hallway` | 1 | 横版背景，安静、略冷色 |
| P0 | `prop_meeting_door` | 1 | 带门把手的会议室门，结尾可复用 |
| P0 | `prop_research_report` | 1 | 主角携带的报告文件 |
| P1 | `prop_office_signs` | 3 | 门牌、指示牌、时钟 |

## 场景二：幼儿园

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `bg_kindergarten_hall` | 1 | 让走廊在孩子视角下显得巨大 |
| P0 | `prop_kindergarten_door` | 1 | 较高的教室门和门把手 |
| P0 | `prop_cubby` | 1 | 书包柜，可做前后两个状态 |
| P0 | `prop_child_backpack` | 1 | 地面/柜中两个状态 |
| P0 | `prop_named_chair` | 1 | 写有主角名字的座位 |
| P0 | `prop_toy` | 1 | 可被递出的玩具 |
| P1 | `deco_kindergarten_set` | 1 套 | 儿童画、挂旗、积木、墙面图案 |

## 场景三：舞台

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `bg_stage_auditorium` | 1 | 观众席保持黑暗，人物以剪影表示 |
| P0 | `prop_stage_curtain` | 左右各 1 | 幕布也是本幕的“门” |
| P0 | `fx_stage_spotlight` | 3 | 三个互动位置的聚光灯 |
| P0 | `audience_eyes` | 6 个变体 | 恐惧状态中的抽象眼睛，可重复排列 |
| P0 | `prop_stage_marker` | 3 | 玩家站位提示，保持在世界内而非 UI |
| P1 | `prop_stage_set` | 1 套 | 两到三件简单演出道具 |
| P1 | `audience_normal` | 4 个变体 | 演出完成后替换眼睛的普通剪影 |

## 场景四：研究空间

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `bg_research_room` | 1 | 现实办公室逐渐过渡到抽象黑暗空间 |
| P0 | `prop_research_desk` | 1 | 中央工作台 |
| P0 | `node_question` | 1 | “问题”发光节点 |
| P0 | `node_evidence` | 1 | “证据”发光节点 |
| P0 | `node_conclusion` | 1 | “结论”发光节点 |
| P0 | `fx_node_connection` | 1 | 节点之间的连接线，可由代码绘制 |
| P0 | `doubt_text_cards` | 4 | 四句怀疑文字的独立透明图层 |
| P1 | `prop_research_clutter` | 1 套 | 资料、便签、书、杯子、台灯 |

## 过场与最终记忆长廊

| 优先级 | 文件名 | 数量 | 说明 |
|---|---|---:|---|
| P0 | `montage_firsts_01_04` | 4 张 | 朋友家过夜、课堂举手、坐公交、父母疲惫 |
| P0 | `montage_growth_01_05` | 5 张 | 被拒绝、争吵、离家、犯错、道歉 |
| P0 | `memory_kindergarten_set` | 1 | 幼儿园场景的简化剪影版本，可从正式背景裁切 |
| P0 | `memory_stage_set` | 1 | 舞台场景的简化剪影版本 |
| P0 | `memory_research_set` | 1 | 研究场景的简化剪影版本 |
| P0 | `prop_unknown_door` | 1 | 无标签的最终门，不显示门后内容 |
| P1 | `memory_transition_masks` | 3 | 门框、幕布、书架遮挡，用于年龄切换 |

## 美术总量估算

- P0 角色动画：5 套主要角色/人物，加 2 个简单 NPC。
- P0 场景背景：4 张正式背景，加 3 张可复用的记忆剪影背景。
- P0 场景道具：约 15 件。
- P0 特效与 UI：约 8 件，其中光线、连接线和淡出可以直接由 Unity 生成。
- P0 过场插画：9 张简单剪影，可使用同一构图模板快速制作。

如果时间不足，首先删除 P1；不要减少三个主要角色年龄形态、三扇门、胸口光和九张过场剪影。
