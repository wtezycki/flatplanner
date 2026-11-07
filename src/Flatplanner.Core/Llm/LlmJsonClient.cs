using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace Flatplanner.Core.Llm;

public sealed class LlmJsonClient
{
    // Case-insensitive camelCase: the default options would silently leave every property empty (design 7.7).
    public static readonly JsonSerializerOptions LlmJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    private readonly Lazy<ChatClient> client;
    private readonly TimeSpan timeout;
    private readonly ILogger<LlmJsonClient> logger;

    // The client is created on first use, so CHAT_MODE=fallback runs without an API key.
    public LlmJsonClient(string model, string apiKey, TimeSpan timeout, int maxRetries, ILogger<LlmJsonClient> logger)
    {
        this.timeout = timeout;
        this.logger = logger;
        client = new Lazy<ChatClient>(() => new ChatClient(
            model,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { NetworkTimeout = timeout, RetryPolicy = new ClientRetryPolicy(maxRetries) }));
    }

    public async Task<T> CompleteJsonAsync<T>(
        string systemPrompt,
        string userText,
        string schemaName,
        string schemaJson,
        string reasoningEffort,
        int maxOutputTokens,
        CancellationToken ct)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: schemaName,
                jsonSchema: BinaryData.FromString(schemaJson),
                jsonSchemaIsStrict: true),
            ReasoningEffortLevel = new ChatReasoningEffortLevel(reasoningEffort),
            MaxOutputTokenCount = maxOutputTokens,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        ChatCompletion completion;
        try
        {
            completion = await client.Value.CompleteChatAsync(
                [new SystemChatMessage(systemPrompt), new UserChatMessage(userText)], options, cts.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new LlmException(LlmFailure.Timeout, ex);
        }
        catch (Exception ex) when (ex is ClientResultException or HttpRequestException)
        {
            throw new LlmException(LlmFailure.ApiError, ex);
        }

        logger.LogInformation("LLM call {Schema}: {InputTokens} input ({CachedTokens} cached), {OutputTokens} output tokens",
            schemaName, completion.Usage?.InputTokenCount, completion.Usage?.InputTokenDetails?.CachedTokenCount, completion.Usage?.OutputTokenCount);

        if (completion.Refusal is not null || completion.FinishReason == ChatFinishReason.ContentFilter)
        {
            throw new LlmException(LlmFailure.Refusal);
        }

        if (completion.FinishReason == ChatFinishReason.Length)
        {
            throw new LlmException(LlmFailure.Truncated);
        }

        if (completion.Content.Count == 0)
        {
            throw new LlmException(LlmFailure.SchemaMismatch);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(completion.Content[0].Text, LlmJson)
                ?? throw new LlmException(LlmFailure.SchemaMismatch);
        }
        catch (JsonException ex)
        {
            throw new LlmException(LlmFailure.SchemaMismatch, ex);
        }
    }
}
