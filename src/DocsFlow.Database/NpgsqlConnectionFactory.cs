using System.Data.Common;
using Npgsql;

namespace DocsFlow.Database;

/// <summary>
/// Открывает соединения из общего <see cref="NpgsqlDataSource"/> — пул и логирование живут в нём.
/// </summary>
internal sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        => await _dataSource.OpenConnectionAsync(cancellationToken);
}
