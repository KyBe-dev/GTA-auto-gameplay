# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-17 19:01:15 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，架构设计与公开仓库安全准备已完成基础文档，尚无可执行原型。
- 当前里程碑：准备实施 M0 第一个可审核切片；因当前环境没有可用的 .NET 10 SDK，在创建解决方案前已停止。

## 3. 本次任务目标

创建 .NET 10 LTS 解决方案、最小 App/Core/Platform.Windows/Core.Tests 项目结构、核心领域契约及单元测试；在缺少 .NET 10 SDK 时必须停止且不得自行安装。

## 4. 已完成事项

- 已读取 `AGENTS.md`、`README.md`、`docs/ARCHITECTURE.md`、公开仓库检查清单和本状态文件。
- 开始任务时工作树干净，`main` 与本地记录的 `origin/main` 一致。
- 已执行 .NET SDK 门槛检查；`dotnet --list-sdks` 退出码为 0 但没有输出任何 SDK，因此确认当前环境没有可用的 .NET 10 SDK。
- 已按要求在创建文件前停止，没有安装 SDK，也没有创建解决方案、项目、领域代码或测试。
- 真实捕获、输入、OCR、Provider、凭据存储、SQLite、游戏逻辑、安装程序、自动更新、commit 和 push：均未执行。

## 5. 修改或新增的文件

当前工作树中的未提交文件：

- 修改：`docs/CODEX_STATUS.md`

## 6. 执行的构建、测试和检查命令及结果

- `dotnet --list-sdks`：退出码 0、无输出；未发现任何已安装 SDK，.NET 10 SDK 门槛未满足。
- `dotnet build`：未运行；SDK 门槛失败且没有解决方案。
- `dotnet test`：未运行；SDK 门槛失败且没有测试项目。
- `git diff --check`：通过；仅有 `docs/CODEX_STATUS.md` 工作区 LF 将按 Git 配置转换为 CRLF 的提示，没有空白错误。

## 7. 已确定的架构和产品决策

- 仓库继续保持 Public；在没有 `LICENSE` 时只能称为公开可见源码，不属于开源发布，也暂不接受需要合并代码的外部贡献。
- 主技术栈为 C#、.NET 10 LTS、WPF。
- 当前只支持 GTA V Windows 离线故事模式；不支持 GTA Online。
- 不注入游戏进程、不读取或修改游戏内存、不使用内核驱动、不绕过反作弊、DRM 或平台保护。
- 顶层 `GameMode` 为 `Gameplay | Paused | Map | Menu | Cutscene | Loading | Failed | Unknown`；`Settings` 是 `Menu` 子状态。
- M0 只建立架构、领域模型、安全协调器、接口和假对象测试；真实窗口选择、捕获、前台 HWND/进程验证和 `SendInput` 属于 M1。
- M0 不实现具体 Provider、真实凭据存储或 GTA VI 工程/占位参数。
- 云端 Provider 必须采用 BYOK，默认关闭，只返回结构化候选，不能直接发送输入。
- 紧急停止是锁存状态；只释放本软件输入账本中仍处于按下状态的输入。
- 不创建 Windows Service，不隐藏运行。
- 所有公开安装包，包括 Developer Preview 安装包，都必须通过 M9 对应检查。
- 自动更新尚未决定；只有未来实际实现时才纳入 M9 验证。

## 8. 尚未解决的问题或阻塞项

- 阻塞项：当前环境没有可用的 .NET 10 SDK。根据本轮要求，Codex 不得自行安装；需要项目维护者在环境中提供 SDK 后才能继续。
- 代码许可证尚未确定。
- M0 的测试框架、密钥扫描工具和精确依赖版本尚未最终确定。
- Windows Credential Manager 与 DPAPI 的最终选择尚未确定；真实实现属于 M7。
- 具体云端 Provider、安装器细节、代码签名和自动更新方案尚未确定，均属于后续里程碑。
- 除缺少 .NET 10 SDK 外，无其他已确认的当前阻塞项。

## 9. 工作树是否存在未提交修改

是。仅 `docs/CODEX_STATUS.md` 因记录本次阻塞状态而有未提交修改；没有 commit 或 push。

## 10. 建议的下一项最小任务

由项目维护者安装或提供可用的 .NET 10 SDK。随后重新运行 `dotnet --list-sdks`；确认出现 10.x SDK 后，再重新执行 M0 第一个可审核切片。本轮不要绕过 SDK 门槛，也不要降低构建和测试要求。
