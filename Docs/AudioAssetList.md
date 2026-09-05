# 《放开我的手》基础音频资产清单

> 当前：12 个新 SFX 已通过 `Assets/Resources/SceneAudioLibrary.asset` 在 `SceneAudio.Start` 接入，原文件位于 `Assets/Audio/SFX/SFX`。环境声 `amb_indoor_loop`、配乐 `bgm_growth_theme_loop` 尚缺，已交 5.6 跟进。音效库只覆盖 SFX，保留场景内的音乐与环境声设置；无需为了启用这批音效重新执行全场景美术绑定。

## 交付规格

- 音效使用 `.wav`，PCM 16-bit，44.1 kHz 或 48 kHz。
- 单次反馈优先 Mono；环境声和音乐使用 Stereo。
- Loop 文件必须无缝循环，首尾不能有明显断点。
- 文件名严格使用下表英文名。
- 不需要为每个场景制作大量变体；同类互动复用同一声音。

## P0 必需音效

| 文件名 | 数量 | 建议时长 | 使用位置 |
|---|---:|---:|---|
| `sfx_footstep_soft_01`、`sfx_footstep_soft_02` | 2 | 各 0.15–0.3 秒 | 所有场景行走，交替播放 |
| `sfx_interact` | 1 | 0.1–0.25 秒 | 普通 `E` 互动确认 |
| `sfx_item_move` | 1 | 0.3–0.6 秒 | 拾取和交付物品，通过音量和音调轻微区分 |
| `sfx_objective_light` | 1 | 0.7–1.2 秒 | 完成目标、胸口光增强 |
| `sfx_door_open` | 1 | 0.8–1.5 秒 | 幼儿园门、舞台幕布、会议室门 |
| `sfx_hand_release` | 1 | 0.5–1 秒 | 第一幕主动放开父母的手 |
| `sfx_heartbeat_loop` | 1 | 2–4 秒 Loop | 舞台紧张、勇气降低时淡入 |
| `sfx_breath_calm` | 1 | 2–3 秒 | 舞台长按 `E` 稳定呼吸 |
| `sfx_applause` | 1 | 3–5 秒 | 舞台最终台词完成后 |
| `sfx_paper_rustle` | 1 | 0.8–1.5 秒 | 拾取资料、查看草稿、整理报告 |
| `sfx_final_light` | 1 | 2–4 秒 | 最终门出现白光并切黑 |

P0 音效共 **12 个文件**，其中脚步占两个文件。

## P0 环境声与音乐

| 文件名 | 数量 | 建议时长 | 使用位置 |
|---|---:|---:|---|
| `amb_indoor_loop` | 1 | 20–30 秒 Loop | 全游戏复用的安静室内底噪，各场景调整音量 |
| `bgm_growth_theme_loop` | 1 | 60–90 秒 Loop | 全游戏复用的克制主旋律，场景间只调整音量 |

环境与音乐共 **2 个文件**。最终基础交付总计 **14 个文件**。

## Unity 接入

将下表音频直接放入 `Assets/Audio/Final`，文件名保持一致，然后执行 `Tools > Let Go > Apply Final Art By Filename`。工具会自动把音频写入五个场景；无需逐个拖到 AudioSource。

当前已接入的反馈包括：交替脚步、抓住、松手、呼吸完成、资料放下、目标完成、开门、舞台掌声、紧张心跳、结尾白光、环境循环和主旋律。

## P1 有时间再做

| 文件名 | 用途 |
|---|---|
| `amb_kindergarten_loop` | 幼儿园专用的远处孩子声 |
| `amb_stage_loop` | 舞台专用的幕布与观众低声 |
| `amb_research_loop` | 研究场景专用的办公室底噪 |
| `sfx_audience_murmur` | 玩家在舞台停留过久时增强紧张感 |
| `sfx_wrong_slot` | 把物品带到错误交付点时的柔和提示 |
| `amb_memory_corridor_loop` | 最终记忆长廊专用环境层；没有时复用主旋律 |

## 混音优先级

1. 互动反馈优先，音乐不得盖住放手、呼吸、提交和掌声。
2. 目标完成、放手、掌声和最终白光必须清楚听见。
3. 环境声保持很轻，进入新场景时用约 1 秒淡入淡出。
4. 心跳只在勇气下降时出现，恢复勇气后立即淡出。
