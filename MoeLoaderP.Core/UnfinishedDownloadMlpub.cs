using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MoeLoaderP.Core.Sites;
using Newtonsoft.Json;

namespace MoeLoaderP.Core;

/// <summary>
///     未成功下载任务包（.mlpub）的 schema 常量与序列化 / 导入导出。
/// </summary>
public static class UnfinishedDownloadMlpub
{
    public const string Schema = "MoeLoaderP.UnfinishedDownloadTasks";
    public const int Version = 1;
    public const string FileExtension = ".mlpub";

    public static bool IsUnfinishedStatus(DownloadStatus s) =>
        s is DownloadStatus.Failed or DownloadStatus.Downloading or DownloadStatus.WaitForDownload
            or DownloadStatus.Stop or DownloadStatus.Cancel;

    /// <summary>
    ///     关闭确认：队列中是否存在「未成功」项（含多图父项）。
    /// </summary>
    public static bool HasUnfinishedWork(MoeItems items) =>
        items.Any(i => IsUnfinishedStatus(i.DlStatus));

    /// <summary>
    ///     可写入 .mlpub 的项：未成功、无子项、且具备可序列化的 http(s) 下载 URL。
    /// </summary>
    public static List<MoeItem> CollectExportableItems(MoeItems downloadItems)
    {
        var list = new List<MoeItem>();
        foreach (var item in downloadItems)
        {
            if (item.ChildrenItems.Count > 0) continue;
            if (!IsUnfinishedStatus(item.DlStatus)) continue;
            var url = item.DownloadUrlInfo?.Url?.Trim();
            if (string.IsNullOrEmpty(url) || !IsAllowedDownloadUrl(url)) continue;
            list.Add(item);
        }

        return list;
    }

