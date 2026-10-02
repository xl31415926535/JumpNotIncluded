# Lab 3 代码与录屏索引

本稿对应 *50.033 Game Design and Development* 的 Lab 3。第三关是本轮扩展；验收演示仍先证明输入、音频与踩怪计分。依据：[官方 Lab 3 Checkoff](https://natalieagus.github.io/50033/docs/toddlers/checkoff/)。

## Unity Input System

[InputRouter.cs](../JumpNotIncluded/Assets/Scripts/InputRouter.cs) 的 `Init` 在运行时创建 `InputActionAsset`，划分 `Gameplay` 和 `UI` 两组动作：

- A/D 或左右方向键移动，Space/W 跳跃，J 攻击。
- 机甲使用 Space/W 推进、S/下方向键下降。
- Esc 暂停，方向键与 Enter 操作菜单。

`SceneRoot.SetMode` 调用 `InputRouter.SetPlaying`；死亡、商店、广告等状态禁用 Gameplay 输入。`PlayerMotor.Update / FixedUpdate` 读取动作并控制角色；脚底向下检测区分真实落地与墙侧、头顶单向平台，避免错误刷新跳跃。游戏的 Jump DLC 是自定义规则：演示普通跳跃前应先解锁 Jump，不能把未购买时的提示音当成输入失效。

## Observer Pattern：踩 Goomba、动画与计分

代码链为：

`EnemyActor.Touch` → `EnemyActor.Defeat` → `GameEvents.EnemyDefeated` → `SceneRoot.OnEnemyDefeated` → `RunModel.ScoreOnce` → `GameEvents.ScoreChanged` → `GameUI.OnScoreChanged`。

- [EnemyActor.cs](../JumpNotIncluded/Assets/Scripts/EnemyActor.cs)：向下落且脚部接触敌人顶部才按踩踏处理；调用 `PlayerMotor.Bounce`，换成 `goomba-dead` 压扁贴图，关闭碰撞，约 0.3 秒后移除。
- [GameEvents.cs](../JumpNotIncluded/Assets/Scripts/GameEvents.cs)：以 C# `event Action<...>` 声明击杀、分数、声音事件。敌人发布事件，分数、界面与音频各自订阅。
- [SceneRoot.cs](../JumpNotIncluded/Assets/Scripts/SceneRoot.cs)：订阅击杀事件，Goomba 奖励为 200 分，并发布新分数和踩踏音效；场景销毁时退订。
- [RunModel.cs](../JumpNotIncluded/Assets/Scripts/RunModel.cs)：`ScoreOnce` 按敌人 ID 防止重复发奖。
- [GameUI.cs](../JumpNotIncluded/Assets/Scripts/GameUI.cs)：订阅 `ScoreChanged` 后更新 HUD 分数；销毁时退订。

仅从 Goomba 上方跳过、没有接触它，不调用 `Defeat`，也没有“跳过敌人奖励”。录屏请使用普通马里奥且不带 Star/VIP/机甲，以清楚展示脚踩判定，而不是无敌接触或远程攻击。

## AudioMixer 与有意义的音效处理

打开 [GameAudio.mixer](../JumpNotIncluded/Assets/GameData/GameAudio.mixer)。Master 下有 `Music`、`WorldSFX`、`UI_Ads` 三组。[AudioDirector.cs](../JumpNotIncluded/Assets/Scripts/AudioDirector.cs) 为三组创建独立 AudioSource；机甲推进、武器和状态音也进入 `WorldSFX`。

`Music` 组包含 `Lowpass`，其 Cutoff freq 参数暴露为 `ShopLowpass`。`AudioDirector.Update` 将该参数设为：

- 普通游玩：22000 Hz，保留完整音乐频段。
- Shop/Ad：850 Hz，让背景音乐像退到商场/促销页面之后，突出音效与当前交互。

这个处理是滤掉高频的低通效果，不是只降低 Game Over 时的背景音量。短音通过 `PlayOneShot` 播放；尝试未购买的跳跃不会重启 BGM。`ToggleMusic / ToggleEffects` 分别控制音乐与效果。

录屏时同时展示 AudioMixer 的 `Music → Lowpass` 属性、暴露参数和运行中的 AudioSource Output 分组；先听正常音乐，再进入商店听低通差异，随后返回游玩。官方要求说明效果用途并展示 AudioMixer/Inspector；仅展示代码或无声录像不足以清楚呈现这一部分。若没有麦克风，可用英文字幕解释用途，并保留分组电平与游戏声音。

## 建议录屏顺序：约 4 分 40 秒

1. **0:00–0:25**：标题、项目名、Unity Play Mode；说明 Jump DLC 已解锁。展示 A/D、Space/W 和暂停输入。
2. **0:25–1:10**：从普通 Goomba 上方跳过，展示分数不变；再踩中它，展示压扁、反弹、音效与 +200 分。
3. **1:10–1:40**：简短定位 `InputRouter`、`GameEvents` 和 `EnemyActor.Defeat`，说明发布/订阅关系。
4. **1:40–2:35**：播放正常 BGM，进入死亡后的商店；展示 Music/WorldSFX/UI_Ads、Lowpass 和 Inspector 中的音源分组，解释 850 Hz 效果并返回游玩对比。
5. **2:35–4:15**：展示第三关城堡、熔岩、旋转火条与压砸机关；依次剪入 Fire Flower、Monarch Wings、Gatling、机甲的不同应对方式。剪辑片段注明装备；不把单个障碍演示称为全程通关。
6. **4:15–4:40**：展示第三关结算或当前验证报告、Console 与公开仓库地址，结束。

录制包含游戏音频、总时长不超过五分钟的公开视频。录屏方案不是已完成录屏。本轮实际通过 **57 项第三关专项、226 项完整回归**，含真实输入、Goomba 跳过不加分/踩踏恰好 +200、事件不重复及 Mixer 低通；另保存 **26 张实际截图**。[第三关专项报告](../verification/world-three-checks.txt) 可用来定位演示项，不能替代带声音的视频。

路线报告另验证 14 个花单跳与 11 个 Wings 独立路段；机甲只按 D 已通过完整第三关且零死亡、零射击。**不要把分段证明表述成连续无机甲 TAS 或真人通关。**

本轮提交使用 [v1.16.0-world3-preview 源码预览](https://github.com/xl31415926535/JumpNotIncluded/releases/tag/v1.16.0-world3-preview)，可在 Unity 6000.3.24f1 编辑器中录制已验证的玩法。Windows 构建因 Bee 编译子进程挂起而未生成；旧 v1.15.0 Windows 包不含第三关。

## 第三关补充实现

关卡、装备分层及验证边界见 [World 3 设计说明](world3-design.md)。新脚本包括 `WorldBuilder.WorldThree`、`CastleArt`、`CastleBarrier`、`CastleHazard`、`CastlePlatform`；既有 `SceneRoot`、`GameUI`、`MechSuit`、`Fireball` 等承担场景衔接、结算与装备交互。新增城堡图形由代码生成，基础角色、美术与音频仍沿用项目已列明来源的资产。

第三关不替代官方 checkoff 的三项核心演示。尤其机甲秒杀不能替代普通 Goomba 踩踏与压扁动画演示。
