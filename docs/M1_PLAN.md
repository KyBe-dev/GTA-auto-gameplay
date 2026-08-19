# M1 最小闭环分步计划

状态：实施中；M1-A 已实现并通过自动化验证，交互式收口验收待完成；M1-B 至 M1-G 尚未实现。
适用基线：`5f932d10bd8b7d6123ae2c6e03b1a4f43c17ae35`  
权威边界：[`AGENTS.md`](../AGENTS.md)、[`ARCHITECTURE.md`](ARCHITECTURE.md) 和本轮项目维护者批准的 M1 范围。

M0 技术验收已经完成并提交；项目当前处于 M1 Windows 平台能力开发阶段。M1-A 已实现窗口发现与终端用户明确选择并通过自动化验证，交互式收口验收待完成；M1-B 至 M1-G 尚未实现，当前没有真实游戏窗口捕获、真实输入控制或 GTA 自动操作能力。

## 1. M1 目标和明确非目标

### 1.1 总体目标

M1 最终只建立一个默认拒绝、可观察、可紧急停止的最小闭环：

1. 发现 Windows 可见顶层窗口。
2. 由终端用户明确选择目标窗口，且选择不会自动 armed。
3. 分别建立 Capture Target 与 Input Target 的身份快照。
4. 只捕获所选窗口，不捕获整个桌面。
5. 使用本地、可替换观察器在合成或已授权数据上产生基础状态 Evidence。
6. 在每个短动作批次、以及批次中的每个动作派发前，重新读取并验证真实前台窗口与进程身份。
7. 只有身份、前台、捕获健康、状态新鲜度全部有效且终端用户明确 armed 后，才允许一次受限输入测试。
8. 失焦、窗口关闭、身份变化、权限不足、捕获失败、状态过期、取消或异常均进入锁存停止。

窗口发现、真实捕获和真实输入不得合并为一个提交或一个验收切片。M1-E 验收通过前真实输入调用次数必须始终为零；只有 M1-F 才能在受控测试窗口引入最小真实输入实现。

### 1.2 明确非目标

- 不支持 GTA Online、多人模式或联网对战场景。
- 不注入游戏进程，不读取或修改游戏内存，不使用内核驱动，不绕过 DRM、反作弊、平台保护或 Windows 权限边界。
- 不要求或自动请求管理员权限，不创建 Windows Service，不隐藏运行。
- 不实现完整 OCR、任务识别、`MissionTracker`、任务阶段迁移、导航、驾驶、连续控制或自动通关。
- 不实现 GTA 专属固定坐标、固定分辨率、固定语言规则、GTA VI 工程或未经实际测试的参数。
- 不接入云端 Provider，不让 Provider 直接决定输入。
- 不保存截图、捕获帧或录像，不创建回放缓冲区。
- 不实现 M2 的完整键位扫描、校准或永久输入配置。
- M1-F/G 不得向 GTA V 发送输入；真实 `SendInput` 首先且仅在项目自建受控测试窗口中验收。任何面向 GTA V 的输入试验都需要后续单独授权。

## 2. 窗口身份和 Core/Windows 边界

窗口身份不能只依赖标题、PID 或 HWND 中的任何单一值。标题是可变的显示文本；PID 和 HWND 都可能复用。

### 2.1 计划中的平台中立 Core 快照

Core 可持有不可变、可防御性复制的以下信息，但不得引用 `HWND`、`IntPtr`、`nint`、WinRT、COM 或 Windows API 类型：

