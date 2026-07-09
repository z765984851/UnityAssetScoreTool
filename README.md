# AssetScoreTool

## 1. 简介

`AssetScoreTool` 是一个仅在 Unity Editor 中运行的美术资产评分工具包。它负责扫描项目 `Assets/...` 下的资源及其依赖，按照项目配置执行性能风险规则，并生成包含问题、扣分、等级、严重度和依赖汇总信息的报告。

- 包名：`com.catfood.unitytools.assetscoretool`
- 当前版本：`1.0.0`
- Unity 基线：`2022.3.0f1`
- 主程序集：`AssetScoreTool.Editor`（仅 Editor 平台）
- 根命名空间：`majsoul.tools.assetscoretool.editor`

## 2. 安装

### 通过 Git URL 引入

1. 在 Unity 中打开 `Window > Package Manager`。
2. 点击左上角的 `+`，选择 `Add package from git URL...`。
3. 如果工具位于当前仓库的 `Assets/Tools/asset_score_tool` 子目录，使用以下格式，并将仓库地址替换为实际 Git URL：

```text
<仓库 Git URL>?path=Assets/Tools/asset_score_tool
```

如果 `package.json` 位于目标 Git 仓库根目录，则直接填写仓库 Git URL，不需要 `?path=...`。

### 通过本地路径引入

1. 在 `Window > Package Manager` 中点击左上角的 `+`。
2. 选择 `Add package from disk...`。
3. 选择本工具根目录下的 `package.json`。

安装完成后，Unity 会将工具作为 Package 加载。Package 中的脚本、配置和文档属于工具内置内容，不应复制到项目 `Assets` 目录后直接维护，也不应在 `Packages/...` 或 `Library/PackageCache/...` 中修改。

## 3. 入口

| 功能 | Unity 入口 |
| --- | --- |
| 资源评分窗口 | `Catfood Tools > Asset Score Tool > 资源评分` |
| 配置中心 | `Catfood Tools > Asset Score Tool > 配置中心` |
| 扫描 Project 选中资源 | 在 Project 面板选中资源或文件夹后，使用 `Assets > Catfood Tools > Asset Score Tool > 扫描选中资源` |

## 4. 快速开始

### 一、扫描

#### 直接打开工具扫描

1. 打开 `Catfood Tools > Asset Score Tool > 资源评分`。
2. 在 `Profile` 下拉框中选择本次扫描使用的检测配置档。
3. 输入当前项目中的 `Assets/...` 资源路径，或者使用路径右侧的按钮选择文件或文件夹。
4. 点击“开始评分”。扫描期间可以点击“取消评分”请求取消。
5. 扫描完成后，在左侧结果树中筛选、排序、展开并选择资源，在右侧查看资源详情、问题和直接依赖。
6. 需要留档时，点击“导出 Json”或“导出 Excel”；也可以点击“打开导出目录”查看报告。

#### 从 Project 面板右键扫描

1. 在 Unity 的 Project 面板中选中一个资源或文件夹。
2. 使用 `Assets > Catfood Tools > Asset Score Tool > 扫描选中资源`。
3. 工具会打开资源评分窗口并自动填写选中资源的 `Assets/...` 路径。
4. 选择检测配置档，然后点击“开始评分”。

扫描时选择的检测配置档必须包含在规则应用项的适用范围内，否则对应规则不会执行。

### 二、配置

打开 `Catfood Tools > Asset Score Tool > 配置中心`。配置卡片会标记为“包内”或“本地”：Package 内置配置只读，可以直接被本地规则应用配置引用；需要调整应用范围时，应先把规则应用配置“复制到本地”，为副本设置新的唯一 ID，再编辑本地副本。所有新配置和检测脚本都必须创建在使用方项目的 `Assets/...` 下，不能保存到 Package 目录。

配置中心使用工作副本编辑本地配置。完成以下步骤后必须点击顶部的“保存全部”，修改才会写入项目 `Assets/...` 下的配置资产；Package 内置内容不会被改写。

