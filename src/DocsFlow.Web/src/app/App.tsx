import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'

import { queryClient } from './providers/query-client'
import { router } from './routes'

/** Корень приложения: только глобальные провайдеры и роутер, никакой продуктовой логики. */
export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  )
}
