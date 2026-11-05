namespace Flatplanner.Core.Search;

public sealed record NormalizedSearch(SearchIntent Intent, IReadOnlyList<SearchWarning> Warnings);
