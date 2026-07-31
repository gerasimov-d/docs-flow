using DocsFlow.Spaces;

namespace DocsFlow.Api.Spaces;

/// <summary>
/// Разбор человекочитаемых имён space и контекстов.
/// </summary>
internal static class SpaceNaming
{
    /// <summary>
    /// Потолок длины имени. Требованиями не задан: это не продуктовое ограничение, а защита от
    /// имени в мегабайт — колонка <c>text</c> приняла бы и его, а список стал бы нечитаемым.
    /// </summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// Приводит присланное имя к каноничному виду: обрезает пробелы по краям и проверяет длину.
    /// </summary>
    /// <returns><c>false</c>, если имя пустое или длиннее допустимого.</returns>
    public static bool TryNormalize(string? raw, out string name)
    {
        name = raw?.Trim() ?? string.Empty;

        return name.Length > 0 && name.Length <= MaxNameLength;
    }

    /// <summary>
    /// Имя роли для внешнего контракта. Числовое значение перечисления наружу не отдаём:
    /// оно зависит от порядка объявления, и добавление роли молча сдвинуло бы смысл ответа.
    /// </summary>
    public static string ToWire(this SpaceRole role) => role == SpaceRole.Owner ? "owner" : "member";
}
