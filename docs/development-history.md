# Jump Not Included 开发修改记录

本文记录项目从原型到 v1.11.0 的实际迭代，用于查看修改原因、定位相关代码和理解已保留的验证证据。内容来自开发记录、现有源码、工程快照、打包脚本和测试报告。

## 记录方式

开发早期没有执行 Git commit，因此无法恢复一条原本就存在的逐次提交历史。本次入库保留 GitHub 初始化提交，并将可以核实的源码整理成以下阶段。Git 时间是入库时间；正文中的版本表示开发阶段。

1. [保留的 v1.10 源码快照](https://github.com/xl31415926535/JumpNotIncluded/commit/2e2c4a3d9e40c24c8a69b78af000c0d66e44a0b5)。来自此前用于 Unity 验证的工程副本，包含完整 Assets、Packages 和 ProjectSettings。排除了临时采图辅助脚本、缓存及可执行构建产物。
2. [v1.10.1 板栗修复阶段](https://github.com/xl31415926535/JumpNotIncluded/commit/c47ba0c8760905f883d3e210d5e81cefdf652b94)。用上述快照及保留的修改重建；不是单独保留下来的完整旧压缩包。此时尚未加入后来的星星音乐修复。
3. [v1.10.2 当时的完整工程](https://github.com/xl31415926535/JumpNotIncluded/commit/fcc1eaecedd9214d5dfcb66d80b1e71e4ee0bd53)。直接导入该版本源码、序列化资源、已有 145 项运行检查报告和画面预览。

更早阶段只有需求、实现说明、部分脚本和后续源码可以核实，以下保留其演变过程，不将它们描述为可以分别 checkout 的独立版本。源文件导入前的 SHA-256 见 [source-manifest.json](../verification/history/source-manifest.json)；Git 会规范文本换行，因此哈希对应导入时的原始文件字节。

## 原型与工程初建

原型围绕“经典操作被拆成另购功能”展开。工程建立了主菜单、加载流程、两个关卡、移动跳跃、敌人、砖块、拾取物、商店、广告奖励、退款、临时会员以及完整重开。

`RunModel` 处理经济与计分规则，`RunState` 保存本局数据和检查点；`SceneRoot` 组织场景与界面状态；角色形态和临时强化使用 ScriptableObject 状态定义。最初先通过 C# 编译和经济规则检查，编辑器许可证可用后才运行实际 Unity 场景。这里保留验证顺序，不把最早的编译通过等同于当时完成了实际试玩。

主要入口：[RunModel](../JumpNotIncluded/Assets/Scripts/RunModel.cs)、[RunState](../JumpNotIncluded/Assets/Scripts/RunState.cs)、[SceneRoot](../JumpNotIncluded/Assets/Scripts/SceneRoot.cs)、[ProjectSetup](../JumpNotIncluded/Assets/Editor/ProjectSetup.cs)。

## 火焰花与初期机制调整

初期方案曾考虑通过蘑菇鉴别制造购买需求，随后先复用原有火焰马里奥和火球机制，减少新机制复杂度：正常蘑菇负责成长，火焰花购买与退款形成经济路线。后续进一步改为购买火焰花后立即进入 Fire 状态，并移除地图上的花朵。蘑菇鉴别后来作为完整六商品系统的一部分重新加入。

这两个阶段的购买行为不同：当前版本以“购买立即变身”为准，不应把早期“购买后再拾取”的说明用于当前演示。

主要入口：[PlayerMotor](../JumpNotIncluded/Assets/Scripts/PlayerMotor.cs)、[PickupActor](../JumpNotIncluded/Assets/Scripts/PickupActor.cs)、[RunModel](../JumpNotIncluded/Assets/Scripts/RunModel.cs)。

## 基础体验问题与商业界面的出现时机

实际运行后发现人物面向与行走方向相反、板栗脚部陷入地板、砖块碰撞不正确，以及尝试跳跃会重新播放背景音乐。修改涉及精灵朝向与裁切、实体脚底对齐、砖块底部触发条件，以及 Music 和短音效的分离。也修复了影响变身与强化计时的状态资源引用问题。

与此同时，普通游玩画面移除了促销文字和客服终端，只保留经典 HUD；死亡后才展示广告和商店。这是交互出现时机的设计调整，不只是换一种颜色。该阶段开发记录报告了 32 项 Unity 运行检查通过；独立的旧 32 项完整报告未保留在本仓库。

主要入口：[PlayerMotor](../JumpNotIncluded/Assets/Scripts/PlayerMotor.cs)、[BlockActor](../JumpNotIncluded/Assets/Scripts/BlockActor.cs)、[AudioDirector](../JumpNotIncluded/Assets/Scripts/AudioDirector.cs)、[GameUI](../JumpNotIncluded/Assets/Scripts/GameUI.cs)。

## 英文文案与两类广告

所有游戏界面、商品、广告和反馈文字改为英文。广告拆分为两种：观看满两秒复活，不赠现金；赚钱广告每满一秒奖励一美元，允许随时退出和重复观看，现金余额封顶 99 美元。失焦时停止广告计时，达到上限自动结束，入账后保存检查点。

开发记录和保留的旧文档更新脚本将这一阶段标为 v1.3，并报告了 49 项运行检查通过。该阶段源代码后来继续更新，49 项完整旧报告没有单独归档。

主要入口：`SceneRoot.StartAd`、`AdvanceAd`、`FinishAd`，以及 `RunModel` 的金额规则。

## 广告视觉与随机轮播

死亡页、广告和商店改为深蓝与金色风格，加入角色展示、商品卡和奖励格，每次只显示一个面板。随后加入 Mario、SUTD/AI、SL Cheater 三类广告，本地图片使其离线可用。

轮播使用随机洗牌：每轮三类各出现一次，相邻两轮交界也避免重复。赚钱广告每五秒换片，换片不重置奖励计时；复活广告保持两秒流程。开发记录报告该阶段 57 项运行检查通过，并检查多种分辨率。原始美术来源随工程保留。

主要入口：`RunModel` 中的广告轮播规则、`RunState`、`SceneRoot`、`GameUI`。[广告素材来源](../JumpNotIncluded/Assets/Art/Ads/SOURCES.md)。

## 两种货币 六种升级与 v1.7 玩法

广告现金和可消费金币分开：看广告获得现金，兑换后用金币购买能力；地图金币同时增加一枚可消费金币和 100 分，花钱不扣积分。加入跳跃、火焰花、蘑菇鉴别、大师引导、帝王之翼和加特林六项可用升级。

隐藏砖只在从下方上升撞击时显现，可释放金币或星星；引导揭示轮廓与奖励，帝王之翼提供空中追加跳跃，加特林提供持续射击。第二关加入库巴、火焰与生命反馈。伤害按 Fire → Super → Small → Dead 处理，每次降级提供短暂无伤时间，变形保持脚底位置。

主要入口：[RunModel](../JumpNotIncluded/Assets/Scripts/RunModel.cs)、[BlockActor](../JumpNotIncluded/Assets/Scripts/BlockActor.cs)、[Fireball](../JumpNotIncluded/Assets/Scripts/Fireball.cs)、[BossActor](../JumpNotIncluded/Assets/Scripts/BossActor.cs)、[PlayerMotor](../JumpNotIncluded/Assets/Scripts/PlayerMotor.cs)。

## v1.8 独立充值页

商品页金币余额旁加入加号，打开独立 COIN TOP-UP 页面。三个礼包为 100、1000 和 2000 金币，价格分别为 1、9.80 和 18.80 美元。现金不足时禁用购买，充值页可进入赚钱广告并在结束后返回同一页；返回商品页期间世界保持暂停。

这解决了商品和充值选项混在同一页的问题，同时保持广告奖励与页面返回逻辑一致。主要入口是 `GameUI.Shop`、`Recharge` 及 `SceneRoot.ExchangeCash`。[保留的界面检查](../verification/top-up-ui-checks.txt)。

## v1.9 悬崖陷阱与加特林清场

第一关开头移除暗砖，首次悬崖扩大为六格，上方布置两层共八块隐藏砖。商品及广告改用夸张推销文案，同时保留金额、按键和退出入口。

加特林调整为清理朝向前方 14 格的区域：包括高于枪口的暗砖、管道、毒蘑菇与敌人，并填平缺口。原地面与新增地面使用连续碰撞形状，避免接缝卡住玩家。清除、铺路与奖励记录随检查点保存，防止重复领取。

主要入口：`WorldBuilder`、`Fireball`、`BlockActor`、`RunState`。[悬崖预览](../verification/previews/cliff-ambush.png)、[铺路预览](../verification/previews/gatling-paved-cliff.png)。

## v1.9.1 山丘贴图

小山按 48×19、大山按 80×35 像素裁切，去除多余透明区域；山脚按底部中心对齐地面，背景山体底边必须完全落在陆地范围内，消除蓝色细缝与悬空山体。

主要入口：`ProjectSetup.CreateSprites`、`WorldBuilder.Backdrop`。该逻辑已包含在保留的 v1.10 快照中。

## v1.10 胜利收据与第二关升级

结算页并排展示实际观看广告秒数、购买金币总额和实际操作秒数，另列退款、净支出以及时间占比。广告时间包含未产生整秒奖励的小数时间；失焦、暂停、菜单和闲置按各自规则排除。累计时间跨重试与关卡保留，完整重开清零。

第二关开头扩为十只独立库巴，后段合计 29 只板栗和 31 个毒蘑菇。检查点设在库巴群之后和悬崖之后。该阶段保留了 [126 项运行报告](../verification/history/v1.10-runtime-checks.txt)，其中包括只买加特林、持续右移加射击通关两关的自动验证。

主要入口：`SceneRoot` 的时间统计、`RunModel` 的累计数据、`GameUI.Results`、`WorldBuilder.BuildTwo`。[结算图](../verification/previews/results-final.png)。

## v1.10.1 板栗空气墙修复

现象：开头板栗在空地上掉头，不能走回玩家出生点。原因是 `Enemy(14,11,16)` 将它限制在固定区间，`EnemyActor.FixedUpdate` 在越界时直接夹紧坐标并反向。

修改后，初始化只传出生位置，使用 `CastTerrain` 检查真实地形来决定移动距离和转向。进入镜头后激活，离开镜头仍继续行走；远处未见敌人保持等待；地面结束则下落。现在开头板栗能够通过真实接触杀死原地未受保护的小马里奥。

`CheckGoombas` 增加十项检查，覆盖旧边界、出生点接触死亡、水管转向、踩踏、镜头激活、暂停和掉坑。旧砖块测试先移除普通敌人，避免自由行走的开场板栗干扰单独测试砖块的场景。阶段总数为 136；完整旧报告被后来的运行覆盖，保留的打包脚本及成功日志支持该阶段记录。[证据说明](../verification/history/v1.10.1-evidence.md)。

## v1.10.2 成长蘑菇与无敌星音乐

成长蘑菇误用了图集中的绿色加命蘑菇，改为红色成长蘑菇的正确裁切坐标。资源版本从 revision 8 升至 9，并在迁移时保持 GUID。

星星音乐文件原本已存在，但没有接入无敌状态。现在 `AudioDirector` 在星星生效时使用 Music 声道循环播放 `05-starman.mp3`，记录普通关卡音乐的位置，在效果结束后继续播放。连续吃星只刷新时间，暂停、死亡和静音行为保持一致。

新增九项真实音频运行检查；当时的 [145 项报告](../verification/history/v1.10.2-runtime-checks.txt)覆盖实际拾取切曲、播放推进、暂停续播、重复拾取、自然到期、死亡停止及第二关静音。[证据说明](../verification/history/v1.10.2-evidence.md)。

## 首次仓库整理与验证范围

保留远程初始化提交，导入三个可核实源码阶段、源码哈希、完整设计文档、阶段打包脚本、现存测试报告和游戏画面预览，并增加 Lab 1 代码索引。三个阶段在整理时分别重新完成 C# 编译和纯规则检查；此次没有重新执行整个 Unity Play Mode 测试套件。

早期没有保留下来的完整源码、被覆盖的报告和逐次编辑操作不能从现有资料恢复。上述记录覆盖能够核实的功能演变，具体可比较的源码范围以三个阶段提交为准。课程录屏、公开录屏链接及最终教师验收仍需另行完成。

## v1.11.0 SGD 虚拟信用卡充值（2026-09-23）

用户希望充值具有新加坡元付款的体验，包括绑定虚拟信用卡。旧版只有广告现金兑换金币；本次新增独立虚拟卡支付来源，保留广告钱包作为可选方式。这是在仓库建立后的实际新开发，按正常提交记录保存。

`PaymentAccount` 保存绑卡状态、S$99 每局额度和交易记录，以整数分验证和扣款。礼包沿用 S$1.00 / 100、S$9.80 / 1000、S$18.80 / 2000 金币。每次确认生成唯一订单号，同一订单不能重复执行；无效礼包、余额不足或溢出时不产生半笔交易。

`SceneRoot.Payment` 将流程划分为选择、绑卡、核对、处理、收据、交易记录。绑卡不收费，处理前取消不扣款，失焦暂停。成功扣款后保存整个账户与金币；死亡重试和换关保留，解绑再绑定也不会重置额度。完整重开才重置模拟账户。

`GameUI.Payment` 重做深蓝金色充值面板：明确 SGD 总价、零手续费、生成的虚拟卡、支付确认、进度、剩余额度和可回看的原始收据。先渲染发现价格、钱包按钮和交易记录入口有文字裁切，再调整高度、宽度和标签。默认键盘焦点指向当前主要操作，处理阶段指向取消。

实际重新执行 C# 编译、纯规则检查与完整 Unity 场景检查，[161 项 PASS](../verification/unity-runtime-checks.txt)。新增 16 项覆盖支付的成功、拒绝、取消、重复执行和保存。额外生成 27 张三种分辨率截图，并检查默认按钮分发与 Windows 构建。[充值验证详情](../verification/payment-ui-checks.txt)、[新充值页](../verification/previews/payment/sgd-packs.png)、[付款收据](../verification/previews/payment/sgd-receipt.png)。

所有支付在本地模拟，不收集真实银行卡资料，交易记录仅保存于当前运行的存档。课程录屏仍需单独录制。
