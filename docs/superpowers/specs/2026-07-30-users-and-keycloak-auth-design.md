# Пользователи и аутентификация через Keycloak (BFF)

**Дата:** 2026-07-30
**Ветка:** `auth/users-and-keycloak` (worktree от `origin/dev`)
**Статус:** реализовано; расхождения с первоначальным планом отмечены по тексту

## Смена продуктовой рамки

Владелец переопределил целевую систему: не приватный self-hosted архив семейных документов, а
**публичный мультиарендный сервис** — пользователи приходят из интернета, сами регистрируются,
хранят свои документы, создают space и подключают к ним других. Клиент пока один — web.

Прямое следствие: продуктовый принцип **«Single-tenant»** в `CLAUDE.md` («запрещает
мультиарендность в модели данных „на будущее“») противоречит новой цели и должен быть заменён.
Изменения `CLAUDE.md` — часть этой задачи, см. раздел «Правка CLAUDE.md».

Решения владельца, зафиксированные в обсуждении:
- Сервис публичный, саморегистрация из интернета. Клиент пока только web.
- Space и шаринг — **вне скоупа** этой задачи, названы только как контекст будущего.
- Скоуп сейчас — личности: вход, регистрация, таблица `users`.
- Ролей и прав не делаем. Вход через Google и Apple не делаем.
- База заметок в Notion («Family Vault») неактуальна и источником требований не является.
- Делать ровно заказанное: не расширять скоуп собственными догадками.

## Цель

Собрать конвейер аутентификации целиком, чтобы продуктовые задачи получали готового
«текущего пользователя»: Keycloak в локальном окружении, вход и выход, саморегистрация с
подтверждением email, таблица `users` со своим идентификатором и автосозданием записи при первом
входе. Схема таблицы рассчитана на то, что к ней придут space, но самих space здесь нет.

## Ключевые архитектурные решения

### 1. BFF, а не токен в браузере

API держит токены на сервере, браузер получает только httpOnly cookie-сессию.

Изначально (для семейной системы за своим периметром) предполагался Bearer: SPA сама ходит в
Keycloak по Authorization Code + PKCE и носит access-token в заголовке. Смена рамки на публичный
интернет разворачивает это решение: токен, доступный JavaScript, уносится любым чужим скриптом на
странице — скомпрометированная npm-зависимость, встроенная аналитика, собственная XSS-ошибка — и
вместе с ним уходят чужие документы. Публичный сервис не может себе этого позволить.

Цена решения: сервер помнит сессии, нужны logout, обновление сессии и защита от CSRF. Всё это
разбирается ниже. UI живёт в этом же проекте и на этом же домене, поэтому схема ложится
естественно, без CORS и cross-site cookie.

Мобильный клиент, когда появится, добавит **вторую** схему аутентификации (`JwtBearer`) рядом с
cookie — это аддитивное изменение, продуктовый код не переписывается. Пакет
`Microsoft.AspNetCore.Authentication.JwtBearer` сейчас **не добавляется**.

### 2. Регистрация и учётные данные — на стороне Keycloak

В realm включаются саморегистрация, подтверждение email, восстановление пароля, парольная
политика и защита от перебора. Своя форма регистрации означала бы, что весь этот набор мы пишем и
поддерживаем сами. Брендирование страниц Keycloak темой — отдельная задача, вместе с UI.

Следствие: **Keycloak Admin API из приложения не вызывается** и service-account клиента не
создаётся. Приложение только валидирует то, что Keycloak уже проверил.

### 3. Права — в своей БД, Keycloak отвечает только за личность

Keycloak отвечает на один вопрос: «этот человек — тот, за кого себя выдаёт». Всё остальное —
предметная область и живёт в нашей базе. Прав и ролей в этой задаче не появляется вовсе: единственное
различие — «вошёл / не вошёл». Решение важно тем, что оно исключает, — когда роли внутри space
понадобятся, они не начнут расти в Keycloak.

