using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core;

/// <summary>
///     用于存储设置、绑定及运行时参数传递（整个软件）。可保存为json文件
/// </summary>
public class Settings : BindingObject
{
    // runtime
    private SearchSession _currentSession;

    [JsonIgnore]
    public SearchSession CurrentSession
    {
        get => _currentSession;
        set => SetField(ref _currentSession, value, nameof(CurrentSession));
    }
     
    [JsonIgnore] public SiteManager SiteManager { get; set; }

    [JsonIgnore]
    public string CustomSitesDir
    {
        get
        {
            if (!Directory.Exists(_customSitesDir)) Directory.CreateDirectory(_customSitesDir);
            return _customSitesDir;
        }
        set => _customSitesDir = value;
    }

    #region Window size / Display

    private double _logWindowWidth = 500d;

    public double LogWindowWidth
    {
        get => _logWindowWidth;
        set => SetField(ref _logWindowWidth, value, nameof(LogWindowWidth));
    }

    private double _logWindowHeight = 350d;

    public double LogWindowHeight
    {
        get => _logWindowHeight;
        set => SetField(ref _logWindowHeight, value, nameof(LogWindowHeight));
    }

    private bool _isLowPerformanceMode;

    public bool IsLowPerformanceMode
    {
        get => _isLowPerformanceMode;
        set => SetField(ref _isLowPerformanceMode, value, nameof(IsLowPerformanceMode));
    }

    private double _mainWindowWidth = 1060d;

    public double MainWindowWidth
    {
        get => _mainWindowWidth;
        set => SetField(ref _mainWindowWidth, value, nameof(MainWindowWidth));
    }

    private double _mainWindowHeight = 760d;

    public double MainWindowHeight
    {
        get => _mainWindowHeight;
        set => SetField(ref _mainWindowHeight, value, nameof(MainWindowHeight));
    }

    /// <summary>
    ///     主窗口上次关闭时的左坐标（DIP）；未保存过则为 null，启动时居中。
    /// </summary>
    private double? _mainWindowLeft;

    public double? MainWindowLeft
    {
        get => _mainWindowLeft;
        set => SetField(ref _mainWindowLeft, value, nameof(MainWindowLeft));
    }

    /// <summary>
    ///     主窗口上次关闭时的顶坐标（DIP）；未保存过则为 null。
    /// </summary>
    private double? _mainWindowTop;

    public double? MainWindowTop
    {
        get => _mainWindowTop;
        set => SetField(ref _mainWindowTop, value, nameof(MainWindowTop));
    }

    private bool _isShowBgImage = true;

    public bool IsShowBgImage
    {
        get => _isShowBgImage;
        set => SetField(ref _isShowBgImage, value, nameof(IsShowBgImage));
    }

    private bool _isEnableAcrylicStyle = true;

    public bool IsEnableAcrylicStyle
    {
        get => _isEnableAcrylicStyle;
        set => SetField(ref _isEnableAcrylicStyle, value, nameof(IsEnableAcrylicStyle));
    }

    private bool _isShowEggWindowOnce;

    public bool IsShowEggWindowOnce
    {
        get => _isShowEggWindowOnce;
        set => SetField(ref _isShowEggWindowOnce, value, nameof(IsShowEggWindowOnce));
    }

    #endregion

    #region Searching Settings

    private int _preLoadPagesCount;

    public int PreLoadPagesCount
    {
        get => _preLoadPagesCount;
        set => SetField(ref _preLoadPagesCount, value, nameof(PreLoadPagesCount));
    }

    private int _maxOnLoadingImageCount = 8;

    public int MaxOnLoadingImageCount
    {
        get => _maxOnLoadingImageCount;
        set => SetField(ref _maxOnLoadingImageCount, value, nameof(MaxOnLoadingImageCount));
    }

    private int _maxOnDownloadingImageCount = 3;

    public int MaxOnDownloadingImageCount
    {
        get => _maxOnDownloadingImageCount;
        set => SetField(ref _maxOnDownloadingImageCount, value, nameof(MaxOnDownloadingImageCount));
    }

    private double _imageItemControlSize = 192d;

    public double ImageItemControlSize
    {
        get => _imageItemControlSize;
        set => SetField(ref _imageItemControlSize, value, nameof(ImageItemControlSize));
    }

    private int _historyKeywordsMaxCount = 25;

    public int HistoryKeywordsMaxCount
    {
        get => _historyKeywordsMaxCount;
        set => SetField(ref _historyKeywordsMaxCount, value, nameof(HistoryKeywordsMaxCount));
    }

