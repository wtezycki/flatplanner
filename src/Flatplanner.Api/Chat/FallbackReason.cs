using Flatplanner.Core.Llm;

namespace Flatplanner.Api.Chat;

public static class FallbackReason
{
    public const string Timeout = "timeout";
    public const string LlmError = "llm_error";
    public const string SchemaMismatch = "schema_mismatch";
    public const string Refusal = "refusal";
    public const string Disabled = "disabled";

    public static string From(LlmFailure failure) => failure switch
    {
        LlmFailure.Timeout => Timeout,
        LlmFailure.ApiError => LlmError,
        LlmFailure.Refusal => Refusal,
        _ => SchemaMismatch,
    };
}