| 字段 | Core 用途 | 约束 |
| --- | --- | --- |
| `SelectionId` | 标识一次终端用户明确选择 | 每次选择新建；应用重启后不得自动恢复为已选择或 armed |
| `WindowInstanceId` | 窗口实例的 opaque 身份 | 由 Platform.Windows 生成；不得等同于标题 |
| `ProcessId` | UI 显示与诊断 | 不能单独证明进程身份 |
| `ProcessInstanceId` | 防止 PID 复用 | 由 PID、进程启动身份和可执行程序身份共同派生的 opaque 值 |
| `ProcessStartedAtUtc` | 进程重启检测 | 无法可靠读取时验证失败，不猜测 |
| `WindowClassName` | 复核窗口类型 | 只作为组合身份的一部分 |
| `ExecutableName` | 终端用户确认展示 | 不包含完整路径 |
| `ExecutableIdentity` | 复核可执行程序实例 | opaque 值；完整路径、文件句柄等原始值留在 Platform.Windows |
| `CapturedAtUtc` | 快照产生时间 | 必填 UTC |
| `ValidUntilUtc` | 快照有效期 | 过期默认拒绝，不能自动续期为已选择状态 |

`WindowCandidate` 还可包含仅供当前 UI 展示的窗口标题。标题不进入身份相等判断，不写入默认日志，也不持久化。Capture Target 和 Input Target 必须是两个不同的目标对象和目标 ID；即使二者最初来自同一次选择，也不能因为捕获成功而隐式授予输入资格。

### 2.2 只留在 Platform.Windows 的信息

- 原始 HWND/`nint`、原始前台 HWND、窗口枚举回调和 Win32 错误码。
- 进程与令牌句柄、完整可执行文件路径、文件身份、签名查询细节和完整性级别 RID。
- Windows Graphics Capture、Direct3D、WinRT/COM 会话、纹理和帧池对象。
- `SendInput` 结构、扫描码、虚拟键、鼠标标志和本机错误码。
- `SelectionId` 到原始 HWND 的仅内存映射；该映射不保存到文件或用户配置。

Platform.Windows 将原生查询结果转换为平台中立快照、健康状态和明确失败枚举后才可跨越项目边界。Core 不解析 HWND 字符串，也不接收可逆的原始句柄数值。

## 3. 切片依赖关系

```text
M1-A 窗口发现与明确选择
  └─> M1-B 身份重新验证
        └─> M1-C 所选窗口捕获

M1-D1 本地观察契约与最小合成观察 ─┐
M1-C + M1-D1 ────────────────────────┼─> M1-D2 四态合成/授权夹具验收
M1-B + M1-C + M1-D2 ────────────────┘
                                      └─> M1-E 前台与输入资格验证
                                            └─> M1-F 受控窗口 SendInput
                                                  └─> M1-G 最小闭环集成
```

M1-D1 的纯接口和合成数据工作可在 M1-C 之前独立开发，但它与真实帧连接的验收必须等待 M1-C。M1-D2 是 M1-D 内部的独立提交，用于在 M1 完成前满足架构文档对 `Gameplay | Paused | Map | Unknown` 的四态要求。

## 4. M1-A：窗口发现与明确选择边界

### 输入

- 终端用户主动点击“刷新窗口列表”。
- Platform.Windows 提供的当前可见顶层窗口只读枚举结果。
- 当前 UTC 时间和集中配置的候选快照有效期。

### 输出和候选接口

- Core：`WindowCandidate`、`WindowIdentitySnapshot`、`WindowSelection`、`WindowSelectionFailure`、`IWindowDiscovery`。
- Platform.Windows：`WindowsWindowDiscovery`；内部 `IWin32WindowApi`/原生句柄映射，用于隔离 P/Invoke 并支持假对象测试。
- App：只读候选列表、刷新、明确选择、取消选择和当前选择摘要。

候选至少显示窗口标题、进程名称和 PID。标题为空、窗口不可见、工具窗口、已关闭窗口和本软件自己的窗口必须按明确规则处理。不得自动选择 GTA 窗口，也不得根据 `GTA`、`Grand Theft Auto`、进程名或标题自动控制或自动 armed。

### 验收标准