    private bool _isClearImagesWhenSearchNextPage = true;

    public bool IsClearImagesWhenSearchNextPage
    {
        get => _isClearImagesWhenSearchNextPage;
        set => SetField(ref _isClearImagesWhenSearchNextPage, value, nameof(IsClearImagesWhenSearchNextPage));
    }

    /// <summary>
    ///     仅影响当前页缩略图展示：为 true 时不向面板添加已读项（不改变 FilterCount / 翻页逻辑）。
    /// </summary>
    private bool _maskViewedInSearch;

    public bool MaskViewedInSearch
    {
        get => _maskViewedInSearch;
        set => SetField(ref _maskViewedInSearch, value, nameof(MaskViewedInSearch));
    }

    #endregion

    #region Download Settings

    private int _downloadFirstSeveralCount = 1;

    public int DownloadFirstSeveralCount
    {
        get => _downloadFirstSeveralCount;
        set => SetField(ref _downloadFirstSeveralCount, value, nameof(DownloadFirstSeveralCount));
    }

    private bool _isDownloadFirstSeveral;

    public bool IsDownloadFirstSeveral
    {
        get => _isDownloadFirstSeveral;
        set => SetField(ref _isDownloadFirstSeveral, value, nameof(IsDownloadFirstSeveral));
    }

    private string _imageSavePath /* = App.MoePicFolder*/;

    public string ImageSavePath
    {
        get => _imageSavePath;
        set => SetField(ref _imageSavePath, value, nameof(ImageSavePath));
    }

    public const string SaveFileNameFormatDefaultValue = "%site %id %title";
    private string _saveFileNameFormat = SaveFileNameFormatDefaultValue;

    public string SaveFileNameFormat
    {
        get => _saveFileNameFormat;
        set => SetField(ref _saveFileNameFormat, value, nameof(SaveFileNameFormat));
    }

    public const string SortFolderNameFormatDefaultValue = "%site\\%title";
    private string _sortFolderNameFormat = SortFolderNameFormatDefaultValue;

    public string SortFolderNameFormat
    {
        get => _sortFolderNameFormat;
        set => SetField(ref _sortFolderNameFormat, value, nameof(SortFolderNameFormat));
    }

    private bool _isAutoRenameWhenSame;

    public bool IsAutoRenameWhenSame
    {
        get => _isAutoRenameWhenSame;
        set => SetField(ref _isAutoRenameWhenSame, value, nameof(IsAutoRenameWhenSame));
    }

    private int _nameFormatTagCount;

    public int NameFormatTagCount
    {
        get => _nameFormatTagCount;
        set => SetField(ref _nameFormatTagCount, value, nameof(NameFormatTagCount));
    }

    #endregion

    #region Proxy Settings

    public enum ProxyModeEnum
    {
        None = 0,
        Custom = 1,
        Ie = 2,
        Default = 3,
    }

    public enum ProxyConnectModeEnum
    {
        Http,
        Socks
    }

    private ProxyModeEnum _proxyMode = ProxyModeEnum.None;

    public ProxyModeEnum ProxyMode
    {
        get => _proxyMode;
        set => SetField(ref _proxyMode, value, nameof(ProxyMode));
    }

    private ProxyConnectModeEnum _proxyConnectMode = ProxyConnectModeEnum.Http;
    public ProxyConnectModeEnum ProxyConnectMode
    {
        get => _proxyConnectMode;
        set => SetField(ref _proxyConnectMode, value, nameof(ProxyMode));
    }

    private string _proxySetting = "127.0.0.1:1080";

    public string ProxySetting
    {
        get => _proxySetting;
        set => SetField(ref _proxySetting, value, nameof(ProxySetting));
    }

    #endregion

    #region R18 Mode Setting

    private bool _isXMode;

    public bool IsXMode
    {
        get => _isXMode;
        set => SetField(ref _isXMode, value, nameof(IsXMode));
    }

    private bool _haveEnteredXMode;

    public bool HaveEnteredXMode
    {
        get => _haveEnteredXMode;
        set => SetField(ref _haveEnteredXMode, value, nameof(HaveEnteredXMode));
    }

    private bool _isDisplayExplicitImages = true;


    public bool IsDisplayExplicitImages
    {
        get => _isDisplayExplicitImages;
        set => SetField(ref _isDisplayExplicitImages, value, nameof(IsDisplayExplicitImages));
    }

    #endregion

