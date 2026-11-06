namespace Flatplanner.Core.Llm;

public sealed class LlmException(LlmFailure failure, Exception? inner = null)
    : Exception($"LLM call failed: {failure}", inner)
{
    public LlmFailure Failure { get; } = failure;
}
