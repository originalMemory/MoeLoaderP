# Spec: download-taskbar-progress-and-clean-retry

## Why
- 下载时只能打开面板查看进度，任务栏按钮没有反馈
- 清理成功项与重试失败项需分两次操作

## Scope
- 下载期间在主窗口任务栏按钮显示全队列进度
- 全部成功/跳过后保留满格绿色；终态含失败项时保留满格错误红色
- 空列表或仅停止/取消时隐藏任务栏进度
- 下载列表右键菜单增加“清除成功并重试失败”
- use WPF `TaskbarItemInfo` 与下载面板现有 1 秒定时器
- 不复制 Delta 的 COM 封装，不新增依赖，不增加设置项

## Plan
- [x] 在下载面板定时刷新中聚合任务进度并更新所属窗口任务栏
- [x] 增加“清除成功并重试失败”菜单按钮，串联现有清理与重试操作
- [x] 修正共用重试入口，重置进度与取消令牌
- [x] 增加中文资源文本
- [x] 构建并检查下载中、完成后、清理重试三种状态

## Apply Notes
- 聚合值为列表项 `Progress` 平均值；复用已有成功项 `100`、等待项 `0` 与下载项实时百分比
- 下载中使用 `Normal`；全部 `Success` / `Skip` 使用满格 `Normal`；存在 `Failed` 使用满格 `Error`
- `TaskbarItemInfo` 的绿色/红色由 Windows 主题决定，不支持传入项目自定义色值
- 空列表或终态仅含 `Stop` / `Cancel` 时使用 `TaskbarItemProgressState.None`
- 组合操作作用于全列表：删除 `Success` / `Skip`，再将 `Failed` 置为等待下载
- 共用 `Retry` 为失败项创建新 `CancellationTokenSource` 并将 `Progress` 归零，避免复用已取消令牌或残留百分比
- 多图任务保持 `Downloading` 至全部子项处理结束；任一子项失败则父任务最终为 `Failed`
- 重试只处理终态 `Failed`，忽略仍在下载的项，避免旧任务与新任务并发写入

## Verify
- [x] `dotnet build MoeLoaderP.sln`
- [x] 多任务下载时任务栏显示绿色进度，数值随下载推进
- [x] 全部成功/跳过后任务栏保留满格绿色
- [x] 下载结束且存在失败项时任务栏显示满格错误红色
- [x] 空列表或终态仅含停止/取消项时任务栏进度消失
- [x] 菜单操作后成功/跳过项移除，失败项重新进入等待下载
- [x] 失败项重试后进度归零，且仍可停止
- [x] 多图任务部分失败时最终为失败，处理期间不提前进入终态

## Status
- State: done
- Archived: yes
