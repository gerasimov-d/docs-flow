using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Таблица пользователей сервиса. Учётные данные и подтверждение личности живут в Keycloak,
/// здесь — собственный идентификатор пользователя и снимок профиля.
/// </summary>
[Migration(20260730130000)]
public sealed class CreateUsers : Migration
{
    public override void Up()
    {
        Create.Table("users")
            // Идентификатор назначает приложение (UUIDv7), дефолта в БД нет.
            .WithColumn("id").AsGuid().PrimaryKey()
            // AsCustom("text"), а не AsString(): последний на Postgres разворачивается
            // в varchar(255), а ограничение длины здесь ничего не защищает.
            .WithColumn("keycloak_subject").AsCustom("text").NotNullable().Unique("ux_users_keycloak_subject")
            .WithColumn("email").AsCustom("text").NotNullable()
            .WithColumn("display_name").AsCustom("text").Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("last_login_at").AsDateTimeOffset().Nullable();

        // Уникальности на email нет намеренно: её обеспечивает Keycloak (duplicateEmailsAllowed: false).
        // Ограничение в БД при расхождении настроек превратило бы вход пользователя в 500, тогда как
        // связь с IdP всё равно идёт по keycloak_subject. Индекс нужен для поиска по email.
        Create.Index("ix_users_email").OnTable("users").OnColumn("email").Ascending();
    }

    public override void Down() =>
        Delete.Table("users");
}
