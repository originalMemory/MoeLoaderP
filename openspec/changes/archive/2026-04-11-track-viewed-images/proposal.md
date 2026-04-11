## Why

用户在站点列表中浏览大量缩略图时，需要区分「已看过」与「未看过」，避免重复点开。可参考 MoeLoader-Delta 的 `ViewedID` 思路（区间压缩、按站点记录）；MoeLoaderP 使用 JSON 设置与独立 UI，**不迁移 Delta 用户数据**。已读在界面上用**边框色**区分（与 Delta 用背景叠色不同）。

## What Changes

- 按**站点**维护「已浏览图片 ID」集合：编码与更新语义参考 Delta 的 `ViewedID`（游程压缩、`AddViewingId` / `IsViewed`、列表拉取时更新已读集合）；持久化落在 MoeLoaderP 现有 **`Settings` JSON**（如 `IndividualSiteSettings` 上的编码字段），**不从 Delta INI 导入任何数据**。
- 缩略图根据 `MoeItem` 是否已读切换**外边框颜色**（未读默认、已读玫红系资源色，与选中蓝区分）。
- **全局**「屏蔽已浏览」开关：放在**软件设置 → 搜索设置**分组（与 `TextSettingsGroupSearch` 同组），**默认关闭**。开启后仅影响**当前正在展示的那一屏**缩略图在填充 `ImageItemsWrapPanel` 时的**最后一道展示过滤**（已读项不加入或不可见）；**不改变** `SearchSession` 翻页、`TryGetRealPage`、`HasNextPage`、各站 `GetRealPageAsync` 等抓取与分页逻辑。
- **子项** `ChildrenItems` **不参与**独立已读记录与独立边框逻辑，**跟随父项**的已读展示即可。
- **主窗口底部状态栏**（与 `GetCurrentSearchStateText()` 同一信息区，现为 `MainWindow.xaml` 中 `SiteTextBlock`）在展示「当前搜索」文案时，**追加**仅针对**当前正在浏览的这一搜索页**（当前选中的 `SearchedVisualPage` 内全部 `RealPages` 条目）的 **N** 与 **M**：**N** = 该页内直接枚举到的 `MoeItem` 总数，**M** = 其中 `IsViewed` 为 true 的条数。文案须能读出「本页」语义（例如「本页共 N 张，已读 M 张」），**不得**与别处已展示的**全会话/全流程**「共多少张」类统计重复同一含义。

## Capabilities

### New Capabilities

- `viewed-image-tracking`：按站点持久化已浏览 ID、拉取列表时标记与更新 `MoeItem` 已读状态、WPF 已读边框、搜索设置中的全局「屏蔽已浏览」及当前页展示层过滤、**状态栏「当前搜索页」本页张数与已读数**；不包含站点 API 协议变更，**不包含**从 Delta 配置迁移。

### Modified Capabilities

- （无）当前 `openspec/specs/` 下尚无主规格。

## Impact

- **Core**：`ViewedID` 等价类型；`MoeItem` / `MoeItems.Add` 中仅做**已读标记与 ID 更新**（不做「屏蔽已浏览」的数据层剔除）；`Settings` 新增全局布尔项。
- **Wpf**：`MoeExplorerControl.ShowVisualPage`（或当前页挂载控件的唯一路径）在加入 `ImageItemsWrapPanel` 时应用展示层屏蔽；`SettingsControl.xaml` 搜索设置分组新增 CheckBox；`MoeItemControl` + `MoeResource.xaml` 已读边框与**选中态叠加**；子项控件绑定/继承父项已读态；`SearchControl` / `MainWindow` 侧在**当前 `SearchedVisualPage`** 变化时刷新 `SiteTextBlock` 的本页 **N/M** 后缀。
- **颜色**：同前——`#FFE993AA` 一类资源键，与 Delta 已读色相一致，仅用边框。

## References（实现时）

- Delta（算法参考）：`ViewedID.cs`、`AbstractImageSite.FilterImg`（仅参考「标记 + 更新 ID」前半；**屏蔽已浏览**在 MoeLoaderP 按产品要求放在展示层，与 Delta 不同）。
- MoeLoaderP：`Settings.cs`、`MoeItem.cs`、`MoeExplorerControl.xaml.cs`（`ShowVisualPage`）、`SettingsControl.xaml`、`MoeItemControl`、`MoeResource.xaml`。
