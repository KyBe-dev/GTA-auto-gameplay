# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-18 16:02:26 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 核心骨架阶段；已有可构建的 WPF 空壳和核心契约，但尚无可用的游戏自动化原型。
- 当前里程碑：M0。第一个可审核切片已经完成，M0 其余安全协调、状态融合等切片尚未实施。

## 3. 本次任务目标

在确认 .NET 10 SDK 可用后，继续此前停止的 M0 第一个可审核切片：创建 .NET 10 LTS 解决方案、最小 App/Core/Platform.Windows/Core.Tests 项目结构、核心领域契约及单元测试，并严格排除所有 M1 真实平台能力和后续功能。

## 4. 已完成事项

- 已读取 `AGENTS.md`、`docs/ARCHITECTURE.md` 和本状态文件；开始任务时工作树干净。
- 已确认 .NET SDK 10.0.400 可用，并通过 `global.json` 固定 10.0.400 特征版本、允许同特征带最新补丁。
- 已创建包含 WPF App、跨平台 Core、Windows 平台边界和 Core.Tests 的解决方案。
- WPF App 仅为明确标注 M0 状态的可构建空壳；Windows 平台项目没有窗口选择、捕获、HWND 验证或输入实现。
- 已实现首版不可变或防御性复制的 `GameMode`、`Evidence`、`GameState`、`ControlSafetyState`，以及 `IGameAdapter`、`IInputController`、`IAIProvider`、`IUserCredentialStore` 接口边界。
- `GameState.Confidence` 只保存摘要置信度；输入许可仅由默认拒绝的 `ControlSafetyState` 独立计算。
- 已为默认值、置信度边界、Evidence 过期/冲突状态、集合防御性复制、捕获/输入目标身份匹配和默认拒绝行为添加单元测试。
- 已登记三个直接测试依赖及恢复得到的传递依赖；直接依赖许可证已从官方 NuGet 页面核实。
- 未实现真实窗口捕获、Windows Graphics Capture、`SendInput`、OCR、Provider API、凭据存储、SQLite、GTA V 任务逻辑、GTA VI 工程、安装程序或自动更新；没有 commit 或 push。

## 5. 修改或新增的文件

- 修改：`README.md`
- 修改：`docs/THIRD_PARTY_INVENTORY.md`
- 修改：`docs/CODEX_STATUS.md`
- 新增：`global.json`
- 新增：`Directory.Build.props`
- 新增：`GtaAutoGameplay.sln`
- 新增：`src/GtaAutoGameplay.App/GtaAutoGameplay.App.csproj`
- 新增：`src/GtaAutoGameplay.App/App.xaml`
- 新增：`src/GtaAutoGameplay.App/App.xaml.cs`
- 新增：`src/GtaAutoGameplay.App/MainWindow.xaml`
- 新增：`src/GtaAutoGameplay.App/MainWindow.xaml.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/GtaAutoGameplay.Platform.Windows.csproj`
- 新增：`src/GtaAutoGameplay.Platform.Windows/README.md`
- 新增：`src/GtaAutoGameplay.Core/GtaAutoGameplay.Core.csproj`
- 新增：`src/GtaAutoGameplay.Core/Domain/Confidence.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/ControlMode.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/Evidence.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/EvidenceSourceType.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/EvidenceStatus.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/GameMode.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/GameState.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/MenuSubstate.cs`
- 新增：`src/GtaAutoGameplay.Core/Domain/ObjectiveType.cs`
- 新增：`src/GtaAutoGameplay.Core/Safety/ControlSafetyState.cs`
- 新增：`src/GtaAutoGameplay.Core/Input/IInputController.cs`
- 新增：`src/GtaAutoGameplay.Core/Input/InputToken.cs`
- 新增：`src/GtaAutoGameplay.Core/Input/SemanticAction.cs`
- 新增：`src/GtaAutoGameplay.Core/Adapters/IGameAdapter.cs`
- 新增：`src/GtaAutoGameplay.Core/AI/AIAnalysisRequest.cs`
- 新增：`src/GtaAutoGameplay.Core/AI/AIAnalysisResult.cs`
- 新增：`src/GtaAutoGameplay.Core/AI/AIProviderAvailability.cs`
- 新增：`src/GtaAutoGameplay.Core/AI/AIStateCandidate.cs`
- 新增：`src/GtaAutoGameplay.Core/AI/IAIProvider.cs`
- 新增：`src/GtaAutoGameplay.Core/Credentials/CredentialReference.cs`
- 新增：`src/GtaAutoGameplay.Core/Credentials/CredentialStatus.cs`
- 新增：`src/GtaAutoGameplay.Core/Credentials/IUserCredentialStore.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/GtaAutoGameplay.Core.Tests.csproj`
- 新增：`tests/GtaAutoGameplay.Core.Tests/ControlSafetyStateTests.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/EvidenceTests.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/GameModeTests.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/GameStateTests.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/TestAssemblyInfo.cs`

