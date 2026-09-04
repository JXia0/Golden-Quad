# Final Art Drop Folder

将定稿 Sprite、Sprite Sheet 与 Animator Controller 放在这个文件夹内。

制作中的素材也可放在 `Assets/Sprites`。绑定时优先使用本文件夹中的同名定稿素材，没有定稿时自动使用 `Assets/Sprites` 中的版本。

- Sprite 文件名使用 `Docs/ArtAssetList.md` 中的 slot 名，不带额外后缀。
- 角色控制器命名为 `<角色 slot>_controller.controller`。
- 主角控制器至少提供 `Speed` Float 参数，用于 Idle/Walk 切换。
- 导入完成后执行 Unity 菜单 `Tools > Let Go > Apply Final Art By Filename`。

绑定工具会更新五个正式场景，并按灰盒占位区域自动缩放；它不在游戏运行时扫描文件。
