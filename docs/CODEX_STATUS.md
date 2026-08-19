# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-19 18:09:37 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M1 Windows 平台能力开发阶段；尚无可执行的游戏自动化原型。
- 当前里程碑：M0 技术验收已经完成并提交；M1-A1 已提交，M1-A2 已实现并通过自动化验证但尚未提交。
- 当前验收状态：M1-A 的交互式收口验收部分执行，尚未全部完成，因此未把 M1-A 标记为完整验收通过。
- M1-B 尚未开始；没有实现捕获、持续前台验证、Capture Target、Input Target、armed 或输入。
- Git 基线：任务开始及当前 `HEAD`、本地 `main`、本地 `origin/main` 均为 `49e61117512589cebf40cea97f2b843d885b06f3`。

## 3. 本次任务目标

在 Platform.Windows 中只读枚举可见顶层窗口，将原生信息转换为 M1-A1 平台中立候选，在 WPF UI 中提供刷新、明确选择和取消，并用仅内存映射隔离原始窗口句柄；同时建立不依赖真实桌面的测试和独立受控测试窗口，不进入 M1-B 或任何捕获、armed、输入能力。

## 4. 已完成事项

- 在 Platform.Windows 中新增窄 `IWin32WindowApi` 边界及 Win32 实现，只封装 M1-A2 所需的窗口枚举、可见/有效/启用/工具窗口检查、标题、PID、类名和最小进程元数据读取。
- `WindowsWindowDiscovery` 实现 `IWindowDiscovery`，在后台执行枚举和初始选择复核，不长时间阻塞 WPF UI 线程。
- 过滤无效、隐藏、禁用、工具窗口、空/不可读标题、当前 App 自身窗口、PID 不可读、进程退出和元数据不完整窗口；重复原生窗口引用会去重。
- 进程访问被拒绝、枚举失败和元数据不完整使用 M1-A1 类型化失败结果；单个窗口在枚举竞争中消失会安全跳过或在选择时返回不可用。
- 候选刷新会清空旧候选与旧选择映射；选择只能来自当前有效批次。候选过期、窗口关闭、PID/进程实例/窗口类/可执行身份变化或句柄复用时拒绝选择。
- `CandidateId` 和 `SelectionId` 继续隔离。CandidateId/SelectionId 到原始窗口句柄的映射只存在于 Platform.Windows 内存；每次明确选择生成新的 SelectionId，取消和刷新会清除选择映射。
- 原始句柄、完整可执行路径和 Windows 类型未进入 Core 或 App 公共/UI 模型。完整路径只在 Platform.Windows 的单次进程查询与 opaque 哈希生成期间短暂存在，不显示、记录或持久化。
- WPF App 新增扫描/刷新、候选列表、标题/进程名/PID 展示、明确选择、取消选择、当前选择和类型化错误提示。单候选不会自动选择，选择不会启动捕获、armed 或输入。
- 新增独立 `GtaAutoGameplay.ControlledWindow` WPF 测试窗口，固定标题且不模仿 GTA；项目不可打包、不可发布，不包含捕获、输入或游戏素材。
- 新增 Platform.Windows 测试项目及 26 个非交互测试，覆盖转换、去重、自身/隐藏/禁用/工具窗口过滤、空标题、读取失败、进程退出、权限不足、刷新失效、过期、句柄复用、明确选择、取消、原生类型隔离和禁止能力边界。
- 没有新增第三方依赖；新测试项目只复用既有且版本相同的 Microsoft.NET.Test.Sdk 18.0.1、MSTest.TestAdapter 4.0.2 和 MSTest.TestFramework 4.0.2。

## 5. 修改或新增的文件

- 修改：`GtaAutoGameplay.sln`
- 修改：`README.md`
- 修改：`docs/ARCHITECTURE.md`
- 修改：`docs/M1_PLAN.md`
- 修改：`docs/CODEX_STATUS.md`
- 修改：`src/GtaAutoGameplay.App/MainWindow.xaml`
- 修改：`src/GtaAutoGameplay.App/MainWindow.xaml.cs`
- 修改：`src/GtaAutoGameplay.Platform.Windows/README.md`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Properties/AssemblyInfo.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/IWin32WindowApi.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/NativeProcessMetadata.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/NativeProcessQueryFailure.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/NativeWindowEnumerationResult.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/NativeWindowReference.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/Win32WindowApi.cs`
- 新增：`src/GtaAutoGameplay.Platform.Windows/Windowing/WindowsWindowDiscovery.cs`
- 新增：`tests/GtaAutoGameplay.Platform.Windows.Tests/GtaAutoGameplay.Platform.Windows.Tests.csproj`
- 新增：`tests/GtaAutoGameplay.Platform.Windows.Tests/TestAssemblyInfo.cs`
- 新增：`tests/GtaAutoGameplay.Platform.Windows.Tests/Fakes/FakeWin32WindowApi.cs`
- 新增：`tests/GtaAutoGameplay.Platform.Windows.Tests/WindowsWindowDiscoveryTests.cs`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/GtaAutoGameplay.ControlledWindow.csproj`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/App.xaml`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/App.xaml.cs`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/MainWindow.xaml`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/MainWindow.xaml.cs`
- 新增：`tests/GtaAutoGameplay.ControlledWindow/README.md`