    #region MoeSites Settings

    /// <summary>
    ///     每一个站点分别的设置（所有站点集合）
    /// </summary>
    public MoeSitesSettings AllSitesSettings { get; set; } = new();


    private bool _isCustomSiteMode;
    private string _customSitesDir;

    [JsonIgnore]
    public bool IsCustomSiteMode
    {
        get => _isCustomSiteMode;
        set => SetField(ref _isCustomSiteMode, value, nameof(IsCustomSiteMode));
    }

    #endregion

    #region Save&Load Func

    public void Save(string jsonPath)
    {
        foreach (var kv in AllSitesSettings) kv.Value.PersistViewedRuntimeIfDirty();

        // 保存 json
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(jsonPath, json);
    }

    public static Settings Load(string jsonPath)
    {
        Settings settings;
        try
        {
            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            else
            {
                settings = new Settings();
            }
        }
        catch (Exception ex)
        {
            Ex.ShowMessage("设置读取失败，将读取默认设置", null, Ex.MessagePos.Window);
            Ex.Log(ex);
            settings = new Settings();
        }

        return settings;
    }

    #endregion
}

#region Settings Helper Class

public class MoeSitesSettings : Dictionary<string, IndividualSiteSettings>
{
    public IndividualSiteSettings GetSettings(MoeSite site)
    {
        if (!ContainsKey(site.ShortName)) Add(site.ShortName, new IndividualSiteSettings());
        var set = this[site.ShortName];
        return set;
    }
}

public class IndividualSiteSettings : BindingObject
{
    private Dictionary<string, string> _items = new();

    private CookieCollection _loginCookies;

    public Dictionary<string, string> Items
    {
        get => _items;
        set => SetField(ref _items, value, nameof(Items));
    }

    public CookieCollection LoginCookies
    {
        get => _loginCookies;
        set => SetField(ref _loginCookies, value, nameof(LoginCookies));
    }

    public DateTime? LoginExpiresTime { get; set; }

    public AutoHintItems History { get; set; } = new();

    public Settings.ProxyModeEnum SiteProxy { get; set; } = Settings.ProxyModeEnum.Default;

    private string _viewedIdsEncoded;

    /// <summary>
    ///     已浏览 ID 游程编码串（按站点一份）。
    /// </summary>
    public string ViewedIdsEncoded
    {
        get => _viewedIdsEncoded;
        set
        {
            if (SetField(ref _viewedIdsEncoded, value, nameof(ViewedIdsEncoded)))
                _viewedRuntimeLoaded = false;
        }
    }

    [JsonIgnore] private ViewedId _viewedRuntime;

    [JsonIgnore] private bool _viewedRuntimeLoaded;

    /// <summary>
    ///     获取或创建内存中的已读集合（懒加载自 <see cref="ViewedIdsEncoded"/>）。
    /// </summary>
    public ViewedId EnsureViewedRuntime()
    {
        if (!_viewedRuntimeLoaded)
        {
            _viewedRuntime = new ViewedId();
            if (!string.IsNullOrWhiteSpace(_viewedIdsEncoded))
                _viewedRuntime.AddViewedRange(_viewedIdsEncoded);
            _viewedRuntimeLoaded = true;
        }

        return _viewedRuntime;
    }

    /// <summary>
    ///     将内存已读写回 <see cref="ViewedIdsEncoded"/>，供 Save 序列化。
    /// </summary>
    public void PersistViewedRuntimeIfDirty()
    {
        if (!_viewedRuntimeLoaded || _viewedRuntime == null) return;
        var s = _viewedRuntime.ToString();
        if (s == _viewedIdsEncoded) return;
        _viewedIdsEncoded = s;
        OnPropertyChanged(nameof(ViewedIdsEncoded));
    }

    public string GetSetting(string key)
    {
        return Items.ContainsKey(key) ? Items[key] : null;
    }

    public void SetSetting(string key, string value)
    {
        if (Items.ContainsKey(key))
            Items[key] = value;
        else
            Items.TryAdd(key, value);

        Ex.Log($"Add Key:{key} Value:{value} ");
    }

    public CookieContainer GetCookieContainer()
    {
        if (!(LoginCookies?.Count > 0)) return null;
        var cc = new CookieContainer();
        foreach (var cookie in LoginCookies.Cast<Cookie>()) cc.Add(cookie);
        return cc;
    }
}


/// <summary>
///     实现绑定所需的属性值变更通知接口
/// </summary>
public class BindingObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetField<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

#endregion