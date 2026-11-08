using Flatplanner.Core.Search;

namespace Flatplanner.Api.Chat;

public static class AskValidation
{
    public static Dictionary<string, string[]>? Validate(string message, IReadOnlyList<string> history, int maxLength)
    {
        var errors = new Dictionary<string, string[]>();
        if (message.Length == 0 || message.Length > maxLength)
        {
            errors["message"] = [$"Wpisz od 1 do {maxLength} znaków."];
        }

        if (history.Count > SearchDefinitions.MaxHistory)
        {
            errors["history"] = [$"Rozmowa może mieć najwyżej {SearchDefinitions.MaxTurns} pytań."];
        }
        else if (history.Any(entry => entry.Length == 0 || entry.Length > maxLength))
        {
            errors["history"] = [$"Wpisz od 1 do {maxLength} znaków."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
