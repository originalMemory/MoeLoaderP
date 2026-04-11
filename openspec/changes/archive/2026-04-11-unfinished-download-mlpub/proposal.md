## Why

下载队列中**未成功**（排队、下载中、失败、停止、取消等）项难以批量备份与跨会话恢复；需要**固定格式** `.mlpub`（UTF-8 JSON）导出与拖入追加，并在关闭应用时用**三按钮**确认是否先导出再退出。本变更独立于缩略图浏览体验优化。

## What Changes

- **`.mlpub` 包**：根字段 `schema`=`MoeLoaderP.UnfinishedDownloadTasks`、`version`=1，`tasks` 数组描述可恢复的下载项（字段见 delta 规格）。
- **导出未成功任务**：下载列表右键 **「导出未成功任务」**；导出集合为 `Failed` / `Downloading` / `WaitForDownload` / `Stop` / `Cancel`，**排除** `Success` / `Skip`；**无 `ChildrenItems`** 的项才写入；多图父项 v1 **不写入**文件但可参与**关闭弹窗**判定。
- **合并保存**：若用户选择**已存在**的合法 `.mlpub`，与已有 `tasks` **合并去重**，不整文件覆盖；非法已存在文件则报错不写。
- **拖入**：下载区**仅**接受 **`.mlpub`** 且校验通过的文件追加队列。
- **关闭主窗口**：存在未成功任务时 **`MessageBoxButton.YesNoCancel`**（正文说明 是/否/取消）；选「是」后若在保存对话框取消则**不关闭**。

## Capabilities

### New Capabilities

- `mlpub-bundle`：`.mlpub` 格式、导出/合并、拖入导入、`MainWindow` 关闭确认与 `DownloaderControl` / `MoeDownloader` 集成（见 `specs/mlpub-bundle/spec.md`）。

### Modified Capabilities

- （无）

## Impact

- **Wpf**：`DownloaderControl`、`MainWindow.OnClosing`、`SaveFileDialog`、拖放、`zh-CN.xaml` 等。
- **Core**：DTO、Newtonsoft.Json 序列化、由记录构造 `MoeItem`、`Settings.SiteManager` 协作。
- **依赖**：Newtonsoft.Json（已有）。

## References（实现时）

- `MoeDownloader.cs`、`DownloaderControl.xaml` / `.xaml.cs`、`MainWindow.xaml.cs`、`DownloadStatus`。
