# Codex Status

本文件只保存当前有效的项目交接状态。更新时应替换已经过时的内容，不追加流水式历史记录。

## 1. 最后更新时间

2026-08-18 18:47:26 +08:00（Asia/Shanghai）

## 2. 当前开发阶段和正在处理的里程碑

- 当前阶段：Developer Preview，M0 仓库与安全骨架阶段；尚无可用的游戏自动化原型。
- 当前里程碑：M0。核心领域契约、安全协调器、字段白名单日志、基础配置和仓库级密钥与受限资源自动检查切片均已完成本地验证。
- 项目维护者已将安全协调器和日志/配置切片提交并推送到 `main`；当前 `HEAD`、`main` 和 `origin/main` 均为 `dd95d2607ba1f51791362f4fe64770ada9bda02d`。

## 3. 本次任务目标

实施 M0 的仓库级密钥与受限资源自动检查切片：建立与产品运行路径分离的只读扫描工具、精确允许列表、合成测试和最小权限 GitHub Actions 工作流，检查候选文件及完整可达 Git 历史，同时更新公开仓库安全文档和第三方清单。

## 4. 已完成事项

- 已完整读取 `AGENTS.md`、`README.md`、`docs/ARCHITECTURE.md`、`docs/CODEX_STATUS.md`、公开仓库检查清单、第三方清单、测试夹具说明、`.gitignore` 和当前解决方案。
- 已确认任务开始时工作树干净，实际提交与项目维护者提供的 `dd95d2607ba1f51791362f4fe64770ada9bda02d` 一致。
- 已新增独立 .NET 10 控制台工具 `GtaAutoGameplay.RepositoryGuard`；工具不被 WPF、Core 或 Windows 平台项目引用。
- 扫描候选范围包括 Git 已跟踪文件和未被 `.gitignore` 排除的待提交文件；`--history` 检查所有本地可达分支和标签中的历史 blob。
- 已建立 API Key/令牌赋值、常见访问令牌、私钥边界和 JWT 基础规则；输出只包含路径、规则 ID、历史对象短标识和安全说明，不打印匹配值。
- 已建立本地秘密配置、证书私钥、GTA 本体/可执行文件/存档/账户数据、截图/录像/捕获帧、模型、构建输出、日志、转储和发布产物路径规则。
- 单个文件超过 1,048,576 字节默认失败；不超过限制但包含 NUL 或不是有效 UTF-8 的文件按二进制默认失败。
- 公开 `tests/fixtures/public/` 路径不做目录级豁免；普通安全文本可以通过，媒体、模型或二进制仍须精确审核。
- 允许列表只接受已知规则 ID、精确文件路径和 10 至 300 字符的具体原因；拒绝通配符、目录路径、未知规则和重复条目。当前允许列表为空。
- 候选符号链接或重解析点不会被跟随，避免读取仓库外路径；扫描器只使用只读 Git 命令，不修改、删除或上传仓库内容。
- 合成秘密均在测试运行时拼接生成；已验证删除后的历史合成令牌仍能被发现，报告不泄露完整值，失败报告返回非零退出码。
- 已新增 GitHub Actions 工作流：仅授予 `contents: read`，关闭 checkout 凭据持久化，使用 `fetch-depth: 0`，执行 Release 构建、全部测试和全历史扫描。
- `actions/checkout` v6.0.2 固定到 `de0fac2e4500dabe0009e67214ff5f5447ce83dd`；`actions/setup-dotnet` v5.2.0 固定到 `c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7`。官方来源、MIT 许可证和完整标签 SHA 已核实并登记。
- README、公开仓库检查清单和第三方清单已同步说明运行方式、CI 范围、容量边界和“基础防线不能替代成熟审计”的限制。

## 5. 修改或新增的文件

