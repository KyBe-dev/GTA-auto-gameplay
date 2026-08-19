# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-19 17:06:09 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M1 Windows 平台能力开发阶段；尚无可执行的游戏自动化原型。
- 当前里程碑：M0 技术验收已经完成并提交；M1-A1“平台中立的窗口发现与选择契约”已完成但尚未提交。
- 当前切片：M1-A1，只包含 Core 平台中立模型、发现/明确选择接口、类型化失败结果、测试假对象和单元测试。
- Git 基线：任务开始及当前 `HEAD`、本地 `main`、本地 `origin/main` 均为 `c8ec95eafc4c4454f9426e9d87f9ef084b0fcaa3`。

## 3. 本次任务目标

建立不可变且不暴露 Windows 原生类型的窗口候选、组合身份快照和明确选择契约，使后续 Windows 平台实现可以列出候选并由终端用户显式选择，同时对取消、过期、不可用和发现失败提供类型化结果；不实现 M1-A2 或任何真实平台能力。

## 4. 已完成事项

- 新增独立强类型 `CandidateId` 与 `SelectionId`，两者不能在接口或模型中互换，且拒绝空 GUID。
- 新增不可变 `WindowIdentitySnapshot`，组合保存 opaque 窗口实例、PID、opaque 进程实例、进程启动时间、窗口类名、仅文件名级可执行名称、opaque 可执行身份以及 UTC 快照有效期；标题或 PID 不被单独视为可信身份。
- 新增不可变 `WindowCandidate` 和 `WindowSelection`。候选携带 UI 展示标题、进程名称和 PID；每次显式选择创建新的 `SelectionId`，不会复用旧选择。
- 新增 `IWindowDiscovery`，将候选发现与显式选择分成两个调用；发现零个或一个候选都不会自动创建选择。
- 新增 `WindowDiscoveryResult`、`WindowSelectionResult`、`WindowDiscoveryFailure` 和 `WindowSelectionFailure`，对取消、不可用、访问拒绝、枚举失败、元数据不完整、候选不存在和过期等情况返回枚举结果。
- 发现结果对候选集合进行防御性复制并拒绝重复 `CandidateId`；模型属性均无公开 setter，过期边界以 UTC 时间确定。
- 新增 `FakeWindowDiscovery` 和 19 个 M1-A1 测试用例，覆盖空/多候选、唯一 ID、ID 类型隔离、取消、过期、不可用、重复选择、单候选不自动选择、发现失败、不可变快照、防御性复制和平台类型隔离。
- 没有实现或调用 Windows 窗口枚举、P/Invoke、捕获、前台验证、Capture Target、Input Target、armed、输入、OCR、Provider、网络或凭据功能。
- M1-A2 尚未开始；没有修改 App、Platform.Windows、项目文件、依赖、许可证或架构文档。

## 5. 修改或新增的文件

- 新增：`src/GtaAutoGameplay.Core/Targeting/CandidateId.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/SelectionId.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowCandidate.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowIdentitySnapshot.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowSelection.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/IWindowDiscovery.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowDiscoveryFailure.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowDiscoveryResult.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowSelectionFailure.cs`
- 新增：`src/GtaAutoGameplay.Core/Targeting/WindowSelectionResult.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/Fakes/FakeWindowDiscovery.cs`
- 新增：`tests/GtaAutoGameplay.Core.Tests/WindowSelectionTests.cs`
- 修改：`docs/CODEX_STATUS.md`

## 6. 执行的构建、测试和检查命令及结果

- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：首次在受限网络中因无法读取 NuGet 漏洞元数据而失败；允许访问现有包源后完成还原并发现一项 MSTest 分析器错误，已按分析器要求修正测试断言。
- `dotnet build GtaAutoGameplay.sln --configuration Release --no-restore`：通过，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；Core 150/150、RepositoryGuard 13/13，共 163/163，无跳过、无失败。
- 当前候选文件仓库守卫扫描：通过，无阻止项；该基础扫描不能证明仓库绝对无秘密，也不能替代成熟秘密扫描审计。
- `git diff --check`：通过，无空白错误。
- Core 平台边界关键词检查：通过；M1-A1 生产文件没有 HWND、`IntPtr`、`nint`、P/Invoke、WinRT、WGC、`SendInput` 或 Windows SDK 类型。
- M1-A1 功能边界检查：通过；没有网络、凭据、截图、捕获帧、Provider、键盘鼠标或输入实现，也没有新增第三方依赖。

## 7. 已确定的架构和产品决策

- M0 技术验收已经完成并提交；M1 按 A 至 G 的顺序分片实现。
- M1-A1 只定义平台中立选择契约；原始窗口句柄及原生映射只能留在后续 Platform.Windows 实现中。
- 窗口身份是窗口实例、PID、进程实例、进程启动时间、窗口类和可执行身份的组合；标题、PID 或进程名均不能单独证明身份。
- 每次终端用户明确选择都创建新的 `SelectionId`；候选刷新、单候选或旧选择不能自动产生或恢复选择。
- 可执行程序只跨 Core 边界提供文件名级展示值与 opaque 身份，不保存完整路径。
- 当前不创建 Capture Target 或 Input Target，不查询前台，不 armed，不捕获，也不发送输入。
- 不支持 GTA Online，不自动选择 GTA 窗口，不注入、不读写游戏内存、不使用驱动或内核组件、不绕过反作弊、DRM 或平台保护。
- 当前没有 `LICENSE`，仓库只能称为公开可见源码；公开安装包仍必须等待 M9。

## 8. 尚未解决的问题或阻塞项

- 阻止提交 M1-A1 或开始 M1-A2 的技术问题：无。
- M1-A2 尚未实现真实 Windows 顶层可见窗口枚举、原生句柄的仅内存映射或终端用户明确选择界面。
- M1-B 及以后尚未实现窗口/进程身份重新验证、Capture Target、Input Target、前台状态或权限验证。
- M1-C 及以后尚未实现 WGC、基础本地观察、安全门接入或受控测试窗口输入。
- M1-C 前仍需项目维护者确定最低 Windows 版本/构建号，并审核 WGC SDK 接入方式和可能新增的依赖。
- 治理待决：代码许可证、测试传递依赖许可证和 GitHub 远程托管内容审计未完成；这些不阻止本地 M1-A2 开发，但阻止声称开源、接受需要合并的外部贡献或公开安装包。

## 9. 工作树是否存在未提交修改

是。当前未提交文件为本次 M1-A1 的 12 个新增 C# 文件及本状态文件：

- `src/GtaAutoGameplay.Core/Targeting/` 下 10 个新增契约文件
- `tests/GtaAutoGameplay.Core.Tests/Fakes/FakeWindowDiscovery.cs`
- `tests/GtaAutoGameplay.Core.Tests/WindowSelectionTests.cs`
- `docs/CODEX_STATUS.md`

没有未提交的 App、Platform.Windows、项目、工作流、依赖、许可证或架构文档修改。

## 10. 建议的下一项最小任务

M1-A2：Windows 顶层可见窗口枚举与用户明确选择界面。只实现只读枚举、标题/进程名/PID 展示、终端用户明确选择和取消，以及 `SelectionId` 到原始句柄的 Platform.Windows 仅内存映射；仍不得捕获、验证前台、创建 Capture/Input Target、armed 或发送输入。
