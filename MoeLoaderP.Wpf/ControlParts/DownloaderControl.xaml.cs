using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts;

public partial class DownloaderControl
{
    public Settings Settings { get; set; }
    public MoeDownloader Downloader { get; set; }
    public DispatcherTimer Timer { get; set; } = new();
    public DownloaderControl()
    {
        InitializeComponent();
    }

    public void Init(Settings settings)
    {
        Settings = settings;
        Downloader = new MoeDownloader(Settings);

        DownloadItemsListBox.ItemsSource = Downloader.DownloadItems;
        DownloadItemsListBox.MouseRightButtonUp += DownloadItemsListBoxOnMouseRightButtonUp;
        OpenFolderButton.Click += OpenFolderButtonOnClick;
        DeleteAllButton.Click += DeleteAllButtonOnClick;
        DeleteButton.Click += DeleteButtonOnClick;
        StopButton.Click += StopButtonOnClick;
        SelectAllButton.Click += SelectAllButtonOnClick;
        RetryButton.Click += RetryButtonOnClick;
        ClearSuccessRetryFailedButton.Click += ClearSuccessRetryFailedButtonOnClick;
        ExportUnfinishedButton.Click += ExportUnfinishedButtonOnClick;
        DownloadItemsListBox.PreviewDragOver += DownloadItemsListBoxOnPreviewDragOver;
        DownloadItemsListBox.Drop += DownloadItemsListBoxOnDrop;
        KeyDown += OnKeyDown;

        Timer.Interval = TimeSpan.FromSeconds(1);
        Timer.Tick += TimerOnTick;
        Timer.Start();
    }

    private void TimerOnTick(object sender, EventArgs e)
    {
        Downloader.TimerOnTick(sender, e);
        UpdateTaskbarProgress();
    }

    private void UpdateTaskbarProgress()
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        var taskbar = window.TaskbarItemInfo ??= new TaskbarItemInfo();
        var items = Downloader.DownloadItems;
        if (items.Count == 0)
        {
            taskbar.ProgressValue = 0;
            taskbar.ProgressState = TaskbarItemProgressState.None;
        }
        else if (Downloader.IsDownloading)
        {
            taskbar.ProgressValue = items.Average(item => Math.Clamp(item.Progress, 0, 100)) / 100d;
            taskbar.ProgressState = TaskbarItemProgressState.Normal;
        }
        else if (items.Any(item => item.DlStatus == DownloadStatus.Failed))
        {
            taskbar.ProgressValue = 1;
            taskbar.ProgressState = TaskbarItemProgressState.Error;
        }
        else if (items.All(item => item.DlStatus is DownloadStatus.Success or DownloadStatus.Skip))
        {
            taskbar.ProgressValue = 1;
            taskbar.ProgressState = TaskbarItemProgressState.Normal;
        }
        else
        {
            taskbar.ProgressValue = 0;
            taskbar.ProgressState = TaskbarItemProgressState.None;
        }
    }
        
    public MoeItems CastSelectToDownloadItems()
    {
        var selectItems = DownloadItemsListBox.SelectedItems;
        var lb = DownloadItemsListBox;

        var di = new MoeItems();
        foreach (var selectItem in selectItems)
        {
            var i = lb.Items.IndexOf(selectItem);
            di.Add(Downloader.DownloadItems[i]);
        }
        return di;
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            DownloadItemsListBox.SelectAll();
        }
    }
    private void DownloadItemsListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ContextMenuPopup.IsOpen = true;
        ContextMenuPopupGrid.EnlargeShowSb().Begin();
    }

    #region 右键菜单

    private void RetryButtonOnClick(object sender, RoutedEventArgs e)
    {
        MoeDownloader.Retry(CastSelectToDownloadItems());
        ContextMenuPopup.IsOpen = false;
    }

    private void ClearSuccessRetryFailedButtonOnClick(object sender, RoutedEventArgs e)
    {
        Downloader.DeleteAllSuccessAndRetryFailed();
        UpdateTaskbarProgress();
        ContextMenuPopup.IsOpen = false;
    }

    private void ExportUnfinishedButtonOnClick(object sender, RoutedEventArgs e)
    {
        var wnd = Window.GetWindow(this);
        UnfinishedDownloadExportUi.TryExportViaSaveDialog(Downloader.DownloadItems, wnd, out _);
        ContextMenuPopup.IsOpen = false;
    }

    private void DownloadItemsListBoxOnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (paths is not { Length: > 0 })
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (paths.Any(p => !p.EndsWith(UnfinishedDownloadMlpub.FileExtension, StringComparison.OrdinalIgnoreCase)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void DownloadItemsListBoxOnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (paths is null || paths.Length == 0) return;
        var mlpubs = paths.Where(p => p.EndsWith(UnfinishedDownloadMlpub.FileExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (mlpubs.Length == 0) return;
        var (added, errors) = UnfinishedDownloadMlpub.ImportFromMlpubFiles(mlpubs, Downloader, Settings);
        var wnd = Window.GetWindow(this);
        var msg = added > 0 ? $"已加入 {added} 个下载任务。" : "未能加入任何任务。";
        if (errors.Count > 0) msg += "\n\n" + string.Join("\n", errors.Take(8));
        MessageBox.Show(wnd, msg, App.DisplayName, MessageBoxButton.OK,
            added > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void SelectAllButtonOnClick(object sender, RoutedEventArgs e)
    {
        DownloadItemsListBox.SelectAll();
        ContextMenuPopup.IsOpen = false;
    }

    private void StopButtonOnClick(object sender, RoutedEventArgs e)
    {
        MoeDownloader.Stop(CastSelectToDownloadItems());

        ContextMenuPopup.IsOpen = false;
    }

    private void DeleteButtonOnClick(object sender, RoutedEventArgs e)
    {
        Downloader.Delete(CastSelectToDownloadItems());
        ContextMenuPopup.IsOpen = false;
    }

    private void DeleteAllButtonOnClick(object sender, RoutedEventArgs e)
    {
        Downloader.DeleteAllSuccess();
        ContextMenuPopup.IsOpen = false;
    }

    private void OpenFolderButtonOnClick(object sender, RoutedEventArgs e)
    {
        var item = CastSelectToDownloadItems().FirstOrDefault();
        if (item?.ChildrenItems.Count > 0)
        {
            item.ChildrenItems[0].LocalFileFullPath.GoFile();
        }
        else
        {
            item?.LocalFileFullPath.GoFile();
        }
        ContextMenuPopup.IsOpen = false;
    }

    #endregion

}