Отклонённая альтернатива: realm-роли Keycloak. Для будущей модели «роль X в space Y» это тупик —
групп становится по числу space, каждое приглашение лезет в Admin API, права размазаны по двум
системам, а управлять ими надо из чужой админки вместо своего UI. Роли из токена не берём вообще:
`RoleClaimType` не настраивается, `[Authorize(Roles = ...)]` не используется.

Realm **один** на всех пользователей: они физические лица, а не организации. Realm-per-tenant не
рассматривается.

### 4. Изоляция данных — граница безопасности, а не аккуратность

В приватной семейной системе запрос без фильтра по владельцу — косметический дефект. В публичной —
утечка чужих документов. Начиная с этой задачи доступ к данным строится так, чтобы «выбрать
сущность, не проверив принадлежность» было технически неудобно. Здесь это ещё не проявляется
(единственная таблица — сами пользователи), но фиксируется как инвариант для задачи про space.

## Опора на существующие паттерны

- Эталон слайса — `DocsFlow.Storage` / `DocsFlow.Database`: интерфейс + реализация + `XxxOptions`
  с DataAnnotations и `ValidateOnStart` + `ServiceCollectionExtensions.AddXxx(IConfiguration)` +
  `InternalsVisibleTo` для тестов; секции конфига вложенные.
- Репозиторий поверх `IDbConnectionFactory` + Dapper, snake_case-конвенция уже включена глобально
  в `AddPostgresDatabase`.
- Миграции — только в `DocsFlow.Database.Migrator`, имена в snake_case задаются явно. Приложение
  прав на DDL не имеет.
- Инфраструктурная настройка — инфраструктурой (`minio-init`, `migrator`), не приложением.
- Тесты: Testcontainers + xunit.v3 + Shouldly; fixture поднимает контейнер и собирает сервисы тем
  же `AddXxx`, что и приложение; образы пинуются теми же версиями, что в compose.
- Комментарии и XML-doc — русские; идентификаторы и имена файлов — английские.

## Архитектура: проекты

| Проект | Тип | Роль | Зависимости (NuGet) | Кто ссылается |
|---|---|---|---|---|
| `src/DocsFlow.Users` | class lib | Модель пользователя, репозиторий, провижининг | — (только `ProjectReference` на `DocsFlow.Database`) | `DocsFlow.Api` |
| `src/DocsFlow.Api` | web | Конвейер аутентификации, эндпоинты | `Microsoft.AspNetCore.Authentication.OpenIdConnect` | — |
| `src/DocsFlow.Database.Migrator` | console | Миграция `CreateUsers` | без изменений | compose |
| `tests/DocsFlow.Users.Tests` | test | Репозиторий и провижининг на Postgres | `Testcontainers.PostgreSql`, `xunit.v3`, `Shouldly` | — |
| `tests/DocsFlow.Api.Tests` | test | Сквозной вход через реальный Keycloak | `Testcontainers.Keycloak`, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing` | — |

`DocsFlow.Users` **не зависит от ASP.NET Core**: на вход провижининга подаётся собственный тип
`ExternalIdentity`, а не `ClaimsPrincipal`. Разбор claims — работа веб-слоя. Так провижининг
тестируется без поднятия приложения, а будущий мобильный Bearer-конвейер переиспользует его как есть.

Конвейер аутентификации кладётся в `src/DocsFlow.Api/Authentication/`, а не в отдельный проект:
он неотделим от ASP.NET-хоста, а тестируется через `WebApplicationFactory` — то есть всё равно
через `DocsFlow.Api`. Вынести в `DocsFlow.Authentication` тривиально, если понадобится.

Новые проекты добавляются в `DocsFlow.slnx` (папки `/src/` и `/tests/`).

## Таблица users

Миграция `CreateUsers` в `DocsFlow.Database.Migrator`, `[Migration(20260730130000)]`:

```
users
  id                uuid          primary key
  keycloak_subject  text          not null, unique   -- claim sub, связь с IdP
  email             text          not null, index
  display_name      text          null
  created_at        timestamptz   not null default now()
  updated_at        timestamptz   not null default now()
  last_login_at     timestamptz   null
