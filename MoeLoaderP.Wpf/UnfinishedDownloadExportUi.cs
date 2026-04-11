using System;
using System.Windows;
using Microsoft.Win32;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf;

/// <summary>
///     未成功任务 .mlpub 导出：保存对话框与写入（供下载面板与主窗口关闭流程共用）。
/// </summary>
internal static class UnfinishedDownloadExportUi
{
    public static bool TryExportViaSaveDialog(MoeItems downloadItems, Window owner, out string error)
    {
        error = null;
        var exportable = UnfinishedDownloadMlpub.CollectExportableItems(downloadItems);
        if (exportable.Count == 0)
        {
            MessageBox.Show(owner,
                "当前没有可导出的任务（需为单图任务且含有效的 http(s) 下载地址）。",
                App.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return true;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "MoeLoaderP 未完成任务包 (*.mlpub)|*.mlpub",
            DefaultExt = "mlpub",
            AddExtension = true,
            FileName = $"unfinished-tasks-{DateTime.Now:yyyyMMdd-HHmmss}.mlpub"
        };

        if (dlg.ShowDialog(owner) != true)
        {
            error = "用户取消了保存";
            return false;
        }

        if (!UnfinishedDownloadMlpub.TryWriteExport(exportable, dlg.FileName, out error))
        {
            MessageBox.Show(owner, $"导出失败：{error}", App.DisplayName, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }
}
