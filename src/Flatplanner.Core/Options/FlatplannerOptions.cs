using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Flatplanner.Core.Options;

public sealed class FlatplannerOptions : IValidatableObject
{
    [Required]
    [ConfigurationKeyName("ConnectionStrings:Default")]
    public string ConnectionString { get; set; } = "";

    [AllowedValues(Data.ChatMode.Llm, Data.ChatMode.Fallback)]
    [ConfigurationKeyName("CHAT_MODE")]
    public string ChatMode { get; set; } = Data.ChatMode.Fallback;

    [Range(1, 10_000)]
    [ConfigurationKeyName("CHAT_MAX_LENGTH")]
    public int ChatMaxLength { get; set; } = 300;

    [Range(1, 60)]
    [ConfigurationKeyName("CHAT_TIMEOUT_SECONDS")]
    public int ChatTimeoutSeconds { get; set; } = 5;

    [ConfigurationKeyName("OPENAI_API_KEY")]
    public string OpenAiApiKey { get; set; } = "";

    [Required]
    [ConfigurationKeyName("LLM_MODEL")]
    public string LlmModel { get; set; } = "gpt-6.1-terra";

    [Required]
    [ConfigurationKeyName("LLM_CHAT_REASONING_EFFORT")]
    public string LlmChatReasoningEffort { get; set; } = "none";

    [Required]
    [ConfigurationKeyName("PROMPTS_DIR")]
    public string PromptsDir { get; set; } = "prompts";

    [ConfigurationKeyName("DB_MIGRATE_ON_STARTUP")]
    public bool DbMigrateOnStartup { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChatMode == Data.ChatMode.Llm && string.IsNullOrWhiteSpace(OpenAiApiKey))
        {
            yield return new ValidationResult("OPENAI_API_KEY is required when CHAT_MODE=llm.", [nameof(OpenAiApiKey)]);
        }
    }
}
