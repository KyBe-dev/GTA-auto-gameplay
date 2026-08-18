# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-18 19:23:34 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 仓库与安全骨架阶段；尚无可执行的游戏自动化原型。
- 当前里程碑：M0 收口审查。
- 当前结论：M0 未通过。大部分安全骨架已通过验证，但核心领域枚举与 `docs/ARCHITECTURE.md` 的统一状态定义不一致，并且多帧融合是否属于 M0 存在文档范围冲突。
- Git 事实：`HEAD` 和本地 `main` 为 `86247e261e99290c5adf3f00ade2a1b9519789e8`；本地远程跟踪引用 `origin/main` 为 `dd95d2607ba1f51791362f4fe64770ada9bda02d`，本地显示 `main...origin/main [ahead 1]`。只读 `git ls-remote` 已确认 GitHub 远程 `main` 当前也是 `dd95d2607ba1f51791362f4fe64770ada9bda02d`；未执行 fetch，未修改本地引用。

## 3. 本次任务目标

只读审查 M0 的文档要求、领域契约、安全协调器、日志与配置、Provider 门控、假对象、仓库守卫、CI 和 M0/M1 边界，形成可追溯的验收矩阵和收口结论；不修复代码、不实现功能、不进入 M1。

## 4. 已完成事项

- 已完整读取 `AGENTS.md`、`README.md`、`docs/ARCHITECTURE.md`、`docs/CODEX_STATUS.md`、`docs/PUBLIC_REPOSITORY_CHECKLIST.md` 和 `docs/THIRD_PARTY_INVENTORY.md`。
- 已检查当前解决方案、项目引用、领域模型、接口、安全协调器、日志、配置、Provider 门控、全部假对象、测试、`.gitignore`、仓库守卫和 GitHub Actions 工作流。
- 已确认 `.NET 10 LTS` WPF 空壳、Core 与 Windows 平台边界、`GameMode`、`GameState`、`Evidence` 基本字段、`ControlSafetyState`、安全协调器、输入账本、日志、配置、接口、Provider 门控、仓库扫描和 CI 均有实现与验证证据。
- 已确认 `FakeControlSafetyStateSource` 能通过不可变 `ControlSafetyState` 快照表达捕获目标、输入目标、窗口/进程身份、前台状态、捕获健康、状态新鲜度和目标身份不匹配；支持批次间替换快照和抛出状态源异常。它等价覆盖假窗口和假捕获状态，不需要为了命名新增独立 `FakeWindow` 或 `FakeCapture` 类型。
- 已确认安全测试覆盖默认拒绝、明确 armed、批次重新验证、目标失配、失焦、捕获异常、状态过期、状态源异常、锁存式紧急停止、账本令牌释放、重复停止、取消、输入异常和并发停止。
- 已确认产品源码没有真实 HWND、Windows Graphics Capture、`SendInput`、P/Invoke、网络 SDK、真实凭据存储、具体 Provider SDK、SQLite、OCR、`StateEstimator`、`MissionTracker` 实现或 GTA VI 工程/参数。
- 已确认 `StateEstimator` 多帧融合的 M0 归属存在文档冲突：`AGENTS.md` 要求从第一个里程碑开始多帧融合，但 `docs/ARCHITECTURE.md` 的 M0 清单和 README 的 M0 边界只要求领域模型、接口、安全协调器和假对象测试。维护者决定前不得自行实现或移入后续里程碑。
- 已确认 `MissionTracker` 阶段候选属于总体架构和 M3 任务观察器范围，不是现有 M0 清单要求。
- 已发现 M0 核心领域契约缺口：`EvidenceSourceType` 缺少架构要求的 `ActionResult`、`PersistedPrior`，并以 `Vision`/`AiProvider` 等不同语义替代 `LocalVision`/`CloudCandidate`；`ControlMode` 和 `ObjectiveType` 也与架构中的统一 `GameState` 值集合不一致。
- 已将代码许可证和传递测试依赖许可证问题与运行功能缺陷分开判断；未添加 `LICENSE` 或依赖。

## 5. 修改或新增的文件

- 本次审查只修改：`docs/CODEX_STATUS.md`。
- 当前工作树还保留上一 Provider 门控切片的未提交文件：
  - `src/GtaAutoGameplay.Core/AI/AIProviderCallGate.cs`
  - `src/GtaAutoGameplay.Core/AI/AIProviderGateLimits.cs`
  - `src/GtaAutoGameplay.Core/AI/LocalCapabilityAssessment.cs`
  - `src/GtaAutoGameplay.Core/AI/ProviderFallbackDirective.cs`
  - `src/GtaAutoGameplay.Core/AI/ProviderGateOutcome.cs`
  - `src/GtaAutoGameplay.Core/AI/ProviderGateResultType.cs`
  - `src/GtaAutoGameplay.Core/Logging/StructuredLogCategory.cs`
  - `src/GtaAutoGameplay.Core/Logging/StructuredLogFieldNames.cs`
  - `src/GtaAutoGameplay.Core/Logging/StructuredLogFieldWhitelist.cs`
  - `tests/GtaAutoGameplay.Core.Tests/AIProviderCallGateTests.cs`
  - `tests/GtaAutoGameplay.Core.Tests/Fakes/FakeAIProvider.cs`
  - `tests/GtaAutoGameplay.Core.Tests/Fakes/FakeUserCredentialStore.cs`

