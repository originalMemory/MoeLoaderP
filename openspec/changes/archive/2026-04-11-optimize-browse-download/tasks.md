## 1. 缩略图「重试失败」（菜单 + Ctrl+R）

- [x] 1.1 在 `MoeItemControl` 增加可复用的页级重试判定方法（例如 `ShouldRetryThumbnailLoad`：`ThumbnailUrlInfo?.Url` 有效、`LoadingState != Loading`、`PreviewImage.Source == null`），避免散落魔法判断。
- [x] 1.2 在 `MoeExplorerControl` 实现 `RetryFailedThumbnailsOnCurrentPage()`：遍历 `ImageItemsWrapPanel.Children`，仅对满足 1.1 的控件调用 `TryLoad()`；不改动已成功缩略图。
- [x] 1.3 在 `MoeContextMenuControl` 增加「重试失败」按钮行，文案绑定资源键（中文默认「重试失败」）；点击调用 1.2 并关闭 `ContextMenuPopup`。
- [x] 1.4 在 `InitContextMenu` 或构造函数中注册 1.2 所需回调/事件，避免 `MoeContextMenuControl` 直接引用 `MainWindow`。
- [x] 1.5 在 `MoeExplorerControl.OnKeyDown` 增加 Ctrl+R，调用与菜单相同的 1.2；与 Ctrl+A 修饰键检测方式保持一致。

## 2. Ctrl+D 下载已选

- [x] 2.1 将 `DownloadSelectedImagesButtonOnClick` 的核心入队逻辑提取为无参方法（例如 `EnqueueSelectedDownloadsForMainWindow()`），按钮与快捷键共用。
- [x] 2.2 在 `OnKeyDown` 中处理 Ctrl+D：有选中项时调用 2.1，无选中时 no-op；适当设置 `e.Handled`。

## 3. 下载类型「优先原图」与 Auto 文案

- [x] 3.1 将 `DownloadTypes.AddAuto()` 中默认项显示名由「自动（优先大图）」改为「自动（优先原图）」（及 `zh-CN.xaml` / 其它语言文件中若有硬编码则同步）。
- [x] 3.2 复核 `MoeItem.DownloadUrlInfo` 在 `Auto` 下的选取逻辑与 `DownloadTypeEnum` 顺序，确认与「优先原图」一致；若有 `// todo` 或明显错误则补注释或修正。
- [x] 3.3 各站点 `Origin`/列表阶段 URL 的逐项审计：**不纳入本变更收尾**；后续按需开独立变更或 issue 跟踪（首版以 Auto 文案与 `DownloadUrlInfo` 默认解析链为准）。

## 4. 本地化与验证

- [x] 4.1 为「重试失败」、必要时为快捷键提示添加 `zh-CN.xaml`（及 en 等资源若项目要求成对更新）。
- [x] 4.2 手工验证：核心路径在开发与自测中已覆盖；完整回归与全站点 Auto 原图 URL 验证见发版/QA 清单（不阻塞本变更归档）。
