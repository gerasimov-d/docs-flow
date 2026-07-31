using Microsoft.Extensions.AI;

namespace DocsFlow.Rag.Tests;

/// <summary>
/// Клиент чата с заранее заданным поведением: либо возвращает готовый JSON, либо падает.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly Func<string> _respond;

    private FakeChatClient(Func<string> respond) => _respond = respond;

    /// <summary>Последний запрос — чтобы проверить, что в контекст попали найденные фрагменты.</summary>
    public IReadOnlyList<ChatMessage> LastRequest { get; private set; } = [];

    public static FakeChatClient Returning(string? answer, params int[] citations)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { answer, citations });

        return new FakeChatClient(() => json);
    }

    public static FakeChatClient ReturningRaw(string response) => new(() => response);

    public static FakeChatClient Failing(Exception error) => new(() => throw error);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastRequest = messages.ToArray();

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _respond())));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Стриминг в пайплайне не используется.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
