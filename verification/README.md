# 验证资料说明

这些文件记录实际执行的检查及保留的历史证据。旧资料的 Git 入库日期不代表执行日期；v1.11.0 的新检查于 2026-09-23 执行。

- `unity-runtime-checks.txt`：v1.11.0 的 161 项 Unity Play Mode 检查结果，结尾为 ALL RUNTIME CHECKS PASSED。新增 16 项覆盖 SGD 虚拟卡与钱包支付的完整流程。
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
