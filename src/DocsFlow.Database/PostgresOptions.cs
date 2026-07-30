using System.ComponentModel.DataAnnotations;

namespace DocsFlow.Database;

public sealed class PostgresOptions
{
    public const string SectionName = "Database:Postgres";

    /// <summary>Строка подключения к Postgres в формате Npgsql.</summary>
    [Required]
    public string ConnectionString { get; init; } = null!;
}
