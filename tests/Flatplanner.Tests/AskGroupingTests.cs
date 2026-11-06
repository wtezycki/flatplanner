using Flatplanner.Api.Chat;

namespace Flatplanner.Tests;

public sealed class AskGroupingTests
{
    private const string Y = Verdict.Yes;
    private const string P = Verdict.Partly;
    private const string N = Verdict.No;
    private const string U = Verdict.Unknown;

    private static readonly HashSet<int> NoneExcluded = [];

    [Theory]
    [InlineData(new[] { Y, Y }, ResultGroup.Full)]
    [InlineData(new[] { Y, P }, ResultGroup.Partial)]
    [InlineData(new[] { U, P }, ResultGroup.Partial)]
    [InlineData(new[] { Y, N }, ResultGroup.Near)]
    [InlineData(new[] { U, N }, ResultGroup.Near)]
    [InlineData(new[] { U, U }, ResultGroup.Unknown)]
    [InlineData(new[] { N, N }, null)]
    [InlineData(new[] { N }, null)]
    public void Arrange_VerdictTable_Group(string[] verdicts, string? expected)
    {
        var arranged = AskGrouping.Arrange([Row(1, 0, verdicts)], NoneExcluded);

        Assert.Equal(expected, arranged.SingleOrDefault()?.Group);
    }

    [Fact]
    public void Arrange_Rows_GroupThenMoreYesThenFewerUnknownThenNewest()
    {
        VerdictRow[] rows =
        [
            Row(1, 5, [U, U, U]),
            Row(2, 4, [Y, N, Y]),
            Row(3, 3, [Y, P, U]),
            Row(4, 2, [Y, P, P]),
            Row(5, 1, [Y, Y, P]),
            Row(6, 9, [Y, P, P]),
            Row(7, 0, [Y, Y, Y]),
        ];

        var arranged = AskGrouping.Arrange(rows, NoneExcluded);

        Assert.Equal([7, 5, 6, 4, 3, 2, 1], arranged.Select(r => r.Row.Id));
    }

    [Fact]
    public void Arrange_ExcludedNeed_Ignored()
    {
        var arranged = AskGrouping.Arrange([Row(1, 0, [Y, N])], new HashSet<int> { 1 });

        Assert.Equal(ResultGroup.Full, Assert.Single(arranged).Group);
    }

    [Fact]
    public void Relaxations_NoFull_CountPerNeedDescending()
    {
        string[] needs = ["szkoła", "cisza", "rower"];
        VerdictRow[] rows =
        [
            Row(1, 3, [Y, N, Y]),
            Row(2, 2, [N, N, Y]),
            Row(3, 1, [Y, N, P]),
            Row(4, 0, [Y, Y, N]),
        ];

        var relaxations = AskGrouping.Relaxations(needs, rows, NoneExcluded);

        Assert.Equal(
            [new Relaxation("cisza", 2, 1), new Relaxation("rower", 1, 4)],
            relaxations);
    }

    [Fact]
    public void Relaxations_FullExists_Empty()
    {
        var relaxations = AskGrouping.Relaxations(["szkoła"], [Row(1, 0, [Y]), Row(2, 0, [N])], NoneExcluded);

        Assert.Empty(relaxations);
    }

    private static VerdictRow Row(long id, int day, string[] verdicts) =>
        new(id, new DateTime(2026, 11, 1).AddDays(day), verdicts);
}
