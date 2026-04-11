## Why

浏览缩略图时网络或站点偶发失败会导致部分缩略图无法显示，用户缺少一键重试入口；批量下载依赖鼠标操作，效率低于快捷键；部分站点同时提供预览图与原图 URL 时，当前下载链路未必优先选择原图，影响成品质量。本变更在不大改架构的前提下补齐上述浏览与下载体验缺口。**未完成任务导出 / `.mlpub` 拖入 / 关闭三键确认**已拆分到独立变更 `unfinished-download-mlpub`。

## What Changes

- **缩略图页右键菜单**：在与「全选」同级（同一 `SpPanel` / 菜单分组语义）增加**简短**文案的重试项（例如「重试失败」）；点击后对**当前可视页**内**仅**仍处于失败/错误/占位态的缩略图触发重新拉取。`TryLoadThumbnailStreamAsync` 每次被调用都会走网络；**批量重试逻辑不得对已成功显示缩略图的项再次调用**。
- **快捷键**：在缩略图浏览区域注册 **Ctrl+D**，行为与「将已选中项加入下载队列」一致；注册 **Ctrl+R**，与上述右键「重试失败」**同一套**重试逻辑。与 `MoeExplorerControl` 中已有 Ctrl+A 全选模式对齐。
- **下载 URL 优先级**：在解析或选择下载地址时，**优先使用原图 / 最高可用分辨率**对应的 `DownloadUrlInfo`；不改变「用户显式指定格式」类设置时的语义（若有）。

## Capabilities

### New Capabilities

- `browse-download-ux`：缩略图网格上下文菜单 + **Ctrl+R** 的失败缩略图重试、Ctrl+D 下载已选、下载时 URL/类型优先原图（delta：`specs/browse-download-ux/spec.md`；主规格：`openspec/specs/browse-download-ux/spec.md`）。

### Modified Capabilities

- （无）现有主规格 `viewed-image-tracking` 的「已读/屏蔽」需求不因本变更改变。

`specs/` 下**仅**保留 `browse-download-ux/` 单一能力 delta（与已拆出的下载队列变更分离）；OpenSpec 校验要求路径为 `specs/<capability>/spec.md`，不可再扁平为单文件。

## Impact

- **Wpf**：`MoeContextMenuControl`、`MoeExplorerControl`、`MoeItemControl`（若需暴露失败态）、`zh-CN.xaml` 等语言资源。
- **Core**：各站点或 `MoeItem` / `MoeItemHelper` 中设置 `DownloadUrlInfo` 与多分辨率候选时的**选择顺序**（优先原图）；若失败态标记在 Core，需与 Wpf 重试约定一致。

## References（实现时）

- 全选与快捷键：`MoeExplorerControl.xaml.cs`、`MoeContextMenuControl.xaml` / `.xaml.cs`。
- 下载入队：`MoeExplorerControl` 中选中项加入下载器的现有逻辑。
- 缩略图加载：`MoeItem.TryLoadThumbnailStreamAsync`、`MoeItemControl` 绑定与错误展示。
