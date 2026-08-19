# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-19 12:04:12 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 仓库与安全骨架阶段；尚无可执行的游戏自动化原型。
- 当前里程碑：M0 技术验收通过，准备规划 M1。
- 当前切片：纯 Core 的最小多帧 Evidence 融合与 `StateEstimator`，已完成本地实现和验证，尚未提交或推送。
- Git 基线：任务开始时工作树干净；当前 `HEAD`、本地 `main` 和本地 `origin/main` 均为 `35c4ed168a656761668ab0598e3d2bac8c6fecb3`。此前领域枚举对齐切片已经提交并推送。

## 3. 本次任务目标

只在 Core 中实现可测试、确定且默认拒绝的多帧 Evidence 融合与 `StateEstimator`，融合 `GameMode`、`ControlMode`、`MenuSubstate` 和 `ObjectiveType`；不进入视觉、OCR、任务逻辑、Provider、持久化、Windows 平台或输入实现。

## 4. 已完成事项

- 新增 `IStateEstimator`、`StateEstimator`、`StateEstimatorOptions`、`StateEstimationResult`，以及字段决策、候选支持和 Evidence 审计类型。
- 稳定目标字段名统一为区分大小写的 `gameMode`、`controlMode`、`menuSubstate`、`objectiveType`；未知字段、未知枚举值、旧枚举名、数字值和大小写不一致值均被拒绝，不做隐式映射。
- 默认配置要求每个候选至少有 2 个不同 Evidence ID、2 个不同观察时间、累计支持度至少为 1.0；阈值和时间窗口全部集中在不可变 `StateEstimatorOptions` 中并验证。
- Evidence 按当前评估时间、5 秒默认观察窗口、有效期、冲突状态以及精确的 adapter ID/版本过滤；未来、过期、窗口外、适配器不匹配、重复 ID、无效候选和不支持字段均保留明确审计原因。
- 同一 Evidence ID 的全部重复项均不计数，避免输入顺序决定哪一份重复证据生效。
- 候选支持度按字段和候选值分别聚合；两个合格候选的支持度都达到 1.0 且差值不超过 0.15 时，字段回退安全默认值并标记冲突，不使用最后到达者覆盖。
- 上一快照默认最多保留 5 秒且摘要置信度至少为 0.5；切换优势默认要求 0.25。只有上一值仍有满足多帧门槛的当前支持时才允许滞回保留，否则回退安全默认值或接受具有足够优势的新候选。
- `CloudCandidate` 只在同一字段、同一候选存在至少一条非云端独立 Evidence 时参与；纯云端候选不能定案，`StateEstimator` 不调用 `IAIProvider`。
- 输出为新的不可变 `GameState`；本切片未融合字段保持空值或安全默认值。`GameMode` 不是 `Menu`（包括 `Unknown`）时强制将 `MenuSubstate` 清为 `None`。
- `GameState.Confidence` 是四个字段决策置信度的平均摘要并限制在 0 到 1，不复制单条最高置信度，不接触或改变安全协调器状态，也不能单独授权输入。
- 结果中的字段决策、候选 Evidence ID、Evidence 审计和 `GameState.Evidence` 均为防御性只读快照；实现不保存共享可变状态，并发相同输入得到确定结果。
- 已新增 35 个 Core 测试用例，覆盖空输入、单帧拒绝、多帧确认、重复 ID、时效、适配器隔离、严格枚举解析、冲突、优势、滞回、云端确认、跨字段一致性、只读快照、摘要置信度、安全锁存和并发确定性。
- 未增加或升级第三方依赖，未修改安全协调器、Provider 门控、平台项目、工作流、扫描规则、README、架构文档或许可证文件。

## 5. 修改或新增的文件

- 新增：`src/GtaAutoGameplay.Core/StateEstimation/IStateEstimator.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateEstimator.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateEstimatorOptions.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateEstimationResult.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateField.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateFieldNames.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateFieldDecision.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateFieldDecisionStatus.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateFieldDecisionReason.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/StateCandidateSupport.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/EvidenceAuditEntry.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/EvidenceAuditStatus.cs`
- 新增：`src/GtaAutoGameplay.Core/StateEstimation/EvidenceRejectionReason.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/StateEstimatorTests.cs`
- 修改：`docs/CODEX_STATUS.md`

## 6. 执行的构建、测试和检查命令及结果

- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：通过；6 个项目，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；Core 131 个、Repository Guard 13 个，共 144 个测试，0 失败、0 跳过。本切片新增 35 个 Core 测试。
- 当前候选文件仓库守卫扫描：通过，无阻止项。
- 完整本地可达分支和标签历史扫描：通过，无阻止项。自建扫描仍不能证明仓库绝对无秘密，也不能替代成熟扫描工具或 GitHub 托管内容审计。
- M0/M1 边界关键词检查：生产态 `StateEstimation` 代码没有网络、文件、数据库、Windows API、Provider SDK、凭据或真实输入依赖；测试只引用假 Provider 和假输入控制器来证明调用次数为 0 且紧急停止锁存不变。
- `git diff --check`：通过，无空白错误。

## 7. 已确定的架构和产品决策

- M0 的纯 Core 多帧融合只处理 `GameMode`、`ControlMode`、`MenuSubstate` 和 `ObjectiveType`；任务 ID、阶段、OCR 文本、目标位置、角色和输入配置不在本切片融合范围。
- `StateEstimator` 采用简单、确定、可解释的时间窗口、累计支持、冲突门槛和滞回规则；这些默认参数只用于领域边界和假对象测试，不代表已经适用于真实 GTA V 画面。
- Evidence 必须匹配当前 adapter ID 和版本；不同适配器或版本的数据不能混合，为未来适配器保持隔离。
- 云端候选只能提供结构化候选 Evidence，不能独立定案、发送输入或改变安全协调器状态。
- `GameState.Confidence` 继续只作为摘要值，输入资格仍由独立、默认拒绝的 `ControlSafetyCoordinator` 和 `ControlSafetyState` 决定。
- M0 技术验收通过；真实窗口选择、Windows Graphics Capture、前台 HWND/进程验证和 `SendInput` 仍属于 M1，不应回填到 M0。
- 当前没有 `LICENSE`，仓库只能称为公开可见源码；许可证仍是治理待决事项，不是运行功能缺陷。

## 8. 尚未解决的问题或阻塞项

- M0 技术缺口：无。基于当前权威文档、既有 M0 验收审查和本切片验证，M0 技术验收通过。
- 治理待决：代码许可证尚未选择，因此不得接受需要合并代码的外部贡献、声称开源或发布安装包。
- 治理待决：测试传递依赖许可证尚未逐项核实；公开发布安装包前必须完成相应许可证与再分发审计。
- 治理待决：GitHub Issue/PR 附件、Actions 历史产物、缓存和 Releases 等远程托管内容不在本地仓库守卫扫描范围内，仍需项目维护者按公开仓库与 M9 清单审计。
- 阻塞项：无。上述治理事项不阻止本地规划和开发 M1，但继续接受外部贡献、声称开源或公开安装包仍受现有规则限制。

## 9. 工作树是否存在未提交修改

是。工作树包含本次新增的纯 Core StateEstimator 契约、实现、测试以及本状态文档修改，尚未 commit 或 push；构建和测试输出由 `.gitignore` 排除。

## 10. 建议的下一项最小任务

先进行只读的 M1 分步规划。建议首个候选实施切片是“用户可见的 GTA V 窗口发现与明确选择边界”：只建立 Windows 平台的窗口枚举、进程/窗口身份只读快照、选择结果和假对象测试，不同时实现画面捕获、前台输入验证或 `SendInput`。