```

**`id` — свой `uuid`, а не Keycloak-овский `sub` и не `bigint identity.** Свой, потому что
идентификатор пользователя пойдёт во внешние ссылки и в чужие таблицы, и он не должен зависеть от
жизни записи в IdP (смена realm, миграция на другой провайдер, перевыпуск пользователя). `uuid`, а
не последовательность, — чтобы идентификатор в URL не раскрывал число пользователей сервиса.
Генерируется приложением через `Guid.CreateVersion7()` (.NET 9+): значения монотонны по времени,
поэтому вставки не фрагментируют B-tree, в отличие от v4. Дефолта в БД нет — идентификатор
назначает код.

**`keycloak_subject` — единственный уникальный ключ связи с IdP.** `sub` неизменен для
пользователя, в отличие от email.

**На `email` — обычный индекс, не уникальный.** Уникальность email обеспечивает Keycloak (realm:
`Duplicate emails: off`, `Login with email: on`). Дублировать её ограничением в БД вредно: если
настройка realm однажды разойдётся с ожиданием, уникальный индекс превратит вход пользователя в
500 на провижининге, тогда как ущерба от двух одинаковых email в нашей таблице нет — связь всё
равно по `sub`. Индекс нужен для поиска пользователя при будущих приглашениях в space.

**Колонки `status` (active/blocked) нет** — осознанно. Блокировка пользователя это работа
Keycloak: отключённый там пользователь не проходит обновление сессии и выпадает из приложения (см.
«Обновление сессии»). Локальный флаг был бы вторым источником истины для того же факта.

`Down()` — `Delete.Table("users")`.

## DocsFlow.Users

### Модель

```csharp
// Профиль пользователя в нашей базе. Личность подтверждает Keycloak, здесь — то, что принадлежит нам.
public sealed record User(
    Guid Id,
    string KeycloakSubject,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt);

// Личность, подтверждённая внешним провайдером. Собирается веб-слоем из claims.
public sealed record ExternalIdentity(
    string Subject,
    string Email,
    string? DisplayName,
    bool EmailVerified);
```

`timestamptz → DateTime` (Kind=Utc) — как в `DemoNote`. Ролей и прав в модели нет: их не просили,
а внутри space они появятся своей задачей и по своей модели.

### Репозиторий

```csharp
public interface IUserRepository
{
    // Создаёт запись или обновляет профиль существующей. Возвращает актуальное состояние.
    Task<User> UpsertBySubjectAsync(ExternalIdentity identity, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

`UpsertBySubjectAsync` — один запрос, без предварительного `SELECT`:

```sql
INSERT INTO users (id, keycloak_subject, email, display_name, last_login_at)
VALUES (@id, @subject, @email, @displayName, now())
ON CONFLICT (keycloak_subject) DO UPDATE
   SET email         = excluded.email,
       display_name  = excluded.display_name,
       updated_at    = now(),
       last_login_at = now()
RETURNING id, keycloak_subject, email, display_name, created_at, updated_at, last_login_at;
```

Почему именно так: два одновременных первых входа (пользователь нажал «войти» в двух вкладках) на
паре `SELECT`+`INSERT` дают гонку и `unique_violation`. `ON CONFLICT ... RETURNING` атомарен и
обходится одним round-trip.

`AddUsers(this IServiceCollection services)` регистрирует `IUserRepository` как scoped.
`InternalsVisibleTo("DocsFlow.Users.Tests")`.

## Конвейер аутентификации (DocsFlow.Api/Authentication)

### Конфигурация

`KeycloakOptions`, секция **`Authentication:Keycloak`**:

| Свойство | Тип | Замечание |
|---|---|---|
| `Authority` | `string` | `[Required]`, напр. `http://localhost:8081/realms/docsflow` |
| `ClientId` | `string` | `[Required]` |
| `ClientSecret` | `string` | `[Required]`, конфиденциальный клиент |
| `RequireHttps` | `bool` | по умолчанию `true`; управляет и метаданными, и флагом `Secure` у cookie |

Регистрация — как у `PostgresOptions`: `Bind` + `ValidateDataAnnotations` + `ValidateOnStart`.

### Схемы

