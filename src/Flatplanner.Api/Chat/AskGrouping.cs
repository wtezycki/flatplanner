namespace Flatplanner.Api.Chat;

public static class AskGrouping
{
    public static IReadOnlyList<GroupedRow> Arrange(IReadOnlyList<VerdictRow> rows, IReadOnlySet<int> excluded)
    {
        return rows
            .Select(row => (Row: row, Verdicts: Active(row.Verdicts, excluded)))
            .Select(r => (r.Row, r.Verdicts, Group: Group(r.Verdicts)))
            .Where(r => r.Group is not null)
            .OrderBy(r => Array.IndexOf(ResultGroup.Ordered, r.Group))
            .ThenByDescending(r => r.Verdicts.Count(v => v == Verdict.Yes))
            .ThenBy(r => r.Verdicts.Count(v => v == Verdict.Unknown))
            .ThenByDescending(r => r.Row.CreatedAtSource)
            .ThenByDescending(r => r.Row.Id)
            .Select(r => new GroupedRow(r.Row, r.Group!))
            .ToList();
    }

    public static IReadOnlyList<Relaxation> Relaxations(
        IReadOnlyList<string> needs,
        IReadOnlyList<VerdictRow> rows,
        IReadOnlySet<int> excluded)
    {
        if (Arrange(rows, excluded).Any(r => r.Group == ResultGroup.Full))
        {
            return [];
        }

        return Enumerable.Range(0, needs.Count)
            .Where(i => !excluded.Contains(i))
            .Select(i =>
            {
                var matching = Arrange(rows, new HashSet<int>(excluded) { i })
                    .Where(r => r.Group is ResultGroup.Full or ResultGroup.Partial)
                    .ToList();
                return new Relaxation(needs[i], matching.Count, matching.FirstOrDefault()?.Row.Id);
            })
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    private static string? Group(IReadOnlyList<string> verdicts)
    {
        var noCount = verdicts.Count(v => v == Verdict.No);
        if (verdicts.All(v => v == Verdict.Yes))
        {
            return ResultGroup.Full;
        }

        if (noCount == 1 && verdicts.Count > 1)
        {
            return ResultGroup.Near;
        }

        if (noCount > 0)
        {
            return null;
        }

        return verdicts.Any(v => v is Verdict.Yes or Verdict.Partly) ? ResultGroup.Partial : ResultGroup.Unknown;
    }

    private static IReadOnlyList<string> Active(IReadOnlyList<string> verdicts, IReadOnlySet<int> excluded) =>
        verdicts.Where((_, i) => !excluded.Contains(i)).ToList();
}