- 默认没有选择，刷新列表不会创建 Input Target，也不会 armed。
- 只有当前候选列表中的未过期项经过终端用户明确确认后才产生新的 `SelectionId`。
- 选择结果不可变；再次刷新、窗口消失或候选过期不会静默迁移到另一个窗口。
- 标题相同的两个窗口保持为两个候选；PID、窗口类和实例身份可区分。
- 枚举访问失败、窗口在枚举期间关闭或元数据读取不完整时返回明确状态，不抛出未处理异常，不猜测身份。
- 自动化测试使用假原生 API；人工 Windows 验收使用项目自建受控测试窗口或普通无敏感内容窗口。
- 真实捕获、前台验证、`ControlSafetyState` armed 和 `IInputController` 调用次数均为零。

### 本切片禁止提前实现

- Windows Graphics Capture、桌面捕获、前台 HWND 验证、完整性级别检查、`SendInput`、全局热键、GTA 标题自动匹配、选择持久化和最近窗口自动恢复。

## 5. M1-B：窗口与进程身份重新验证

### 输入

- M1-A 产生的 `WindowSelection`。
- 分离的 Capture Target 与 Input Target 快照。
- 当前 UTC 时间和 Platform.Windows 中仅内存的原始句柄映射。

### 输出和候选接口

- Core：`IWindowIdentityVerifier`、`WindowVerificationResult`、`WindowVerificationFailure`、`CaptureTargetSnapshot`、`InputTargetSnapshot`。
- Platform.Windows：`WindowsWindowIdentityVerifier`，内部使用可替换原生 API facade。
- 失败至少区分：快照过期、映射不存在、窗口关闭、句柄无效、PID 变化、进程启动身份变化、窗口类变化、可执行程序身份变化、进程已退出、权限不足、目标完整性级别高于本软件、原生查询失败。

### 验收标准

- 每次验证都重新查询 HWND 有效性、当前 PID、进程启动身份、窗口类和可执行程序身份，不能复用上次“有效”结果。
- 句柄复用、PID 复用、进程重启和窗口重建均不能继承旧选择。
- Capture Target 与 Input Target 独立验证并产生独立结果；相同身份只表示它们当前指向同一窗口实例，不合并两个目标对象。
- 目标以更高完整性级别运行、进程令牌不可读取或身份字段无法验证时，返回默认拒绝；不自动提权。
- 验证只读，不切换前台、不激活窗口、不捕获画面、不发送输入。

### 本切片禁止提前实现

- `SetForegroundWindow`、焦点窃取、管理员重启、UAC 提示、WGC、`SendInput`、输入映射和 GTA 模式判断。

## 6. M1-C：Windows Graphics Capture 会话

### 输入

- 已通过 M1-B 验证的 Capture Target。
- 用户发起的开始/停止捕获操作。
- 集中配置的帧容量、健康超时和尺寸限制。

### 输出和候选接口

- Platform.Windows：`IWindowCaptureSessionFactory`、`IWindowCaptureSession`、`CapturedFrameLease`、`CaptureHealthSnapshot` 和明确的 `CaptureFailure`。
- Core 只接收捕获目标 ID、帧时间、尺寸、健康状态、新鲜度和失败原因；原始纹理/像素不进入 `ControlSafetyState` 或日志。
- 观察层可通过有生命周期的只读帧 lease 消费像素；lease 释放后不能继续访问，不提供默认保存方法。

### 验收标准

- 只为用户选择的窗口创建 capture item；M1 不启用 DXGI Desktop Duplication 或任何整个桌面回退。
- 会话使用有界帧池，不无限缓存；默认不保存截图、帧或录像。
- 窗口最小化、关闭、尺寸/DPI/显示器变化、独占全屏不兼容、持续黑帧、受保护内容、设备丢失和捕获异常均产生明确健康状态。
- 任何无法确认有效画面的状态立即使 `IsCaptureHealthy=false`；恢复捕获不会自动解除安全停止或自动 armed。
- 尺寸变化后旧帧立即过期，完成会话重建并收到新鲜帧前保持不健康。
- 自动化测试使用假帧源和合成像素；真实 WGC 验收只在交互式 Windows 桌面执行。

### 本切片禁止提前实现