```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(/* сессия */)
    .AddOpenIdConnect("keycloak", /* вход */);
```

Cookie — схема по умолчанию: каждый обычный запрос проверяет только её, в Keycloak не ходит.
OIDC-схема задействуется исключительно на входе и выходе.

**Cookie:**
- `HttpOnly = true` — JavaScript не читает; в этом весь смысл BFF.
- `SecurePolicy` — `Always` либо `SameAsRequest` по настройке `RequireHttps`. Не по имени
  окружения: настройка, ослабляющая защиту сессии, должна быть видна в конфиге, а не выводиться
  из «Development».
- `SameSite = Lax`, а не `Strict`: `Strict` не отправит cookie на возврате из Keycloak, и вход
  зациклится. `Lax` при этом уже блокирует cross-site POST — базовая защита от CSRF (см. ниже).
- `ExpireTimeSpan = 14 дней`, `SlidingExpiration = false` — срок жизни определяется обновлением
  токена, а не активностью.
- `SaveTokens = true` на OIDC: id/access/refresh-токены лежат внутри шифрованной cookie. Тикет
  может перевалить 4 КБ — ASP.NET режёт cookie на части автоматически. Серверное хранилище
  тикетов (`ITicketStore`) — возможное будущее ужесточение, сейчас YAGNI.

**OIDC:**
- `ResponseType = "code"`, PKCE включён по умолчанию (`UsePkce`), клиент конфиденциальный.
- `Scope = openid profile email`. `offline_access` **не запрашивается**: он выдаёт offline-токен,
  живущий дольше SSO-сессии, что противоречит идее «выход из Keycloak гасит доступ». Обычный
  refresh-токен стандартного flow — то, что нужно BFF.
- `MapInboundClaims = false` — иначе `sub`/`email` переименовываются в длинные URI-имена и код
  начинает зависеть от таблицы легаси-маппинга. Читаем `sub`, `email`, `email_verified`,
  `preferred_username` под их настоящими именами; `NameClaimType = "preferred_username"`.
- `GetClaimsFromUserInfoEndpoint` не включается: `profile`+`email` уже приносят нужное в id-токене.

### Провижининг при входе

Хук — `OnTicketReceived` OIDC-схемы (claims уже есть, cookie ещё не записана):

1. Собрать `ExternalIdentity` из claims.
2. Если `email_verified != true` — прервать вход (`403`, понятная запись в лог). Keycloak с
   включённой проверкой email до сюда и не пустит; это второй рубеж, а не основной.
3. `IUserRepository.UpsertBySubjectAsync(...)`.
4. Добавить в principal claim `docsflow:user_id` = `User.Id`.

**Что кладётся в cookie, а что читается из БД.** В cookie попадает только `user_id` — он неизменен
навсегда, поэтому не устаревает. Изменяемая часть профиля (email, отображаемое имя, а в будущем —
членство в space) в cookie **не кладётся**: иначе она применялась бы лишь после перелогина.
Такие данные читаются из БД в тот момент, когда нужны.

```csharp
public interface ICurrentUser
{
    // Идентификатор из claim — без обращения к БД. null, если запрос не аутентифицирован.
    Guid? UserId { get; }

    // Полный профиль из БД. null, если запрос не аутентифицирован.
    Task<User?> GetAsync(CancellationToken ct = default);
}
```

Реализация — scoped, поверх `IHttpContextAccessor`, с мемоизацией результата на время запроса.

### Обновление сессии и отзыв доступа

Событие `OnValidatePrincipal` cookie-схемы: если access-токен из тикета истёк — обменять
refresh-токен в Keycloak, обновить тикет (`ShouldRenew = true`); если обмен не удался —
`RejectPrincipal()` и `SignOutAsync`.

Это не только про продление сессии: так до приложения доходит отзыв доступа. Пользователь,
отключённый или удалённый в Keycloak, либо нажавший «выйти со всех устройств», перестаёт проходить
обновление и выпадает из сервиса в пределах времени жизни access-токена (5 минут), а не через 14
дней. Именно поэтому локальная колонка `status` не нужна.

### Эндпоинты

