## Context

- MoeLoaderP 使用 **JSON** 保存 `Settings`；每站 `IndividualSiteSettings` 适合存放已读编码串。
- 缩略图数据为 `MoeItem`（整数 `Id`），通过 `MoeItems.Add` 进入 `SearchedPage`；当前页 UI 在 `MoeExplorerControl.ShowVisualPage` 中遍历 `page.RealPages` 并向 `ImageItemsWrapPanel` 添加 `MoeItemControl`。
- **Delta 仅作算法与交互参考**，不向 MoeLoaderP **导入**任何 Delta 用户配置或已读数据。
- 产品要求：**「屏蔽已浏览」为全局设置、默认关，且只作用于当前页缩略图的最终展示**，不介入翻页与网络请求。

## Goals / Non-Goals

**Goals:**

- Core：`ViewedID` 等价实现；按 `ShortName` 分站点持久化编码串；在条目进入 `MoeItems` 时设置 `IsViewed` 并更新浏览 ID（与 Delta「标记 + AddViewingId」一致），**不在此阶段剔除**已读条目以免动分页语义。
- Wpf：已读边框资源 + `MoeItemControl`；**搜索设置**分组下新增「屏蔽已浏览」CheckBox，绑定 `Settings` 全局布尔，默认 `false`。
- **展示层**：`ShowVisualPage`（或等价的当前页挂载路径）在往 `ImageItemsWrapPanel` 添加控件时，若开启屏蔽且 `MoeItem.IsViewed`，则**跳过添加**或设为不占用布局的隐藏（实现择一，以不改变 `RealPages` / `SearchSession` 内数据为准）。
- **子图**：`ChildrenItems` 不单独记已读、不单独画已读框；子项 UI **继承父项**是否已读（绑定父 `MoeItem` 或等价）。
- **状态栏统计**：在 `SiteTextBlock` 与 `GetCurrentSearchStateText()` 拼接，**仅统计当前选中的搜索分页（`SearchedVisualPage`）** 的本页 **N/M**；与别处可能出现的**非本页**「共多少张」语义区分（见 §7）。

**Non-Goals:**

- 从 Delta INI 或其它外部工具**迁移**已读数据。
- 为异常 ID、重复 ID 或非标站点做额外兼容（明确不纳入本变更）。
- 改变各站 `GetRealPageAsync` 与 `SearchSession.TryGetRealPage` 的翻页与计数逻辑。

## Decisions

### 1. 持久化字段：`IndividualSiteSettings.ViewedIdsEncoded`

- 字符串，与整份 `Settings` JSON 一并序列化；加载后构建 `ViewedID`。

### 2. 核心类型：`MoeLoaderP.Core` 内 `ViewedID`

- 算法参考 Delta，编码串格式保持一致，便于人工对照与维护；**不要求**与 Delta 程序互导数据。

### 3. 已读标记与更新：`MoeItems.Add` 内、`LocalFilter()` 之前

- **仅**：根据站点 `ViewedID` 设置 `MoeItem.IsViewed`；对未在持久集合中的 ID 在拉列表时调用 `AddViewingId`（等价 Delta `updateViewed == true`）。
- **不**在此处根据「屏蔽已浏览」设置 `IsLocalFilter` 或剔除条目，避免影响 `FilterCount`、分页与「每页条数」的 Core 语义。

### 4. 「屏蔽已浏览」：`Settings` 全局 + 搜索设置 UI + 仅当前页展示

- 属性名建议 `MaskViewedInSearch` 或 `IsMaskViewedInThumbnailList`（实现时二选一，与 JSON 字段一致即可），默认 `false`。
- UI：放在 `SettingsControl.xaml` 中 **TextSettingsGroupSearch** 所在 `GroupBox` 的 `StackPanel` 内（与「并行加载」「历史条数」等同级），文案例如「当前页不显示已浏览图片」或「屏蔽已浏览（仅当前页展示）」。
- 行为：仅在 **构建当前屏 `ImageItemsWrapPanel` 子控件列表** 时判断；`RealPages` / `SearchedPage` 内 `MoeItem` 集合保持完整，翻页、再搜、会话内数据**不变**。
- **硬约束（保持现状）**：不得将「屏蔽已浏览」并入 `LocalFilter()` 或任何依赖 `FilterCount` 的路径。否则 `SearchNextVisualPage` 会用 `Count - FilterCount` 误判「可展示条数」，从而**多拉真实分页请求**，改变翻页/凑满 `CountLimit` 的行为。

### 5. `ViewedID` 生命周期

- 按站点懒加载/缓存；变更后写回 `ViewedIdsEncoded`；`MainWindow.OnClosing` 等已有 `Settings.Save` 路径落盘。

### 6. UI：已读边框、选中与子项

- 顶层：`MoeItem.IsViewed` 驱动**未选中**时的外框/底框玫红资源；与现有 `ImageItemCheckBoxControlTemplate` 的**选中高亮**（蓝色系）**叠加**：勾选选中时**仍会**出现选中描边；已读**不**因选中而被「改成只有一种颜色」——采用**双层或可分层**（例如外圈已读色 + 内层/叠加选中框），使用户仍能同时读出「已读」与「已选中」。未读项选中时仅保留原有选中样式。
- 子项：`MoeItemControl`（或子项模板）对 `FatherItem != null` 时使用父项的已读态参与边框或不再单独查询 `ViewedID`。

### 7. 状态栏：仅「当前搜索页」本页「共 N / 已读 M」

- **统计范围（重要）**：**仅**当前用户正在查看的那一个 **`SearchedVisualPage`**（分页条上当前高亮对应的那一页）内，其所有 `RealPages` 中各 `SearchedPage` 里 **`foreach (var item in rp)` 直接枚举到的顶层 `MoeItem`**：**N** = 本页条目总数，**M** = 其中 `IsViewed == true` 的数量。不展开 `ChildrenItems` 重复计数。切换分页标签到另一 `SearchedVisualPage` 时，**N/M 随该页重算**。
- **与其它「共多少张」输出的关系**：搜索过程中 `Ex.ShowMessage` / 页内提示等可能已输出**抓取或会话级**累计信息；本段后缀**只表达当前展示页**，文案必须含「本页」或等价语义，避免用户理解为「整个搜索会话累计」。
- **刷新时机**：`ShowVisualPage`、切换当前 `SearchedVisualPage`、或当前页内 `RealPages` 追加完毕且 UI 刷新时；新搜索且尚未有当前页时可隐藏该后缀或显示「本页共 0 张」。
- **展示位置**：仍在 `SiteTextBlock` 与 `GetCurrentSearchStateText()` 拼接，不占用 `StatusTextBlock`。

## Risks / Trade-offs

- **[Trade-off] 开启屏蔽时当前页可见张数可能少于抓取张数** → 属预期；不在 Core 层伪造「满页」以免误导下一页请求。

- **[Trade-off] 选中/全选与未展示的已读项** → 若实现为不添加控件，则当前页无法框选被省略的已读项；首版接受该限制，若需「仍可选中」再在后续迭代用占位或其它交互解决。

## Migration Plan

- 仅新增 JSON 字段与 UI；旧设置文件缺字段时反序列化为默认「不屏蔽」、无已读数据。

## Open Questions

- （无）子项、迁移、特殊 ID 已由产品结论关闭。