#### 2.1 项目配置（Setting）

1. 打开“项目设置”页面。首次打开扫描窗口或配置中心时，工具会自动创建 `Assets/AssetScoreTool/ProjectSettings/AssetScoreProjectSettings.asset`。
2. 查看“当前生效规则应用配置”。该字段留空时，扫描会自动回退到 Package 内置的 `RuleApplyConfig_Default`；需要自定义时，完成 2.5 后再选择本地规则应用配置。
3. 根据项目目录调整“默认保存位置”。检测配置档、规则应用配置、检测规则配置、资源类型配置和 CheckRule 检测脚本必须保存在项目的 `Assets/...` 下；报告导出目录可以使用本机绝对路径。
4. 如果项目同时安装了 `spine-unity` 和 `spine-csharp`，可以在“可选检测”中启用 Spine 检测；没有这些依赖时不要开启。

项目中只能保留一个可用的项目设置资产。发现多个 `UserSettingConfig` 时，配置中心无法确定应使用哪一份设置。

#### 2.2 新增平台（检测配置档）

界面中的正式名称是“检测配置档”，可以用它表示 PC、iOS、Android、Web 或项目自定义平台。

Package 已内置 PC、Web、Android 和 iOS 检测配置档，可以直接使用，不需要在 Package 中重新创建。只有项目需要额外平台或独立配置档时才执行以下步骤。

1. 打开“检测配置档”页面，点击“新建检测配置档”。
2. 填写不会与 Package 内置配置重复的“配置档 ID”，例如 `project.pc`；ID 只能使用界面提示允许的格式。
3. 填写“显示名称”，例如 `项目 PC 配置`。
4. 选择项目 `Assets/...` 下的保存目录并创建配置，不要选择 Package 目录。

后续需要在规则应用项中选择该检测配置档，并在扫描窗口中选择同一配置档，规则才能匹配。

#### 2.3 新增资源类型

1. 打开“资源类型配置”页面，点击“新建资源类型配置”。
2. 填写唯一的“资源类型 ID”，例如 `custom_texture`，并填写显示名称。
3. 选择项目 `Assets/...` 下的保存目录并创建配置。
4. 在项目自己的 Editor 目录中实现可被 Unity `TypeCache` 发现的 `IAssetTypeChecker`，例如 `Assets/AssetScoreTool/Extensions/Editor/AssetTypes/`，并保证 Checker 的 `AssetTypeId` 与配置中的资源类型 ID 完全一致。

资源类型配置只定义可被规则引用的类型标识，不负责识别资源。仅创建配置但没有对应的 `IAssetTypeChecker` 时，扫描不会匹配到该资源类型。如果目标类型已经由 Package 内置，可以直接引用已有资源类型配置，不要创建重复 ID；自定义实现要求见第 8 节“新增资源类型”。

#### 2.4 新增规则

1. 打开“检测规则配置”页面，点击“新建检测规则配置”。
2. 填写唯一的“规则 ID”、显示名称、描述和优先级。规则 ID 必须与对应检测脚本的 `RuleId` 完全一致。
3. 在规则详情的“问题项”区域点击“+ 添加问题项”。
4. 填写唯一的“问题 ID”、显示名称、扣分、问题级别和问题提示，然后点击“添加”。一个规则可以包含多个问题项。
5. 在“检测脚本”区域点击“创建检测脚本”，确认类名，并把脚本保存到项目自己的 Editor 目录，例如 `Assets/AssetScoreTool/Extensions/Editor/Rules/`。检测脚本会进入等待保存状态。
6. 点击顶部“保存全部”，让工具创建 CheckRule 脚本文件，并等待 Unity 编译完成。
7. 打开生成的检测脚本，实现规则的适用条件和检测逻辑，确保命中时返回规则配置中存在的问题 ID。

