# 验证资料说明

v1.14.1 已通过 226 项完整 Unity 运行检查、59 项推进专项检查和 44 项机甲音频检查，Windows x64 构建成功。九种新音效的 WAV 格式、信号分析与源码 SHA256 已核对，另保留 11.50 秒分段试听与 6.2 秒真实 Unity 登场混音；原视觉截图及登场／商品页画面报告仍归属 v1.14.0，未声称重新执行。自动检查与试听素材不替代人工游玩或课程验收录屏。

本次检查对象是九种新合成音效与游戏事件、推进循环和登场阶段的连接，配合完整运行及推进回归。新音频报告、分析与试听单独保留。当前视觉沿用 v1.14.0；旧截图和登场／商品页画面报告不会改成新版本结果。

## v1.14.0 历史视觉证据

该版本通过 226 项完整 Unity 运行检查、59 项推进专项检查、24 项登场画面检查和 15 项机甲商品页／飞行检查，保存 10 张推进图、24 张登场图和 14 张机甲图，并成功构建 Windows x64。上述结果不包含 v1.14.1 的声音替换。

- `history/v1.14.0-runtime-checks.txt` 与 `history/v1.14.0-propulsion-checks.txt`：声音更新前的完整与推进报告。
- `arrival-ui-checks.txt` 与 `previews/arrival/`：仍为 v1.14.0 的 24 项登场画面检查和 24 张截图，保留原版本标记。
- `mech-ui-checks.txt` 与 `previews/mech/`：仍为 v1.14.0 的 15 项机甲商品页／飞行检查和 14 张截图，保留原版本标记。
- `previews/propulsion/`：仍为 v1.14.0 的 10 张推进图，包括真实大马里奥并排尺度参照；本次不替换这些视觉证据。

## v1.13.1 历史证据

该版本当时通过 226 项完整 Unity 运行检查、46 项推进专项检查和 24 项登场画面检查，保存 9 张推进状态图与 24 张登场图并成功构建 Windows x64。不包含 v1.14.0 的像素重绘与特效。

- `history/v1.13.1-runtime-checks.txt`：历史完整 226 项检查，包含首次登场与真实输入连续完成两关。
- `history/v1.13.1-propulsion-checks.txt`：历史 46 项状态、音频、地面／管顶落地与深渊保护检查；9 张历史截图见 `history/v1.13.1-propulsion-previews/`。
- `history/v1.13.1-arrival-runtime-checks.txt`：从该版本完整报告摘录的登场相关检查，包含在总数内。
- `history/v1.13.1-arrival-ui-checks.txt` 与 `history/v1.13.1-arrival-previews/`：历史 24 项检查、24 张登场图与该版本 Windows 构建结果。

## 更早的历史证据

这些文件记录实际执行的检查及保留的历史证据。旧资料的 Git 入库日期不代表执行日期；v1.11.0 的新检查于 2026-09-23 执行。

- `history/v1.13.0-runtime-checks.txt`：v1.13.0 的 226 项 Unity Play Mode 检查结果，结尾为 ALL RUNTIME CHECKS PASSED。加入首次登场完整流程并重跑既有玩法、支付、复活与机甲检查。
- `history/v1.13.0-arrival-runtime-checks.txt`：v1.13.0 的 23 项登场专项检查，涵盖不可跳过、世界冻结、失焦、声音、计时、保存、中断重播与重开。
- `history/v1.13.0-arrival-ui-checks.txt` 与 `history/v1.13.0-arrival-previews/`：v1.13.0 的 24 项画面检查、8 组画面 × 3 种分辨率的 24 张实际渲染图及 Windows x64 构建结果（含第二关高处接入）。测试购买余额经过布置，动画按实际时间推进；不把阶段停表截图误称为玩家录像。源码包只含阶段图，不含逐帧序列或预览视频。
- `history/v1.12.0-runtime-checks.txt`：上一版本的 201 项检查结果，不包含登场动画。
- `history/v1.11.2-runtime-checks.txt`：此前179项报告。
- `mech-runtime-checks.txt`：v1.12.0 保留的机甲和加特林地形调整专项检查，不是 v1.14.0 的新报告。
- `history/v1.13.0-mech-ui-checks.txt` 与 `history/v1.13.0-mech-previews/`：v1.13.0 当时重新执行的商品页三个分辨率、真实按钮分发、飞行与激光检查，共 14 项 PASS、14 张实际渲染图。保留为历史，不作为新版像素美术验证；测试场景图片不能替代人工录屏。
- `history/v1.12.0-mech-ui-checks.txt` 与 `history/v1.12.0-mech-previews/`：独立保存的上一版本商品页、飞行和激光验证。
- `history/v1.11.0-runtime-checks.txt`：SGD 充值阶段保留的 161 项结果。
- `revive-ui-checks.txt` 与 `previews/revive/`：死亡页、商店、复活广告共 9 张实际渲染图及 Windows 构建结果。
- `menu-ui-checks.txt` 与 `previews/menu-free-to-play*.png`：v1.11.1 副标题及爆炸框的三种分辨率检查；这次外观修改未重新执行完整玩法检查。
- `payment-ui-checks.txt`：新充值页的渲染、确认和取消分发检查，以及 Windows 构建结果。
- `previews/payment/`：9 个新充值界面状态 × 3 种分辨率的 27 张 Unity 实际渲染图。测试账户及额度是可重复验证使用的数据。
- `history/v1.10.2-runtime-checks.txt`：保留的 v1.10.2 阶段 145 项结果。
- `history/v1.10-runtime-checks.txt`：保留的 v1.10 阶段 126 项结果。
- `history/v1.10.1-evidence.md`：板栗修复阶段的重建方法，以及 136 项检查记录的可用范围。
- `history/v1.10.2-evidence.md`：蘑菇、星星音乐和 Unity 导入数据变更说明。
- `history/source-manifest.json`：导入前 Assets 和 ProjectSettings 文件的 SHA-256。Git 文本换行规范化可能改变工作区字节表示，核对时应考虑这一点。
- `top-up-ui-checks.txt` 与 `receipt-ui-checks.txt`：旧版充值页、结算页及不同分辨率的检查记录。
- `previews/`：Unity 渲染的游戏图片。部分结算页使用测试数据验证布局，不是声称真实玩家取得这些统计数值。

首次整理历史时，对 v1.10、重建的 v1.10.1、当时最终的 v1.10.2 分别重新运行了 `tools/check-project.ps1`，C# 编译及纯规则检查均通过。历史 Play Mode 报告没有被改写为新执行结果。其后的 v1.11.0 新开发在 Unity 6000.3.24f1 中重新运行完整场景检查，并独立执行充值界面渲染及 Windows 构建。

图片不替代课程要求的带声音录屏；自动检查不代表已经完成完整人工试玩、玩家盲测或教师验收。
