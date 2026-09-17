# Spec: shift-range-image-selection

## Why
- 连续选择大量图片需逐张点击或框选
- use `Shift` 快速选择两个点击索引间的图片

## Scope
- 记录当前页上次点击的卡片索引
- 新点击使图片变为选中且按住任一 `Shift` 时，选中两个索引的闭区间
- 区间选择保留区间外已有选择
- 翻页、刷新或切换会话时清空索引
- 不改变取消选中、框选、全选和反选行为

## Plan
- [x] 在 `MoeExplorerControl` 增加上次点击索引
- [x] 卡片复选框点击后执行区间选择
- [x] 重置当前可视页时清空索引
- [x] 构建并验证正向、反向和边界选择

## Apply Notes
- use `ImageItemsWrapPanel.Children` 作为当前页顺序，不给 `MoeItemControl` 增加索引状态
- 无有效上次索引或当前点击变为未选中，只更新索引
- 区间循环仅将 `IsChecked` 设为 `true`
- Debug `Assert` 检查区间全部选中

## Verify
- [x] `dotnet build MoeLoaderP.sln`
- [x] 点击索引 2，再 `Shift` 点击索引 6，索引 2..6 全部选中
- [x] 点击索引 6，再 `Shift` 点击索引 2，索引 2..6 全部选中
- [x] 首次点击同时按 `Shift` 时仅切换当前图片
- [x] `Shift` 点击使图片取消选中时不批量取消
- [x] 翻页后首次 `Shift` 点击不使用旧页索引

## Status
- State: done
- Archived: yes
