# Realm Keycloak

`realm-export.json` импортируется контейнером Keycloak при старте (`--import-realm`) и тем же
файлом пользуются интеграционные тесты — realm в тестах и локально совпадает с точностью до
адресов.

Импорт происходит **только если realm ещё не существует**. Правки в файле не подхватываются на
уже созданном realm: чтобы применить их, нужно удалить состояние Keycloak (оно лежит в базе
`keycloak`, см. `infra/postgres/init`) или изменить настройку руками в админке.

## Это локальный realm, не продовый

| Что | Здесь | В продакшне |
|---|---|---|
| `secret` клиента | `docsflow-web-dev-secret` в git | из окружения, в git не попадает |
| `sslRequired` | `none` — локально всё по http | `all` |
| `redirectUris` | `localhost:8080` (nginx с клиентом), `localhost:5023` и `localhost:7207` (API напрямую) | реальный домен |
| SMTP | контейнер `mailpit`, письма никуда не уходят | настоящий провайдер |

Dev-креденшелы в репозитории — тот же порядок, что уже действует для MinIO и Postgres
(`docsflow-secret`).

## Что настроено и почему

- `registrationAllowed`, `verifyEmail`, `resetPasswordAllowed`, `bruteForceProtected` — публичный
  вход. Регистрацию, подтверждение адреса, восстановление пароля и защиту от перебора делает
  Keycloak, приложение к этому не причастно.
- `duplicateEmailsAllowed: false` — именно эта настройка обеспечивает уникальность email. В таблице
  `users` уникального индекса на email нет намеренно.
- `accessTokenLifespan: 300` — определяет, через сколько до приложения доходит отзыв доступа:
  отключённый в Keycloak пользователь не проходит обмен refresh-токена и выпадает из сервиса.
- PKCE `S256` обязателен, `directAccessGrantsEnabled: false` — вход только через браузерный flow.
- Адреса возврата — `/api/auth/signin-callback` и `/api/auth/signout-callback`, а не дефолтные
  `/signin-oidc` и `/signout-callback-oidc`: nginx проксирует на бэкенд только `/api/`, поэтому
  колбэк вне этого префикса до приложения не дойдёт. Пути задаются в конвейере аутентификации
  (`CallbackPath` и `SignedOutCallbackPath`) и обязаны совпадать с этим списком.

## Локальные адреса

- Админка Keycloak — http://localhost:8081 (логин/пароль из `docker-compose.yml`). Порт 8081, а не
  8080: на 8080 хоста живёт nginx с веб-клиентом.
- Письма подтверждения — http://localhost:8025 (Mailpit).
