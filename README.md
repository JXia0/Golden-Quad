# Letting Go · 放开我的手

关于成长与告别的 2D 叙事游戏。按住、松开、示范、等待和重新尝试会改变对方学会的做法。

## 引擎与平台

- **Unity 6000.6.0f1**；精确版本见 ProjectSettings/ProjectVersion.txt。
- 提交平台：Windows x86-64；可运行版本入口为 LetGo.exe。
- 运行环境、启动与必要操作见 [玩家 README](Docs/PlayerReadme.md)。
- 章节说明见 [试玩说明](Docs/PlaytestQuickGuide.md)。
- 素材来源和许可见 [素材说明](Docs/AssetSourcesAndPermissions.md)。

## 打开工程

1. 解压源码 ZIP，选择包含 Assets、Packages、ProjectSettings 的这一层为工程根目录。
2. 在 Unity Hub 安装 **6000.6.0f1**，添加并打开工程。首次导入需要联网恢复 Packages/manifest.json 和 packages-lock.json 中的 Unity 官方包；Library 等缓存会自动重建。
3. 打开 Assets/Scenes/00_Prologue.unity 并点击 Play。在开始画面点击 START 或按 Enter / Space。

七个已启用的构建场景顺序保存在 ProjectSettings/EditorBuildSettings.asset：

00_Prologue → 01_Kindergarten → 02_Interlude_Firsts → 03_Stage → 04_Interlude_Growing → 05_Research → 06_FinalWalk

场景均位于 Assets/Scenes。当前玩法和部分呈现在运行时接入，直接使用随包场景即可。**不要执行 Rebuild All Game Scenes**，该旧工具会重建原型并覆盖手动排版。

## 编译与导出 Windows

通过 Unity Hub 安装此编辑器对应的 Windows 构建支持。完成首次导入后，在编辑器 Build Profiles 中选择 Windows，使用上述七个场景并构建。

也可使用已验证的项目导出入口。关闭打开此工程的编辑器，或对单独工程副本构建。在 PowerShell 中运行以下单行命令，将尖括号内容替换为实际绝对路径，并根据安装位置修改 Unity.exe 路径：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath '<工程根目录的绝对路径>' -executeMethod JourneyPlaythroughChecks.BuildWindowsBatch -logFile '<构建日志文件的绝对路径>'
```

该入口刷新已交付素材及角色控制器，按已启用场景构建 StandaloneWindows64，结束后自动退出。

- 输出：Builds/Windows/LetGo.exe 及同目录的数据和运行库。
- 构建结果：Logs/JourneyBuild.txt。
- 分发时压缩整个 Windows 输出目录，附玩家 README、素材来源说明及字体许可，不能只分发 EXE。

## 工程内容

| 目录 | 内容 |
| --- | --- |
| Assets/Scripts、Assets/Editor | 程序源码、资源接入、构建及验证工具 |
| Assets/Scenes、Assets/Sprites、Assets/Art | 场景、美术、UI、动画及呈现资源 |
| Assets/Audio、Assets/Resources/Voice | 音乐、音效和录制旁白 |
| Assets/Resources/Ending | 约 30 秒 flashback 视频 |
| Assets/Resources/Fonts | 随包字体、OFL 许可和修改说明 |

源码包包含原始素材及 .meta，不包含 Unity 缓存、Git 历史或已导出的游戏文件；这些不是重建所需的源文件。Unity 编辑器、运行库及官方包由 Unity 提供，依其各自条款使用。

仓库：https://github.com/JXia0/Golden-Quad
