using Flatplanner.Api.Chat;
using Flatplanner.Core.Data;

namespace Flatplanner.Tests;

public sealed class ListingJudgeTests
{
    private static Listing Created(long id, DateTime createdAt) => new()
    {
        Id = id,
        CreatedAtSource = createdAt,
        Source = "otodom",
        ExternalId = id.ToString(),
        Url = "https://example.test",
        Title = "t",
        DescriptionText = "d",
        SearchText = "s",
        CitySlug = "gdansk",
        Market = "secondary",
    };

    [Fact]
    public void Newest_MoreListingsThanLimit_KeepsTheNewestOnes()
    {
        var start = new DateTime(2026, 1, 1);
        var listings = Enumerable.Range(1, ListingJudge.MaxCandidates + 10)
            .Select(i => Created(i, start.AddDays(i)))
            .ToList();

        var result = ListingJudge.Newest(listings);

        Assert.Equal(ListingJudge.MaxCandidates, result.Count);
        Assert.Equal(ListingJudge.MaxCandidates + 10, result[0].Id);
        Assert.DoesNotContain(result, l => l.Id <= 10);
    }

    [Fact]
    public void Newest_SameCreationDate_BreaksTieByHigherId()
    {
        var date = new DateTime(2026, 1, 1);

        var result = ListingJudge.Newest([Created(1, date), Created(3, date), Created(2, date)]);

        Assert.Equal([3L, 2L, 1L], result.Select(l => l.Id));
    }
}