- 整桌面捕获、截图/录像保存、OCR、OpenCV/ONNX、云端上传、前台输入验证和 `SendInput`。

## 7. M1-D：最基础本地状态观察

### M1-D1：观察契约与最小合成观察

#### 输入和输出

- 输入：M1-C 的短生命周期只读帧、捕获时间、目标 ID、adapter ID/版本。
- Core 候选接口：`ILocalStateObserver`、`LocalStateObservationResult`。
- 输出：只允许产生 `LocalVision` Evidence，由既有 `StateEstimator` 做多帧融合；观察器不能直接替换 `GameState`。

#### 验收标准

- 首版只在项目自建受控窗口的确定性合成画面中区分 `Unknown` 和至少一个可安全验证的状态。
- 单帧结果不能形成全局状态，必须通过既有 StateEstimator 多帧门槛。
- 不可识别、尺寸异常、帧过期或观察异常均输出 `Unknown`/无有效 Evidence，不猜测。
- 不引入第三方视觉依赖，不使用 GTA 固定坐标，不调用 Provider。

### M1-D2：四态测试数据验收

#### 输入和输出

- 输入：自制合成夹具、项目自建受控窗口，或只在贡献者本机保存的合法授权 GTA 样本。
- 输出：可追溯的 `Gameplay | Paused | Map | Unknown` Evidence 和 StateEstimator 快照。

#### 验收标准

- 在公开合成夹具上覆盖四态、模糊/冲突、缩放和过期帧；真实 GTA 样本只做本机人工验证，不提交仓库。
- `Settings` 不得作为顶层状态；本切片不扩展到完整 `Menu`、OCR 或任务阶段。
- 指标只描述合成/授权测试集，不声称真实 GTA 普遍准确率。

### M1-D 全部阶段禁止提前实现

- OCR、任务文本、`MissionTracker`、GTA HUD 固定坐标、小地图、模型训练、云端 Provider、状态持久化和输入控制。

## 8. M1-E：前台目标和输入资格验证

### 输入

- M1-B 的最新 Input Target 与 Capture Target 验证结果。
- 每次重新读取的真实前台 HWND 及其完整组合身份。
- M1-C 的捕获健康/新鲜度。
- M1-D/StateEstimator 的最新快照时间与字段决策状态。
- 既有 `ControlSafetyCoordinator` 的 armed 和停止锁存状态。

### 输出和候选接口

- Platform.Windows：`WindowsControlSafetyStateSource` 实现既有 `IControlSafetyStateSource`。
- 内部可替换来源：`IForegroundWindowReader`、身份验证器、捕获健康源和状态新鲜度源。
- 输出使用现有 `ControlSafetyState`；`CaptureTargetId`/`InputTargetId` 保持分离，窗口与进程字段使用 Core 快照派生的 opaque 身份 ID。

### 验收标准

- `ControlSafetyState` 默认全部为 false/空；任何来源缺失、异常或超时都返回默认拒绝或使协调器进入 `SafetyStateUnavailable` 锁存停止。
- 每个短动作批次以及批次中的每个动作派发前都重新调用真实前台和身份验证；不得缓存“仍在前台”的结论。
- 前台 HWND 相同但 PID、启动身份、类名或可执行程序身份变化时拒绝。
- 捕获恢复、重新前台或状态恢复新鲜都不能自动 re-arm。
- 目标以更高完整性级别运行时明确拒绝并提示终端用户以标准权限重新启动目标；不得提示本软件自动提权。
- 使用 FakeInputController 的自动化测试证明本切片真实输入调用次数为零。

### 本切片禁止提前实现

- `SendInput`、物理键位、焦点切换、自动重新 armed、全局键盘钩子、管理员重启和连续动作循环。

## 9. M1-F：真实输入控制器的最小安全实现

### 前置条件

M1-A 至 M1-E 全部通过；真实安全状态源已证明默认拒绝；受控测试窗口可显示收到的输入及状态变化。此前不得添加 `SendInput` 实现。

