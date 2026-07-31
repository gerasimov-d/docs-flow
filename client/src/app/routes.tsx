import { createBrowserRouter } from 'react-router'

import { HomePage } from '@/pages/home'

/**
 * Карта маршрутов. Каждый маршрут ссылается на страницу из слоя `pages` и ничего
 * не знает о её внутренностях.
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: <HomePage />,
  },
])
