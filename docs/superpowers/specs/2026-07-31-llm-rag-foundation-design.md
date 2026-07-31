# Основа LLM и RAG: Microsoft.Extensions.AI + pgvector

**Дата:** 2026-07-31
**Ветка:** `ai/llm-rag-foundation` (worktree от `origin/dev`)
**Статус:** одобрено владельцем

## Цель

Собрать инфраструктуру работы с LLM и сквозной скелет RAG, чтобы продуктовые задачи делались
«на готовом»: подключение к любому OpenAI-совместимому провайдеру, эмбеддинги, хранение векторов
в Postgres, поиск по схожести, генерация ответа с обязательными ссылками на фрагменты.

Доменной модели документов ещё нет, поэтому пайплайн работает с произвольным текстом и
**строковым локатором источника** (`source_key`). Когда появятся документы, они подключатся к
готовому пайплайну, отдавая свой ключ и извлечённый текст.

Решения владельца, зафиксированные до реализации:
- Клиентская абстракция — **Microsoft.Extensions.AI** (`IChatClient`, `IEmbeddingGenerator`),
  провайдер — `Microsoft.Extensions.AI.OpenAI` поверх любого OpenAI-совместимого endpoint.
- Вектора — **pgvector** с ANN-индексом **HNSW** и косинусной метрикой.
- Скоуп — LLM-слой + векторное хранилище + сквозной RAG-скелет на демо-данных.
- Устойчивость — **Microsoft.Extensions.Http.Resilience** (таймауты, ретраи, circuit breaker).

## Опора на существующие паттерны

- Эталон слайса — `DocsFlow.Users` и `DocsFlow.Storage`: public-интерфейс + `internal sealed`
  реализация + `XxxOptions` с DataAnnotations и `ValidateOnStart` +
  `ServiceCollectionExtensions.AddXxx(...)` + `InternalsVisibleTo` для тестов.
- Репозиторий — поверх `IDbConnectionFactory` и Dapper, SQL явный, колонки в snake_case,
  идентификаторы — UUIDv7 (`Guid.CreateVersion7()`), как в `UserRepository`.
- Миграции — в `DocsFlow.Database.Migrator`, приложение прав на DDL не имеет.
- Тесты — Testcontainers + xunit.v3 + Shouldly, fixture собирает сервисы тем же `AddXxx`,
  что и приложение, образ пинуется той же версией, что в compose.
- Комментарии и XML-doc — русские; идентификаторы и имена файлов — английские.

## Архитектура: два проекта, а не один

| Проект | Роль | Зависимости |
|---|---|---|
| `src/DocsFlow.Llm` | Адаптер к провайдерам LLM: конфиг, устойчивость, регистрация клиентов | `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Microsoft.Extensions.Http.Resilience` |
| `src/DocsFlow.Rag` | Пайплайн RAG: чанкинг, индексация, поиск, ответ с цитатами | `Microsoft.Extensions.AI`, `Dapper`, `Pgvector`, → `DocsFlow.Database` |

**Ключевой инвариант:** `DocsFlow.Rag` **не ссылается ни на `DocsFlow.Llm`, ни на пакет
провайдера** (`Microsoft.Extensions.AI.OpenAI` и подобные) — только на вендор-нейтральный
`Microsoft.Extensions.AI` с его `IChatClient` / `IEmbeddingGenerator`. Продуктовый принцип
«provider-agnostic» перестаёт быть договорённостью и становится ошибкой компиляции: обратиться
к OpenAI SDK из пайплайна физически нечем. Тот же приём, что с ESLint-правилами слоёв в клиенте —
правило проверяет инструмент, а не code review.

Оба проекта добавляются в `DocsFlow.slnx`, `DocsFlow.Api` ссылается на оба и регистрирует их
в `Program.cs`.

## DocsFlow.Llm

### Конфигурация — секция `Llm`

```jsonc
"Llm": {
  "Chat":       { "Enabled": true, "Endpoint": "...", "ApiKey": "", "Model": "...",
                  "AttemptTimeoutSeconds": 100, "TotalTimeoutSeconds": 300, "MaxRetryAttempts": 3 },
  "Embeddings": { "Endpoint": "...", "ApiKey": "", "Model": "...",
                  "AttemptTimeoutSeconds": 30,  "TotalTimeoutSeconds": 120, "MaxRetryAttempts": 3 }
}
```

- Две независимые секции с общим базовым классом `LlmEndpointOptions`, а не одна вложенная:
  `ValidateDataAnnotations` не спускается во вложенные объекты, и «одна секция с подобъектами»
  молча осталась бы непроверенной.
