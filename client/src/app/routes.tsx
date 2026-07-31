import { createBrowserRouter } from 'react-router'

import { HomePage } from '@/pages/home'
import { LoginPage } from '@/pages/login'

import { RequireAuth } from './require-auth'

/**
 * Карта маршрутов. Всё, кроме страницы входа, лежит под `RequireAuth`: закрытым по умолчанию
 * ошибиться сложнее, чем открытым, где легко забыть навесить проверку на новый маршрут.
 */
export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/',
        element: <HomePage />,
      },
    ],
  },
])
