# 配置选择面板多选 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让规则应用项中的资源类型配置和检测配置档在现有居中选择面板内一次勾选多项，同时保持所有单选入口行为不变。

**Architecture:** 在 `ConfigSelectionPanel` 内增加与单选状态隔离的多选模式，由 `ConfigWindow` 统一加载候选项并暴露多选入口。已有规则应用项确认后整体替换工作副本数组；新建规则应用项确认后只替换创建面板的临时集合，最终仍通过现有 `ConfigWindowData` 流程提交。

**Tech Stack:** Unity 2022.3.0f1、C#、Unity Editor、UI Toolkit、`SerializedObject` / `SerializedProperty`。

## Global Constraints

- 仅使用 Unity 2022.3.0f1 支持的 API，不提高 C# 或 Unity 版本要求。
- 只修改 Editor 程序集，不新增或恢复 Runtime 程序集。
- 不修改 `RuleApplyConfig`、`RuleApplyItem` 的序列化字段和公共数据语义。
- 本仓库没有自动化测试工程；未经用户要求，不新增测试程序集、测试脚本或临时验证代码。
- 每个手写具名方法和构造函数必须有说明职责的 `/// <summary>`。
- 不手动生成 `.meta` 文件。
- 未经用户明确要求，不执行任何 Git 命令；本计划不包含暂存或提交步骤。
- 每个任务完成后都必须检查 Unity Console；只有 `AssetScoreTool.Editor` 无编译错误时才能进入下一任务。
- 设计依据：`docs/superpowers/specs/2026-07-23-config-multi-select-design.md`。
- 用户后续决定：不修改参考 HTML；Task 4 的 HTML 修改步骤不再执行，以静态调用链核对为准。

---

## 文件职责与修改边界

- `Editor/UI/ConfigWindow/Dialogs/ConfigSelectionPanel.cs`：维护单次弹窗的候选项、搜索、单选或多选临时状态，并在确认时返回结果。
- `Editor/UI/ConfigWindow/ConfigWindow.cs`：从 `ConfigWindowData` 加载候选配置，向页面和弹窗提供统一的单选、多选入口。
- `Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.cs`：展示已有规则应用项的引用标签，并将多选确认结果整体写入工作副本数组。
- `Editor/UI/ConfigWindow/Dialogs/CreateRuleApplyItemPanel.cs`：维护新建规则应用项期间的资源类型和检测配置档临时集合。
- `docs/配置界面.html`：仅作为视觉与交互参考稿，本次不修改。

### Task 1: 为公共配置选择面板增加隔离的多选模式

**Files:**

- Modify: `Editor/UI/ConfigWindow/Dialogs/ConfigSelectionPanel.cs:27-325`
- Modify: `Editor/UI/ConfigWindow/ConfigWindow.cs:1002-1068`

**Interfaces:**

- Consumes: 现有 `ConfigSelectionOption`、`ConfigWindowDialogHost`、`BuildSelectionOptions(Type)`。
- Produces: `ConfigSelectionPanel.OpenMultiple(...)` 和 `ConfigWindow.ShowMultiSelectionDialog(...)`。
- 多选回调统一使用 `Action<IReadOnlyList<ScriptableObject>>`。

- [ ] **Step 1: 在 `ConfigSelectionPanel` 中加入独立的多选状态**

保留现有 `selectedValue`、`allowNone`、`noneText` 和 `confirmAction`，新增以下字段；更新类注释为“配置单选与多选 Panel”。

```csharp
private readonly HashSet<ScriptableObject> selectedValues =
    new HashSet<ScriptableObject>();

private bool isMultiSelect;
private Action<IReadOnlyList<ScriptableObject>> multiConfirmAction;
```

多选集合只保存当前弹窗的临时状态，不得写入配置工作副本。

- [ ] **Step 2: 将确认按钮处理分成单选和多选两条明确路径**

用以下逻辑替换构造函数内现有的 `confirmButton.clicked` 处理。必须先捕获回调和结果，再调用 `Close()`，因为关闭事件负责清理字段。

```csharp
confirmButton.clicked +=
    delegate
    {
        if (isMultiSelect)
        {
            List<ScriptableObject> values =
                BuildOrderedSelectedValues();
            Action<IReadOnlyList<ScriptableObject>> action =
                multiConfirmAction;

            Close();
            action?.Invoke(values);
            return;
        }

        Confirmed?.Invoke(selectedValue);
        Action<ScriptableObject> singleAction = confirmAction;
        ScriptableObject value = selectedValue;

        Close();
        singleAction?.Invoke(value);
    };
```

