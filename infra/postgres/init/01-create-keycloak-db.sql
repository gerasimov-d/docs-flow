-- Отдельная база для Keycloak в том же инстансе Postgres: своя схема у него сложная и
-- пересекаться с нашей ей незачем. Отдельный контейнер для локального окружения избыточен.
--
-- Скрипты из /docker-entrypoint-initdb.d исполняются ТОЛЬКО при инициализации пустого тома.
-- Если том postgres-data уже существует, базу нужно создать руками:
--   docker compose exec postgres createdb -U docsflow keycloak
CREATE DATABASE keycloak;