## 6. 执行的构建、测试和检查命令及结果

- Git 状态、提交、远程和差异检查：当前分支为 `main...origin/main [ahead 1]`；上一 Provider 门控切片仍在工作树中，未提交文件列表见第 5 节。`git ls-remote origin refs/heads/main` 只读确认远程 `main` 为 `dd95d2607ba1f51791362f4fe64770ada9bda02d`。
- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：首次因沙箱无法访问 NuGet 漏洞元数据而以 `NU1900` 失败；获得最小联网许可后原命令重跑通过，6 个项目，0 警告、0 错误，未新增或升级依赖。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；Core 90 个、Repository Guard 13 个，共 103 个测试，0 失败、0 跳过。
- 当前候选文件扫描（不含 `--history`）：通过，无阻止项。
- 完整本地可达历史扫描（含 `--history`）：通过，无阻止项；该自建扫描不能证明仓库绝对无秘密，也不能替代成熟工具或 GitHub 托管内容审计。
- `git diff --check`：通过，无空白错误；存在既有 LF 到 CRLF 转换提示。
- M0/M1 边界关键词检查：产品源码无平台/网络/凭据/M1 实现匹配；仓库工具中仅命中用于拦截 `.onnx` 文件的扫描规则和合成测试路径。
- `.gitignore` 路径验证：源码和 Markdown 可跟踪，`tests/fixtures/public/` 可公开跟踪；私有夹具、`.env`、日志、构建输出和 GTA 存档路径被排除。

## 7. 已确定的架构和产品决策

- M0 只建立仓库与安全骨架、领域契约、安全协调器、日志、配置、接口和假对象测试；真实窗口、捕获、前台验证、输入和基础识别属于 M1。
- 假窗口与假捕获状态不要求独立类型；只要等价抽象能表达全部安全条件、状态变化和异常路径即可。当前实现满足这一点。
- `MissionTracker` 阶段候选契约不是当前 M0 的必需实现，不应为收口 M0 而提前创建。
- Provider、凭据存储和游戏适配器在 M0 只建立边界；不得添加真实 SDK、凭据保存、网络请求或 GTA VI 工程。
- 当前无代码许可证不属于 M0 运行功能缺陷，也不阻止本地 M1 开发，但阻止接受需要合并代码的外部贡献、声称开源以及公开安装包发布。
- 测试传递依赖许可证待审核不阻止本地测试；它阻止声称依赖审计完成，并必须在任何公开安装包的 M9 审计前关闭。

## 8. 尚未解决的问题或阻塞项

- M0 技术阻塞：`EvidenceSourceType` 未覆盖架构明确要求的全部来源类型。
- M0 技术/契约阻塞：`ControlMode`、`ObjectiveType` 与架构 `GameState` 定义不一致；需要维护者决定以架构值为准修改实现，或明确批准并先修订架构。
- M0 范围阻塞：多帧融合/`StateEstimator` 是否必须在 M0 实现存在文档冲突，需要维护者决定；决定前不得开始实现。
- 治理待决：代码许可证尚未选择，因此不得接受需合并代码的外部贡献、声称开源或发布安装包。
- 治理待决：测试传递依赖许可证尚未逐项核实，公开依赖检查清单不能标记为完成，M9 发布审计不能据此通过。
- GitHub Issue/PR 附件、Actions 历史产物、缓存和 Releases 等远程托管内容未在本地扫描覆盖范围内。

## 9. 工作树是否存在未提交修改

是。工作树包含上一 Provider 门控切片的未提交代码、测试和日志白名单变更，以及本次审查更新的 `docs/CODEX_STATUS.md`。本轮未 commit、未 push，也未修改任何 `.cs`、项目、测试、工作流或扫描规则。

## 10. 建议的下一项最小任务

优先实施一个独立的 M0 领域契约修正切片：只对齐 `EvidenceSourceType` 与架构明确要求的来源类型并补充精确枚举测试。随后由项目维护者分别决定 `ControlMode`/`ObjectiveType` 的权威值集合，以及多帧融合/`StateEstimator` 的里程碑归属，再安排相互独立的后续切片。
