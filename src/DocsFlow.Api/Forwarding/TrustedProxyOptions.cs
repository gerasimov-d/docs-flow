namespace DocsFlow.Api.Forwarding;

/// <summary>
/// Прокси, чьим заголовкам <c>X-Forwarded-*</c> приложение доверяет.
/// </summary>
/// <remarks>
/// Список обязателен именно потому, что эти заголовки подделываются кем угодно. Доверяя им без
/// разбора, приложение начинает верить чужому мнению о схеме запроса и адресе клиента: в логах и в
/// защите от перебора оказывается подставной IP, а <c>https</c> в <c>X-Forwarded-Proto</c>
/// заставляет считать защищённым соединение, пришедшее по http. Поэтому пустой список — не «всем
/// можно», а «только loopback», как и по умолчанию в ASP.NET.
/// </remarks>
public sealed class TrustedProxyOptions
{
    public const string SectionName = "Network:TrustedProxies";

    /// <summary>Адреса отдельных прокси, например <c>10.0.0.7</c>.</summary>
    public string[] Addresses { get; init; } = [];

    /// <summary>Сети прокси в нотации CIDR, например <c>172.16.0.0/12</c>.</summary>
    public string[] Networks { get; init; } = [];
}