### 输入和输出

- 输入：既有 `SemanticAction`、取消令牌，以及仅由受控测试配置显式注入的 Windows 平台绑定。
- Platform.Windows：`WindowsInputController` 实现既有 `IInputController`；内部 `ISendInputNative` facade 和 token 到物理绑定的短生命周期映射。
- 输出：成功完成的短动作，或明确异常；按下操作只在系统确认发送成功后返回 `InputToken`。

### 验收标准

- `WindowsInputController` 不包含 GTA 默认键位，不在业务代码硬编码 W、E、Esc 等物理键；M1 绑定只由受控测试窗口的临时配置显式提供且不持久化。
- 首版只支持完成“前进、一次转向、停止/释放”受控验收所需的最小键盘动作；鼠标移动、组合技能和连续保持循环不属于本切片。
- `ControlSafetyCoordinator` 继续是 held token 的权威账本；Windows 控制器只保留释放已成功发送 token 所需的最小映射。
- 不提供“释放所有物理按键”接口；重复释放已移除 token 不产生新的系统输入。
- `SendInput` 返回数量不完整、取消、异常或安全条件变化均视为失败，协调器锁存停止并只释放账本 token。
- 只在项目自建受控测试窗口执行人工验收；不得选择 GTA V 作为本切片输入目标。
- 标准用户运行，不注入、不安装驱动、不调用 `SetForegroundWindow`、不自动提权。

### 本切片禁止提前实现

- 面向 GTA V 的真实输入、完整用户键位配置、鼠标驾驶、全局键盘钩子、宏、重复动作、自动驾驶、任务逻辑或规避 UIPI。

## 10. M1-G：最小闭环集成

### 输入

- M1-A 至 M1-F 的已验收实现。
- 项目自建受控测试窗口及合成状态画面。
- 终端用户明确的选择和一次性 armed 请求。

### 输出

- 可见的状态摘要：当前选择、Capture/Input Target、捕获健康、最新 GameState、状态新鲜度、armed/锁存状态、停止原因和本软件持有 token 数。
- 单个短语义动作及其后续本地观察结果；可选产生 `ActionResult` Evidence，但不能单独证明全局状态。

### 验收标准

- 严格按“明确选择 → 身份验证 → 捕获 → 多帧观察 → 前台复核 → 明确 armed → 单个短动作 → 观察结果”执行。
- armed 请求必须绑定精确 `SelectionId` 和身份快照，单次消费、短时过期；未完成前台切换时只是 pending，不得提前 armed。该交互方案实施前需要项目维护者确认。
- 每次运行最多一个短动作，不自动重试、不连续循环、不后台运行。
- 任一环节发生失焦、窗口关闭、身份变化、捕获失败、状态过期、取消或异常时，立即锁存停止并释放账本 token；后续恢复不自动继续。
- 自动控制状态持续可见。终端用户切换焦点会立即触发停止；返回软件后可看到停止原因并决定是否重新选择或明确 re-arm。
- 全闭环真实输入仍只针对受控测试窗口。GTA V 在 M1 只做终端用户明确选择后的只读窗口识别、捕获和本地观察验收。

### 本切片禁止提前实现

- 连续自动操作、任务通关、驾驶、恢复重试、GTA V 真实输入、云端后备、截图保存、安装包和自动更新。

## 11. 安全门和默认拒绝条件

下列条件必须全部为真，才具备调用 `IInputController` 的资格：

1. 存在未过期且由终端用户明确产生的 Selection。
2. Capture Target 和 Input Target 都存在并分别通过最新身份验证。
3. 二者的窗口实例和进程实例身份一致，但仍保留不同目标 ID。
4. Input Target 是当前真实前台窗口。
5. 目标完整性级别不高于本软件，且所有必要身份字段可读取。
6. 捕获会话健康，最新帧未过期且属于当前 Capture Target。
7. 最新 GameState 未过期，所需字段没有冲突；`GameState.Confidence` 只作摘要，不能单独满足本门。
8. 安全状态来源可用，未发生取消或异常。
9. 紧急停止未锁存，终端用户针对当前选择明确 armed。
10. 动作受当前 M1 受控测试绑定支持，并且批次短、有界、可取消。

