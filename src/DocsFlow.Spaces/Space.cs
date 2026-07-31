namespace DocsFlow.Spaces;

/// <summary>
/// Space — группа доступа: множество пользователей, разделяющих одно пространство документов.
/// Единица изоляции данных, а не каталог хранения: у него нет ни пути, ни вложенности.
/// </summary>
public sealed record Space(Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>
/// Роль в space. Тоньше этих двух ролей деления нет: участник — полноправный соавтор,
/// и от владельца его отличает только право управлять доступом и именем space.
/// </summary>
public enum SpaceRole
{
    /// <summary>Полноправный соавтор: читает, создаёт контексты, загружает документы.</summary>
    Member,

    /// <summary>Создатель space. Дополнительно управляет составом участников и именем.</summary>
    Owner,
}

/// <summary>Space глазами конкретного пользователя: сам space плюс его роль в нём.</summary>
public sealed record SpaceMembership(Guid Id, string Name, SpaceRole Role, DateTime CreatedAt);

/// <summary>
/// Участник space. Профиль показывается только своим: наружу состав space не раскрывается.
/// </summary>
public sealed record SpaceMember(Guid UserId, string Email, string? DisplayName, SpaceRole Role);
