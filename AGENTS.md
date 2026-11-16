# AGENTS.md

## 适用范围

- 本文件适用于仓库根目录及所有子目录。
- 这是一个 Unity Editor 工具包。所有修改都应以当前仓库中的实际代码、资源和调用关系为依据。
- 用户在当前任务中的明确要求优先于本文件。

## 项目概况

- 包名：`com.play.unityassetscoretool`；版本：`1.0.0`；Unity 基线：`2022.3.0f1`。
- 主程序集为仅 Editor 平台的 `Editor/AssetScoreTool.Editor.asmdef`，根命名空间为 `majsoul.tools.assetscoretool.editor`。
- 项目不包含 Runtime 程序集；不要主动恢复 Runtime，也不要将 UnityEditor API 引入运行时程序集。
- `Editor/Spine/AssetScoreTool.Spine.Editor.asmdef` 是可选的 Spine Editor 扩展，依赖 `AssetScoreTool.Editor`、`spine-unity`、`spine-csharp`，且必须定义 `ASSET_SCORE_TOOL_SPINE` 才会参与编译。
- 已存在 `Editor/Spine/Tests/AssetScoreTool.Spine.Editor.Tests.asmdef` EditMode 测试程序集，依赖 Spine 扩展并同样受 `ASSET_SCORE_TOOL_SPINE` 限制。除非用户明确要求，不新增测试程序集、测试脚本、测试资源、示例场景或临时验证代码。

## 开始修改前

1. 未经用户明确要求，不执行任何 Git 命令或 Git 操作，包括状态检查、差异检查、暂存、提交、切换分支和推送。
2. 保留用户已有的工作区改动，不覆盖或回退无关文件。
3. 仓库存在 `.codegraph/`。理解或定位代码时，优先使用 `codegraph explore`、`codegraph node`、`codegraph callers`、`codegraph callees` 或对应 MCP 工具，再使用 `rg` 补充文本检索。
4. 阅读相关实现、调用方、资源引用和相邻目录规则后再提出修改计划。
5. 计划至少说明目标行为、修改文件、复用点、风险和验证方式；得到用户确认后再编辑代码或 Unity 资源。
6. 只修改完成当前需求必需的代码和资源。发现额外问题时先说明，不要顺带重构。

## 目录与职责

- `Editor/Core/Scanning/`：扫描上下文、原始扫描数据、扫描调度、白名单过滤和路径工具。
- `Editor/Core/Rules/`：问题数据、问题严重度、规则与资源类型检测契约。
- `Editor/Core/Scoring/`：扫描结果、资源等级和评分计算。
- `Editor/Core/Reporting/`：扫描报告数据及报告构建流程。
- `Editor/Config/`：Profile、规则应用、规则、资源类型、项目设置模型与生效配置解析；`AssetScoreToolSpineSettings` 管理可选 Spine 依赖和定义符号。
- `Editor/Checkers/`：内置评分规则和资源类型检测实现。
- `Editor/Spine/`：可选 Spine 的资源类型、规则、共享工具和 EditMode 测试；该目录代码不得泄漏到主程序集。
- `Editor/UI/ConfigWindow/Data/ConfigWindowData.cs`：配置窗口的数据加载、工作副本、保存、重置、复制、删除和脚本生成流程。
- `Editor/UI/ScanWindow/`：扫描窗口及其输入、状态栏、结果树、详情和 Project 菜单入口。
- `Editor/UI/ConfigWindow/`：配置中心窗口、各配置页面和创建/选择/编辑弹窗。
- `Editor/UI/Common/`：可复用的 UI Toolkit 控件、UXML 和 USS。
- `Configs/`：项目自带的资源类型、Profile、规则应用、规则和项目设置配置资产。修改前必须检查序列化字段、引用和 GUID 兼容性。
- `docs/`：规则资料与界面参考稿；`配置界面.html`、`扫描界面.html` 为视觉与交互参考，不是默认必须同步维护的功能文档。

## 核心数据流

### 扫描与报告