新规则配置和检测脚本都应位于项目 `Assets/...` 下。如果规则没有可用的 `IScoreRuleChecker`、脚本中的 `RuleId` 不一致，或者返回了配置中不存在的问题 ID，该规则不能产生有效扫描结果。项目使用自定义 Editor `.asmdef` 时，该程序集需要引用 `AssetScoreTool.Editor`。

#### 2.5 为规则配置相关的生效内容

如果要完整沿用 Package 默认规则，只需让项目设置中的“当前生效规则应用配置”保持为空。需要调整规则范围、启用状态或白名单时，可以在“规则应用配置”页面找到标记为“包内”的默认配置，点击“复制到本地”，将副本保存到项目 `Assets/...`，并把副本的规则应用配置 ID 改为新的唯一值。也可以执行以下步骤创建一份全新的本地配置。

1. 打开“规则应用配置”页面，点击“新建规则应用配置”。
2. 填写唯一的规则应用配置 ID 和显示名称，选择项目 `Assets/...` 下的保存目录并创建配置。
3. 在新配置中点击“+ 添加规则应用项”。
4. 开启“是否启用”。
5. “规则配置”选择 2.4 新增的规则。
6. “资源类型配置”选择 2.3 新增的资源类型，或者选择对应的内置资源类型。
7. “检测配置档”选择 2.2 新增的平台配置档，然后点击“添加”。
8. 返回“项目设置”，将“当前生效规则应用配置”设置为刚刚创建或复制到本地的配置。该选择列表只使用本地规则应用配置；Package 默认配置通过字段留空时的回退逻辑生效。

规则真正生效必须同时满足：规则应用配置是项目当前生效配置、规则应用项已启用、规则具有可用的检测脚本、资源被所选资源类型识别，并且扫描时选择的检测配置档属于该应用项。规则、资源类型或检测配置档留空时，该应用项不会匹配完整的扫描条件。

#### 2.6 白名单

白名单保存在具体的规则应用配置中，并应用于使用该配置生成的报告。Package 内置规则应用配置只读；需要为默认规则增加白名单时，必须先按 2.5 将它复制到项目 `Assets/...`，设置新的唯一 ID，并将本地副本设为当前生效配置。

1. 在“规则应用配置”页面展开需要编辑的本地配置。
2. 在“白名单”区域点击“+ 添加白名单”。
3. 选择单个资源或文件夹，也可以手动填写当前项目中的 `Assets/...` 路径。文件夹会递归匹配其下资源。
4. 根据需要选择“规则配置”并填写“问题 ID”，也可以填写备注说明加入白名单的原因。
5. 点击“添加”。

白名单的匹配范围如下：

- “规则配置”为空：忽略该资源路径匹配到的全部规则。
- 已选择规则但“问题 ID”为空：忽略该规则下的全部问题。
- 同时选择规则并填写“问题 ID”：只忽略该规则下的指定问题。

#### 2.7 保存与生效检查

1. 点击配置中心顶部的“保存全部”。
2. 如果配置中心显示校验错误，按提示修正重复 ID、无效引用、缺少 Checker 或不完整的规则应用项，然后再次保存。
3. 新建检测脚本后等待 Unity 完成编译，并确认规则详情显示检测脚本“已创建”。
4. 确认项目设置指向正确的当前生效规则应用配置。
5. 打开资源评分窗口，选择规则应用项关联的检测配置档，扫描能够被目标资源类型识别的资源。
6. 在结果树和详情面板中确认新规则的问题；如果配置了白名单，再确认命中的问题已从最终报告中移除。

“保存全部”只会写入项目 `Assets/...` 下的本地配置和待生成脚本，不会修改 Package 内置配置。

## 5. 注意事项