## 6. 执行的构建、测试和检查命令及结果

- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：首次在受限网络中因 NuGet 漏洞元数据不可达而失败；允许访问既有包源后完成还原并发现一项 nullable 编译错误，已修正。
- `dotnet build GtaAutoGameplay.sln --configuration Release --no-restore`：通过，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：通过；Core 150/150、Platform.Windows 26/26、RepositoryGuard 13/13，共 189/189，无跳过、无失败。
- 当前候选文件仓库守卫扫描：通过，无阻止项；该基础扫描不能证明仓库绝对无秘密，也不能替代成熟秘密扫描审计。
- `git diff --check`：通过，无空白错误；Git 仅提示部分工作区 LF 将来可能转换为 CRLF。
- Core 与 App 平台边界扫描：通过；没有原始句柄、`IntPtr`、`nint`、P/Invoke 或 Windows API 类型泄漏。
- 生产能力边界扫描：通过；没有 WGC、DXGI、`SendInput`、armed、Capture/Input Target、网络、Provider、凭据、OCR 或持久化实现。
- 依赖检查：通过；新测试项目只复用现有测试依赖版本，没有引入新的第三方包。
- 修改文档的相对链接检查：通过，目标文件均存在。
- 交互式 Windows 验收：部分执行。正式 App 和独立受控窗口均成功启动，受控窗口出现在候选列表中，取消后界面回到未选择状态；由于桌面持续出现并发用户输入，候选索引多次变化，已停止自动操作。未完成受控窗口的明确选择以及刷新/关闭后的人工失效检查，未伪报通过。

## 7. 已确定的架构和产品决策

- M1-A 原始窗口句柄和 P/Invoke 只存在于 Platform.Windows；Core 和 UI 只接收平台中立模型。
- 候选身份由 opaque 窗口实例、PID、opaque 进程实例、进程启动时间、窗口类和 opaque 可执行身份组合；标题、PID、进程名或原始句柄均不单独视为可信身份。
- opaque 身份使用当前进程会话随机盐生成，原生映射不持久化；程序重启不能恢复旧句柄或旧选择。
- 刷新和取消都回到安全未选择状态；单候选、标题匹配或进程名匹配均不会自动选择。
- 进程完整路径只用于 Platform.Windows 内存中的身份派生，不进入 UI、Core、日志、配置或磁盘。
- 当前没有日志接入，因此不会记录标题、路径、候选列表或句柄。
- 受控测试窗口与正式 App 分离，并通过 `IsPublishable=false` 排除发布用途。
- 当前仍不支持 GTA Online，不自动识别或选择 GTA，不捕获，不 armed，不发送输入，不提权，不创建 Service。

## 8. 尚未解决的问题或阻塞项

- M1-A 交互式收口尚未全部完成：仍需在没有并发用户操作的桌面中确认明确选择受控窗口，以及刷新/关闭受控窗口后旧候选和旧选择失效。
- 代码和自动化测试没有发现阻止 M1-B 的技术缺陷；按切片验收顺序，完成上述人工收口后再开始 M1-B。
- M1-B 尚未实现持续窗口/进程身份复核、权限/完整性级别检查，以及 Capture Target 与 Input Target 分离模型。
- M1-C 前仍需项目维护者确定最低 Windows 版本/构建号，并审核 WGC SDK 接入方式和可能新增的依赖。
- 治理待决：代码许可证、测试传递依赖许可证和 GitHub 远程托管内容审计未完成；这些不影响本地 M1 技术开发，但阻止声称开源、接受需要合并的外部贡献或公开安装包。

## 9. 工作树是否存在未提交修改

是。当前未提交内容全部属于本次 M1-A2：

- 修改：解决方案、README、架构/M1 计划/状态文档、正式 App 主窗口、Platform.Windows README。
- 新增：Platform.Windows 的 Properties 和 Windowing 文件。
- 新增：`GtaAutoGameplay.Platform.Windows.Tests` 测试项目。
- 新增：`GtaAutoGameplay.ControlledWindow` 独立受控窗口项目及运行说明。

没有未提交的 Core、Provider、凭据、捕获、输入、工作流、许可证或第三方清单修改。

## 10. 建议的下一项最小任务

在项目维护者完成 M1-A 交互式收口验收后，下一实施切片为 M1-B：窗口与进程身份重新验证。只基于 M1-A 的 WindowSelection 和 Platform.Windows 仅内存映射重新验证 HWND 有效性、PID、进程启动身份、窗口类和可执行身份；不得提前实现捕获、前台输入资格、armed 或输入。
