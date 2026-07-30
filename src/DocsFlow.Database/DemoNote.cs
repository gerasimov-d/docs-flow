namespace DocsFlow.Database;

/// <summary>
/// Демонстрационная запись из таблицы <c>demo_notes</c>. Показывает сквозной путь
/// «миграция → таблица → репозиторий» и маппинг snake_case → PascalCase в Dapper.
/// Не доменная модель — уходит, когда появится реальная схема.
/// </summary>
public sealed record DemoNote(long Id, string Title, DateTime CreatedAt);
