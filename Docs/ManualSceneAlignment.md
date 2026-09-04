# 手动场景排版与背景对齐

## 保存手动排版

1. 退出 Play Mode，再修改场景。Play Mode 中的 Transform 修改不会保存。
2. 打开需要调整的 `.unity` 场景，点击 Scene 窗口顶部的 **2D**。
3. 完成调整后按 `Ctrl+S` 保存场景。
4. 可以继续使用 `Tools > Let Go > Apply Final Art By Filename` 更新贴图；它不会修改物体的 Position。
5. 不要执行 `Tools > Let Go > Rebuild All Game Scenes (Overwrites Manual Layout)`，除非确定要用代码中的初始布局覆盖所有手动调整。

构建器不再在脚本编译或打开 Unity 时自动重建场景。

## 按背景调整物体

以 Prologue 为例：

- 背景：`MeetingRoom`
- 交互门：`Meeting Door`
- 装饰：`Office Clock`、`Meeting Way Sign`、`Meeting Door Sign`

当前背景参考坐标：

- `bg_office_hallway.png` 为 `3400 × 550`，场景显示范围约为 X `-16.98` 到 `16.98`。背景门洞位于像素 X `3225` 到 `3367`，所以 `Meeting Door` 的中心是 `(15.95, -1.35)`。
- `bg_kindergarten_hall.png` 为 `3400 × 550`，场景显示范围约为 X `-7.98` 到 `25.98`。最右侧发光教室门的中心约为 X `24.44`，所以 `Classroom Exit` 的位置是 `(24.44, -1.1)`。
- 幼儿园背景已经画出了打开的教室门，`Classroom Exit` 的 SpriteRenderer 默认关闭，只保留互动逻辑。需要检查位置时可以在 Inspector 临时开启，检查后再关闭。

先确定背景的位置和尺寸，之后不要再移动背景。选中需要对齐的物体：

- 按 `W` 移动。
- 按 `R` 缩放；门和角色尽量保持 X、Y 等比缩放。
- Inspector 的 `Transform > Position` 用于精确微调。
- 按住 `V` 拖动可以使用顶点吸附。
- SpriteRenderer 的 `Sorting Order` 必须高于背景。背景通常为 `-4` 或 `-5`，前景物体通常使用 `0` 或 `1`。

## 背景中已经画了门

如果背景图已经包含完整的门，不要再叠一张门图：

1. 选中 `Meeting Door`。
2. 关闭它的 `SpriteRenderer`。
3. 保留 `StoryDoor` 和 `BoxCollider2D`。
4. 把 GameObject 的 Transform 移到背景门的中心。
5. 点击 `BoxCollider2D > Edit Collider`，让绿色碰撞框覆盖门洞。

交互检测以 GameObject 的 Transform 为中心，默认有效距离是 1.5 个世界单位，所以 Transform 应放在玩家能够走到的门边，而不是画面外或门的顶部。

## 使用独立门图片

如果门需要单独显示或以后播放开门动画：

1. 保持 `SpriteRenderer` 开启。
2. 移动和等比缩放 `Meeting Door`，让门框覆盖背景中的门洞。
3. 再编辑 `BoxCollider2D` 的大小。
4. 在 Game 视图走到门前测试，确认提示出现的位置自然。

## 其他物体

纯装饰物只需要调整 Transform 和 Sorting Order。带交互脚本的物体要同时检查视觉位置和交互中心：

- `StoryDoor`：门。
- `ObjectiveStation`、`InspectPoint`：检查点或任务物体。
- `CarryItem`、`DeliveryStation`：可拾取和交付物。
- `CoreHoldTarget`、`CoreSocket`：牵手、搬运和放置目标。

背景里已经画好的装饰，可以关闭重复物体的 SpriteRenderer；如果物体承担互动，只关闭 SpriteRenderer，保留脚本和碰撞体。
