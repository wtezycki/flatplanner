using Flatplanner.Core.Llm;

namespace Flatplanner.Core.Search;

public sealed class IntentInterpreter(LlmJsonClient llm, string promptsDir, string reasoningEffort)
{
    private const int MaxOutputTokens = 1024;

    public async Task<AskIntent> InterpretWithLlmAsync(string message, SearchMeta meta, CancellationToken ct)
    {
        var template = await PromptFiles.LoadAsync(promptsDir, PromptVersions.SearchIntent, ct);
        var systemPrompt = template
            .Replace("{{CITY_LIST}}", string.Join(", ", meta.CitySlugs))
            .Replace("{{DISTRICT_LIST}}", string.Join('\n', meta.Districts.Select(d => $"{d.Key} — {d.Name} ({d.CitySlug})")));

        var intent = await llm.CompleteJsonAsync<AskIntent>(
            systemPrompt, message, SearchIntentSchema.Name, SearchIntentSchema.Build(meta), reasoningEffort, MaxOutputTokens, ct);

        return intent with
        {
            Needs = NonEmpty(intent.Needs).Take(SearchDefinitions.MaxNeeds).ToArray(),
            Declined = intent.Declined.Where(d => !string.IsNullOrWhiteSpace(d.Text)).ToArray(),
            Clarifications = NonEmpty(intent.Clarifications).ToArray(),
        };
    }

    private static IEnumerable<string> NonEmpty(IEnumerable<string> values) =>
        values.Select(v => v.Trim()).Where(v => v.Length > 0);
}