## 6. 执行的构建、测试和检查命令及结果

- `dotnet --list-sdks`：通过；输出 `10.0.400 [C:\Program Files\dotnet\sdk]`。
- `dotnet build GtaAutoGameplay.sln --configuration Release`：沙箱内首次尝试因无法读取用户级 `NuGet.Config` 而停止；在获准的本机权限下成功恢复依赖。编译器随后报告 MSTest 4 分析规则，测试声明和并行策略已修正。
- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：最终通过；4 个项目均成功构建，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：最终通过；22 个测试通过，0 失败，0 跳过。
- `git diff --check`：通过，无空白错误。
- Git 差异和忽略规则检查：通过；构建输出未进入候选变更，源码、项目文件和公开测试夹具路径未被 `.gitignore` 排除。

## 7. 已确定的架构和产品决策

- 仓库保持 Public；当前没有 `LICENSE`，只能称为公开可见源码，不属于开源发布，也暂不接受需要合并代码的外部贡献。
- 主技术栈为 C#、.NET 10 LTS、WPF；当前 SDK 基线为 10.0.400。
- Core 目标框架为 `net10.0` 且不依赖 Windows API；App 和 Platform.Windows 目标框架为 `net10.0-windows`。
- 当前只支持 GTA V Windows 离线故事模式；不支持 GTA Online，不创建 GTA VI 工程或占位参数。
- 不注入游戏进程、不读取或修改游戏内存、不使用内核驱动、不绕过反作弊、DRM 或平台保护。
- 顶层 `GameMode` 为 `Gameplay | Paused | Map | Menu | Cutscene | Loading | Failed | Unknown`；`Settings` 是 `Menu` 子状态。
- `Evidence` 包含 ID、来源、观察和过期时间、目标字段、候选值、字段置信度、适配器标识/版本及新鲜、过期或冲突状态。
- `ControlSafetyState` 将捕获目标与输入目标分开保存，默认 disarmed；即使 `GameState.Confidence` 为 1，也不能单独授权输入。
- 输入接口只接受语义动作，并以 `InputToken` 表示本软件持有的输入账本项；没有盲目释放所有物理按键的接口。
- M0 只定义 Provider、凭据存储和游戏适配器边界；没有具体 Provider SDK、真实凭据保存或游戏适配器实现。
- 测试直接依赖为 Microsoft.NET.Test.Sdk 18.0.1、MSTest.TestAdapter 4.0.2 和 MSTest.TestFramework 4.0.2；均只用于测试，不进入安装包。
- 真实窗口选择、Windows Graphics Capture、前台 HWND/进程验证和 `SendInput` 属于 M1；本轮均未实现。
- 不创建 Windows Service，不隐藏运行；所有公开安装包必须通过 M9 对应发布检查，自动更新仍为条件式未来选项。

## 8. 尚未解决的问题或阻塞项

- 无阻止继续 M0 的环境阻塞项。
- M0 尚未完成：安全协调器、紧急停止锁存状态迁移、Evidence 融合/`StateEstimator`、任务阶段候选契约、配置与结构化日志边界仍需后续独立切片。
- 代码许可证尚未确定。
- 测试包的传递依赖已登记版本，但许可证和再分发条件尚未逐项核实。
- Windows Credential Manager 与 DPAPI 的最终选择尚未确定；真实凭据存储属于 M7。
- 具体云端 Provider、安装器细节、代码签名和自动更新方案尚未确定，均属于后续里程碑。

## 9. 工作树是否存在未提交修改

是。存在本次 M0 切片的新增和修改文件；构建输出位于 `.gitignore` 覆盖的 `bin/`、`obj/` 目录。没有 commit 或 push。

## 10. 建议的下一项最小任务

实施 M0 的独立安全协调器切片：只使用 Core 接口和假对象实现紧急停止锁存、明确重新 armed、每个短动作批次前的默认拒绝校验，以及只释放 `ControlSafetyState.HeldInputs` 中输入令牌的行为测试；仍不接入任何真实 Windows 捕获或输入 API。
