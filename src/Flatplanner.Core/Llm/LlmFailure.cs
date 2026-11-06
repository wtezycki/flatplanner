namespace Flatplanner.Core.Llm;

public enum LlmFailure
{
    Timeout,
    ApiError,
    Refusal,
    Truncated,
    SchemaMismatch,
}
