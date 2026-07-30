using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Демонстрационная миграция: заводит таблицу <c>demo_notes</c>. Показывает, как оформляются
/// миграции и создаются таблицы в snake_case. Заменяется реальной схемой, когда та появится.
/// </summary>
[Migration(20260730120000)]
public sealed class CreateDemoNotes : Migration
{
    public override void Up() =>
        Create.Table("demo_notes")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("title").AsString().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);

    public override void Down() =>
        Delete.Table("demo_notes");
}