    public static bool IsAllowedDownloadUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)) return false;
        return u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps;
    }

    public static UnfinishedDownloadBundleDto BuildBundleFromItems(IEnumerable<MoeItem> items, DateTime? exportedAtUtc = null)
    {
        var utc = exportedAtUtc ?? DateTime.UtcNow;
        var dto = new UnfinishedDownloadBundleDto
        {
            Schema = Schema,
            Version = Version,
            ExportedAt = utc.ToString("o", CultureInfo.InvariantCulture),
            Tasks = new List<UnfinishedTaskRecordDto>()
        };
        foreach (var item in items)
        {
            var url = item.DownloadUrlInfo?.Url?.Trim();
            if (string.IsNullOrEmpty(url) || !IsAllowedDownloadUrl(url)) continue;
            dto.Tasks.Add(new UnfinishedTaskRecordDto
            {
                SiteShortName = item.Site.ShortName,
                DownloadUrl = url,
                Referer = item.DownloadUrlInfo?.Referer,
                DetailUrl = item.DetailUrl,
                Id = item.Sid.IsEmpty() ? item.Id.ToString(CultureInfo.InvariantCulture) : item.Sid,
                Title = item.Title,
                LocalFileShortNameWithoutExt = item.LocalFileShortNameWithoutExt,
                DlStatusAtExport = item.DlStatus.ToString()
            });
        }

        return dto;
    }

    public static string SerializeBundle(UnfinishedDownloadBundleDto dto) =>
        JsonConvert.SerializeObject(dto, Formatting.None);

    public static bool TryParseBundle(string json, out UnfinishedDownloadBundleDto bundle, out string error)
    {
        bundle = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "内容为空";
            return false;
        }

        try
        {
            bundle = JsonConvert.DeserializeObject<UnfinishedDownloadBundleDto>(json);
        }
        catch (Exception ex)
        {
            error = $"JSON 解析失败: {ex.Message}";
            return false;
        }

        if (bundle == null)
        {
            error = "JSON 根对象无效";
            return false;
        }

        if (!string.Equals(bundle.Schema, Schema, StringComparison.Ordinal))
        {
            error = $"不支持的 schema: {bundle.Schema}";
            return false;
        }

        if (bundle.Version != Version)
        {
            error = $"不支持的 version: {bundle.Version}";
            return false;
        }

        bundle.Tasks ??= new List<UnfinishedTaskRecordDto>();
        return true;
    }

    public static string DedupeKey(UnfinishedTaskRecordDto t)
    {
        var site = (t.SiteShortName ?? "").Trim();
        var url = (t.DownloadUrl ?? "").Trim();
        if (!string.IsNullOrEmpty(url)) return site + "\0" + url;
        var id = t.Id ?? "";
        return site + "\0" + id;
    }

    public static void MergeTasksInto(UnfinishedDownloadBundleDto target, IEnumerable<UnfinishedTaskRecordDto> incoming)
    {
        var keys = new HashSet<string>(target.Tasks.Select(DedupeKey));
        foreach (var t in incoming)
        {
            var k = DedupeKey(t);
            if (keys.Contains(k)) continue;
            keys.Add(k);
            target.Tasks.Add(t);
        }
    }

    /// <summary>
    ///     写入导出：若 path 已存在且为合法包则合并 tasks 后写回；否则整文件写入新包。
    /// </summary>
    public static bool TryWriteExport(IReadOnlyList<MoeItem> exportableItems, string path, out string error)
    {
        error = null;
        try
        {
            var fresh = BuildBundleFromItems(exportableItems);
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (!TryParseBundle(text, out var existing, out error)) return false;
                MergeTasksInto(existing, fresh.Tasks);
                existing.ExportedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                File.WriteAllText(path, SerializeBundle(existing), new UTF8Encoding(false));
            }
            else
            {
                File.WriteAllText(path, SerializeBundle(fresh), new UTF8Encoding(false));
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    ///     从文件解析并返回任务列表（不校验单条 URL，导入时再过滤）。
    /// </summary>
    public static bool TryReadTasksFromFile(string path, out List<UnfinishedTaskRecordDto> tasks, out string error)
    {
        tasks = null;
        error = null;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            if (!TryParseBundle(json, out var bundle, out error)) return false;
            tasks = bundle.Tasks;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    ///     由一条记录构造可入队的 <see cref="MoeItem"/>；站点未知或 URL 非法时返回 null。
    /// </summary>
    public static MoeItem TryCreateItemFromRecord(UnfinishedTaskRecordDto r, Settings settings, out string skipReason)
    {
        skipReason = null;
        var siteName = (r.SiteShortName ?? "").Trim();
        if (siteName.Length == 0)
        {
            skipReason = "缺少 siteShortName";
            return null;
        }

        var site = settings.SiteManager?.GetSiteByShortName(siteName);
        if (site == null)
        {
            skipReason = $"未知站点: {siteName}";
            return null;
        }

        var url = (r.DownloadUrl ?? "").Trim();
        if (!IsAllowedDownloadUrl(url))
        {
            skipReason = "downloadUrl 非法或为空";
            return null;
        }

        var para = new SearchPara
        {
            Site = site,
            Keyword = string.Empty,
            Config = site.Config,
            CountLimit = 40
        };

        var session = new SearchSession(para);
        session.CurrentDownloadType = session.ResultDownloadTypes.FirstOrDefault(dt => dt.Type == DownloadTypeEnum.Auto)
                                      ?? session.ResultDownloadTypes.FirstOrDefault();
        if (session.CurrentDownloadType == null)
        {
            skipReason = "无法确定下载类型";
            return null;
        }

        var item = new MoeItem(site, para)
        {
            DetailUrl = r.DetailUrl ?? string.Empty,
            Title = r.Title ?? string.Empty,
            LocalFileShortNameWithoutExt = r.LocalFileShortNameWithoutExt ?? string.Empty,
            DlStatus = DownloadStatus.WaitForDownload
        };

        if (!string.IsNullOrEmpty(r.Id))
        {
            item.Sid = r.Id;
            if (int.TryParse(r.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idInt))
                item.Id = idInt;
        }

        item.Urls.Add(DownloadTypeEnum.Origin, url, r.Referer);
        return item;
    }

    /// <summary>
    ///     将多个 .mlpub 文件中的任务追加到下载器。
    /// </summary>
    public static (int added, IReadOnlyList<string> errors) ImportFromMlpubFiles(
        IEnumerable<string> filePaths,
        MoeDownloader downloader,
        Settings settings)
    {
        var added = 0;
        var errors = new List<string>();
        foreach (var path in filePaths)
        {
            if (!path.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)) continue;
            if (!TryReadTasksFromFile(path, out var tasks, out var err))
            {
                errors.Add($"{Path.GetFileName(path)}: {err}");
                continue;
            }

            foreach (var t in tasks)
            {
                var item = TryCreateItemFromRecord(t, settings, out var skip);
                if (item == null)
                {
                    errors.Add($"{Path.GetFileName(path)} 任务: {skip}");
                    continue;
                }

                downloader.AddDownload(item, null);
                added++;
            }
        }

        return (added, errors);
    }
}

public sealed class UnfinishedDownloadBundleDto
{
    [JsonProperty("schema")] public string Schema { get; set; }

    [JsonProperty("version")] public int Version { get; set; }

    [JsonProperty("exportedAt")] public string ExportedAt { get; set; }

    [JsonProperty("tasks")] public List<UnfinishedTaskRecordDto> Tasks { get; set; }
}

public sealed class UnfinishedTaskRecordDto
{
    [JsonProperty("siteShortName", Required = Required.Default)]
    public string SiteShortName { get; set; }

    [JsonProperty("downloadUrl", Required = Required.Default)]
    public string DownloadUrl { get; set; }

    [JsonProperty("referer", Required = Required.Default)]
    public string Referer { get; set; }

    [JsonProperty("detailUrl", Required = Required.Default)]
    public string DetailUrl { get; set; }

    [JsonProperty("id", Required = Required.Default)]
    public string Id { get; set; }

    [JsonProperty("title", Required = Required.Default)]
    public string Title { get; set; }

    [JsonProperty("localFileShortNameWithoutExt", Required = Required.Default)]
    public string LocalFileShortNameWithoutExt { get; set; }

    [JsonProperty("dlStatusAtExport", Required = Required.Default)]
    public string DlStatusAtExport { get; set; }
}
