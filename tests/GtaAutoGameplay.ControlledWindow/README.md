# M1-A Controlled Test Window

此项目是与正式 App 分离的人工验收窗口，不进入正式发布产物。它只显示固定测试标题，不执行捕获、输入控制或 GTA 模拟，也不包含游戏素材。

在交互式 Windows 桌面中先运行：

```powershell
dotnet run --project tests/GtaAutoGameplay.ControlledWindow --configuration Release
```

然后运行正式 App，点击“扫描/刷新窗口”，确认列表出现 `GTA Auto Gameplay Controlled Window — M1-A Test`。扫描不得自动选择；只有明确点击“选择”后才显示当前选择。点击“取消选择”、再次刷新或关闭测试窗口后，旧选择必须失效。
