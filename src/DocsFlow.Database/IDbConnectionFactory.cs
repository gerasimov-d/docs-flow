using System.Data.Common;

namespace DocsFlow.Database;

/// <summary>
/// Источник открытых соединений с базой. Прячет конкретную реализацию (Npgsql, пул) от
/// репозиториев — те работают только с <see cref="DbConnection"/> и Dapper.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Открывает соединение из пула. Вызывающий обязан освободить его (<c>await using</c>).
    /// </summary>
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
