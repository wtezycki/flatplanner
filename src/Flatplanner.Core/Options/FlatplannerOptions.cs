using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Flatplanner.Core.Options;

public sealed class FlatplannerOptions
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

    [ConfigurationKeyName("DB_MIGRATE_ON_STARTUP")]
    public bool DbMigrateOnStartup { get; set; } = true;
}
