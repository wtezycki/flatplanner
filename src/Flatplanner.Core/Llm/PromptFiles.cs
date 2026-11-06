namespace Flatplanner.Core.Llm;

public static class PromptFiles
{
    public static Task<string> LoadAsync(string promptsDir, string version, CancellationToken ct) =>
        File.ReadAllTextAsync(Path.Combine(promptsDir, $"{version}.md"), ct);
}
