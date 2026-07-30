# Клиент — статика, собранная заранее. Отдаёт её nginx, бэкенд к раздаче непричастен.
# Контекст сборки — папка проекта: остальной репозиторий фронту не нужен.
# syntax=docker/dockerfile:1

FROM node:24.18.0-alpine AS build
WORKDIR /app

# Отдельный слой под зависимости: пересобирается только при изменении package-lock.json.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.30.4-alpine AS final
# Файлы из templates образ на старте прогоняет через envsubst. Так адрес API попадает
# в конфиг из окружения, а не зашивается в образ при сборке.
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY nginx/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
