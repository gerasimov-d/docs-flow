using Dapper;
using DocsFlow.Database;

namespace DocsFlow.Spaces;

internal sealed class ContextRepository : IContextRepository
{
    // Порядок колонок обязан совпадать с порядком параметров конструктора SpaceContext.
    private const string Columns = "id, space_id, name, created_at, updated_at";

    private readonly IDbConnectionFactory _connectionFactory;

    public ContextRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<SpaceContext?> CreateAsync(
        Guid spaceId,
        string name,
        CancellationToken cancellationToken = default)
    {
        // Занятое имя — обычный ответ API, а не сбой, поэтому ON CONFLICT DO NOTHING вместо ловли
        // нарушения уникальности. Цель конфликта повторяет выражение индекса ux_contexts_space_name:
        // без lower(name) Postgres не сопоставит его с индексом и запрос упадёт.
        const string sql = $"""
            INSERT INTO contexts (id, space_id, name)
            VALUES (@Id, @SpaceId, @Name)
            ON CONFLICT (space_id, lower(name)) DO NOTHING
            RETURNING {Columns}
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<SpaceContext>(new CommandDefinition(
            sql,
            // UUIDv7 монотонен по времени, поэтому вставки не фрагментируют B-tree.
            new { Id = Guid.CreateVersion7(), SpaceId = spaceId, Name = name },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SpaceContext>> ListAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var contexts = await connection.QueryAsync<SpaceContext>(new CommandDefinition(
            $"SELECT {Columns} FROM contexts WHERE space_id = @spaceId ORDER BY name",
            new { spaceId },
            cancellationToken: cancellationToken));

        return contexts.ToArray();
    }
}