- 修改：`GtaAutoGameplay.sln`
- 修改：`README.md`
- 修改：`docs/PUBLIC_REPOSITORY_CHECKLIST.md`
- 修改：`docs/THIRD_PARTY_INVENTORY.md`
- 修改：`docs/CODEX_STATUS.md`
- 新增：`.github/workflows/repository-security.yml`
- 新增：`tools/repository-guard.allowlist.json`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/GtaAutoGameplay.RepositoryGuard.csproj`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/Program.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/AllowlistEntry.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/GitRepositoryReader.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/README.md`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryAllowlist.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryFile.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryGuardApplication.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryGuardOptions.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryGuardReporter.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryGuardRuleIds.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/RepositoryScanner.cs`
- 新增：`tools/GtaAutoGameplay.RepositoryGuard/ScanFinding.cs`
- 新增：`tests/GtaAutoGameplay.RepositoryGuard.Tests/GtaAutoGameplay.RepositoryGuard.Tests.csproj`
- 新增：`tests/GtaAutoGameplay.RepositoryGuard.Tests/RepositoryScannerTests.cs`
- 新增：`tests/GtaAutoGameplay.RepositoryGuard.Tests/GitRepositoryReaderTests.cs`
- 新增：`tests/GtaAutoGameplay.RepositoryGuard.Tests/WorkflowSecurityTests.cs`
- 新增：`tests/GtaAutoGameplay.RepositoryGuard.Tests/TestAssemblyInfo.cs`

## 6. 执行的构建、测试和检查命令及结果

- `git status --short --branch --untracked-files=all`、`git rev-parse HEAD`、`git log -1 --oneline --decorate`：任务开始时工作树干净，`main` 与 `origin/main` 一致，提交为 `dd95d2607ba1f51791362f4fe64770ada9bda02d`。
- `git ls-remote`：从两个 GitHub 官方 Action 仓库核实 v6.0.2 和 v5.2.0 的完整标签提交 SHA。
- 首次沙箱内 Release 构建因无法访问 NuGet 漏洞元数据而失败；按授权重新联网恢复后成功获取元数据。随后发现并修正一个编译参数错误和 MSTest 并行化声明要求。
- `dotnet build GtaAutoGameplay.sln --configuration Release --force`：最终通过；6 个项目成功构建，0 警告、0 错误。
- `dotnet test GtaAutoGameplay.sln --configuration Release --no-build --no-restore`：最终通过；73 个测试通过，0 失败，0 跳过。其中原有 Core 测试 60 个，本切片新增扫描器/历史/工作流测试 13 个。
- `dotnet run --project tools/GtaAutoGameplay.RepositoryGuard/GtaAutoGameplay.RepositoryGuard.csproj --configuration Release --no-build --no-restore -- --repository . --history --allowlist tools/repository-guard.allowlist.json`：通过；当前候选文件和完整本地可达历史无阻止项。
- `git diff --check`：最终通过，无空白错误；仅显示工作区 LF 将按 Git 配置转换为 CRLF 的提示。
- 工作流自动测试确认：只有 `contents: read`、checkout 获取完整历史、凭据不持久化、两个第三方 `uses:` 均为 40 位小写十六进制 SHA。
- `git --version`：开发机为 2.55.0.windows.3，已登记；CI Git 版本由 GitHub runner 提供并保留在运行日志。

## 7. 已确定的架构和产品决策

- 仓库保持 Public；当前没有 `LICENSE`，只能称为公开可见源码，不属于开源发布。
- 主技术栈为 C#、.NET 10 LTS、WPF；仓库检查工具是独立控制台项目，不进入正式应用运行路径或安装包。
- 仓库检查工具只依赖 .NET 标准库和环境中已有的 Git CLI，没有新增 NuGet 依赖。
- 扫描规则默认拒绝；允许列表必须精确到规则与文件，不能以通配符豁免目录。
- 扫描报告永不显示完整匹配值；真实凭据一旦发现仍须先吊销或轮换，不能只依赖文件删除。
- GitHub Actions 使用最小 `contents: read` 权限、完整历史、无持久化 checkout 凭据和固定完整提交 SHA。
- 自建规则只是基础防线，不保证发现所有秘密，不替代成熟扫描器、GitHub 托管内容审查或 M9 发布审计。
- 本切片不实现 Provider、凭据存储、窗口捕获、输入、OCR、状态估计、任务逻辑或 GTA VI 工程。

## 8. 尚未解决的问题或阻塞项

- 无阻止继续 M0 的环境或代码阻塞项。
- 本地扫描只能检查本地可达 Git 历史和候选文件；GitHub Issue/PR 正文与附件、Actions 旧日志/产物、缓存和历史 Releases 仍需单独审核。
- 自建模式库并不完整，后续公开或发布审计仍需评估成熟秘密扫描工具；本切片未新增此类第三方工具。
- M0 尚未完成：无用户凭据时禁止调用 Provider 的独立门控与假 Provider 测试，以及架构中后续安排的 Evidence 融合/`StateEstimator`、`MissionTracker` 阶段候选边界仍需独立确认和切片。
- 代码许可证尚未确定。
- 既有测试包的传递依赖许可证和再分发条件仍未逐项核实。
- 真实窗口、捕获和输入能力仍未实现，按架构属于 M1。

## 9. 工作树是否存在未提交修改

是。当前 `HEAD` 仍为已推送的 `dd95d2607ba1f51791362f4fe64770ada9bda02d`；工作树只包含本次仓库级密钥与受限资源自动检查切片的新增和修改文件，尚未 commit 或 push。构建与测试输出由 `.gitignore` 排除。

## 10. 建议的下一项最小任务

实施 M0 的“无用户凭据时禁止调用云端 Provider”门控切片：只使用现有 `RuntimeConfiguration`、`IAIProvider` 和假 Provider 建立默认关闭、未配置凭据不调用、失败安全降级及调用计数测试，不接入任何真实 Provider SDK、API Key、网络请求或凭据存储。
