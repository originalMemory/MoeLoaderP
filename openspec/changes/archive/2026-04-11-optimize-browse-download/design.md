## Status

- **已完成**（2026-04-11）：实现已合入主线；本变更已归档至 `openspec/changes/archive/2026-04-11-optimize-browse-download/`；delta 仍为 `specs/browse-download-ux/spec.md`（OpenSpec 要求按能力分子目录）。

## Context

- 缩略图在 `MoeExplorerControl` 的 `ImageItemsWrapPanel` 中由 `MoeItemControl` 承载；失败态通过 `LoadingState` / `PreviewImage.Source` 等体现。
- 全选与 Ctrl+A 已在 `MoeExplorerControl.OnKeyDown` 中处理；本设计与之对齐修饰键与焦点范围。

### 与 MoeLoader-Delta（可选对照）

- Delta 使用 `.lst` 文本列表导出下载 URL，与本变更的浏览重试、快捷键、Auto 原图无直接耦合。

## Goals / Non-Goals

**Goals：** 页级「重试失败」与 Ctrl+R 行为一致且**不重拉已成功缩略图**；Ctrl+D 与现有「下载已选」同路径；`Auto` 下 `DownloadUrlInfo` 指向最高保真资源（见 delta 规格）。

**Non-Goals：** 下载队列 `.mlpub` 导出/拖入/关闭确认（见 `unfinished-download-mlpub`）；不引入全局键盘钩子。

## Decisions

### 1. 失败判定与重试入口

- 在 `MoeItemControl` 集中判定「应重试」（例如 `ShouldRetryThumbnailLoad`），页级遍历仅对满足条件的控件调用 `TryLoad()`。
- 菜单项与 Ctrl+R 共用同一方法（如 `RetryFailedThumbnailsOnCurrentPage`），避免分叉逻辑。

### 2. Ctrl+D

- 从按钮点击路径提取无参入队方法，快捷键与按钮共用；无选中时为 no-op。

### 3. 原图优先

- `MoeItemHelper.AddAuto()` 等展示文案与 `DownloadUrlInfo` 选取顺序一致；各站点在填充 `Urls` 时保证 `Origin` 等类型在 Auto 解析链中优先于缩略/预览（按站点审计在 `tasks.md` 3.3 跟踪）。

## Risks / Trade-offs

- 部分站点列表阶段未写入原图 URL 时，Auto 仍可能落到预览；需在站点层补数据而非仅改 UI 文案。

## Open Questions

- 是否在菜单或工具提示中提示 Ctrl+R / Ctrl+D——首版可选，以 delta 规格为准。
