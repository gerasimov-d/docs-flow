import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// В dev фронт и API живут на разных портах, в проде — за одним nginx.
// Прокси уравнивает их: код всегда ходит на относительный `/api` и не знает про хосты.
const API_PROXY_TARGET = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5023'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // Порт фиксирован: он входит в адреса возврата, зарегистрированные в realm Keycloak.
    // Съехав на соседний порт, вход перестанет работать с невнятной ошибкой.
    port: 5173,
    strictPort: true,
    proxy: {
      // Сюда же попадают колбэки OIDC — они лежат под /api/auth/, а не по путям по умолчанию.
      '/api': {
        target: API_PROXY_TARGET,
        // Host намеренно не подменяется: из него API собирает redirect_uri для Keycloak.
        // С changeOrigin вход уводил бы на порт API (5023), где нет клиента, вместо 5173.
        changeOrigin: false,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./vitest.setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    css: true,
  },
})
