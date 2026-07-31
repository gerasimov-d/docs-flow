namespace DocsFlow.Llm;

/// <summary>
/// Настройки генерации ответов. Секция <c>Llm:Chat</c>.
/// </summary>
public sealed class LlmChatOptions : LlmEndpointOptions
{
    public const string SectionName = "Llm:Chat";

    /// <summary>
    /// Выключатель генерации. При <c>false</c> клиент чата не регистрируется в DI вообще, и
    /// потребители деградируют до поиска без ответа модели. Обязателен по продуктовому принципу
    /// «приватность по умолчанию»: любое обращение к модели должно выключаться конфигом.
    /// </summary>
    public bool Enabled { get; init; } = true;

    public LlmChatOptions()
    {
        // Генерация ответа по нескольким фрагментам занимает десятки секунд — дефолты пакета
        // устойчивости (10 с на попытку, 30 с всего) рубили бы нормальные ответы как сбой.
        AttemptTimeoutSeconds = 100;
        TotalTimeoutSeconds = 300;
    }
}
