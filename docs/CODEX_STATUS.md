# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-17 18:39:20 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，架构设计与公开仓库安全准备阶段，尚无可执行原型。
- 当前里程碑：M0 规划与准备；尚未开始正式 C# 实现。

## 3. 本次任务目标

创建本状态交接文件，并在 `AGENTS.md` 中增加每次开发、文档修改或审查任务完成后必须更新本文件的长期规则。

## 4. 已完成事项

- 已统一 README、长期代理规则和架构文档中的 .NET 10 LTS、GameMode、公开可见源码、M0/M1 边界、M9 发布门槛和安全状态定义。
- 已补充 `Evidence`、`ControlSafetyState`、MissionTracker/StateEstimator 职责、任务阶段迁移、输入账本、锁存式紧急停止、地图技能取消/超时、日志白名单以及 Capture Target/Input Target 分离规则。
- 已建立公开仓库 `.gitignore`、公开仓库检查清单、第三方清单模板和测试夹具规则。
- 已创建本状态文件并在 `AGENTS.md` 中加入强制更新规则。
- 正式 M0 C# 代码、依赖、安装包、真实 Provider SDK、真实凭据存储、commit 和 push：均未执行。

## 5. 修改或新增的文件

当前工作树中的未提交文件：

- 修改：`AGENTS.md`
- 修改：`README.md`
- 修改：`docs/ARCHITECTURE.md`
- 新增：`.gitignore`
- 新增：`docs/CODEX_STATUS.md`
- 新增：`docs/PUBLIC_REPOSITORY_CHECKLIST.md`
- 新增：`docs/THIRD_PARTY_INVENTORY.md`
- 新增：`tests/fixtures/README.md`

## 6. 执行的构建、测试和检查命令及结果

- 构建：无。仓库尚无 C#、项目或解决方案文件。
- 自动化测试：无。仓库尚无测试工程。
- `git diff --check`：通过；仅有现有文档工作区 LF 将按 Git 配置转换为 CRLF 的提示，没有空白错误。
- `.NET 8` 残留检查：通过，三份主文档中无残留。
- GameMode 一致性检查：通过，三份主文档使用统一顶层枚举，`Settings` 仅为 `Menu` 子状态。
- Markdown 本地链接检查：通过。
- `.gitignore` 正反用例检查：通过；构建产物和私人数据被忽略，源码、Markdown 和公开夹具保持可跟踪。
- C# 范围检查：通过；未创建 `.cs`、`.csproj` 或 `.sln` 文件。

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

- 代码许可证尚未确定。
- M0 的精确项目拆分、测试框架、密钥扫描工具和依赖版本尚未确定。
- Windows Credential Manager 与 DPAPI 的最终选择尚未确定；真实实现属于 M7。
- 具体云端 Provider、安装器细节、代码签名和自动更新方案尚未确定，均属于后续里程碑。
- 当前无阻止文档与安全准备继续审查的技术阻塞项。

## 9. 工作树是否存在未提交修改

是。当前存在第 5 节列出的未提交文档和公开仓库安全准备文件；没有 commit 或 push。

## 10. 建议的下一项最小任务

由项目维护者审查当前未提交的文档、安全准备文件和本状态文件；确认无误后，将它们作为仅包含文档与仓库安全准备的独立提交处理。执行下一次推送前，先逐项完成 `docs/PUBLIC_REPOSITORY_CHECKLIST.md` 中适用的检查。
