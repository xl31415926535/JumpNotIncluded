# Jump Not Included 跳跃另购

Unity 2D 课程游戏，当前源码版本 **v1.10.2**，编辑器版本 **6000.3.24f1**。
开场保留经典平台游戏的画面，死亡后出现模拟广告与升级商店。
包含两个关卡、六种升级、检查点、形态变化、星星、库巴和通关收据。
所有交易均在本地模拟。

## 打开工程

1. 克隆或下载本仓库。
2. Unity Hub 中添加仓库里的 **`JumpNotIncluded` 子文件夹**，这一层包含 `Assets`、`Packages`、`ProjectSettings`。
3. 使用 Unity **6000.3.24f1**，等待导入和编译完成。
4. 点击 **Tools → Jump Not Included → Play from MainMenu**，或打开 `Assets/Scenes/MainMenu.unity` 后运行。
5. 游戏中选择 **1 PLAYER GAME → START**。

A/D 或左右方向键移动，Space/W 跳跃，J 射击，Esc 暂停。
基础跳跃需要先在游戏内解锁。完整操作和机制见[工程说明](JumpNotIncluded/README.md)。
Input System 1.20.0 作为嵌入包随源码保留。
Windows 可执行文件在 Unity 中通过 **Tools → Jump Not Included → Build Windows** 生成；构建产物与缓存不加入源码仓库。

## 修改过程与代码定位

- [开发修改记录](docs/development-history.md)：从原型到当前版本的需求、问题、修改方式和证据。
- [Lab 1 代码与演示索引](docs/lab1-code-guide.md)：移动、碰撞、Game Over、计分、完整重开的代码入口。
- [当前设计文档](docs/jump-not-included-design.md)：完整玩法与后续各 Lab 的演示目标。
- [验证记录](verification/README.md)：历史运行报告、界面检查及图片的来源和范围。

**历史说明：** 开发初期没有创建 Git 提交。现在的阶段提交根据保留下来的工程快照与实际修复内容整理，提交时间表示本次入库时间。v1.10 是保留的源码快照，v1.10.1 是基于快照和已知修改重建的阶段，v1.10.2 来自当前完整工程。更早的修改以文档记录保留，没有伪称存在逐次完整源码快照。GitHub 原有初始化提交也保留在历史中。

## 验证

开发期间保留的[最终运行报告](verification/unity-runtime-checks.txt)有 **145 项 PASS**，包含板栗移动、真实碰撞、音频播放位置、形态、经济规则与两关通关检查。
整理历史时重新对三个源码阶段做了 C# 编译及纯规则检查；没有将历史 Play Mode 报告称为此次重新运行的结果。

本机安装 Unity 后可在 PowerShell 中运行：

```powershell
.\tools\check-project.ps1 -EditorRoot 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor'
```

完整运行检查入口：**Tools → Jump Not Included → Run runtime checks**。
这些自动检查与界面图片不替代课程要求的带声音录屏，也不代表教师已经验收。

## 素材来源

保留了[游戏素材说明](JumpNotIncluded/Assets/Art/SOURCES.md)、[广告素材说明](JumpNotIncluded/Assets/Art/Ads/SOURCES.md)以及嵌入包自身的许可证。