| Метод и путь | Поведение |
|---|---|
| `GET /api/auth/login?returnUrl=/` | `Challenge("keycloak")`. `returnUrl` проверяется на локальность (`Url.IsLocalUrl`) — иначе open redirect. |
| `POST /api/auth/logout` | `SignOutAsync` cookie + OIDC (RP-initiated logout, гасит и SSO-сессию Keycloak). |
| `GET /api/me` | `[Authorize]`. Отдаёт `id`, `email`, `displayName`. |

**Неаутентифицированный запрос к API получает `401`, а не редирект на Keycloak.** Дефолтное
поведение cookie-схемы — `302` на страницу входа; для API это превращает «нужно войти» в
непарсируемый HTML логин-страницы. `Events.OnRedirectToLogin` заменяется на возврат `401`.
Редирект в Keycloak происходит только через явный `/api/auth/login`.

### CSRF

`SameSite = Lax` не отправляет cookie при cross-site POST — то есть основной вектор CSRF закрыт
уже им, при условии что GET-эндпоинты не имеют побочных эффектов (это и так инвариант REST).
Полноценные antiforgery-токены ASP.NET Core добавляются вместе с UI, когда появятся формы и
станет понятен формат запросов SPA. Здесь фиксируется как явно отложенное, а не забытое.

### Data Protection

Cookie шифруется ключами Data Protection. По умолчанию в контейнере они лежат в файловой системе и
теряются при пересоздании — все сессии инвалидируются, а при нескольких репликах API сессия,
выданная одной, не читается другой. **Перед публичным запуском** ключи должны переехать в общее
персистентное хранилище (том или Postgres). Сейчас API в compose нет, поэтому это фиксируется как
блокер запуска, а не делается.

## docker compose

Добавляются два сервиса, инициализация БД Keycloak и один том.

```yaml
  keycloak:
    image: quay.io/keycloak/keycloak:26.7.0
    command: ["start-dev", "--import-realm"]
    environment:
      KC_BOOTSTRAP_ADMIN_USERNAME: ${KEYCLOAK_ADMIN:-admin}
      KC_BOOTSTRAP_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD:-admin}
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: ${POSTGRES_USER:-docsflow}
      KC_DB_PASSWORD: ${POSTGRES_PASSWORD:-docsflow-secret}
      KC_HEALTH_ENABLED: "true"
    volumes:
      - ./infra/keycloak/realm-export.json:/opt/keycloak/data/import/realm.json:ro
    ports:
      - "8081:8080"
    depends_on:
      postgres:
        condition: service_healthy

  mailpit:
    image: axllent/mailpit:v1.30.6
    ports:
      - "1025:1025"   # SMTP для Keycloak
      - "8025:8025"   # веб-интерфейс: читать письма подтверждения
```

Замечания, каждое из которых иначе стоит часа отладки:

- **`start-dev` — только для локальной разработки.** Он выключает проверку hostname и работает без
  TLS. Публичный запуск — `start` с `KC_HOSTNAME` и TLS; это часть будущей задачи про деплой.
- **Keycloak нужна своя база в том же инстансе Postgres.** Заводится скриптом
  `infra/postgres/init/01-create-keycloak-db.sql` (`CREATE DATABASE keycloak;`), подключённым в
  `/docker-entrypoint-initdb.d/`. Скрипт исполняется **только при инициализации пустого тома** —
  на уже существующем `postgres-data` базу придётся создать руками. Записать в README-раздел
  `CLAUDE.md`. Отдельный инстанс Postgres для Keycloak — избыточен для локального окружения.
- **Healthcheck не заводится** (отступление от первоначального плана). В образе Keycloak 26 нет ни
  `curl`, ни `wget`, так что проверку пришлось бы писать пробой через `/dev/tcp` из bash. При этом
  от готовности Keycloak в compose ничего не зависит — приложения там нет, — поэтому хрупкая
  проверка приносила бы только ложное «unhealthy» в `docker compose ps`. Заводить её имеет смысл
  тогда, когда в compose появится API с `depends_on`.
