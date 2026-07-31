import { Navigate, Outlet, useLocation } from 'react-router'

import { useSession } from '@/entities/session'

/**
 * Закрывает вложенные маршруты от неаутентифицированных.
 *
 * Это удобство, а не защита: настоящая проверка живёт в API, который на любой запрос без
 * сессии отвечает 401. Гард лишь избавляет от пустых экранов с ошибками.
 */
export function RequireAuth() {
  const { user, isLoading } = useSession()
  const location = useLocation()

  // Пока сессия неизвестна, нельзя ни пускать, ни отправлять на вход: перезагрузка страницы
  // вошедшим пользователем мигала бы формой входа.
  if (isLoading) {
    return <p className="p-8 text-gray-500">Загрузка…</p>
  }

  if (!user) {
    const returnUrl = `${location.pathname}${location.search}`

    return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
  }

  return <Outlet />
}
