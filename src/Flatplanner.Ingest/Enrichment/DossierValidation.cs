using System.Text.Json;
using Flatplanner.Core.Data;
using Flatplanner.Core.Text;

namespace Flatplanner.Ingest.Enrichment;

public sealed class DossierValidation
{
    private const int MaxSummaryLength = 1_000;

    public Dictionary<string, Dossier> Valid { get; } = [];

    public List<DroppedRisk> DroppedRisks { get; } = [];

    public List<string> InvalidLines { get; } = [];

    public static DossierValidation Run(IEnumerable<string> lines, IReadOnlyDictionary<string, string> descriptions)
    {
        var validation = new DossierValidation();
        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (!string.IsNullOrWhiteSpace(line))
            {
                validation.Add(lineNumber, line, descriptions);
            }
        }

        return validation;
    }

    private void Add(int lineNumber, string line, IReadOnlyDictionary<string, string> descriptions)
    {
        DossierLine? dossier;
        try
        {
            dossier = JsonSerializer.Deserialize<DossierLine>(line, DossierJson.Options);
        }
        catch (JsonException)
        {
            InvalidLines.Add($"{lineNumber}: invalid_json");
            return;
        }

        if (dossier?.ExternalId is not { } externalId || !descriptions.TryGetValue(externalId, out var description))
        {
            InvalidLines.Add($"{lineNumber}: unknown_external_id");
            return;
        }

        if (string.IsNullOrWhiteSpace(dossier.Summary) || dossier.Summary.Length > MaxSummaryLength)
        {
            InvalidLines.Add($"{lineNumber}: invalid_summary");
            return;
        }

        if (Valid.ContainsKey(externalId))
        {
            InvalidLines.Add($"{lineNumber}: duplicate_external_id");
            return;
        }

        var normalizedDescription = AnchorText.Normalize(description);
        var accepted = new List<ListingRisk>();
        foreach (var risk in dossier.Risks ?? [])
        {
            if (AnchorValidator.Validate(risk, normalizedDescription) is { } reason)
            {
                DroppedRisks.Add(new DroppedRisk(externalId, risk.Kind, reason));
            }
            else
            {
                accepted.Add(risk);
            }
        }

        Valid[externalId] = new Dossier(dossier.Summary, accepted);
    }
}