- **Порт на хосте — 8081.** Изначально планировался 8080, но параллельная задача успела занять его
  под nginx с веб-клиентом; 8080 отдан клиенту как входной точке. Как и MinIO, Keycloak
  поднимается **только в основном репозитории**, один на все worktree.

Том `keycloak` не нужен: состояние Keycloak живёт в Postgres.

## Realm в git

`infra/keycloak/realm-export.json` — realm `docsflow`, версионируется вместе с кодом, чтобы
окружение поднималось одной командой и настройки не расходились между машинами.

| Настройка | Значение | Зачем |
|---|---|---|
| `registrationAllowed` | `true` | Саморегистрация из интернета |
| `verifyEmail` | `true` | Иначе регистрация на чужой email |
| `loginWithEmailAllowed` | `true` | Вход по email |
| `duplicateEmailsAllowed` | `false` | Обеспечивает уникальность email вместо БД |
| `resetPasswordAllowed` | `true` | Восстановление пароля |
| `bruteForceProtected` | `true` | Публичный вход без этого не выставляют |
| `accessTokenLifespan` | 5 мин | Определяет задержку отзыва доступа |
| `ssoSessionIdleTimeout` | 14 дней | Согласовано с `ExpireTimeSpan` cookie |
| SMTP | `mailpit:1025` | Локально письма читаются в UI Mailpit |

Клиент `docsflow-web`: конфиденциальный, Standard Flow, PKCE `S256` обязателен, Direct Access
Grants выключен, redirect URI `http://localhost:5xxx/signin-oidc` и
`http://localhost:5xxx/signout-callback-oidc` (порт — из `launchSettings.json`).

**Секрет клиента в git — только dev-значение**, тем же порядком, что уже действует для
`docsflow-secret` у MinIO и Postgres: dev-креденшелы лежат в репозитории, продовые приходят из
окружения. `appsettings.Development.json` дополняется секцией `Authentication:Keycloak` с этим же
значением.

## Тесты

### tests/DocsFlow.Users.Tests

Postgres в Testcontainers, миграции — тем же раннером; `PostgresFixture` копируется по образцу
существующей (общий базовый класс между тестовыми проектами не заводим — дублирование фикстуры
дешевле связи между тестовыми сборками).

1. Первый `UpsertBySubjectAsync` создаёт запись: `Id` не пустой, `CreatedAt` и `LastLoginAt`
   заполнены.
2. Повторный вызов с тем же `sub` и изменённым email — обновляет профиль, `Id` и `CreatedAt` те же,
   строк по-прежнему одна.
3. Два `UpsertBySubjectAsync` с одним `sub` параллельно (`Task.WhenAll`) — оба успешны, в таблице
   одна строка. Это тест ровно на ту гонку, из-за которой выбран `ON CONFLICT`.
4. `GetByIdAsync` для неизвестного id — `null`, для созданного — профиль.
5. Миграция: таблица `users` существует, `keycloak_subject` уникален, `email` проиндексирован
   **без** уникальности.

Валидация `KeycloakOptions` живёт в `DocsFlow.Api.Tests` — сам тип принадлежит `DocsFlow.Api`.

### tests/DocsFlow.Api.Tests

Сквозной вход через настоящий Keycloak — единственная проверка, подтверждающая, что конвейер
собран верно.

Фикстура: `KeycloakContainer` (`Testcontainers.Keycloak`, образ пинуется = `quay.io/keycloak/keycloak:26.7.0`)
с тем же `realm-export.json` через `WithResourceMapping` + `--import-realm`, плюс
`PostgreSqlContainer` с накатанными миграциями, плюс `WebApplicationFactory<Program>` с
переопределённой конфигурацией на адреса контейнеров. Тестовый пользователь заводится через Admin
API Keycloak (в тесте это допустимо — в приложении Admin API не используется), сразу с
`emailVerified = true`.

1. `GET /api/me` без cookie → `401` (а не `302`).
2. Полный вход: `HttpClient` с `CookieContainer` и `AllowAutoRedirect`, `GET /api/auth/login` →
   редирект на Keycloak → разбор HTML формы входа и POST креденшелов → редирект на
   `/signin-oidc` → `GET /api/me` отдаёт `200` с email тестового пользователя. Далее — в таблице
   `users` появилась строка с этим `sub`.
