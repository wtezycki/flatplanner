using Flatplanner.Api.Chat;

namespace Flatplanner.Tests;

public sealed class ListingJudgeTests
{
    private static readonly IReadOnlyList<long> Ranked = Enumerable.Range(1, 50).Select(i => (long)i).ToList();

    private static Func<long, bool> JudgedUpTo(int last) => id => id <= last;

    [Fact]
    public void NextWindow_NothingJudged_TakesFirstWindow()
    {
        var window = ListingJudge.NextWindow(Ranked, JudgedUpTo(0), more: false);

        Assert.Equal(Ranked.Take(ListingJudge.WindowSize), window);
    }

    [Fact]
    public void NextWindow_More_TakesWindowAfterJudgedPrefix()
    {
        var window = ListingJudge.NextWindow(Ranked, JudgedUpTo(20), more: true);

        Assert.Equal(Ranked.Skip(20).Take(ListingJudge.WindowSize), window);
    }

    [Fact]
    public void NextWindow_PrefixCoversWindow_NoJudgeCall()
    {
        Assert.Empty(ListingJudge.NextWindow(Ranked, JudgedUpTo(20), more: false));
    }

    [Fact]
    public void NextWindow_FailedListing_RetriedWithRestOfWindowAfterIt()
    {
        var window = ListingJudge.NextWindow(Ranked, id => id != 5 && id <= 20, more: false);

        Assert.Equal([5L, 21L, 22L, 23L, 24L], window);
    }

    [Fact]
    public void JudgedPrefix_StopsAtFirstUnjudged()
    {
        Assert.Equal(4, ListingJudge.JudgedPrefix(Ranked, id => id != 5));
    }
}
