# Миграционный раннер как one-shot контейнер (аналог minio-init для бакета).
# Контекст сборки — корень репозитория: нужны Directory.Build.props и Directory.Packages.props (CPM).
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Свойства сборки и версии пакетов MSBuild ищет вверх по дереву от проекта.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/DocsFlow.Database.Migrator/ ./src/DocsFlow.Database.Migrator/

RUN dotnet publish src/DocsFlow.Database.Migrator/DocsFlow.Database.Migrator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
# Npgsql при старте пробует подгрузить Kerberos/GSSAPI; без библиотеки он это переживает,
# но пишет пугающую ошибку в лог. Ставим её, чтобы логи мигратора были чистыми.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "DocsFlow.Database.Migrator.dll"]