3. Второй вход тем же пользователем не создаёт вторую строку и продвигает `last_login_at`.
4. `POST /api/auth/logout` → последующий `GET /api/me` даёт `401`.

Тест 2 разбирает HTML страницы входа Keycloak — то есть зависит от её разметки. Это осознанная
плата за проверку настоящего flow; поэтому образ Keycloak пинуется точной версией, а не тегом
`26.7`. Если `Testcontainers.Keycloak` не позволит переопределить команду для `--import-realm`,
падаем на обычный `ContainerBuilder` с образом Keycloak — модуль здесь удобство, а не необходимость.

Если сквозной тест окажется неустойчивым (тайминги старта Keycloak, разметка страницы), правильная
реакция — не удалять его, а стабилизировать: он единственный, кто ловит ошибки в связке
realm ↔ конфиг клиента ↔ настройки cookie.

## Пакеты (CPM → Directory.Packages.props)

| Пакет | Версия | Куда |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 10.0.10 | `DocsFlow.Api` |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.10 | `DocsFlow.Api.Tests` |
| `Testcontainers.Keycloak` | 4.13.0 | `DocsFlow.Api.Tests` |

Версии проверены в NuGet на 2026-07-30: MS-пакеты выравнены к уже стоящим 10.0.10,
`Testcontainers.Keycloak` — к уже стоящим модулям Testcontainers 4.13.0. Новых инфраструктурных
компонентов — два образа: Keycloak 26.7.0 и Mailpit v1.30.6 (по Mailpit см. «Принятое допущение»).

## Правка CLAUDE.md

Требуется решением владельца о публичном мультиарендном сервисе. Пункты 1–4 **уже применены**
(правка направления сервиса, отдельно от реализации); 5 — вместе с реализацией, когда сервисы
действительно появятся в compose.

1. **Раздел «Проект»** — переписать: не «приватная self-hosted система для семейных документов», а
   публичный сервис с саморегистрацией, личными документами и space с приглашёнными участниками.
2. **Принцип «Single-tenant»** — заменить на **«Изоляция данных арендатора»**; в колонке «что
   запрещает»: запрос к данным без фильтра по владельцу или членству в space; идентификатор
   арендатора, приходящий из тела запроса, а не из аутентифицированной сессии.
3. **Принцип «Приватность по умолчанию»** — сохранить, уточнив: речь о данных пользователей, а не
   об изоляции инсталляции. Формулировка запрета («вызов внешнего API, который нельзя выключить
   конфигом») остаётся в силе.
4. **Раздел «Статус»** — убрать Notion как источник контекста: база заметок «Family Vault»
   неактуальна. Источник требований — прямые указания владельца, принятые решения — спеки в
   `docs/superpowers/specs/`.
5. **Раздел «Окружение»** — Keycloak и Mailpit поднимаются только в основном репозитории, порты
   8081/8025/1025; про базу `keycloak` и то, что init-скрипт не отработает на существующем томе.
6. **Раздел «Команды»** — без изменений, `docker compose up -d` покрывает новые сервисы.

Принципы «Оригинал — источник истины», «Provider-agnostic», «Деградация, а не отказ», «Никаких
ответов без ссылки» не затрагиваются.

## Принятое допущение

**Mailpit** — единственный компонент в этой спеке, которого владелец прямо не заказывал. Он нужен,
чтобы работало то, что заказано: саморегистрация с подтверждением email. Реальный SMTP в
разработке использовать нельзя, а выключить `verifyEmail` в dev-realm означало бы, что локальный
realm расходится с продовым и flow регистрации не проверяется ни тестом, ни руками. Контейнер
только принимает письма и показывает их в веб-интерфейсе; в приложение он не входит и в
продакшн-конфигурацию не попадает. Убирается одной строкой из compose, если не нужен.

## Воркфлоу

- Вся работа — в worktree `auth/users-and-keycloak` от `origin/dev`; основной репозиторий не
  трогаем.