任一条件为 false 或无法验证都必须拒绝。捕获恢复、窗口重新前台、重新获得有效状态或重新发现相同标题均不能自动解除锁存。只有终端用户针对当前身份明确重新 armed 才能恢复资格。

建议在 M1-B/E 增加明确停止原因：窗口关闭、窗口身份变化、进程身份变化、权限不足和目标完整性级别过高。增加枚举前必须单独审查与现有 `ControlStopReason` 的兼容性。

## 12. Windows 权限与完整性级别

- 软件默认且持续以标准用户权限运行；manifest 不要求管理员权限。
- M1-B/E 需要查询本软件和目标进程的完整性级别。目标高于本软件、令牌查询被拒绝或结果不确定时，输入资格为 false。
- UI 只说明“目标权限高于本软件或无法验证，因此拒绝控制”；不提供自动重启为管理员按钮，不引导关闭系统保护。
- UIPI 导致 `SendInput` 失败时按输入控制器异常处理并锁存停止，不尝试绕过。
- 独占全屏、受保护内容或平台策略导致 WGC 失败时标记不支持，不回退到整个桌面捕获，也不绕过保护。
- 不创建 Service、计划任务、驱动、注入器或隐藏辅助进程。

## 13. 测试策略和隔离

### 13.1 自动化层次

| 层次 | 适用内容 | 默认 CI |
| --- | --- | --- |
| Core 单元测试 | 不可变身份、过期、组合相等、选择显式性、失败映射、安全门、状态新鲜度 | 运行 |
| Platform.Windows facade 测试 | 假 Win32/WGC/SendInput facade，句柄/PID 复用、访问拒绝、部分发送、设备丢失 | 运行；不需要真实桌面 |
| Windows 假对象集成测试 | 假窗口目录、假帧源、假前台源、假完整性级别和 FakeInputController | 运行 |
| 受控窗口自动化 | 项目自建 WPF 测试窗口；要求交互桌面 | 默认不在 GitHub Actions 运行 |
| 人工 Windows 验收 | 多显示器、DPI、最小化、窗口关闭、独占全屏、权限差异、真实 WGC/SendInput | 仅人工 |
| GTA V 只读验收 | 明确选择、窗口捕获和本地观察；仅离线故事模式 | 仅本机人工，不提交素材，不发送输入 |

### 13.2 建议测试项目和标记

- `tests/GtaAutoGameplay.Platform.Windows.Tests`：可在 CI 运行的 facade/假对象测试。
- `tests/GtaAutoGameplay.Windows.IntegrationTests`：真实桌面测试，标记 `WindowsInteractive`、`RequiresDesktopSession`。
- `tests/GtaAutoGameplay.ControlledWindow`：项目自建 WPF 测试窗口，不进入安装包。
- GitHub Actions 默认使用测试筛选排除 `WindowsInteractive` 和 `RequiresDesktopSession`；只有项目维护者在本机显式设置测试开关并拥有交互式桌面时才运行。
- 被排除的测试不能显示为普通 CI 已验证；每次 M1-C/F/G 人工验收记录 Windows 版本、显示模式、DPI、权限级别、目标提交和结果，但不附截图或大段日志。

### 13.3 每个切片最低测试要求

- M1-A：相同标题、多窗口、空标题、窗口中途关闭、明确选择、取消和过期。
- M1-B：无效 HWND、句柄复用、PID 复用、进程重启、类名/可执行身份变化、访问拒绝和完整性级别差异。
- M1-C：开始/停止、帧上限、最小化、关闭、resize、DPI、多显示器、黑帧、受保护/不支持、设备丢失和恢复不自动 armed。
- M1-D：四态合成数据、多帧、Unknown、冲突、过期、adapter 版本隔离和无 Provider 调用。
- M1-E：每动作重新验证、失焦、捕获失败、状态过期、来源异常、恢复仍锁存和输入调用为零。
- M1-F：精确绑定、部分发送、取消、异常、账本释放、重复停止、无 ReleaseAll、标准用户和受控目标限制。
- M1-G：完整 happy path、每个环节失败、焦点切换、窗口关闭、状态过期、单动作上限和明确 re-arm。

