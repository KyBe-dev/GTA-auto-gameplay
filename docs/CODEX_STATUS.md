# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-19 11:39:14 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 仓库与安全骨架阶段；尚无可执行的游戏自动化原型。
- 当前里程碑：M0 收口。
- 当前切片：`EvidenceSourceType`、`ControlMode` 和 `ObjectiveType` 领域契约对齐，已完成本地实现和验证，尚未提交或推送。
- Git 基线：任务开始时工作树干净，`HEAD`、本地 `main` 和本地 `origin/main` 均为 `0ac22234684063730184f07ddb724c0b6827fb05`；只读 `git ls-remote` 已确认 GitHub 远程 `main` 也是该提交。此前 Provider 门控、仓库守卫和验收状态均已提交并推送。

## 3. 本次任务目标

只修正 M0 的三个领域枚举及其直接引用和测试，使 `EvidenceSourceType`、`ControlMode`、`ObjectiveType` 与 `docs/ARCHITECTURE.md` 的统一 `Evidence`/`GameState` 定义一致；不实现 `StateEstimator`、`MissionTracker`、平台能力或其他里程碑内容。

## 4. 已完成事项

- 已完整读取 `AGENTS.md`、`README.md`、`docs/ARCHITECTURE.md` 和任务开始时的 `docs/CODEX_STATUS.md`，并确认主要文档已经使用权威值，无需重复修改。
- `EvidenceSourceType` 已对齐为 `Unknown = 0 | LocalVision | Ocr | MissionTracker | ActionResult | PersistedPrior | CloudCandidate | UserConfirmation`。
- `ControlMode` 已对齐为 `Unknown = 0 | OnFoot | Driving | Aiming | UI`，并保持其语义只表示角色/操作上下文。
- `ObjectiveType` 已对齐为 `Unknown = 0 | GoTo | Follow | Interact | Drive | Wait | Search`。
- 已删除旧枚举成员，没有保留枚举别名，也没有增加字符串转换兼容层。
- `GameState` 的默认 `ControlMode` 已从旧的 `Manual` 改为 `Unknown`；默认 `ObjectiveType` 继续为 `Unknown`。
- 已检查 `GameState`、`Evidence`、`AIStateCandidate`、Provider 门控、日志字段和测试假对象；只有 `GameState` 默认值和两个既有领域测试文件需要引用调整，Provider 与日志边界无需修改。
- 已为三个枚举增加精确名称和连续数值集合测试，并增加非法 `EvidenceSourceType`、`ControlMode`、`ObjectiveType` 的构造拒绝测试。
- 已确认旧限定枚举成员和旧领域枚举定义均无残留。`SemanticAction.EnterVehicle`/`ExitVehicle` 是独立语义输入动作，不属于已删除的 `ObjectiveType` 成员，因此保留。
- 本轮未增加依赖，未修改安全协调器、Provider 门控、日志策略、工作流、扫描规则、README、架构文档或许可证文件。

## 5. 修改或新增的文件

- 修改：`src/GtaAutoGameplay.Core/Domain/EvidenceSourceType.cs`
- 修改：`src/GtaAutoGameplay.Core/Domain/ControlMode.cs`
- 修改：`src/GtaAutoGameplay.Core/Domain/ObjectiveType.cs`
- 修改：`src/GtaAutoGameplay.Core/Domain/GameState.cs`
- 修改：`tests/GtaAutoGameplay.Core.Tests/EvidenceTests.cs`
- 修改：`tests/GtaAutoGameplay.Core.Tests/GameStateTests.cs`
- 修改：`docs/CODEX_STATUS.md`
- 新增文件：无。

## 6. 执行的构建、测试和检查命令及结果

- Git 状态与远程检查：任务开始时工作树干净；本地和远程 `main` 均为 `0ac22234684063730184f07ddb724c0b6827fb05`。
- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：首次因沙箱无法访问 NuGet 漏洞元数据而出现 `NU1900`；联网重跑后发现三个直接 `Unknown == 0` 断言被 MSTest 分析器判为恒真。改为完整动态数值序列检查后最终构建通过，6 个项目，0 警告、0 错误；未新增或升级依赖。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；Core 96 个、Repository Guard 13 个，共 109 个测试，0 失败、0 跳过。本切片新增 6 个测试。
- 当前候选文件仓库守卫扫描：通过，无阻止项。
- 完整本地可达分支和标签历史扫描：通过，无阻止项。自建扫描仍不能证明仓库绝对无秘密，也不能替代成熟扫描工具或 GitHub 托管内容审计。
- `git diff --check`：通过，无空白错误；仅有工作区文件下次由 Git 触碰时从 LF 转换为 CRLF 的提示。
- 旧值检查：无 `EvidenceSourceType.Vision/GameAdapter/AiProvider`、`ControlMode.Manual/Assisted/Automated/Suspended` 或 `ObjectiveType.Navigate/EnterVehicle/ExitVehicle/Combat` 限定引用；领域枚举定义中也无旧成员。
- M0/M1 边界关键词检查：产品源码无 HWND、Windows Graphics Capture、`SendInput`、P/Invoke、网络、真实凭据、SQLite、OCR、`StateEstimator` 或 `MissionTracker` 实现。

## 7. 已确定的架构和产品决策

- 三个领域枚举的权威集合以本文件第 4 节及 `docs/ARCHITECTURE.md` 为准。
- C# 使用 `Ocr` 对应架构文档中的 OCR；这只是命名风格差异，不是语义差异。
- `ControlMode` 表示游戏中的角色/操作上下文，不表示软件自动化等级；本轮不创建 Manual/Assisted/Automated 等新概念。
- `GameState` 的 `ControlMode` 和 `ObjectiveType` 默认值均为 `Unknown`。
- 本轮不实现多帧融合、`StateEstimator`、`MissionTracker`、真实 Provider/凭据、Windows 平台能力或 GTA 任务逻辑。
- 当前没有 `LICENSE`，仓库只能称为公开可见源码；许可证仍是治理待决事项，不是本次领域代码缺陷。

## 8. 尚未解决的问题或阻塞项

- M0 已知剩余技术缺口：纯 Core 的最小 `StateEstimator` 多帧证据融合边界及测试尚未实现。
- 除 `StateEstimator` 多帧融合外，本次复核没有发现其他尚未解决的 M0 技术缺口。
- 治理待决：代码许可证尚未选择，因此不得接受需要合并代码的外部贡献、声称开源或发布安装包。
- 治理待决：测试传递依赖许可证尚未逐项核实，M9 发布审计不能据此通过。
- GitHub Issue/PR 附件、Actions 历史产物、缓存和 Releases 等远程托管内容不在本地仓库守卫扫描范围内。

## 9. 工作树是否存在未提交修改

是。工作树只包含本次三个领域枚举、`GameState` 默认值、对应测试和本状态文档的修改，尚未 commit 或 push。构建与测试输出由 `.gitignore` 排除。

## 10. 建议的下一项最小任务

纯 Core 的最小 StateEstimator 多帧融合切片。