- 仅支持 Unity Editor，不包含 Runtime 程序集，也不会向构建后的 Player 注入代码。
- Unity 基线为 `2022.3.0f1`；应优先使用 Unity 2022.3 LTS 或兼容版本。
- 扫描路径必须是当前项目中的 `Assets/...` 路径。通过系统文件选择器取得的路径也必须能够转换到当前项目的 `Assets` 目录。
- 首次打开扫描窗口或配置中心时，工具会在 `Assets/AssetScoreTool/ProjectSettings/AssetScoreProjectSettings.asset` 创建项目设置资产。项目中只能保留一个可用的 `UserSettingConfig`。
- Package 内置配置只读，可以直接引用但不能编辑或删除。需要调整规则应用范围或白名单时，应先“复制到本地”、设置新的唯一 ID，再编辑项目 `Assets/...` 下的副本；“保存全部”不会改写 Package。
- Excel 导出生成 `.xlsx` 文件，不依赖本机安装 Microsoft Excel；工具不依赖 Python。
- Spine 检测是可选功能。启用前必须同时提供 `spine-unity` 与 `spine-csharp`，并在项目设置中开启 Spine 检测。
- 新增 Unity 可导入文件时不要手写 `.meta`；切回 Unity 后由 Unity 生成和维护元数据。

## 6. 能力概览

- 扫描单个资源、文件夹或 Project 面板当前选择。
- 识别动画、材质、FBX 网格、粒子、Prefab、Shader、纹理等内置资源类型。
- 按 PC、iOS、Android、Web 等 Profile 调度规则。
- 通过规则应用配置组合规则、资源类型、Profile 和白名单。
- 对资源自身及其依赖的问题进行评分、白名单过滤和汇总。
- 在树形结果中按资源类型、严重度、分数、问题数或路径筛选和排序。
- 展示资源自身问题、汇总分数、等级、直接依赖和依赖问题数。
- 在 Prefab 问题详情中定位缺失脚本、缺失引用、Renderer、Animator、Canvas、物理组件和动画绑定缺失对应的内部对象。
- 将最终报告导出为 JSON 或 Excel。Excel 文件包含“扫描摘要”“资源总览”“问题明细”“依赖关系”四个工作表。
- 通过配置中心创建、复制、编辑、删除和保存本地配置。
- 可选启用 Spine Atlas、Skeleton 与 Prefab 的资源类型识别和评分规则。

## 7. 架构与数据流（扩展参考）

以下内容用于帮助使用方在项目 `Assets/...` 下开发自定义 Checker 或排查配置问题。这里列出的类型和目录属于 Package 内部实现，不应直接修改。

### 扫描与报告

```text
Profile + Assets 路径
        │
        ▼
AssetScoreToolScanner
        │  资源发现、依赖构建、规则执行
        ▼
AssetScanData
        │  同一次扫描使用的 RuleApplyConfig
        ▼
AssetScoreReportBuilder
        │  白名单、评分、等级和依赖汇总
        ▼
AssetScoreReport
        ├── ScanResultListView
        ├── ScanDetailPanelView
        └── AssetScoreReportExporter（JSON / Excel）
```

各层职责如下：

| 区域 | 主要类型 | 职责 |
| --- | --- | --- |
| 扫描 | `AssetScoreToolScanner`、`AssetScanData` | 发现资源和依赖、调度 Checker、保存原始扫描事实 |
| 评分与报告 | `AssetScoreReportBuilder`、`AssetScoreReport`、`AssetScanResult` | 应用白名单，计算自身与依赖汇总结果，并提供筛选和排序查询 |
| 扫描窗口协调 | `ScanWindow` | 管理窗口生命周期、扫描启动/取消、顶部输入、导出和状态栏 |
| 结果列表 | `ScanResultListView` | 绑定筛选控件，创建结果树，维护展开和行选中状态 |
| 详情面板 | `ScanDetailPanelView` | 展示详情、问题和依赖，并处理资源选择与 Prefab 问题定位 |
| 报告导出 | `AssetScoreReportExporter` | 将同一份最终报告导出为 JSON 或 Excel |

Scanner 只生成 `AssetScanData`，不负责 UI、等级文案或树形布局。UI 只读取 `AssetScoreReport`，不重复计算评分、白名单或依赖汇总。报告构建必须使用 `scanner.GetRuleApplyConfig()` 返回的同一份扫描配置。