## 14. 隐私与日志边界

- 日志继续只使用字段白名单结构化事件；M1 新日志字段必须先单独审查并加入白名单。
- 默认可记录：事件 ID、SelectionId、opaque 目标/身份 ID、失败枚举、帧尺寸、耗时、健康状态、状态类型和停止原因。
- 默认不得记录：窗口标题、完整可执行路径、用户名、账户信息、原始 HWND、原始帧、截图、录像、OCR 原文、桌面内容或原生结构体内存。
- 窗口标题只在当前选择 UI 中短时显示，不进入日志或持久化。完整路径只在 Platform.Windows 内存中用于身份复核。
- 帧缓冲有界、短生命周期、消费后释放；不得提供文件写入或网络上传路径。
- 公开自动化夹具只能是自制、合成或已明确授权内容，并按 [`tests/fixtures/README.md`](../tests/fixtures/README.md) 记录。真实 GTA 截图和录像只能保存在贡献者本机忽略目录。
- 若 M1-C 需要新增 Windows SDK 包或其他依赖，必须先核实官方来源、精确版本、许可证与再分发边界，并更新 [`THIRD_PARTY_INVENTORY.md`](THIRD_PARTY_INVENTORY.md)。

## 15. M1 完成定义

只有同时满足以下条件才可宣布 M1 技术完成：

- M1-A 至 M1-G 全部独立验收，M1-D2 的四态合成/授权夹具测试通过。
- GTA V 窗口只能由终端用户明确选择；只读发现、身份复核、捕获和基础观察已在离线故事模式本机人工验证。
- 真实输入闭环仅在项目自建受控测试窗口通过；没有向 GTA V 发送输入。
- 每个动作派发前的真实前台、组合身份、完整性级别、捕获健康和状态新鲜度复核均有测试证据。
- 锁存式停止、token 账本释放、重复停止、取消、异常和恢复不自动 armed 全部通过。
- 自动控制状态持续可见，标准用户权限可运行；高权限目标被拒绝。
- 没有真实 GTA 素材、截图、录像、凭据、日志或构建产物进入仓库。
- Release 构建、全部非交互测试、仓库候选文件扫描、完整可达历史扫描和 `git diff --check` 通过。
- 文档准确说明 M1 只是最小技术闭环，不代表普通终端用户可用、完整任务支持或可公开安装；任何公开安装包仍需 M9。

## 16. 文档冲突、风险和维护者待决事项

### 已在本计划和权威文档中统一的项目

- `docs/ARCHITECTURE.md` 的 M1 要求最终识别四个状态，而本轮要求 M1-D 首版只做最小子集。本计划将 M1-D 拆为 D1 和 D2：D1 保持最小，M1 完成前由 D2 补齐四态，因此不降低最终架构验收标准。
- `docs/ARCHITECTURE.md` 已明确 M1 只使用 Windows Graphics Capture 捕获终端用户选择的单个窗口，禁止把 DXGI Desktop Duplication 作为 M1 备用方案或失败回退。DXGI 仅是 M1 之外的未来研究项，在满足隐私和目标窗口隔离要求前不得启用。
- 现有 `ControlSafetyState` 使用字符串身份。本计划保留它作为安全协调器的 opaque 比较边界，同时新增强类型、平台中立身份快照作为 M1-B 的权威模型；不把 HWND 字符串化塞入 Core。

### 实施前需要项目维护者决定

