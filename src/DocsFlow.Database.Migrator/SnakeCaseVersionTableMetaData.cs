using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Options;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Служебная таблица FluentMigrator в snake_case — как и вся остальная схема, вместо дефолтной
/// PascalCase <c>VersionInfo</c>. Зависимости базового класса приходят из DI FluentMigrator.
/// </summary>
public sealed class SnakeCaseVersionTableMetaData(
    IConventionSet conventionSet,
    IOptions<RunnerOptions> runnerOptions)
    : DefaultVersionTableMetaData(conventionSet, runnerOptions)
{
    public override string TableName => "version_info";
    public override string ColumnName => "version";
    public override string AppliedOnColumnName => "applied_on";
    public override string DescriptionColumnName => "description";
    public override string UniqueIndexName => "ux_version_info_version";
}
