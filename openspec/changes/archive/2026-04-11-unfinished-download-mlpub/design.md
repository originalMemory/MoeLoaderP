## Status

- **已完成**（2026-04-11）：实现已合入主线；本变更已归档至 `openspec/changes/archive/2026-04-11-unfinished-download-mlpub/`；主规格见 `openspec/specs/mlpub-bundle/spec.md`。

## Context

- 队列为 `MoeDownloader.DownloadItems`；`DownloadStatus` 枚举见 Core。
- 扩展名 **`.mlpub`**（**M**oe**L**oader**P** **Unfinished** **B**undle），与 Delta 临时 `.moe`、列表 `.lst` 区分。
- Core 已有 **Newtonsoft.Json**。

### 与 MoeLoader-Delta 的对照

- Delta **`.lst`**：纯文本 `|` 分隔；本方案为 **JSON**，且扩展名专用。
- Delta **`.moe`**：仅为下载中临时文件，非列表导出。
- **壳层注册**：首版不注册 ProgId；资源管理器对 `.mlpub` 为未知类型属预期。

## Goals / Non-Goals

**Goals：** 见 `proposal.md`；导入后统一 `WaitForDownload`；URL 仅 `http`/`https`。

**Non-Goals：** 拖入任意 `.json`；首版不保证仅 `ResolveUrlFunc` 的项可恢复。

## Decisions

### 1. 扩展名与魔数

- 扩展名 **`.mlpub`**；`DragOver` 仅当全部为 `.mlpub` 时 `Copy`。
- `schema` **`MoeLoaderP.UnfinishedDownloadTasks`**，`version` **1**；**不**兼容 `.mlpfd` 或其它旧扩展（规划阶段未实施，无迁移负担）。

### 2. 单文件 vs 多文件拖入

- 支持一次拖入**多个** `.mlpub`：依次解析、合并追加；任一文件校验失败则跳过该文件并汇总提示，不中断其它文件（实现可整批原子，首版允许「部分成功」）。

### 3. 导出集合与多图

- 含 **`ChildrenItems`** 的父项：**不写入** `tasks`；若其状态属未成功，**仍触发**关闭三键弹窗，但「是 → 导出」不产生该父项记录。

### 4. 合并与去重

- 与 `proposal` 一致：`siteShortName` + Trim 后 `downloadUrl`；`downloadUrl` 空时用 `siteShortName` + `id`。

### 5. 关闭与保存对话框

- **是** → `SaveFileDialog` → 成功保存后允许关闭；保存取消 → **不关闭**。
- **否** → 直接关闭；**取消** → `e.Cancel = true`。

### 6. 默认文件名

- 建议 `unfinished-tasks-{yyyyMMdd-HHmmss}.mlpub`。

### 7. 由记录恢复 `MoeItem`

- 按 `siteShortName` 在 `Settings.SiteManager` 解析站点，构造 `MoeItem` 并回填 URL / `DetailUrl` / `Id` / `Title` / `LocalFileShortNameWithoutExt`，`DlStatus = WaitForDownload`。
- 站点缺失或未知：跳过该 task 并计入错误列表。

## Risks / Trade-offs

- Yes/No/Cancel 依赖正文说明；若混淆可改自定义三按钮窗体。

## Open Questions

- `DownloadUrlInfo` 为空时是否写 `detailUrl` 并在导入拉详情——首版可跳过。
