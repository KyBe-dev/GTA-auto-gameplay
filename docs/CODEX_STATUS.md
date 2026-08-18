# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-18 18:02:05 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 核心骨架阶段；已有可构建的 WPF 空壳、核心领域契约、锁存式安全协调器、字段白名单结构化日志和基础运行配置边界，但尚无可用的游戏自动化原型。
- 当前里程碑：M0。第一个核心契约切片已由项目维护者提交并推送；第二个安全协调器切片与第三个日志/配置切片已在本地完成验证，尚未提交或推送。

## 3. 本次任务目标

实施 M0 第三个独立切片：在 Core 中建立不依赖具体日志框架的字段白名单结构化日志、容量和保留期限受限的测试用内存日志、不可变且默认完全本地的运行配置，以及只返回防御性快照的内存配置来源；使用自动化测试验证默认拒绝、数据最小化和与安全停止锁存的隔离。

## 4. 已完成事项

- 已完整读取 `AGENTS.md`、`README.md`、`docs/ARCHITECTURE.md` 和本状态文件，并检查当前 Git 工作树与现有安全协调器实现。
- 已确认当前 `HEAD` 为 `5db8bee06cc90260746228350460f080ac847c7b`，`main` 与 `origin/main` 一致；开始本任务时上一安全协调器切片仍为未提交修改，且差异检查通过。
- 已定义稳定事件 ID、UTC 时间、日志级别、事件类别、结构化字段和值类型。
- 日志字段按事件类别使用固定白名单；未知字段及 API Key、访问令牌、密码、原始 Provider 回复、OCR 原文、截图和桌面内容等明显敏感字段名默认拒绝。
- 日志值 API 不接受任意 `object`，不使用反射序列化；只提供字符串、布尔、64 位整数、有限双精度数、非空 GUID 和 UTC 时间工厂，字符串上限为 256 个字符。
- 已实现仅用于 M0 和测试的有界内存日志：容量满时按写入顺序淘汰最早事件，读取时按 UTC 观察时间清理超过保留期的事件，对外返回只读快照，不写文件、数据库、Event Log、网络或遥测服务。
- 已实现不可变运行配置：安全默认值关闭云端 Provider、标记凭据未配置、关闭截图/录像/画面回放，并强制日志只使用字段白名单；默认容量为 512 个事件，默认保留期为 24 小时。
- 日志容量只接受 1 至 100,000 个事件，保留期只接受 1 分钟至 30 天；未知配置枚举值默认拒绝，云端模式不能在未配置用户凭据引用时启用。
- 已实现只保存不可变副本并返回防御性副本的内存配置来源；未读取 `appsettings`、环境变量、注册表、Credential Manager、DPAPI 或用户文件。
- 已验证日志写入、日志读取和配置读取不会解除安全协调器的紧急停止锁存，也不会发送新动作。
- 本切片没有新增第三方依赖，没有实现真实 Provider、凭据内容、Windows API、OCR、状态估计、任务逻辑、GTA VI 工程或任何 M1 平台能力。

## 5. 修改或新增的文件

- 当前工作树中的既有未提交修改：`README.md`、`src/GtaAutoGameplay.Core/Safety/ControlSafetyState.cs`。
- 当前工作树中的既有安全协调器新增文件：`src/GtaAutoGameplay.Core/Safety/ControlSafetyCoordinator.cs`、`ControlSafetyException.cs`、`ControlStopReason.cs`、`IControlSafetyStateSource.cs`。
- 当前工作树中的既有安全协调器测试文件：`tests/GtaAutoGameplay.Core.Tests/ControlSafetyCoordinatorTests.cs`、`Fakes/FakeControlSafetyStateSource.cs`、`Fakes/FakeInputController.cs`。
- 本切片新增配置文件：`src/GtaAutoGameplay.Core/Configuration/CaptureDataOptions.cs`、`CloudProviderMode.cs`、`CredentialConfigurationState.cs`、`IRuntimeConfigurationSource.cs`、`InMemoryRuntimeConfigurationSource.cs`、`RuntimeConfiguration.cs`、`StructuredLogOptions.cs`。
- 本切片新增日志文件：`src/GtaAutoGameplay.Core/Logging/IStructuredLogReader.cs`、`IStructuredLogSink.cs`、`InMemoryStructuredLog.cs`、`StructuredLogCategory.cs`、`StructuredLogEvent.cs`、`StructuredLogField.cs`、`StructuredLogFieldNames.cs`、`StructuredLogFieldWhitelist.cs`、`StructuredLogLevel.cs`、`StructuredLogLimits.cs`、`StructuredLogValue.cs`、`StructuredLogValueKind.cs`。
- 本切片新增测试文件：`tests/GtaAutoGameplay.Core.Tests/RuntimeConfigurationTests.cs`、`StructuredLoggingTests.cs`。
- 本切片修改：`docs/CODEX_STATUS.md`。

