using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MoeLoaderP.Core;
using MoeLoaderP.Wpf.ControlParts;

namespace MoeLoaderP.Wpf;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public Settings Settings { get; set; }
    
    public void Init(Settings settings)
    {
        // gen custom test 请删除后运行
        //if (Debugger.IsAttached)
        //{
        //    var cus = new CustomSiteFactory();
        //    cus.GenTestSites();
        //    cus.OutputJson(App.CustomSiteDir);
        //    Thread.Sleep(1000);
        //}

        

        Settings = settings;
        Settings.CustomSitesDir = App.CustomSiteDir;
        Settings.SiteManager = new SiteManager(Settings);
        DataContext = Settings;
            
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
        MouseLeftButtonDown += delegate { DragMove(); };
        Ex.ShowMessageAction += ShowMessage;

        // logo
        //LogoImageButton.MouseRightButtonDown += LogoImageButtonOnMouseRightButtonDown;

        // menu
        DownloaderMenuCheckBox.Checked += DownloaderMenuCheckBoxCheckChanged;
        DownloaderMenuCheckBox.Unchecked += DownloaderMenuCheckBoxCheckChanged;
        
        // user ctrl
        AboutControl.Init();
        SearchControl.Init(Settings);
        
        MoeDownloaderControl.Init(Settings);
        MoeSettingsControl.Init(Settings);
        MoeExplorer.Init(Settings);

        // helper : collect ,log
        CollectCopyAllButton.Click += delegate { CollectTextBox.Text.CopyToClipboard(); };
        CollectClearButton.Click += delegate { CollectTextBox.Text = string.Empty; };
        new LogWindowHelper().Init(LogButton, Settings);
        
        ImageSizeSlider.MouseWheel += ImageSizeSliderOnMouseWheel;

        // egg
        new EggWindowHelper().Init(this);

        

        // ali
        this.SetWindowFluent(settings );

        ApplySavedMainWindowPlacement();

        Settings.SiteManager.PropertyChanged += SiteManagerOnPropertyChanged;
    }

    /// <summary>
    ///     若有上次保存的坐标则恢复（并夹紧到虚拟屏内）；否则居中。
    /// </summary>
    private void ApplySavedMainWindowPlacement()
    {
        if (Settings.MainWindowLeft is not { } savedLeft || Settings.MainWindowTop is not { } savedTop)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        if (double.IsNaN(savedLeft) || double.IsNaN(savedTop)
            || double.IsInfinity(savedLeft) || double.IsInfinity(savedTop))
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var vsLeft = SystemParameters.VirtualScreenLeft;
        var vsTop = SystemParameters.VirtualScreenTop;
        var vsW = SystemParameters.VirtualScreenWidth;
        var vsH = SystemParameters.VirtualScreenHeight;
        var w = ActualWidth > 0 ? ActualWidth : Width;
        var h = ActualHeight > 0 ? ActualHeight : Height;
        if (w <= 0 || double.IsNaN(w)) w = Settings.MainWindowWidth > 0 ? Settings.MainWindowWidth : MinWidth;
        if (h <= 0 || double.IsNaN(h)) h = Settings.MainWindowHeight > 0 ? Settings.MainWindowHeight : MinHeight;

        const double margin = 40;
        var left = Math.Min(Math.Max(savedLeft, vsLeft - w + margin), vsLeft + vsW - margin);
        var top = Math.Min(Math.Max(savedTop, vsTop - h + margin), vsTop + vsH - margin);

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
    }

    /// <summary>
    ///     将当前窗口位置写入设置（最大化时用还原前外接矩形）。
    /// </summary>
    private void PersistMainWindowPlacement()
    {
        if (WindowState == WindowState.Maximized)
        {
            var rb = RestoreBounds;
            Settings.MainWindowLeft = rb.Left;
            Settings.MainWindowTop = rb.Top;
        }
        else
        {
            Settings.MainWindowLeft = Left;
            Settings.MainWindowTop = Top;
        }
    }

    private void SiteManagerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.SiteManager.CurrentSelectedSite))
        {
            var isCsm = Settings.SiteManager.CurrentSelectedSite.Config?.IsCustomSite == true;
            if (Settings.IsCustomSiteMode == isCsm) return;

            Settings.IsCustomSiteMode = isCsm;
            LayoutRoot.GoElementState(isCsm ? nameof(CustomSitesState) : nameof(DefaultSitesState));
            LogoImage.Visibility = isCsm ? Visibility.Collapsed : Visibility.Visible;
            LogoImage2.Visibility = isCsm ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    
    
    public  async void ShowMessage(string mes, string detailMes = null, Ex.MessagePos pos = Ex.MessagePos.Popup, bool ishighlight =false)
    {
        switch (pos)
        {
            case Ex.MessagePos.Popup:
                PopupMessageTextBlock.Text = mes;
                this.Sb("PopupMessageShowSb").Begin();
                break;
            case Ex.MessagePos.InfoBar:
                StatusTextBlock.Text = mes;
                this.Sb("InfoBarEmphasisSb").Begin();
                break;
            case Ex.MessagePos.Window:
                MessageWindow.ShowDialog(mes, detailMes, this);
                break;
            case Ex.MessagePos.Searching:
                var mesctrl = new SearchMessageControl();
                mesctrl.Set(mes,ishighlight);
                SearchMessageStackPanel.Children.Add(mesctrl);
                mesctrl.ShowOneTime(6);
                await Task.Delay(TimeSpan.FromSeconds(7));
                SearchMessageStackPanel.Children.Remove(mesctrl);
                break;
        }
    }

    private void DownloaderMenuCheckBoxCheckChanged(object sender, RoutedEventArgs e)
    {
        var ischecked = DownloaderMenuCheckBox.IsChecked == true;
        LayoutRoot.GoElementState(ischecked ? nameof(ShowDownloadPanelState) : nameof(HideDownloadPanelState));
    }

    private void ImageSizeSliderOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var v = ImageSizeSlider.Value;
        v += e.Delta / 5d;
        if (v > ImageSizeSlider.Minimum && v < ImageSizeSlider.Maximum) ImageSizeSlider.Value += e.Delta / 5d;
    }


    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1: // 用于测试功能
                ShowMessage("test");
                break;
            case Key.F8:
                if (Settings.SiteManager.R18Check()) SearchControl.MoeSitesLv1ComboBox.SelectedIndex = 0;
                break;
        }
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        PersistMainWindowPlacement();
        Settings.Save(App.SettingJsonFilePath);
        var items = MoeDownloaderControl.Downloader.DownloadItems;
        if (!UnfinishedDownloadMlpub.HasUnfinishedWork(items)) return;

        var body = TryFindResource("TextMainCloseUnfinishedWork") as string
                   ?? "下载队列中仍有未完成的任务（含排队、下载中、失败、停止或取消）。\n\n"
                      + "是：导出未成功任务到 .mlpub 后关闭\n"
                      + "否：直接关闭（不导出）\n"
                      + "取消：不关闭";
        var result = MessageBox.Show(this, body, App.DisplayName, MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == MessageBoxResult.No) return;

        if (!UnfinishedDownloadExportUi.TryExportViaSaveDialog(items, this, out _)) e.Cancel = true;
    }
}