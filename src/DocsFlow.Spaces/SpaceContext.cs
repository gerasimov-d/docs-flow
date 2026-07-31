namespace DocsFlow.Spaces;

/// <summary>
/// Контекст — тематическое направление внутри space: «ребёнок», «авто», «ремонт», «финансы».
/// Контексты отличаются друг от друга только именем и образуют плоский список, а не дерево.
/// </summary>
/// <remarks>
/// Тип назван <c>SpaceContext</c>, а не <c>Context</c>: голое «Context» в C# читается как
/// контекст выполнения (HttpContext, DbContext), и на месте использования смысл терялся бы.
/// В требованиях, в API и в таблице сущность остаётся «контекстом».
/// </remarks>
public sealed record SpaceContext(Guid Id, Guid SpaceId, string Name, DateTime CreatedAt, DateTime UpdatedAt);
