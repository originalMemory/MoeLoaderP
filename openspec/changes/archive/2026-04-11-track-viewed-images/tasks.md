## 1. Core — ViewedID 与持久化字段

- [x] 1.1 在 `MoeLoaderP.Core` 新增 `ViewedId.cs`，参考 Delta 实现区间存储、`IsViewed`、`AddViewingId`、`AddViewedRange`、`ToString()` 及私有 `IdRange`（不要求与 Delta 互导数据，仅编码算法一致）。
- [x] 1.2 在 `IndividualSiteSettings` 增加 `ViewedIdsEncoded`（或最终命名）字符串属性，默认 null/空。
- [x] 1.3 实现按站点懒加载/缓存 `ViewedId`：从 `ViewedIdsEncoded` 初始化；变更后写回该属性，随 `Settings.Save` 落盘。

## 2. Core — 已读标记（不动屏蔽）

- [x] 2.1 在 `MoeItem` 增加 `IsViewed`（`BindingObject` 通知）。在 `MoeItems.Add` 中于 `LocalFilter()` **之前**仅执行：根据站点 `ViewedId` 设置 `IsViewed`、对未持久已读 ID 调用 `AddViewingId`；**不得**在此处根据「屏蔽已浏览」修改 `IsLocalFilter` 或从集合移除条目。
- [x] 2.2 在 `Settings` 增加全局「屏蔽已浏览」布尔属性，默认 `false`（名称与 JSON 字段在实现时保持一致）。

## 3. Wpf — 设置、展示层过滤与视觉

- [x] 3.1 在 `SettingsControl.xaml` 的搜索设置 `GroupBox`（`TextSettingsGroupSearch`）内增加 CheckBox，双向绑定 2.2 的全局属性；文案标明**仅影响当前页缩略图展示**。
- [x] 3.2 在 `MoeExplorerControl.ShowVisualPage`（或当前页向 `ImageItemsWrapPanel` 添加控件的唯一路径）中：若全局屏蔽开启且 `MoeItem.IsViewed`，则**不添加**或**隐藏且不占位**该缩略图控件；**不得**修改 `RealPages` / `SearchedPage` 内已有 `MoeItem` 集合。
- [x] 3.3 在 `MoeResource.xaml` 新增 `MoeViewedImageBorderBrush`（`#FFE993AA` 或对比度微调）；`MoeItemControl` 按 `IsViewed` 切换边框；**子项**（`FatherItem != null`）边框/已读态**绑定或继承父项**，不对子项 ID 写 `ViewedId`。
- [x] 3.4 处理「已读 + 选中」组合态，避免与 `#FF00B9FF` 选中样式混淆。
- [x] 3.5 实现**当前选中 `SearchedVisualPage` 的本页**统计：`N`/`M` 仅对该页 `RealPages` 内各 `SearchedPage` 直接枚举的 `MoeItem`（不展开 `ChildrenItems`）；切换分页、`ShowVisualPage`、新搜索时刷新；`SiteTextBlock` 拼接 `GetCurrentSearchStateText()` 与带**「本页」**语义的「本页共 N 张，已读 M 张」类后缀，与会话级其它「共多少张」提示区分。
- [x] 3.6 已读边框与 `ImageItemCheckBoxControlTemplate` 选中态**叠加**：选中时仍显示蓝色选中描边，已读玫红外圈/底层保留，避免选中后与未读选中无法区分已读（玫红环叠在 `CheckBox` 之上且 `IsHitTestVisible="False"`，避免被模板 `BgBorder` 完全遮挡）。

## 4. 验证与收尾

- [x] 4.1 手工验证：已读边框、重启后持久化；开启屏蔽时当前页已读不显示但翻页/再搜行为与关闭屏蔽时一致；子项随父项显示已读样式；状态栏 **本页 N/M** 随切换分页变化、新搜索重置；**选中+已读**时仍能看出已读与选中两层信息。
- [x] 4.2 运行 `openspec validate track-viewed-images` 对照变更与 `specs/viewed-image-tracking/spec.md`（OpenSpec CLI 无 `verify-change` 子命令时用本项）。