## 6. 执行的构建、测试和检查命令及结果

- `git status --short --branch --untracked-files=all`、`git rev-parse HEAD`、`git diff --check` 和 `git diff --stat`：已确认基线提交和未提交安全协调器切片；开始任务时无空白错误。
- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：通过；4 个项目成功构建，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；60 个测试通过，0 失败，0 跳过。本切片新增 24 个展开后的测试用例。
- `git diff --check`：最终通过，无空白错误；仅显示工作区 LF 将按 Git 配置转换为 CRLF 的提示。
- M0/M1 边界关键词检查：新增日志、配置和测试代码中没有文件日志、数据库、网络、遥测、Credential Manager、DPAPI、环境变量、注册表、`SendInput`、Windows Graphics Capture、HWND 或 P/Invoke 调用。
- 依赖检查：Core 没有 `PackageReference`；测试项目只保留既有 Microsoft.NET.Test.Sdk 和 MSTest 包，本切片没有新增依赖。

## 7. 已确定的架构和产品决策

- 仓库保持 Public；当前没有 `LICENSE`，只能称为公开可见源码，不属于开源发布，也暂不接受需要合并代码的外部贡献。
- 主技术栈为 C#、.NET 10 LTS、WPF；Core 保持不依赖 Windows API。
- 当前只支持 GTA V Windows 离线故事模式；不支持 GTA Online，不创建 GTA VI 工程或占位参数。
- 不注入游戏进程、不读取或修改游戏内存、不使用内核驱动、不绕过反作弊、DRM 或平台保护。
- 顶层 `GameMode` 为 `Gameplay | Paused | Map | Menu | Cutscene | Loading | Failed | Unknown`；`Settings` 是 `Menu` 子状态。
- `GameState.Confidence` 只是摘要值；自动输入许可由独立安全状态和协调器 armed 锁存共同决定。
- 捕获目标与输入目标分离；每个短动作批次前必须重新验证身份、前台、捕获健康和状态新鲜度。
- 紧急停止是锁存状态，环境恢复不会自动解除；停止只释放协调器账本中的 `InputToken`。
- 日志默认采用固定字段白名单和显式基础类型，不保存截图、帧、录像、原始 Provider 内容、未经筛选的 OCR 原文、凭据、账户、存档或桌面内容。
- M0 内存日志同时受事件容量和时间保留期限制；容量满时淘汰最早写入事件，快照只读。
- 安全默认配置完全本地：云端 Provider 关闭、凭据未配置、截图/录像/回放关闭；配置对象和配置来源均不可由调用者反向修改。
- M0 只定义 Provider、凭据存储和游戏适配器边界；真实 Provider、凭据保存、文件配置、SQLite 和 Windows 平台实现属于后续里程碑。
- 不创建 Windows Service，不隐藏运行；所有公开安装包必须通过 M9 对应发布检查，自动更新仍为条件式未来选项。

## 8. 尚未解决的问题或阻塞项

- 无阻止继续 M0 的环境或代码阻塞项。
- M0 尚未完成：Evidence 多帧融合/`StateEstimator` 边界、`MissionTracker` 阶段候选契约、无凭据时禁止调用 Provider 的独立策略测试、仓库级密钥与受限资源自动检查仍需后续独立切片。
- 当前内存日志只用于 M0 测试，不进行任何持久化；持久化日志、目录、清理和用户设置均未设计或实现。
- 真实窗口状态来源和真实输入控制器均未实现，按架构属于 M1。
- 代码许可证尚未确定。
- 既有测试包的传递依赖许可证和再分发条件尚未逐项核实。
- Windows Credential Manager 与 DPAPI 的最终选择尚未确定；真实凭据存储属于 M7。
- 具体云端 Provider、安装器细节、代码签名和自动更新方案尚未确定，均属于后续里程碑。

## 9. 工作树是否存在未提交修改

是。当前 `HEAD` 仍为已经推送的 `5db8bee06cc90260746228350460f080ac847c7b`；工作树同时包含 M0 第二个安全协调器切片和第三个日志/配置切片的新增及修改文件，尚未 commit 或 push。构建输出位于 `.gitignore` 覆盖的 `bin/`、`obj/` 目录。

## 10. 建议的下一项最小任务

实施 M0 的仓库级密钥与受限资源自动检查切片：只增加可在本地和 GitHub Actions 中运行的只读扫描规则及合成测试样本，覆盖真实凭据模式、受限游戏资源和误报控制，不读取真实 Key、不上传数据，也不开始 M1 平台实现。