`StartScanAsync` 通过 `await Task.Yield()` 在 Unity 主线程分帧推进。Checker 和扫描流程不得通过 `Task.Run` 调用 Unity Editor API。

### 配置编辑

`ConfigWindow` 是配置中心的窗口协调器，主要组件包括：

| 组件 | 职责 |
| --- | --- |
| `ConfigWindowData` | 加载配置、创建工作副本、保存、重置、复制、删除和清理引用 |
| `ConfigWindowPageHost` | 注册、切换和刷新配置页面 |
| `ConfigWindowDialogHost` | 统一挂载和显示模态面板 |
| `ConfigWindowValidationPresenter` | 汇总验证结果，刷新消息和字段样式，并处理问题定位 |
| `ConfigWindowToolbarView` | 绑定保存、放弃、刷新和打开目录等工具栏操作 |
| `ConfigPage<TConfig>` | 各配置页面共享的选择、刷新、验证和编辑流程 |

只有项目 `Assets/...` 下的本地配置会建立可编辑工作副本。编辑期间真实资产不会直接被修改，保存时再通过 Undo、序列化复制、Dirty 标记和 `AssetDatabase.SaveAssets` 写回本地资产；Package 配置只提供预览和引用，不参与写回。

### 程序集边界

- `AssetScoreTool.Editor` 是主 Editor 程序集，不依赖 Spine 类型。
- `AssetScoreTool.Spine.Editor` 是可选扩展程序集，依赖主程序集、`spine-unity` 和 `spine-csharp`。
- Spine 扩展只有在定义 `ASSET_SCORE_TOOL_SPINE` 时参与编译。
- `AssetScoreTool.Spine.Editor.Tests` 是受同一符号限制的 EditMode 测试程序集。

## 8. 配置与扩展

配置中心包含以下页面：

- 检测配置档（`ScoreRuleProfileConfig`）
- 规则应用配置（`RuleApplyConfig`）
- 检测规则配置（`ScoreRuleConfig`）
- 资源类型配置（`AssetTypeConfig`）
- 项目设置（`UserSettingConfig`）

`RuleApplyConfigResolver.GetActiveConfig` 是生效规则应用配置的统一入口：优先使用项目设置指定的配置，未指定时回退为默认 `RuleApplyConfig_Default`。同一份生效配置中，同一 `RuleId` 只能存在一项启用的规则应用。

项目设置中的“报告导出默认保存目录”支持任意本机绝对路径。路径未设置、失效或不存在时，保存对话框默认打开项目根目录下与 `Assets` 同级的 `docs/`。

Package 内置的只读配置资产位于 `Configs/`。使用方可以直接引用这些配置，但不应在 Package 中修改；项目设置和自定义配置应创建在项目 `Assets/...` 下：

```text
Configs/
├── AssetTypes/       资源类型配置
├── Profiles/         PC、iOS、Android、Web 等 Profile
├── ProjectSettings/  Package 内部资产，不作为使用方项目设置
├── RuleApply/        生效规则、资源类型、Profile 与白名单的组合
└── Rules/            评分规则与问题项配置
```

### 新增评分规则

1. 通过配置中心在项目 `Assets/...` 下创建 `ScoreRuleConfig` 资产。
2. 在项目自己的 Editor 目录中实现可被 `TypeCache` 发现、可由无参构造函数实例化的 `IScoreRuleChecker`。
3. 保证 Checker 的 `RuleId` 与配置资产中的规则 ID 完全一致。
4. 在本地 `RuleApplyConfig` 中为目标资源类型和 Profile 启用该规则，并在项目设置中选择这份本地配置。

建议把自定义规则脚本放在 `Assets/AssetScoreTool/Extensions/Editor/Rules/` 或项目已有的 Editor 扩展目录，不要放入 Package 的 `Editor/Checkers/Rules/`。项目使用自定义 Editor `.asmdef` 时，需要引用 `AssetScoreTool.Editor`。依赖 Spine API 的自定义规则还需要由项目自己的 Editor 程序集引用对应 Spine 程序集，并保持依赖可用。

