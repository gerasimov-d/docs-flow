using Dapper;
using DocsFlow.Database;

namespace DocsFlow.Users;

internal sealed class UserRepository : IUserRepository
{
    // Порядок колонок обязан совпадать с порядком параметров конструктора User: Dapper подбирает
    // конструктор записи, сверяя имена позиционно (underscore-конвенция включена в AddPostgresDatabase).
    private const string Columns =
        "id, keycloak_subject, email, display_name, created_at, updated_at, last_login_at";

    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<User> UpsertBySubjectAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken = default)
    {
        // Один запрос вместо SELECT + INSERT: два одновременных первых входа (например, пользователь
        // нажал «войти» в двух вкладках) на паре запросов дают гонку и unique_violation.
        // ON CONFLICT ... RETURNING атомарен и обходится одним round-trip.
        const string sql = $"""
            INSERT INTO users (id, keycloak_subject, email, display_name, last_login_at)
            VALUES (@Id, @Subject, @Email, @DisplayName, now())
            ON CONFLICT (keycloak_subject) DO UPDATE
               SET email         = excluded.email,
                   display_name  = excluded.display_name,
                   updated_at    = now(),
                   last_login_at = now()
            RETURNING {Columns}
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<User>(new CommandDefinition(
            sql,
            new
            {
                // UUIDv7 монотонен по времени, поэтому вставки не фрагментируют B-tree, в отличие от v4.
                Id = Guid.CreateVersion7(),
                identity.Subject,
                identity.Email,
                identity.DisplayName,
            },
            cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            $"SELECT {Columns} FROM users WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }
}
