# Third-Party Inventory

本文件登记计划进入源码仓库、开发流程或发布产物的第三方依赖、模型、数据集、测试数据、字体、图标和其他素材。任何第三方项在引入前必须完成登记和审核；未知信息不得猜测，应填写“尚未核实”。

当前状态：生产项目尚未引入第三方 NuGet 包、模型或公开测试数据；核心测试项目已引入下列测试专用 NuGet 包。直接依赖许可证已核实，传递依赖仍需逐项审核。

## 字段说明

- **名称**：第三方项目、包、模型、数据集或素材的正式名称。
- **类型**：NuGet 包、开发工具、模型、数据集、测试夹具、字体、图标、素材或其他。
- **版本**：精确版本、提交哈希或不可变资源标识。
- **来源**：官方包页面、官方仓库或权利人提供的下载地址。
- **许可证**：SPDX 标识或许可证正式名称；未核实时写“尚未核实”。
- **允许修改**：是、否、有限制或尚未核实。
- **允许再分发**：区分源码、二进制、模型权重和数据内容；不明确时写“尚未核实”。
- **进入源码仓库**：该项本体或副本是否提交到 Git。
- **进入安装包**：该项是否随任何公开安装包或便携包分发。
- **审核状态**：待审核、已审核、禁止引入或需法律复核。

## 清单

| 名称 | 类型 | 版本 | 来源 | 许可证 | 允许修改 | 允许再分发 | 进入源码仓库 | 进入安装包 | 审核状态 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Microsoft.NET.Test.Sdk | NuGet 包（直接测试依赖） | 18.0.1 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.0.1) | MIT | 是 | 是，须遵守 MIT | 否，仅提交 `PackageReference` | 否 | 已审核（2026-08-18） |
| MSTest.TestAdapter | NuGet 包（直接测试依赖） | 4.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/MSTest.TestAdapter/4.0.2) | MIT | 是 | 是，须遵守 MIT | 否，仅提交 `PackageReference` | 否 | 已审核（2026-08-18） |
| MSTest.TestFramework | NuGet 包（直接测试依赖） | 4.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/MSTest.TestFramework/4.0.2) | MIT | 是 | 是，须遵守 MIT | 否，仅提交 `PackageReference` | 否 | 已审核（2026-08-18） |
| Microsoft.ApplicationInsights | NuGet 包（传递测试依赖） | 2.23.0 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.ApplicationInsights/2.23.0) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.CodeCoverage | NuGet 包（传递测试依赖） | 18.0.1 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.CodeCoverage/18.0.1) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.Testing.Extensions.Telemetry | NuGet 包（传递测试依赖） | 2.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.Testing.Extensions.Telemetry/2.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.Testing.Extensions.TrxReport.Abstractions | NuGet 包（传递测试依赖） | 2.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.Testing.Extensions.TrxReport.Abstractions/2.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.Testing.Extensions.VSTestBridge | NuGet 包（传递测试依赖） | 2.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.Testing.Extensions.VSTestBridge/2.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.Testing.Platform.MSBuild | NuGet 包（传递测试依赖） | 2.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.Testing.Platform.MSBuild/2.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.Testing.Platform | NuGet 包（传递测试依赖） | 2.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.Testing.Platform/2.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.TestPlatform.AdapterUtilities | NuGet 包（传递测试依赖） | 18.0.1 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.TestPlatform.AdapterUtilities/18.0.1) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.TestPlatform.ObjectModel | NuGet 包（传递测试依赖） | 18.0.1 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.TestPlatform.ObjectModel/18.0.1) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Microsoft.TestPlatform.TestHost | NuGet 包（传递测试依赖） | 18.0.1 | [NuGet 官方包页面](https://www.nuget.org/packages/Microsoft.TestPlatform.TestHost/18.0.1) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| MSTest.Analyzers | NuGet 包（传递测试依赖） | 4.0.2 | [NuGet 官方包页面](https://www.nuget.org/packages/MSTest.Analyzers/4.0.2) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| Newtonsoft.Json | NuGet 包（传递测试依赖） | 13.0.3 | [NuGet 官方包页面](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3) | 尚未核实 | 尚未核实 | 尚未核实 | 否 | 否 | 待审核 |
| actions/checkout | GitHub Action（CI 直接依赖） | v6.0.2；`de0fac2e4500dabe0009e67214ff5f5447ce83dd` | [官方仓库固定提交](https://github.com/actions/checkout/tree/de0fac2e4500dabe0009e67214ff5f5447ce83dd) | MIT | 是 | 是，须遵守 MIT | 否，仅提交固定 SHA 引用 | 否 | 已审核（2026-08-18） |
| actions/setup-dotnet | GitHub Action（CI 直接依赖） | v5.2.0；`c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7` | [官方仓库固定提交](https://github.com/actions/setup-dotnet/tree/c2fa09f4bde5ebb9d1777cf28262a3eb3db3ced7) | MIT | 是 | 是，须遵守 MIT | 否，仅提交固定 SHA 引用 | 否 | 已审核（2026-08-18） |
| Git CLI | 开发与 CI 工具前置条件 | 开发机核验：2.55.0.windows.3；CI 版本由 GitHub runner 提供并记录在运行日志 | [Git 官方源码仓库](https://github.com/git/git) | GNU GPL v2；部分组件使用与 GPLv2 兼容的其他许可证 | 是，须遵守对应许可证 | 有条件，须遵守对应许可证；本项目不再分发 Git | 否 | 否 | 许可证来源已审核；CI 执行版本随环境记录 |

版本清单来自本轮成功恢复后生成的 `project.assets.json`，该文件位于被忽略的 `obj/` 目录，不提交到 Git。传递依赖在许可证逐项核实前不得改为“已审核”，也不得据此进入安装包。

## 单项审核记录模板

### `<名称>`

- 类型：
- 精确版本或提交：
- 官方来源：
- 许可证原文位置：
- 允许修改：
- 允许源码再分发：
- 允许二进制/模型/数据再分发：
- 必须保留的版权、LICENSE 或 NOTICE：
- 是否进入源码仓库：
- 是否进入安装包：
- 传递依赖或附带资源：
- 数据使用、隐私或保留条件：
- 审核状态：
- 审核日期与审核人：
- 备注：

## 禁止做法

- 不得仅凭项目名称、搜索摘要或记忆填写许可证。
- 不得把“可下载”视为允许修改或再分发。
- 不得提交许可证和来源尚未核实的模型权重、数据集或素材。
- 不得把游戏本体、游戏可执行文件、用户存档、账户信息或未经授权的 GTA 素材登记后直接提交；登记不能替代权利许可。
- 不得因某项只用于测试就省略许可证、来源或隐私审查。
