# FinalWalk 回忆影片接入

在 `06_FinalWalk` 的最终淡出和原有短暂停顿之后，`StorySceneDirector` 等待 `EndingFlashbackPlayer.PlayIfAvailable()` 播放可选影片，再显示已交付的结尾图片和重玩提示。没有影片时立即进入原有结尾，不会空等约 30 秒。

## 影片文件

已交付影片位于 **`Assets/Resources/Ending/flashback.mp4`**，原文件保持不变。Unity 将其导入为 `VideoClip`，运行时通过 `Resources.Load<VideoClip>("Ending/flashback")` 读取。替换影片时保留这个路径及 `.meta`，不要另放同名不同格式的 Resources 资源。

本次文件的容器信息已离线检查：视频为 **30.000 秒、1280×720、16:9、24 fps、720 帧、H.264（avc1）**；包含一条 **30.037 秒、双声道、48 kHz、mp4a** 音轨。文件大小为 27,705,560 字节。Unity 的 Video Clip 导入设置须保留音频。代码保留完整画面比例，其他比例会出现黑边。

## 声音

用户已确认：本次影片**尚未混入旁白**。程序保留影片第一条音轨作为音乐，同时叠加原始录音 **`Assets/Audio/Voice/vo_flashback.wav`**。录音通过 `SceneAudioLibrary.flashbackVoice` 的资产引用加载，原 WAV 和 MP4 均不修改、不搬动。

旁白使用独立二维 `AudioSource`，对象名 **`Flashback Narration`**，音量为 1。在影片实际播放时间达到 **0.5 秒**后只启动一次，不把影片准备时间计入延迟。录音长约 **27.477 秒**，自带约 0.98 秒开头留白，因此第一句约在画面 1.48 秒出现；录音约在画面 27.98 秒结束，给 30 秒影片保留约 2 秒收尾。

旁白播放期间，影片音乐用 0.4 秒平滑降至原音量的 **30%**；旁白结束后用 0.8 秒恢复。没有独立录音时正常播放影片原音轨；没有影片音轨时仍可配合独立旁白播放。将来若替换成已混入相同旁白的完整影片，应清空 `SceneAudioLibrary.flashbackVoice` 引用，以免重复。

影片准备和播放期间，`SceneAudio.SetFilmPlaybackMuted(true)` 只临时静音场景自己的音效、环境、音乐、心跳音源；完成、跳过或取消后恢复各音源原来的静音状态。它不会暂停全局 `AudioListener`，也不会停止和重启场景循环。

## 失败与清理

准备最多等待 8 秒；开始或播放中连续 5 秒没有新帧即跳过；总播放上限是片长加 8 秒，最多 120 秒。缺失资源直接跳过，解码错误和超时会给出 Console 警告并继续结尾。正常播放以 `loopPointReached` 为结束信号。首次有效帧前保持黑屏；结束时先隐藏影片，再停止影片音轨和独立旁白、释放 RenderTexture 和 Canvas，避免旧帧盖住结尾图。重玩每次新建并只启动一次旁白音源；离开场景或停用播放器也会停止两条音源。

## 接入后验收

1. 暂不放影片，完成 FinalWalk：结尾图和 Enter 重玩正常，无额外 30 秒等待。
2. 完成 FinalWalk：画面无拉伸，约 0.5 秒时 `EndingFlashbackPlayer.NarrationPlaying` 为真，`Flashback Narration` 播放原录音；旁白期间影片音乐降至 0.3，场景声音不与影片叠加。
3. 等影片完整结束：进入已交付结尾图，随后出现重玩提示；Enter 回到序章。
4. 在测试副本中使用无法解码的影片：出现警告后仍进入结尾图；中途退出场景后无影片音轨继续播放。

影片与旁白均已到位，资源路径、录音引用和离线媒体信息已核对；Unity 原生解码、完整结束切换和实际听感仍需按上述流程验收。
