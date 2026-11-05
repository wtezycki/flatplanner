using Flatplanner.Core.Search;

namespace Flatplanner.Api.Listings;

public sealed record Interpretation(string Summary, IReadOnlyList<Chip> Chips, IReadOnlyList<SearchWarning> Warnings);
