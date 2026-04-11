namespace MoeLoaderP.Core;

/// <summary>
///     在条目进入结果集时标记是否历史上已读，并记录本次浏览 ID（与 Delta FilterImg 前半段一致；不含屏蔽已浏览）。
/// </summary>
public static class ViewedItemHelper
{
    public static void ApplyViewedState(MoeItem item)
    {
        if (item.FatherItem != null)
        {
            item.IsViewed = item.FatherItem.IsViewed;
            return;
        }

        var viewed = item.Site.SiteSettings.EnsureViewedRuntime();
        var id = item.Id;
        if (viewed.IsViewed(id))
            item.IsViewed = true;
        else
        {
            item.IsViewed = false;
            viewed.AddViewingId(id);
        }
    }
}
