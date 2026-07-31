import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'

/**
 * Свой клиент на каждый тест: общий кеш протёк бы между тестами, и порядок их запуска
 * начал бы влиять на результат. Повторы выключены — иначе упавший запрос молча
 * растягивает тест на секунды.
 */
function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 0 },
    },
  })
}

/** Рендер с провайдерами приложения. Роутер тест добавляет сам, если он нужен. */
export function renderWithProviders(ui: ReactElement) {
  const queryClient = createTestQueryClient()

  return {
    queryClient,
    ...render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>),
  }
}
