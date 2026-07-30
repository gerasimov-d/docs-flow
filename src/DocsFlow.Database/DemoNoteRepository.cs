using Dapper;

namespace DocsFlow.Database;

/// <summary>
/// Доступ к таблице <c>demo_notes</c> через Dapper. Демонстрирует паттерн репозитория поверх
/// <see cref="IDbConnectionFactory"/>, который повторят продуктовые репозитории.
/// </summary>
public sealed class DemoNoteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DemoNoteRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <summary>Добавляет запись и возвращает сгенерированный идентификатор.</summary>
    public async Task<long> AddAsync(string title, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO demo_notes (title) VALUES (@title) RETURNING id",
            new { title },
            cancellationToken: cancellationToken));
    }

    /// <summary>Возвращает запись по идентификатору или <c>null</c>, если её нет.</summary>
    public async Task<DemoNote?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DemoNote>(new CommandDefinition(
            "SELECT id, title, created_at FROM demo_notes WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }
}