1. **最低 Windows 版本/构建号**：这会决定 WGC、WinRT 互操作、DPI 和测试矩阵。建议在 M1-C 前确定，不阻止 M1-A/B。
2. **WGC 的 .NET/Windows SDK 接入方式**：如需新 NuGet 包，必须先核实官方来源和许可证；本计划不预先选择或添加。
3. **明确 armed 的真实交互**：建议 M1-G 使用与精确 Selection 绑定、单次消费、短时过期的 pending-arm 交接，让终端用户点击后切换到目标；不得在目标恢复前台时无用户请求自动 armed。是否改用经确认的全局热键需单独决定。
4. **受控测试窗口项目形式**：建议建立独立、仅测试用 WPF 可执行项目，不复用正式 App 的隐藏测试模式，也不进入发布产物。
5. **M1 后是否允许面向 GTA V 的输入试验**：本计划明确不允许。若未来需要，必须单独授权，并先解决如何可靠排除 GTA Online；仅凭标题、PID 或进程名无法证明处于离线故事模式。

### 已完成的开始前文档同步

- `README.md` 已统一为“M0 技术验收完成、M1 Windows 平台能力开发准备阶段”，并明确 M1 尚未实现。
- `docs/ARCHITECTURE.md` 第 13 节已改为 M1 当前状态与下一步，不再保留过时的 M0 启动步骤。
- README、架构文档、本计划和 `CODEX_STATUS.md` 使用一致的 M1-A 至 M1-G 名称、顺序和输入安全边界。

此前同步只修正文档状态和边界。当前 M1-A 已按下述两个独立切片实现；这不表示 M1-B 或任何捕获、前台验证、armed、输入能力已经实现。

## 17. M1-A 实施范围

M1-A 已拆成两个独立提交范围；实现和自动化验证完成后，还需使用独立受控窗口完成交互式收口验收：

### M1-A1：平台中立选择契约

状态：已完成并提交。

计划文件范围：

- `src/GtaAutoGameplay.Core/Targeting/WindowCandidate.cs`
- `src/GtaAutoGameplay.Core/Targeting/WindowIdentitySnapshot.cs`
- `src/GtaAutoGameplay.Core/Targeting/WindowSelection.cs`
- `src/GtaAutoGameplay.Core/Targeting/WindowSelectionFailure.cs`
- `src/GtaAutoGameplay.Core/Targeting/IWindowDiscovery.cs`
- `tests/GtaAutoGameplay.Core.Tests/WindowSelectionTests.cs`

只实现不可变模型、显式选择语义、候选/选择过期、防御性复制和假发现源测试。不得出现 Windows API、HWND、捕获、前台或输入类型。

### M1-A2：Windows 枚举和可见选择 UI

状态：已实现并通过自动化验证，等待交互式收口验收、项目维护者审核和提交。

计划文件范围：

- `src/GtaAutoGameplay.Platform.Windows/Windowing/IWin32WindowApi.cs`
- `src/GtaAutoGameplay.Platform.Windows/Windowing/Win32WindowApi.cs`
- `src/GtaAutoGameplay.Platform.Windows/Windowing/WindowsWindowDiscovery.cs`
- `src/GtaAutoGameplay.Platform.Windows/Windowing/NativeWindowReference.cs`（仅 internal）
- `src/GtaAutoGameplay.App/MainWindow.xaml`
- `src/GtaAutoGameplay.App/MainWindow.xaml.cs` 或不引入第三方框架的最小 ViewModel
- `tests/GtaAutoGameplay.Platform.Windows.Tests/`（新测试项目，仅复用已经审核的测试依赖版本）
- `tests/GtaAutoGameplay.ControlledWindow/`（独立测试窗口，不进入产品发布路径）

只实现：刷新可见顶层窗口、显示标题/进程名/PID、终端用户明确选择与取消、内存中的 SelectionId→原始 HWND 映射、访问失败提示。完成时仍不得创建 Capture/Input Target、不得查询前台资格、不得 armed、不得捕获、不得调用 `IInputController`。M1-A 的安全验收核心是“可以选择，但绝不控制”。
