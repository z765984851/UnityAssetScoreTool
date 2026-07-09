# UI 组合式重构实施计划

**目标：** 按窗口拆成两个可独立编译、验证和交付的最小生效单位，移除窗口中的展示构建职责，同时保持现有 UXML、USS、文案和业务行为。

**技术边界：** Unity 2022.3 Editor、C#、UI Toolkit；沿用现有 `ConfigWindowData`、`ConfigWindowValidation`、`AssetScoreToolScanner` 和 `AssetScoreReport` 数据流。

## 全局约束

- 先完成并验收 ConfigWindow，再进入 ScanWindow。
- 不修改扫描、评分、白名单、序列化字段和配置保存语义。
- 不修改 UXML 元素名称、USS class、菜单路径和用户可见文案。
- 不新增测试程序集、测试资源或临时验证代码。
- 不执行 Git 操作；新增 Unity 可导入文件不手动创建 `.meta`。

## 第一步：ConfigWindow 重构

**最小生效单位：** 完成配置窗口的验证展示、页面刷新和弹窗宿主迁移，并删除窗口中的旧 UI 协调实现。此步骤不修改 ScanWindow。

**新增文件：**

- `Editor/UI/ConfigWindow/ConfigWindowValidationPresenter.cs`

**主要修改文件：**

- `Editor/UI/ConfigWindow/ConfigWindow.cs`
- `Editor/UI/ConfigWindow/ConfigWindowPageHost.cs`
- `Editor/UI/ConfigWindow/ConfigWindowDialogHost.cs`
- `Editor/UI/ConfigWindow/Pages/RuleConfigPage.cs`
- `Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.cs`
- `Editor/UI/ConfigWindow/Dialogs/*.cs`

### 1.1 验证 Presenter

- [x] 创建各页面的 `ValidationMessageView`。
- [x] 保存统一诊断集合和页面局部索引映射。
- [x] 刷新页面诊断消息以及字段错误/警告样式。
- [x] 将“定位”索引转换为具体 `ConfigValidationIssue` 回调。
- [x] 负责字段定位名称、滚动和焦点设置。
- [x] 删除窗口中的验证视图字典、页面错误索引和字段展示辅助方法。

### 1.2 页面宿主

- [x] 统一页面注册、类型查找、切换和当前页面状态。
- [x] 将全部页面遍历刷新迁移到 `ConfigWindowPageHost.RefreshAll`。
- [x] 保持“验证 → 页面重建 → 诊断样式刷新”的现有顺序。
- [x] 删除窗口中的 `pages`、`currentPage` 和 `FreshAllPages`。

### 1.3 弹窗宿主

- [x] 页面和窗口统一传递 `ConfigWindowDialogHost`，不再传递裸挂载节点。
- [x] `ConfigWindowDialogHost` 负责 ModalPanel 的挂载和显示。
- [x] 各弹窗继续负责实例化并绑定自己的 UXML 内容，关闭与清理由 `ModalPanel` 及现有回调负责。
- [x] 删除未接入现有弹窗流程的 overlay 式实例化和显隐方法。

### 1.4 清理和验收

- [x] 清理迁移后无用的字段、方法和 using。
- [x] 静态检查页面、工具栏、弹窗、验证展示、保存校验和错误定位调用链。
- [x] 检查全部 UXML 查询名称和回调签名。
- [x] 在 Unity 中确认 `AssetScoreTool.Editor` 无编译错误。

## 第二步：ScanWindow 两个轻量 View 重构

**最小生效单位：** 将结果列表和详情面板迁移到两个直接绑定现有 UXML 的轻量 View。窗口保留生命周期、扫描启动/取消、顶部输入与导出、报告构建和状态栏协调。本步骤不引入 Service、Presenter、接口、额外状态层或 `partial class ScanWindow`。

**新增文件：**

- `Editor/UI/ScanWindow/ScanResultListView.cs`
- `Editor/UI/ScanWindow/ScanDetailPanelView.cs`

**修改文件：**

- `Editor/UI/ScanWindow/ScanWindow.cs`
- `docs/superpowers/plans/2026-07-22-ui-composition-refactor.md`

**保持不变：**

- 不修改 `ScanWindow.uxml`、`ScanWindow.uss`、扫描器、报告模型、导出逻辑和用户可见文案。
- 不新增测试程序集、测试脚本、测试资源或临时验证代码。
- 不执行 Git 操作；新增 Unity 可导入脚本不手动创建 `.meta`。

### 2.1 ScanResultListView

**接口：**

- 构造函数接收窗口根节点和 `Action<AssetContext>` 选择回调。
- `SetReport(AssetScoreReport report)` 替换当前报告并刷新结果树。
- `ShowScanning()` 显示扫描中状态并禁用筛选控件。
- `Clear()` 清空报告、选择和结果树。

**职责：**

- [x] 管理类型、严重度和排序控件绑定。
- [x] 使用 `AssetScoreReport.GetDisplayRootResults(...)` 补齐当前缺失的结果筛选和排序实现。
- [x] 创建依赖树、处理循环保护、展开按钮和选中行高亮。
- [x] 当前选择被筛选掉时选择第一棵可见树的根资源，并通过回调通知详情面板。
- [x] 只读取 `AssetScoreReport`，不访问 scanner，不重复计算评分、等级、白名单或报告数据。

### 2.2 ScanDetailPanelView

**接口：**

- 构造函数接收窗口根节点、资源选择回调、打开目录回调和 Prefab 问题定位回调。
- `SetReport(AssetScoreReport report, string profileText)` 替换详情使用的报告与 Profile 展示文本。
- `ShowAsset(AssetContext context)` 展示指定资源。
- `Clear()` 清空当前资源和详情内容。

**职责：**

- [x] 展示资源字段、评分、等级、自身/汇总问题数、问题列表和直接依赖列表。
- [x] 使用现有 `scan-dependency-*` USS 类补齐当前缺失的依赖项创建实现，显示依赖路径和汇总问题数。
- [x] 构建 Prefab 问题定位项，并将资源选择、打开目录和 Prefab 定位操作转交给构造函数回调。
- [x] 只读取 `AssetScoreReport`，不访问 scanner 内部数据。

### 2.3 ScanWindow 接线与清理

- [x] 在 `CreateGUI` 的绑定阶段创建两个 View，并将结果选择回调连接到详情刷新。
- [x] 扫描开始时清空详情并让结果 View 显示扫描中状态。
- [x] 扫描完成时把同一份 `AssetScoreReport` 交给两个 View，并刷新结果树。
- [x] 取消或失败时清空两个 View，保持现有导出、状态栏和错误反馈行为。
- [x] 将 Project 资源选择、打开目录和 Prefab Stage 定位作为详情 View 的直接回调，不新增中间服务。
- [x] 删除窗口中已迁移的 UI 字段、常量和展示辅助方法，清理仅由迁移产生的无用 `using`。
- [x] 保持 `ScanWindowProjectMenu` 只获取选择路径、打开窗口并调用 `SetScanPath`。

### 2.4 验收

- [x] 静态检查所有 UXML 查询名称、回调签名、报告 API、命名空间、重复声明和迁移后遗留引用。
- [x] 确认 `GetSortedScanResults` 与 `CreateDependencyElement` 不再是未定义调用。
- [x] 使用 Unity 生成的 `AssetScoreTool.Editor.csproj` 编译主程序集，结果为 0 警告、0 错误。
- [ ] 触发 Unity 重新编译，并确认 `AssetScoreTool.Editor` 无相关编译错误。
- [x] 不运行 EditMode、PlayMode、单元、集成或回归测试。
