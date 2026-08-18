# Repository Guard

该工具只读取当前仓库中已经跟踪及未被 `.gitignore` 排除的候选文件。使用 `--history` 时，它还通过只读 Git 命令检查所有本地可达分支和标签中的历史 blob。

工具不会修改、删除或上传文件。发现项只输出文件路径、规则 ID、历史对象短标识和安全说明，不输出完整疑似秘密值。

本地运行：

```powershell
dotnet run --project tools/GtaAutoGameplay.RepositoryGuard --configuration Release -- --repository . --history --allowlist tools/repository-guard.allowlist.json
```

允许列表只接受“精确规则 ID + 精确文件路径 + 具体原因”。禁止通配符、目录级豁免和无说明条目。公开测试夹具路径不会被整体豁免；任何二进制、媒体或模型文件仍须逐文件审核。

容量边界：单个跟踪文件超过 1,048,576 字节时默认失败；不超过该容量但无法作为 UTF-8 文本检查的二进制文件同样默认失败。

该工具是项目自建的基础防线，不保证发现所有凭据格式，也不能替代成熟秘密扫描工具、GitHub 托管内容检查或 M9 发布审计。
