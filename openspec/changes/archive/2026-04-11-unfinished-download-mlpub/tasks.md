## 1. Core 与格式

- [x] 1.1 定义 `.mlpub` DTO、常量 `MoeLoaderP.UnfinishedDownloadTasks` / `version`、Newtonsoft.Json 读写、URL 白名单。
- [x] 1.2 从 `DownloadItems` 收集未成功且无子项的项序列化；解析 `.mlpub` 并追加 `MoeItem`（`WaitForDownload`）；可选 `dlStatusAtExport`。

## 2. UI 与拖放

- [x] 2.1 `DownloaderControl`：右键「导出未成功任务」、`SaveFileDialog`、合并保存、无可导出项提示。
- [x] 2.2 `DragOver`/`Drop`：仅 `.mlpub` 且校验通过后入队。

## 3. 主窗口与本地化

- [x] 3.1 `MainWindow.OnClosing`：三键逻辑与 `design.md` 一致。
- [x] 3.2 `zh-CN.xaml`：`TextDownloaderContextExportUnfinished`、关闭提示等；手工验证导出→拖入→关闭流程。
