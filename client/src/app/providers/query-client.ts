import { QueryClient } from '@tanstack/react-query'

/**
 * Единый клиент серверного состояния. Данные тянутся только через него — ручной
 * `useEffect` + `fetch` в компонентах не используется.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
})