增加下列方法。它只从 `options` 输出已选引用，因此自动忽略不在候选项中的初始引用，并保持候选项排序。

```csharp
/// <summary>
/// 按当前候选项顺序返回多选结果，避免结果顺序受点击和搜索顺序影响。
/// </summary>
private List<ScriptableObject> BuildOrderedSelectedValues()
{
    List<ScriptableObject> values =
        new List<ScriptableObject>();

    for (int i = 0; i < options.Count; i++)
    {
        ScriptableObject value = options[i].Value;

        if (value != null && selectedValues.Contains(value))
        {
            values.Add(value);
        }
    }

    return values;
}
```

- [ ] **Step 3: 增加多选打开入口并确保每次打开都重置状态**

保留两个现有单选 `Open` 重载。单选 `SetData` 开头必须设置 `isMultiSelect = false`，清空 `selectedValues` 和 `multiConfirmAction`。

增加仅供配置窗口宿主使用的入口：

```csharp
/// <summary>
/// 通过配置窗口弹窗宿主打开多选 Panel，并保存本次确认操作。
/// </summary>
internal void OpenMultiple(
    ConfigWindowDialogHost host,
    string title,
    IReadOnlyList<ConfigSelectionOption> selectionOptions,
    IReadOnlyList<ScriptableObject> currentValues,
    Action<IReadOnlyList<ScriptableObject>> onConfirmed)
{
    isMultiSelect = true;
    confirmAction = null;
    multiConfirmAction = onConfirmed;
    selectedValue = null;
    allowNone = false;
    noneText = string.Empty;

    options.Clear();

    if (selectionOptions != null)
    {
        for (int i = 0; i < selectionOptions.Count; i++)
        {
            options.Add(selectionOptions[i]);
        }
    }

    selectedValues.Clear();

    if (currentValues != null)
    {
        for (int i = 0; i < currentValues.Count; i++)
        {
            ScriptableObject value = currentValues[i];

            if (value != null)
            {
                selectedValues.Add(value);
            }
        }
    }

    Title = title;
    searchField.SetValueWithoutNotify(string.Empty);
    RefreshList();
    host.Show(this);
}
```

在 `Closed` 回调中同时清理 `confirmAction`、`multiConfirmAction`、`selectedValues`，避免下次打开继承状态。不要清理 `options`，因为下次 `SetData` 或 `OpenMultiple` 会统一覆盖。

- [ ] **Step 4: 让列表行根据模式切换选择状态**

增加以下方法，并让 `BuildRow` 使用它决定 `selection-row-selected` 与“已选择”文本：

```csharp
/// <summary>
/// 判断给定配置是否是当前单选值或多选集合成员。
/// </summary>
private bool IsSelected(ScriptableObject value)
{
    return isMultiSelect
        ? value != null && selectedValues.Contains(value)
        : value == selectedValue;
}
```

将行点击逻辑改为：

```csharp
row.clicked +=
    delegate
    {
        // 只更新 Panel 内的临时选择；确认后才通过事件交给调用方处理。
        if (isMultiSelect)
        {
            if (!selectedValues.Add(value))
            {
                selectedValues.Remove(value);
            }
        }
        else
        {
            selectedValue = value;
        }

        RefreshList();
    };
```

`RefreshList` 中空值选项只在单选模式下显示：`if (!isMultiSelect && allowNone && ...)`。确认按钮启用条件改为：

```csharp
UIElementUtility.SetEnabled(
    confirmButton,
    isMultiSelect || allowNone || selectedValue != null);
```

- [ ] **Step 5: 在 `ConfigWindow` 暴露统一多选入口**

紧邻 `ShowSelectionDialog` 增加：

```csharp
/// <summary>
/// 打开配置多选面板。窗口负责准备候选数据，Panel 负责临时选择和确认。
/// </summary>
internal void ShowMultiSelectionDialog(
    string title,
    Type configType,
    IReadOnlyList<ScriptableObject> currentValues,
    Action<IReadOnlyList<ScriptableObject>> confirmAction)
{
    configSelectionPanel.OpenMultiple(
        dialogHost,
        title,
        BuildSelectionOptions(configType),
        currentValues,
        confirmAction);
}
```

不要修改 `BuildSelectionOptions`：它已经按显示名稳定排序，并返回真实配置资产引用。

- [ ] **Step 6: 完成静态和 Unity 编译检查**

运行：

```powershell
rg -n "OpenMultiple|ShowMultiSelectionDialog|isMultiSelect|selectedValues|multiConfirmAction" Editor/UI/ConfigWindow
rg -n "ShowSelectionDialog\(" Editor/UI/ConfigWindow
```