- `Endpoint` — база OpenAI-совместимого API (`/v1`). Локальный сервер (Ollama, vLLM, LM Studio)
  и облачный провайдер отличаются только этим полем: продуктовый принцип «приватность по
  умолчанию» выполняется тем, что внешний вызов **выключается конфигом**, а не кодом.
- `ApiKey` — не `[Required]`: локальные серверы ключа не требуют. Пустое значение заменяется
  плейсхолдером, потому что клиент OpenAI не принимает пустую строку.
- `Chat.Enabled: false` — генерация выключена, `IChatClient` **не регистрируется вовсе**, и
  секция `Llm:Chat` тогда вообще не валидируется. Опциональность выражена в DI, а не в проверках
  внутри пайплайна.

Таймауты у чата и эмбеддингов разные не случайно: генерация идёт десятки секунд, эмбеддинги —
доли секунды, и один общий таймаут был бы либо слишком коротким для первого, либо бессмысленно
длинным для второго. Модели тоже настраиваются раздельно: типовой сценарий — лёгкая локальная
модель для эмбеддингов и сильная для ответов.

### Устойчивость

`AddStandardResilienceHandler` (таймаут на попытку и общий, ретраи с экспоненциальной задержкой
и джиттером, circuit breaker) на именованном `HttpClient`, который передаётся клиенту OpenAI
через `HttpClientPipelineTransport`.

**Дефолты пакета не годятся для LLM:** attempt timeout 10 с и total 30 с рубят обычную генерацию.
Поэтому таймауты вынесены в опции (100 / 300 с), а `CircuitBreaker.SamplingDuration` поднимается
следом — пакет требует, чтобы окно было не меньше удвоенного attempt-таймаута, иначе валидация
падает на старте.

### Регистрация

`AddLlm(IServiceCollection, IConfiguration)`:
1. Биндит и валидирует `LlmOptions` (`ValidateDataAnnotations().ValidateOnStart()`).
2. Регистрирует `IEmbeddingGenerator<string, Embedding<float>>` — всегда.
3. Регистрирует `IChatClient` — только при `Chat.Enabled`.
4. Оба клиента оборачиваются в `UseLogging()` из Microsoft.Extensions.AI: запросы к модели видны
   в общем логе приложения без своего кода. OpenTelemetry — когда в проекте появится трассировка.

## Размерность вектора

pgvector требует фиксированную размерность колонки для HNSW-индекса. Размерность зафиксирована
в миграции константой **1024**: столько отдают распространённые локальные модели (bge-m3,
mxbai-embed-large), а облачные `text-embedding-3-*` умеют усекаться до заданной длины. Привязка
к 1536 сделала бы локальный запуск невозможным без миграции, то есть противоречила бы принципу
«приватность по умолчанию».

Число живёт в **одном** месте конфига — `Rag:EmbeddingDimensions`; в секции `Llm` его нет.
Размерность диктует схема БД, поэтому её задаёт и запрашивает у провайдера тот слой, который
владеет хранилищем: `DocumentIndexer` передаёт её в `EmbeddingGenerationOptions.Dimensions`.
Рассинхрон со схемой ловится перед записью — вектор неверной длины даёт `RagException` с обоими
числами в тексте, а не `22000` из Postgres.

Смена модели на другую размерность — отдельная миграция плюс переиндексация; поэтому рядом
с вектором хранится `embedding_model`, чтобы было видно, чем считали.

## Схема БД: таблица `rag_chunks`

Миграция `CreateRagChunks` в `DocsFlow.Database.Migrator`:

```sql
CREATE EXTENSION IF NOT EXISTS vector;

rag_chunks (
  id              uuid primary key,           -- UUIDv7, назначает приложение
  source_key      text not null,              -- локатор источника (ключ в объектном хранилище)
  ordinal         int  not null,              -- номер чанка внутри источника, 0-based
  content         text not null,
  embedding       vector(1024) not null,
  embedding_model text not null,
  created_at      timestamptz not null default now(),
  unique (source_key, ordinal)
)

CREATE INDEX ix_rag_chunks_embedding ON rag_chunks USING hnsw (embedding vector_cosine_ops);
CREATE INDEX ix_rag_chunks_source_key ON rag_chunks (source_key);
```

