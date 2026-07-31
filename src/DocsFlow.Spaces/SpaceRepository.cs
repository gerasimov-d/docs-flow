using Dapper;
using DocsFlow.Database;

namespace DocsFlow.Spaces;

internal sealed class SpaceRepository : ISpaceRepository
{
    // Роли пишутся в базу строками — так их видно в psql и так же их проверяет ck_space_members_role.
    private const string OwnerRole = "owner";
    private const string MemberRole = "member";

    // Порядок колонок обязан совпадать с порядком параметров конструктора Space: Dapper подбирает
    // конструктор записи, сверяя имена позиционно (underscore-конвенция включена в AddPostgresDatabase).
    private const string SpaceColumns = "id, name, created_at, updated_at";

    // Space и владелец появляются одним statement: отдельными запросами между ними существует
    // момент, когда space есть, а владельца у него нет.
    private const string CreateSql = $"""
        WITH created AS (
            INSERT INTO spaces (id, name)
            VALUES (@Id, @Name)
            RETURNING {SpaceColumns}
        ), owner_membership AS (
            INSERT INTO space_members (space_id, user_id, role)
            SELECT id, @OwnerId, '{OwnerRole}' FROM created
        )
        SELECT {SpaceColumns} FROM created
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public SpaceRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<Space> CreateAsync(Guid ownerId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<Space>(new CommandDefinition(
            CreateSql,
            // UUIDv7 монотонен по времени, поэтому вставки не фрагментируют B-tree, в отличие от v4.
            new { Id = Guid.CreateVersion7(), Name = name, OwnerId = ownerId },
            cancellationToken: cancellationToken));
    }

    public async Task<Space?> CreateFirstIfMissingAsync(
        Guid ownerId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Блокировка строки пользователя сериализует параллельные входы: без неё два одновременных
        // первых входа (пользователь нажал «войти» в двух вкладках) прочитали бы «space нет» оба
        // и создали по одному. Проверка идёт отдельным запросом уже после блокировки — в READ
        // COMMITTED каждый statement берёт свежий снимок, а внутри одного CTE его не обновить.
        var userExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT true FROM users WHERE id = @ownerId FOR UPDATE",
            new { ownerId },
            transaction,
            cancellationToken: cancellationToken));

        if (!userExists)
        {
            return null;
        }

        var alreadyHasSpace = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM space_members WHERE user_id = @ownerId)",
            new { ownerId },
            transaction,
            cancellationToken: cancellationToken));

        if (alreadyHasSpace)
        {
            return null;
        }

        var space = await connection.QuerySingleAsync<Space>(new CommandDefinition(
            CreateSql,
            new { Id = Guid.CreateVersion7(), Name = name, OwnerId = ownerId },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return space;
    }

    public async Task<IReadOnlyList<SpaceMembership>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT s.id, s.name, m.role, s.created_at
            FROM space_members m
            JOIN spaces s ON s.id = m.space_id
            WHERE m.user_id = @userId
            ORDER BY s.created_at
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var memberships = await connection.QueryAsync<SpaceMembership>(new CommandDefinition(
            sql,
            new { userId },
            cancellationToken: cancellationToken));

        return memberships.ToArray();
    }

    public async Task<SpaceRole?> FindRoleAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var role = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT role FROM space_members WHERE space_id = @spaceId AND user_id = @userId",
            new { spaceId, userId },
            cancellationToken: cancellationToken));

        return ParseRole(role);
    }

    public async Task<Space?> FindAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Space>(new CommandDefinition(
            $"SELECT {SpaceColumns} FROM spaces WHERE id = @spaceId",
            new { spaceId },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> RenameAsync(Guid spaceId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE spaces SET name = @name, updated_at = now() WHERE id = @spaceId",
            new { spaceId, name },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<IReadOnlyList<SpaceMember>> ListMembersAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT m.user_id, u.email, u.display_name, m.role
            FROM space_members m
            JOIN users u ON u.id = m.user_id
            WHERE m.space_id = @spaceId
            ORDER BY m.created_at
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var members = await connection.QueryAsync<SpaceMember>(new CommandDefinition(
            sql,
            new { spaceId },
            cancellationToken: cancellationToken));

        return members.ToArray();
    }

    public async Task<AddMemberResult> AddMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // WHERE EXISTS вместо ловли нарушения внешнего ключа: несуществующий пользователь —
        // обычный ответ API, а не исключительная ситуация.
        const string sql = $"""
            INSERT INTO space_members (space_id, user_id, role)
            SELECT @spaceId, @userId, '{MemberRole}'
            WHERE EXISTS (SELECT 1 FROM users WHERE id = @userId)
            ON CONFLICT (space_id, user_id) DO NOTHING
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { spaceId, userId },
            cancellationToken: cancellationToken));

        if (affected > 0)
        {
            return AddMemberResult.Added;
        }

        // Ноль строк означает одно из двух — разбираемся только на этом пути, чтобы обычная
        // выдача доступа оставалась одним запросом.
        var userExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE id = @userId)",
            new { userId },
            cancellationToken: cancellationToken));

        return userExists ? AddMemberResult.AlreadyMember : AddMemberResult.UserNotFound;
    }

    public async Task<RemoveMemberResult> RemoveMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Владельца не удаляем условием в самом DELETE: проверка «сначала прочитать роль, потом
        // удалить» на параллельных запросах разъезжается, а здесь ограничение держит база.
        const string sql = $"""
            DELETE FROM space_members
            WHERE space_id = @spaceId AND user_id = @userId AND role <> '{OwnerRole}'
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { spaceId, userId },
            cancellationToken: cancellationToken));

        if (affected > 0)
        {
            return RemoveMemberResult.Removed;
        }

        var role = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT role FROM space_members WHERE space_id = @spaceId AND user_id = @userId",
            new { spaceId, userId },
            cancellationToken: cancellationToken));

        return ParseRole(role) == SpaceRole.Owner
            ? RemoveMemberResult.OwnerCannotBeRemoved
            : RemoveMemberResult.NotMember;
    }

    /// <summary>
    /// Разбирает роль из базы. Значение приходит строкой: nullable-enum Dapper напрямую
    /// из <c>text</c> не собирает, а «нет строки» здесь означает «нет доступа» и должно
    /// отличаться от любой существующей роли.
    /// </summary>
    private static SpaceRole? ParseRole(string? role) => role switch
    {
        OwnerRole => SpaceRole.Owner,
        MemberRole => SpaceRole.Member,
        _ => null,
    };
}
