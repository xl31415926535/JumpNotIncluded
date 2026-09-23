# 验证资料说明

这些文件是开发期间已经生成并保留下来的证据。Git 入库日期不代表报告执行日期。

- `unity-runtime-checks.txt`：v1.10.2 的 145 项 Unity Play Mode 检查结果，原始报告结尾为 ALL RUNTIME CHECKS PASSED。
- `history/v1.10-runtime-checks.txt`：保留的 v1.10 阶段 126 项结果。
- `history/v1.10.1-evidence.md`：板栗修复阶段的重建方法，以及 136 项检查记录的可用范围。
- `history/v1.10.2-evidence.md`：蘑菇、星星音乐和 Unity 导入数据变更说明。
- `history/source-manifest.json`：导入前 Assets 和 ProjectSettings 文件的 SHA-256。Git 文本换行规范化可能改变工作区字节表示，核对时应考虑这一点。
- `top-up-ui-checks.txt` 与 `receipt-ui-checks.txt`：充值页、结算页及不同分辨率的检查记录。
- `previews/`：Unity 渲染的游戏图片。部分结算页使用测试数据验证布局，不是声称真实玩家取得这些统计数值。

本次整理时，对 v1.10、重建的 v1.10.1、最终 v1.10.2 分别重新运行了 `tools/check-project.ps1`，C# 编译及纯规则检查均通过。历史 Play Mode 报告没有被改写为新执行结果。

图片不替代课程要求的带声音录屏；自动检查不代表已经完成完整人工试玩、玩家盲测或教师验收。