- Регулярный `git fetch origin && git merge origin/dev`; интеграция в `dev` по правилам
  `CLAUDE.md` (зелёные `dotnet build` и `dotnet test`, `push origin HEAD:dev`, без форса).

## Замечания по реализации (по итогам прогона)

Неочевидные вещи, на которые наткнётся следующая задача. Первые три касаются только тестов, но
съели больше всего времени.

- **`ConfigureAppConfiguration` в `WebApplicationFactory` не работает при minimal hosting в нашем
  случае.** Колбэки отложенного хост-билдера применяются только к первому `builder.Build()`, а
  сквозной тест поднимает второй хост (см. ниже) — и настройки до него не доходят. Молча: значения
  берутся из `appsettings`, тест валится с непонятной ошибкой. Работают `UseSetting` и
  `UseEnvironment` — они часть конфигурации хоста, а не колбэк.
- **`WebApplicationFactory` требует хост именно с `TestServer`**: она сразу приводит `IServer` к
  этому типу и падает с `InvalidCastException`, если подменить сервер на Kestrel. Поэтому строятся
  **два** хоста: один с `TestServer` для самой фабрики, второй на реальном Kestrel — по нему ходят
  тесты. Реальный Kestrel обязателен: вход уводит клиента редиректами на Keycloak в контейнере, а
  `TestServer` живёт только в памяти.
- **Keycloak ставит `Secure` на свои cookie даже при `sslRequired: none`** (`KC_RESTART`,
  `AUTH_SESSION_ID` — `Secure; SameSite=None`). Браузеры считают `http://localhost` доверенным
  источником и такие cookie возвращают, а `CookieContainer` в .NET исключения не делает: сохраняет,
  но по http не отправляет. Keycloak на это отвечает «Restart login cookie not found». Перехватить
  снаружи нельзя — cookie обрабатываются внутри `HttpClientHandler`, — поэтому в тестах свой
  `BrowserClient` с ручным хранилищем cookie и ручным проходом по редиректам.
- **Пользователю нужны имя и фамилия.** Без них Keycloak не пускает дальше входа и требует
  дозаполнить профиль (обязательное действие `VERIFY_PROFILE`) — вход останавливается на странице
  «Update Account Information». Касается и тестовых пользователей, и живой регистрации.
- **`AsString()` в FluentMigrator на Postgres — это `varchar(255)`**, а не `text`. Для `email` и
  `keycloak_subject` ограничение длины ничего не защищает, поэтому колонки объявлены через
  `AsCustom("text")`.
- **Два `RedirectContext`.** У cookie-схемы он обобщённый и лежит в
  `Microsoft.AspNetCore.Authentication`, у OIDC — необобщённый в
  `...Authentication.OpenIdConnect`. При обоих `using` компилятор выбирает второй; разводится
  алиасом.
- **`id_token` для выхода нужно достать до разлогина.** `SignOutAsync` OIDC-схемы берёт
  `id_token_hint` из переданных ему `AuthenticationProperties`, а не из текущей сессии. Порядок:
  `AuthenticateAsync` → забрать `id_token` → выйти из cookie → выйти из OIDC с этим токеном.

## Вне скоупа

- **Space, участники, приглашения, шаринг** — следующая задача. Здесь только фундамент.
- **Роли и права любого вида**, включая глобальное «пользователь / оператор сервиса». Различие
  только «вошёл / не вошёл».
- **Вход через Google и Apple** — решением владельца. Включается в realm позже, код не меняется.
- Bearer-схема для мобильных клиентов (аддитивна, когда появится клиент).
- Antiforgery-токены и CORS — вместе с UI.
- Брендированная тема Keycloak.
- Продакшн-конфигурация: `start` вместо `start-dev`, hostname и TLS, персистентные ключи Data
  Protection, продовые секреты из окружения, API в compose.
- Удаление аккаунта и выгрузка данных пользователя (потребуется публичному сервису, но это
  продуктовая задача со своими решениями по каскадам и срокам хранения).
- Ограниченная роль БД для приложения (least-privilege) — как и раньше, отложено.