### 新增资源类型

1. 通过配置中心在项目 `Assets/...` 下创建 `AssetTypeConfig` 资产。
2. 在项目自己的 Editor 目录中实现可被 `TypeCache` 发现、可由无参构造函数实例化的 `IAssetTypeChecker`。
3. 保证 Checker 的 `AssetTypeId` 与配置资产中的资源类型 ID 完全一致。

建议把自定义资源类型脚本放在 `Assets/AssetScoreTool/Extensions/Editor/AssetTypes/` 或项目已有的 Editor 扩展目录，不要修改 Package 的 `Editor/Checkers/AssetTypes/`。资源类型检测只判断资源是否匹配，不执行评分或构建展示数据。

### 可选 Spine 检测

安装 `spine-unity` 和 `spine-csharp` 后，在配置中心的项目设置中启用 Spine 检测。工具会确认程序集可用，并将 `ASSET_SCORE_TOOL_SPINE` 同步到有效的 Build Target Group。依赖缺失或符号未启用时，Spine 扩展不会参与编译和规则发现。

## 9. Package 内部结构（只读）

以下目录由 Package 提供，用于说明工具组成，不是使用方创建或修改内容的目标位置：

```text
Packages/com.catfood.unitytools.assetscoretool/
├── Editor/
│   ├── Checkers/                   内置资源类型与评分规则 Checker
│   ├── Config/                     配置模型、生效配置解析与 Spine 设置
│   ├── Core/
│   │   ├── Reporting/              报告构建、查询和导出
│   │   ├── Rules/                  问题模型与检测契约
│   │   ├── Scanning/               扫描、依赖发现、调度和路径工具
│   │   └── Scoring/                评分、等级与扫描结果
│   ├── Spine/                      可选 Spine 扩展及其 EditMode 测试
│   └── UI/
│       ├── Common/                 公共 UI Toolkit 控件、UXML 和 USS
│       ├── ConfigWindow/           配置中心、页面、弹窗、验证和工作副本
│       └── ScanWindow/
│           ├── ScanWindow.cs       生命周期、扫描、输入、导出和状态栏
│           ├── ScanResultListView.cs
│           └── ScanDetailPanelView.cs
├── Configs/                        随包配置资产
├── docs/                           规则资料、实施文档与界面参考稿
├── package.json
└── README.md
```

## 10. 验证

- 新建配置后，确认资产位于项目 `Assets/...` 下，而不是 Package 目录。
- 新建资源类型或规则检测脚本后，确认 Unity Console 没有相关编译错误；使用自定义 `.asmdef` 时，确认它已引用 `AssetScoreTool.Editor`。
- 在项目设置中选择本地规则应用配置，或保持为空以使用 Package 默认配置，然后使用匹配的检测配置档扫描目标资源。
- 确认新规则能够命中预期问题，并检查取消、空结果和白名单过滤等场景。
- 启用 Spine 检测时，确认项目已经同时安装 `spine-unity` 与 `spine-csharp`，且 Unity Console 没有 Spine 扩展相关编译错误。

## 11. 资产评分规则配置表

[资产评分规则配置表](docs/资产评分规则配置表.md)汇总了当前 Package 版本内置的 81 条资产评分规则，包括规则配置资产名、规则 ID、检测内容、问题信息、扣分、等级、适用资源类型、检测配置档和启用状态。

表内规则均具有对应的评分规则配置资产和 Checker 实现，并已在 Package 默认规则应用配置中启用。内置配置只读，可以直接用于扫描，也可以被本地规则应用配置引用；需要调整应用范围或白名单时，应先把默认规则应用配置复制到项目 `Assets/...`，设置新的唯一 ID，再将本地副本设为当前生效配置。Spine 规则需要同时安装 `spine-unity` 与 `spine-csharp`，并在项目设置中启用 Spine 检测后才会参与扫描。