1. `ScanWindow.OnStartScanClicked` 校验 Profile 和 Unity 资源路径，并调用 `AssetScoreToolScanner.StartScanAsync`。
2. `AssetScoreToolScanner` 加载生效配置、构建依赖、调度规则检查，只产生 `AssetScanData` 原始数据。
3. `AssetScoreReportBuilder.Build` 基于原始数据和同一份 `RuleApplyConfig` 应用白名单、计算自身与依赖汇总结果，并生成展示根节点。
4. `ScanWindow` 只消费 `AssetScoreReport.DisplayRootResults` 和报告源数据刷新列表、详情与状态。

保持以下边界：

- Scanner 负责资源发现、依赖和规则执行，不负责 UI 展示、等级文案或树形布局。
- 评分、白名单和汇总结果在 Scanner 外部构建，不要在 UI 中重复计算。
- `AssetScanData` 是原始扫描事实，`AssetScoreReport` 和 `AssetScanResult` 是派生结果；不要混用状态归属。
- 报告白名单必须使用 `scanner.GetRuleApplyConfig()` 返回的扫描配置，不能重新解析另一份配置。
- `StartScanAsync` 通过 `await Task.Yield()` 在 Unity 主线程分帧推进；不得在 `Task.Run` 中调用 Unity Editor API。

### 配置编辑

- `RuleApplyConfigResolver.GetActiveConfig` 是生效规则应用配置的统一入口：优先使用项目设置指定的配置，未指定时回退默认配置。
- `ConfigWindowData` 为真实 ScriptableObject 创建 `HideFlags.DontSave` 工作副本。编辑期间只修改工作副本。
- 保存时通过 `CommitWorkingCopies`、`Undo.RecordObject`、`EditorUtility.CopySerialized`、`EditorUtility.SetDirty` 和 `AssetDatabase.SaveAssets` 写回真实资产。
- 放弃或关闭未保存编辑时，应保持真实资产不受污染，并正确销毁或重建工作副本。
- 配置页面通过 `IConfigPage` 和 `ConfigPage<TConfig>` 接入现有页面流程；不要为新页面另建平行的加载、验证或保存机制。
- Package 中的配置只能预览，不能直接编辑或删除；需要修改时应先复制到本地配置，再编辑本地工作副本。
- RuleApply 页面的规则编辑必须使用独立规则编辑弹窗，并复用 `RuleConfigPage` 的字段构建流程。

### 可选 Spine 检测

- `UserSettingConfig.EnableSpineChecks` 控制配置中心中的 Spine 开关；`AssetScoreToolSpineSettings` 会先确认 `spine-unity` 与 `spine-csharp` 程序集均可用，再管理 `ASSET_SCORE_TOOL_SPINE` 定义符号。
- 不要让主程序集直接引用 Spine 类型。Spine 类型识别和规则实现仅放在 `Editor/Spine/`，并保持对应程序集的依赖与定义约束。
- 修改 Spine 规则、资源类型或程序集定义后，在依赖可用且符号开启时应检查 `AssetScoreTool.Spine.Editor` 和 `AssetScoreTool.Spine.Editor.Tests` 的编译结果；仅在用户要求时运行相关 EditMode 测试。

## 扩展规则和资源类型

### 新增评分规则

- 实现 `IScoreRuleChecker`，保持具体类可被 `TypeCache.GetTypesDerivedFrom<IScoreRuleChecker>()` 发现并可由无参构造函数实例化。
- `IScoreRuleChecker.RuleId` 必须与对应 `ScoreRuleConfig.RuleId` 完全一致并保持唯一。
- `ShouldCheck` 只判断规则是否适用于当前 `AssetContext`；`Check` 返回命中的 `IssueInfo`，未命中返回 `null`。
- 同时检查规则配置资产、Profile、RuleApply 配置和配置中心 UI 是否需要接入或引用。
- 通用内置规则放在 `Editor/Checkers/Rules/`；仅依赖 Spine API 的规则放在 `Editor/Spine/Rules/`，不得将 Spine 引用带入主程序集。

### 新增资源类型

- 实现 `IAssetTypeChecker`，保持具体类可被 `TypeCache.GetTypesDerivedFrom<IAssetTypeChecker>()` 发现并可由无参构造函数实例化。
- `IAssetTypeChecker.AssetTypeId` 必须与对应 `AssetTypeConfig.AssetTypeId` 完全一致并保持唯一。
- 类型识别逻辑只判断资源是否匹配，不在其中执行评分规则或构建展示数据。
- 通用内置类型放在 `Editor/Checkers/AssetTypes/`；Spine 类型放在 `Editor/Spine/AssetTypes/`。

