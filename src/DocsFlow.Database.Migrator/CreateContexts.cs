using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Контекст — тематическое направление внутри space («ребёнок», «авто», «ремонт»). Не папка
/// и не категория таксономии: кроме имени, у него нет атрибутов, а список плоский.
/// </summary>
[Migration(20260731151000)]
public sealed class CreateContexts : Migration
{
    public override void Up()
    {
        Create.Table("contexts")
            // Идентификатор назначает приложение (UUIDv7), как и для остальных таблиц.
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("space_id").AsGuid().NotNullable()
            .WithColumn("name").AsCustom("text").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);

        // Контекст существует только в границах своего space — вне его он не имеет смысла.
        Create.ForeignKey("fk_contexts_space")
            .FromTable("contexts").ForeignColumn("space_id")
            .ToTable("spaces").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Уникальность по lower(name), а не по name: «Авто» и «авто» на экране неразличимы,
        // и два таких контекста в одном списке — не разные направления, а ошибка ввода.
        // Сравнение регистронезависимое только для латиницы и кириллицы одной локали — большего
        // требование «имя уникально в пределах space» и не подразумевает.
        Execute.Sql(
            """
            CREATE UNIQUE INDEX ux_contexts_space_name
                ON contexts (space_id, lower(name))
            """);
    }

    public override void Down() =>
        Delete.Table("contexts");
}
