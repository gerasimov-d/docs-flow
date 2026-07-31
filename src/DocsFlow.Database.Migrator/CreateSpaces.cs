using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Space — группа доступа и единица изоляции данных: всё содержимое архива принадлежит ровно
/// одному space. Не каталог хранения, поэтому вложенности и путей здесь нет.
/// </summary>
[Migration(20260731150000)]
public sealed class CreateSpaces : Migration
{
    public override void Up()
    {
        Create.Table("spaces")
            // Идентификатор назначает приложение (UUIDv7), как и для users.
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsCustom("text").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);

        // Владелец хранится не колонкой в spaces, а ролью в этой таблице: иначе владение и членство
        // разъезжаются по двум источникам, и каждая проверка доступа обязана смотреть в оба.
        // Здесь же проверка одна — «есть ли строка», а роль отвечает только за право управлять доступом.
        Create.Table("space_members")
            .WithColumn("space_id").AsGuid().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("role").AsCustom("text").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);

        Create.PrimaryKey("pk_space_members")
            .OnTable("space_members")
            .Columns("space_id", "user_id");

        // Каскад: строка членства без своего space смысла не имеет. Удаления space в API пока нет,
        // но ссылочная целостность не должна зависеть от того, что именно API успел реализовать.
        Create.ForeignKey("fk_space_members_space")
            .FromTable("space_members").ForeignColumn("space_id")
            .ToTable("spaces").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_space_members_user")
            .FromTable("space_members").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Набор ролей ловится базой, а не кодом: тоньше «владельца» и «участника» ролей нет,
        // и опечатка в роли не должна превращаться в невидимую дыру в проверке прав.
        Execute.Sql(
            """
            ALTER TABLE space_members
                ADD CONSTRAINT ck_space_members_role CHECK (role IN ('owner', 'member'))
            """);

        // Владелец ровно один: частичный уникальный индекс не даёт появиться второму,
        // в том числе при гонке двух параллельных запросов.
        Execute.Sql(
            """
            CREATE UNIQUE INDEX ux_space_members_owner
                ON space_members (space_id) WHERE role = 'owner'
            """);

        // «Список space, в которых состоит пользователь» — самый частый запрос фичи, он идёт
        // от пользователя, а первая колонка первичного ключа для этого не годится.
        Create.Index("ix_space_members_user")
            .OnTable("space_members")
            .OnColumn("user_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("space_members");
        Delete.Table("spaces");
    }
}