### 路径、资源与序列化

- Scanner 接受 `Assets/...` 形式的 Unity 资源路径。统一使用 `ScanToolUtility.NormalizePath`，不要硬编码本机绝对路径。
- 通过系统文件选择器取得的路径必须先转换并验证为当前项目 `Assets` 下的路径。
- 修改资源路径、默认配置名、菜单路径、规则 ID、资源类型 ID、Profile ID 或序列化字段名前，先检查所有代码和 `.asset` 引用。
- 不随意重命名、移动或删除 ScriptableObject 脚本和资源；确需操作时保持 `.meta` 与 GUID 的正确对应。
- 新增 C#、UXML、USS、Markdown 或其他 Unity 可导入文件时，不手动创建 `.meta`。

## UI Toolkit 约束

- UXML 负责稳定结构，USS 负责通用稳定样式，C# 负责数据绑定、动态状态和临时高亮。
- UXML 能够表达的布局、层级、尺寸、对齐、间距和静态控件属性必须优先通过 UXML 修改；不要为了复用组件在初始化代码中随意移动、删除或重新挂载 UXML 已定义的 VisualElement。
- C# 只负责 UXML 控件的查找、事件绑定、数据绑定和运行时状态刷新。只有在 UXML 无法表达或确实需要根据运行时数据改变布局时，才允许通过代码修改视觉树，并在注释中说明原因。
- 扫描窗口按现有 partial 文件职责扩展：顶部输入、状态栏、结果列表、详情和 Project 菜单不要相互复制状态。
- 配置中心的列表、页面、弹窗和字段编辑应复用 `ConfigPage<TConfig>`、公共控件以及窗口已有的选择、验证和变更跟踪流程。
- `Common.uss` 只放至少被多个页面或公共组件实际复用的稳定视觉样式；页面专用样式放在对应页面 USS 中。
- 仅在用户明确要求更新参考稿或执行视觉对齐时，才维护 `docs/配置界面.html` 或 `docs/扫描界面.html`。视觉对齐必须同时核对 HTML DOM、UXML 静态层级、C# 动态生成层级与最终 USS 命中关系。

## C# 实现约定

- 遵循仓库现有的 C# 写法和命名；不要为了现代语法而提高 Unity/C# 版本要求。
- 每个手写具名方法和构造函数都应有说明其职责的 `/// <summary>`；复杂分支需说明业务原因、顺序和边界条件。
- 优先复用现有工具、状态模型和配置加载方式。只有职责独立且会被多处使用时才新增公共抽象。
- 不创建只调用一次的包装方法、转发类或平行数据流，也不进行与需求无关的格式化、重命名或目录调整。
- 错误处理遵循现有风格：记录异常、恢复 UI 状态，并在需要时向用户显示明确反馈；不要静默吞异常。

## 最小充分验证

- 修改主包 C#、程序集定义或 Unity 可导入代码资源后，默认验收是 Unity 编译通过：`AssetScoreTool.Editor` 没有相关编译错误。
- 修改可选 Spine 代码、Spine 资产或其程序集定义时，若已安装依赖且启用定义符号，还必须检查 `AssetScoreTool.Spine.Editor` 与 `AssetScoreTool.Spine.Editor.Tests` 的编译结果。
- 默认不要求运行 EditMode、PlayMode、单元、集成、回归或功能测试；用户明确要求时，优先运行受影响的既有 Spine EditMode 测试，不为验证创建临时测试代码或资源。
- 无法直接读取 Unity 编译结果时，必须完成静态编译风险检查：核对 `using`、命名空间、类型歧义、方法和字段引用、重复声明、partial 合并后的成员与依赖完整性，以及明显的 C# 语法结构。
- 无法运行 Unity 时，不得声称编译已通过；应明确说明未实际验证编译，并提醒用户切回 Unity 确认 Console。

## 交付要求

- 明确列出新增、修改和删除的文件。
- 说明实际改动、影响范围、验证结果和未验证部分。
- 不声称执行过未实际运行的 Unity 编译或测试。
- 不执行任何 Git 命令或 Git 操作，除非用户明确要求。
- 新增 Unity 可导入文件后，不手动生成 `.meta`；交付时提醒用户切回 Unity，让 Unity 自动生成。
