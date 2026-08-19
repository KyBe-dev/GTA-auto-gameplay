# GtaAutoGameplay.Platform.Windows

此项目承载 Windows 专属平台边界。M1-A 已实现可见顶层窗口枚举、候选转换和仅内存的原生窗口映射；原始句柄与 P/Invoke 不跨越此项目边界。

当前仍未实现 Windows Graphics Capture、持续前台/身份验证、Capture Target、Input Target、armed 或 `SendInput`。这些能力只能按 `docs/M1_PLAN.md` 的后续独立切片实施。