- Тип колонки задаётся через `AsCustom("vector(1024)")` — FluentMigrator про pgvector не знает.
- `CREATE EXTENSION` и HNSW-индекс — через `Execute.Sql`.
- `unique (source_key, ordinal)` делает переиндексацию источника идемпотентной.
- Индекс по `source_key` нужен для удаления и выборки чанков одного источника.

**Инфраструктурное следствие:** образ Postgres меняется на `pgvector/pgvector:0.8.6-pg17`
везде — в `docker-compose.yml` и во **всех** fixture (`Database.Tests`, `Users.Tests`,
`Api.Tests`, новый `Rag.Tests`). Миграции накатываются целиком, поэтому на «чистом» postgres:17.6
упадут и чужие тесты.

Маппинг типа `vector` включается в `DocsFlow.Database`: `NpgsqlDataSourceBuilder.UseVector()`
плюс `SqlMapper.AddTypeHandler` из `Pgvector.Dapper` — там же, где живёт snake_case-конвенция
Dapper. Настройка маппинга типов Postgres — ответственность слоя доступа к данным, а не RAG.

## DocsFlow.Rag

### Конфигурация — секция `Rag`

| Опция | Дефолт | Смысл |
|---|---|---|
| `ChunkSize` | 1000 | Целевой размер чанка в символах |
| `ChunkOverlap` | 150 | Перекрытие соседних чанков |
| `EmbeddingBatchSize` | 16 | Сколько чанков уходит в один запрос эмбеддингов |
| `EmbeddingDimensions` | 1024 | Запрашивается у провайдера и сверяется с длиной вектора перед записью |
| `TopK` | 5 | Сколько фрагментов идёт в контекст |
| `MinScore` | 0.2 | Отсечка косинусной близости |
| `UseJsonSchemaResponseFormat` | true | Часть совместимых серверов не умеет `response_format=json_schema` |

### Компоненты

- **`TextChunker`** — режет текст с перекрытием, стараясь резать по границе абзаца, затем
  предложения, и лишь в крайнем случае посередине. Токенайзер не подключаем: символьная оценка
  не требует ещё одной зависимости, а точный подсчёт токенов нужен только для биллинга и лимитов.
- **`IChunkRepository`** — `ReplaceAsync(sourceKey, chunks)` в одной транзакции (удалить чанки
  источника, вставить новые) и `SearchAsync(embedding, topK, minScore)` — `ORDER BY embedding <=>
  @query`, score = `1 - distance`.
- **`IDocumentIndexer`** — текст → чанки → батч эмбеддингов → запись. Идемпотентен по
  `source_key`: повторная индексация заменяет содержимое источника целиком.
- **`IRagService.AskAsync(question)`** — эмбеддинг вопроса → поиск → сборка контекста → ответ.

### Ответ: только со ссылкой на первоисточник

```csharp
public sealed record RagAnswer(RagAnswerStatus Status, string? Text, IReadOnlyList<RagCitation> Citations);
public enum RagAnswerStatus { Answered, NothingFound, NoGrounding, GenerationUnavailable }
```

Модель отвечает **структурированным JSON** (`GetResponseAsync<T>` из Microsoft.Extensions.AI):
`{ "answer": "...", "citations": [1, 3] }`, где числа — номера фрагментов в переданном контексте.
Дальше ответ проверяется кодом, а не доверием к модели:

- номера вне диапазона отбрасываются;
- пустой список цитат → статус `NoGrounding`, **текст ответа не отдаётся**. Продуктовый принцип
  «никаких ответов без ссылки» реализуется здесь, а не в промпте: промпт можно проигнорировать,
  проверку — нет;
- ответ, который не разобрался в структуру, приравнивается к недоступной генерации.

При `Answered` в `Citations` попадают только фрагменты, на которые модель действительно сослалась —
это ссылки к тексту ответа. При остальных статусах возвращаются все найденные фрагменты, поэтому
вызывающий код всегда может показать первоисточник, даже когда генерации не было.

### Деградация, а не отказ

- `Chat.Enabled: false` или чат не зарегистрирован → `GenerationUnavailable` + найденные
  фрагменты. Поиск продолжает работать.
- Провайдер чата упал (после ретраев и circuit breaker) → то же самое: исключение логируется,
  наружу идёт `GenerationUnavailable` с фрагментами.
- Провайдер эмбеддингов упал → `RagException`. Это не деградация: без эмбеддинга вопроса искать
  нечем, и молчаливый пустой результат был бы хуже явной ошибки.

## Тесты

