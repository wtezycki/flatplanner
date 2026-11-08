using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public sealed record AskInterpretation(AskIntent Asked, SearchIntent Intent, IReadOnlyList<AskNotice> Notices, string? FallbackReason);
