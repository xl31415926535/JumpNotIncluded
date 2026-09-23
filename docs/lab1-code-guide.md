# Lab 1 代码与演示索引

本项目将 Lab 1 功能整合到完整游戏里，脚本按职责命名。打开 Unity 的 Project → Assets → Scripts，双击脚本后搜索以下函数即可定位。

## 移动 跳跃与地面

[InputRouter.cs](../JumpNotIncluded/Assets/Scripts/InputRouter.cs) 的 `Init` 定义 Gameplay/UI 输入。A/D 和方向键负责移动，Space/W 跳跃。

[PlayerMotor.cs](../JumpNotIncluded/Assets/Scripts/PlayerMotor.cs) 的 `Update` 检查地面、缓存跳跃输入；`FixedUpdate` 修改刚体速度，`UpdateSprite` 更新朝向。这个游戏需要先解锁 Jump 或 Monarch Wings，录屏应说明这一自定义规则。

[WorldBuilder.cs](../JumpNotIncluded/Assets/Scripts/WorldBuilder.cs) 的 `Floor` 构造地形碰撞体，`BuildOne` 布置第一关。

## 敌人接触与 Game Over

[EnemyActor.cs](../JumpNotIncluded/Assets/Scripts/EnemyActor.cs) 的 `FixedUpdate` 和 `CastTerrain` 控制移动与真实地形转向。`OnTriggerEnter2D`、`OnTriggerStay2D` 调用 `Touch`。

`Touch` 区分踩踏与侧面接触，侧面接触调用 `PlayerMotor.Hit`。无保护的小马里奥调用 [SceneRoot.cs](../JumpNotIncluded/Assets/Scripts/SceneRoot.cs) 的 `KillPlayer`，再通过 `SetMode(ScreenMode.Dead)` 冻结世界并禁用 Gameplay 输入。

大马里奥先缩小，有无敌保护时不会按此路径死亡；演示时应使用没有保护的小马里奥。

## 分数与死亡界面

击败敌人触发 `EnemyActor.Defeat` → `GameEvents.Defeat` → `SceneRoot.OnEnemyDefeated` → `AddScore`。`RunModel.ScoreOnce` 按奖励 ID 去重。金币与击败敌人加分；本项目没有照搬教程“跳过敌人计分”的方式。

[GameUI.cs](../JumpNotIncluded/Assets/Scripts/GameUI.cs) 的 `OnGUI` 根据当前状态选择界面，`Death` 绘制死亡提示、SCORE、DEATHS 和 RESTART RUN。这里采用运行时 IMGUI，而不是场景里的 Canvas/TextMeshPro 对象。

## 完整重开

`GameUI.Death` 的 RESTART RUN 按钮调用 `SceneRoot.NewGame`，再调用 [RunState.cs](../JumpNotIncluded/Assets/Scripts/RunState.cs) 的 `NewRun`。它创建新的本局数据，清零分数与经济状态，回到第一关流程并恢复时间。

`SceneRoot.Retry` 现在进入两秒复活广告；只有 `FinishAd` 确认计时完成，才解除 `RunState.reviveRequired` 并继续检查点。`SetMode` 会阻止尚未复活时直接恢复游戏。死亡页已移除免费重试和 X；展示“完整重置”时仍点击 RESTART RUN。

## 充值代码与文字编辑

在 `Assets/Scripts/GameUI.Payment.cs` 搜索 `PaymentPacks`（礼包与支付来源）、`VirtualCard`（卡片外观）、`PaymentSummary`（订单与按钮）、`PaymentReceiptView`（收据）和 `PaymentActivity`（交易记录），可以直接找到英文文字及坐标。

`SceneRoot.Payment.cs` 的 `ContinueCheckout`、`LinkPaymentCard`、`ConfirmTopUp`、`AdvancePayment` 管理流程；`PaymentAccount.cs` 的 `Problem` 和 `Purchase` 校验额度、扣减选中的来源并生成收据。`RunState.Capture / Restore` 保存与恢复整个支付账户。此功能属于游戏的额外设计，不代替 Lab 1 的死亡、计分和重开演示。

## 演示顺序

1. 解锁跳跃，展示移动、跳跃与敌人移动。
2. 通过金币或击败敌人获得可见分数。
3. 让没有保护的小马里奥从侧面接触 Goomba。
4. 展示死亡界面中的分数，并停留几秒证明世界停止移动。
5. 点击 RESTART RUN，展示分数清零和世界重新开始。
6. 简单展示脚本位置及 Console 状态。

依据已提供的 Lab Submission Template，提交表需填写学号姓名、是否参赛、不超过五分钟且带声音的公开录屏链接、公开仓库链接以及简短实现说明。录屏与姓名学号应由提交者填写真实信息；此仓库不虚构这些材料。

公开 [Lab 1 Check-off](https://natalieagus.github.io/50033/docs/newborns/checkoff/) 允许类似项目实现，核心是死亡后停止游戏、报告分数并允许重新开始。仓库提供实现与验证证据，最终以当期 eDimension 要求和教师验收为准。