`tests/DocsFlow.Llm.Tests` — 8 тестов, без сети:
1. Валидация опций: отсутствующие `Endpoint` / `Model`, некорректный URL, разумные дефолты таймаутов.
2. `AddLlm` регистрирует `IEmbeddingGenerator` и `IChatClient`.
3. `Chat.Enabled: false` → `IChatClient` в контейнере отсутствует, а настройки чата не требуются.
4. Локальный endpoint без ключа: клиент создаётся (проверка подстановки плейсхолдера).

`tests/DocsFlow.Rag.Tests` — 34 теста на Testcontainers + pgvector с фейковыми клиентами:
1. `TextChunker`: перекрытие соседей, границы абзаца и предложения, покрытие текста без потерь,
   короткий текст, пустой текст.
2. Репозиторий: ранжирование по близости, порог `MinScore`, лимит `TopK`, замена версии источника,
   идемпотентное удаление.
3. Индексатор: нарезка длинного текста и сплошная нумерация, переиндексация, очистка пустым
   текстом, запись модели рядом с вектором, внятная ошибка при несовпадении размерности, отказ
   провайдера не проглатывается.
4. Миграция: расширение `vector`, размерность колонки, HNSW с `vector_cosine_ops`, ограничение
   уникальности.
5. `RagService`: ответ с цитатами и попадание фрагментов в контекст; ответ без цитат →
   `NoGrounding` без текста; несуществующие номера отбрасываются; упавший и неразобранный ответ →
   `GenerationUnavailable` с фрагментами; чат не зарегистрирован → то же; пустая база →
   `NothingFound`; `IRagService` разрешается из DI без `IChatClient`.

Фейковый `IEmbeddingGenerator` раскладывает слова текста по измерениям (FNV-1a вместо
`GetHashCode` — тот рандомизирован между процессами) и нормирует вектор, поэтому тексты с общими
словами близки по косинусу. Этого достаточно, чтобы проверять ранжирование и пороги без сети.

## Пакеты (CPM → `Directory.Packages.props`)

`Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.Abstractions`, `Microsoft.Extensions.AI.OpenAI`
(10.8.3), `Microsoft.Extensions.Http.Resilience` (10.8.0), `Microsoft.Extensions.Logging` и
`Microsoft.Extensions.Logging.Abstractions` (10.0.10), `Pgvector` (0.3.2), `Pgvector.Dapper` (0.3.1).

## Замечания по реализации (по итогам прогона)

- **`ConfigureOptions` есть только у `ChatClientBuilder`.** У `EmbeddingGeneratorBuilder` его нет,
  поэтому размерность нельзя задать один раз при регистрации — её передаёт вызывающий код
  в `EmbeddingGenerationOptions`. Это и подтолкнуло убрать `Dimensions` из секции `Llm`.
- **`GetResponseAsync<T>` живёт в `Microsoft.Extensions.AI`, а не в `.Abstractions`.** Пакет
  вендор-нейтральный (логирование, кэш, структурированный вывод), так что инвариант
  «`Rag` не знает провайдера» он не нарушает.
- **`NpgsqlDataSourceBuilder.UseVector()` возвращает `INpgsqlTypeMapper`,** а не сам builder —
  в цепочку с `.Build()` не ставится.
- **Встроенный DI поддерживает опциональные зависимости** через параметр конструктора со значением
  по умолчанию: `RagService` получает `IChatClient? chatClient = null` и разрешается, когда клиент
  не зарегистрирован. Отдельный тест закрепляет это поведение.
- **Нулевой вектор в pgvector даёт NaN** при косинусном расстоянии, поэтому текст без слов должен
  получать не нулевой вектор (в тестовом фейке — орт первого измерения).
- **`MaxRetryAttempts = 0` недопустим:** стратегия ретраев требует хотя бы одной попытки, и ноль
  роняет приложение на старте изнутри Polly. Поэтому в опциях стоит `[Range(1, 10)]` — конфиг
  отсекается валидацией с понятным сообщением. Закреплено тестом.

## Вне скоупа

- Эндпоинты API поверх RAG и UI — регистрация в DI есть, HTTP-контракта нет.
- Доменная модель документов: пайплайн принимает текст и `source_key`, извлечение текста из
  файлов (OCR, PDF) — отдельная задача.
- Гибридный поиск (полнотекстовый + векторный) и реранкинг — следующий шаг после того, как
  появится реальный корпус.
- Кэш эмбеддингов, учёт токенов и стоимости, OpenTelemetry-трассировка вызовов модели.
- Вызов инструментов моделью (function calling) — `Microsoft.Extensions.AI` его поддерживает,
  но задач под него сейчас нет.