预期：新接口只在新增定义中出现；现有五个 `ShowSelectionDialog` 单选调用方签名不变。

切回 Unity，等待脚本重新编译。预期：Unity Console 中 `AssetScoreTool.Editor` 无编译错误。若当前环境无法读取 Unity Console，停止在此检查点并明确记录“尚未实际验证 Unity 编译”，不得宣称任务完成。

### Task 2: 已有规则应用项一次选择并整体替换引用数组

**Files:**

- Modify: `Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.cs:743-945`

**Interfaces:**

- Consumes: Task 1 的 `ConfigWindow.ShowMultiSelectionDialog(string, Type, IReadOnlyList<ScriptableObject>, Action<IReadOnlyList<ScriptableObject>>)`。
- Produces: 已有规则应用项中资源类型配置和检测配置档的完整多选编辑行为。

- [ ] **Step 1: 在构建引用标签时收集当前有效数组值**

在 `BuildObjectReferenceArray` 找到并验证 `arrayProperty` 后，创建 `currentValues`。在原有绘制 chip 的循环中，仅将非空引用加入列表；chip 和单项移除按钮保持现状。

```csharp
List<ScriptableObject> currentValues =
    new List<ScriptableObject>();

for (int i = 0; i < arrayProperty.arraySize; i++)
{
    int elementIndex = i;
    SerializedProperty element =
        arrayProperty.GetArrayElementAtIndex(i);
    ScriptableObject value =
        element.objectReferenceValue as ScriptableObject;

    if (value != null)
    {
        currentValues.Add(value);
    }

    VisualElement chip =
        BuildReferenceChip(
            value,
            elementType == typeof(AssetTypeConfig)
                ? "config-reference-chip-asset-type"
                : "config-reference-chip-profile");

    if (editable)
    {
        Button removeButton =
            new Button(
                delegate
                {
                    host.DeleteArrayElement(
                        owner,
                        arrayPath,
                        elementIndex,
                        "移除配置引用",
                        refreshAction);
                });

        removeButton.text = "×";
        removeButton.AddToClassList(
            "config-reference-chip-remove");
        chip.Add(removeButton);
    }

    list.Add(chip);
}
```

- [ ] **Step 2: 将“添加”按钮改为打开完整多选面板**

将按钮回调中的 `ShowSelectionDialog` 替换为：

```csharp
host.ShowMultiSelectionDialog(
    "选择" + displayName,
    elementType,
    currentValues,
    delegate(IReadOnlyList<ScriptableObject> selected)
    {
        ReplaceArrayObjectReferences(
            owner,
            arrayPath,
            selected,
            refreshAction);
    });
```

按钮文案改为“选择”，因为它现在既能增加也能取消已有引用。现有 chip 的“×”快捷移除继续保留。

- [ ] **Step 3: 将单项追加实现替换为数组整体替换**

把仅由此按钮使用的 `AddArrayObjectReference` 改名并替换为以下完整职责；不要保留旧追加逻辑或兼容包装。

```csharp
/// <summary>
/// 在一次 Undo 记录中整体替换配置引用数组，并通知配置窗口刷新工作副本状态。
/// </summary>
private void ReplaceArrayObjectReferences(
    ScriptableObject owner,
    string arrayPath,
    IReadOnlyList<ScriptableObject> selected,
    Action refreshAction)
{
    SerializedObject serializedObject =
        new SerializedObject(owner);
    SerializedProperty arrayProperty =
        serializedObject.FindProperty(arrayPath);

    if (arrayProperty == null || !arrayProperty.isArray)
    {
        return;
    }

    Undo.RecordObject(owner, "修改配置引用");
    arrayProperty.arraySize = selected == null ? 0 : selected.Count;

    for (int i = 0; i < arrayProperty.arraySize; i++)
    {
        arrayProperty
            .GetArrayElementAtIndex(i)
            .objectReferenceValue = selected[i];
    }

    serializedObject.ApplyModifiedProperties();
    host.NotifyWorkingCopyChanged(owner);
    refreshAction?.Invoke();
}
```

多选面板已经按候选项顺序去重，因此此方法只负责忠实写入确认结果。

- [ ] **Step 4: 完成静态和 Unity 编译检查**

运行：

```powershell
rg -n "AddArrayObjectReference|ReplaceArrayObjectReferences|ShowMultiSelectionDialog" Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.cs
rg -n "BuildObjectReferenceArray\(" Editor/UI/ConfigWindow/Pages/RuleApplyConfigPage.cs
```

