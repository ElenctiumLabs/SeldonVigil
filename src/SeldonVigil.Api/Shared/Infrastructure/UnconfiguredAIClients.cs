namespace SeldonVigil.Api.Shared.Infrastructure;

/// <summary>
/// Fallback <see cref="IChatClient"/> used when no AI provider is configured.
/// Fails fast at call time with a clear message instead of crashing app startup.
/// </summary>
internal sealed class UnconfiguredChatClient(string providerName) : IChatClient
{
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            $"AI chat is not configured. Set the '{providerName}' settings to enable root cause analysis.");
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            $"AI chat is not configured. Set the '{providerName}' settings to enable root cause analysis.");
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Fallback <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> used when no AI provider is configured.
/// </summary>
internal sealed class UnconfiguredEmbeddingGenerator(string providerName)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            $"Embedding generation is not configured. Set the '{providerName}' settings to enable memory features.");
    }

    public void Dispose()
    {
    }
}
