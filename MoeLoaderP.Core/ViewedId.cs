using System.Collections.Generic;
using System.Text;

namespace MoeLoaderP.Core;

/// <summary>
///     按站点记录已浏览图片 ID（游程压缩），算法与 MoeLoader-Delta ViewedID 一致。
/// </summary>
public class ViewedId
{
    private readonly List<IdRange> _viewedIds = new();
    private readonly List<int> _viewingIds = new();

    public bool IsViewed(int id)
    {
        foreach (var r in _viewedIds)
            if (r.Contains(id))
                return true;
        return false;
    }

    public int ViewedBiggestId { get; private set; }

    public void AddViewingId(int id)
    {
        _viewingIds.Add(id);
    }

    public void AddViewedRange(string rangeStr)
    {
        if (rangeStr.Contains(','))
        {
            var parts = rangeStr.Split(';');
            var last = -1;
            foreach (var part in parts)
            {
                var sp = part.IndexOf(',');
                var start = int.Parse(part.Substring(0, sp));
                var range = int.Parse(part.Substring(sp + 1));

                if (last > -1) start += last;
                _viewedIds.Add(new IdRange(start, range, last == -1));
                last = start + range;

                if (ViewedBiggestId < start + range)
                    ViewedBiggestId = start + range;
            }
        }
        else if (rangeStr.Length > 0)
        {
            var id = int.Parse(rangeStr);
            _viewedIds.Add(new IdRange(0, id, true));
            ViewedBiggestId = id;
        }
    }

    public override string ToString()
    {
        var temp = new List<int>();
        IdRange firstPart = new(0, 0, true);
        foreach (var idr in _viewedIds)
        {
            if (!idr.IsFirst)
                temp.AddRange(idr.ToList());
            else
                firstPart = idr;
        }

        temp.AddRange(_viewingIds);
        temp.Sort();

        if (temp.Count > 1)
        {
            const int maxId = 1000;
            var startIndex = 0;
            if (temp.Count > maxId)
            {
                firstPart.Range = temp[temp.Count - maxId - 1];
                startIndex = temp.Count - maxId;
            }

            var sb = new StringBuilder();
            var last = temp[startIndex];
            var range = 0;
            var lastTrim = firstPart.Range;
            for (var i = startIndex + 1; i < temp.Count; i++)
            {
                if (i < temp.Count - 1 && temp[i] == temp[i + 1])
                    continue;

                if (temp[i] == last + range + 1)
                {
                    range++;
                }
                else if (temp[i] != last)
                {
                    sb.Append(last - lastTrim).Append(',').Append(range).Append(';');
                    lastTrim = last + range;
                    last = temp[i];
                    range = 0;
                }
            }

            return firstPart.Start + "," + firstPart.Range + ";" + sb + (last - lastTrim) + "," + range;
        }

        if (temp.Count == 1)
            return firstPart.Start + "," + firstPart.Range + ";" + (temp[0] - firstPart.Range) + ",0";

        return firstPart.Start + "," + firstPart.Range;
    }

    private sealed class IdRange
    {
        public int Start;
        public int Range;
        public bool IsFirst;

        public IdRange(int start, int range, bool isFirst)
        {
            Start = start;
            Range = range;
            IsFirst = isFirst;
        }

        public bool Contains(int id)
        {
            return id >= Start && id <= Start + Range;
        }

        public int[] ToList()
        {
            var re = new int[Range + 1];
            for (var i = 0; i <= Range; i++)
                re[i] = Start + i;
            return re;
        }
    }
}