预期：不存在 `AddArrayObjectReference`；`ReplaceArrayObjectReferences` 只有定义和多选确认回调一个调用位置；资源类型与检测配置档仍通过同一个 `BuildObjectReferenceArray` 入口复用该行为。

切回 Unity 并确认 `AssetScoreTool.Editor` 无编译错误。随后在已有规则应用项中分别检查资源类型和检测配置档：打开时已有项被选中、一次确认可增加或取消多项、确认空集合可清空、取消不改变原数组。

### Task 3: 新建规则应用项使用同一多选交互

**Files:**

- Modify: `Editor/UI/ConfigWindow/Dialogs/CreateRuleApplyItemPanel.cs:14-238`
- Modify: `Editor/UI/ConfigWindow/ConfigWindow.cs:722-734`

**Interfaces:**

- Consumes: Task 1 的 `ConfigWindow.ShowMultiSelectionDialog(...)`。
- Produces: `CreateRuleApplyItemPanel` 构造参数 `requestMultiSelection`，其签名为 `Action<string, Type, IReadOnlyList<ScriptableObject>, Action<IReadOnlyList<ScriptableObject>>>`。

- [ ] **Step 1: 为创建面板注入单选和多选两个明确入口**

将现有 `selectionRequester` 改名为 `singleSelectionRequester`，保持其签名不变。新增：

```csharp
private readonly Action<
    string,
    Type,
    IReadOnlyList<ScriptableObject>,
    Action<IReadOnlyList<ScriptableObject>>> multiSelectionRequester;
```

构造函数在 `requestSelection` 后增加同签名的 `requestMultiSelection` 参数，并保存两个委托。构造函数继续保留完整职责注释；无需增加新的包装类或接口。

- [ ] **Step 2: 将资源类型按钮改为一次多选并整体更新临时集合**

用下列逻辑替换 `addAssetTypeButton.clicked`。不要调用原 `AddAssetType`，避免逐项刷新。

```csharp
addAssetTypeButton.clicked +=
    delegate
    {
        AssetTypeSelectionRequested?.Invoke();
        multiSelectionRequester(
            "选择资源类型配置",
            typeof(AssetTypeConfig),
            assetTypes,
            delegate(IReadOnlyList<ScriptableObject> selected)
            {
                assetTypes.Clear();

                for (int i = 0; i < selected.Count; i++)
                {
                    AssetTypeConfig value = selected[i] as AssetTypeConfig;

                    if (value != null)
                    {
                        assetTypes.Add(value);
                    }
                }

                RefreshAssetTypes();
            });
    };
```

- [ ] **Step 3: 将检测配置档按钮改为一次多选并整体更新临时集合**

用下列逻辑替换 `addProfileButton.clicked`：

```csharp
addProfileButton.clicked +=
    delegate
    {
        ProfileSelectionRequested?.Invoke();
        multiSelectionRequester(
            "选择检测配置档",
            typeof(ScoreRuleProfileConfig),
            profiles,
            delegate(IReadOnlyList<ScriptableObject> selected)
            {
                profiles.Clear();

                for (int i = 0; i < selected.Count; i++)
                {
                    ScoreRuleProfileConfig value =
                        selected[i] as ScoreRuleProfileConfig;

                    if (value != null)
                    {
                        profiles.Add(value);
                    }
                }

                RefreshProfiles();
            });
    };
```

删除只为旧逐项选择服务、且不再有调用方的 `AddAssetType` 和 `AddProfile` 方法，不保留过渡接口。

- [ ] **Step 4: 保持规则引用走单选，并更新构造位置**

`RefreshRule` 中的选择调用只将名称改为 `singleSelectionRequester(...)`，其参数和行为不变。

在 `ConfigWindow.BindCreateRuleApplyItemDialogUI` 构造调用末尾依次传入：

```csharp
ShowSelectionDialog,
ShowMultiSelectionDialog
```

这保证规则配置仍是单选，而资源类型和检测配置档使用多选。

- [ ] **Step 5: 完成静态和 Unity 编译检查**

运行：

```powershell
rg -n "selectionRequester|singleSelectionRequester|multiSelectionRequester|AddAssetType|AddProfile" Editor/UI/ConfigWindow/Dialogs/CreateRuleApplyItemPanel.cs
rg -n "new CreateRuleApplyItemPanel|ShowMultiSelectionDialog" Editor/UI/ConfigWindow/ConfigWindow.cs
```

预期：旧 `selectionRequester`、`AddAssetType` 和 `AddProfile` 不再存在；构造函数的多选委托只有一个定义和一个注入位置；规则引用继续使用单选委托。

切回 Unity 并确认 `AssetScoreTool.Editor` 无编译错误。打开“新建规则应用项”，分别一次选择多个资源类型和检测配置档；关闭子选择面板应保留创建面板但不改变临时列表，确认子选择后刷新临时列表，最终取消创建不得写入工作副本。

### Task 4: 同步交互说明并完成全链路验收

**Files:**

- Modify: `docs/配置界面.html:1643-1675, 2558-2607, 2863-2895, 2981-2988, 3101-3105`
- Review: `docs/superpowers/specs/2026-07-23-config-multi-select-design.md`

**Interfaces:**

- Consumes: Tasks 1–3 的最终用户交互。
- Produces: 与实际单选、多选行为一致的配置界面交互说明和最终验收记录。

- [ ] **Step 1: 区分 HTML 中的单选与多选入口**

保留现有 `data-open-selection="rule"`、`"assetType"` 和 `"profile"`，把资源类型和检测配置档按钮文字从“添加”改为“选择”，与运行时按钮一致。

在 `dialogs.selection` 后新增 `dialogs.multiSelection`。复用同一搜索字段和列表结构，但让两个示例行都带 `selected` class 和“已选择”状态，明确表达多选：

```javascript
multiSelection: {
    title: "选择多个配置",
    confirm: "确认",
    body:
        field(
            "搜索",
            '<input class="input" placeholder="搜索显示名称或资源名">',
            "可以选择多个现有配置。",
            false
        ) +
        `
        <div class="selection-list">
            <div class="selection-row selected">
                <div class="selection-text">
                    <div class="selection-name">Texture</div>
                    <div class="selection-asset">AssetType_Texture</div>
                </div>
                <div class="selection-state">已选择</div>
            </div>
            <div class="selection-row selected">
                <div class="selection-text">
                    <div class="selection-name">Sprite</div>
                    <div class="selection-asset">AssetType_Sprite</div>
                </div>
                <div class="selection-state">已选择</div>
            </div>
        </div>
        `
}
```

把页面主体和弹窗内容中的两个 selection 事件处理器统一改为：

```javascript
button.addEventListener("click", event => {
    event.preventDefault();
    openDialog(
        button.dataset.openSelection === "rule"
            ? "selection"
            : "multiSelection"
    );
});
```

全局事件处理器原本没有 `event` 参数，应同步改成相同签名。规则入口继续打开 `selection`，资源类型和检测配置档入口打开 `multiSelection`。

- [ ] **Step 2: 核对 HTML DOM 与运行时节点映射**

记录并逐项检查：

```text
ConfigSelectionPanel / .selection-dialog-panel
  -> ConfigSelectionPanel.uxml 的搜索、列表、按钮节点
  -> docs/配置界面.html 的 selection dialog

BuildRow / .selection-row / .selection-row-selected
  -> docs/配置界面.html 的 .selection-row / .selection-row.selected

BuildObjectReferenceArray / .config-reference-chip
  -> docs/配置界面.html 的已有资源类型和检测配置档 chip
```

预期：多选只改变选择状态数量和切换语义，不需要新增 USS 或改变 UXML 稳定结构。

- [ ] **Step 3: 执行最终静态检查**

运行：

```powershell
rg -n "ShowSelectionDialog\(|ShowMultiSelectionDialog\(" Editor/UI/ConfigWindow
rg -n "OpenMultiple|BuildOrderedSelectedValues|ReplaceArrayObjectReferences" Editor/UI/ConfigWindow
rg -n "multiSelection|data-open-selection|selection-row selected|已选择" docs/配置界面.html
```

预期：规则和白名单入口使用单选；资源类型和检测配置档的已有编辑、新建编辑都使用多选；HTML 同时保留单选和多选示例。

- [ ] **Step 4: 在 Unity 中执行最终验收**

必须完成以下检查：

1. Unity Console 中 `AssetScoreTool.Editor` 无编译错误。
2. 已有规则应用项的资源类型和检测配置档打开时正确回显全部已选项。
3. 新建规则应用项的资源类型和检测配置档可一次选择多项。
4. 搜索过滤前后，隐藏项的临时选择不丢失。
5. 确认空选择可以清空对应集合。
6. 取消按钮和右上角关闭均不修改原选择。
7. 重复打开面板不会继承上一轮临时状态或回调。
8. 规则选择、白名单规则选择等单选入口仍只能选择一个值，允许空值的入口保持原确认条件。
9. 保存全部后关闭并重新打开配置窗口，确认多选结果从真实资产正确恢复。

若无法直接读取 Unity Console，只能报告静态检查结果，并明确列出上述未执行的 Unity 验收项；不得声称功能已经完成。
