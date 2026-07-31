import { Navigate, useSearchParams } from 'react-router'

import { useSession } from '@/entities/session'
import { LoginButton } from '@/features/login'

/**
 * Отсекает чужие адреса. Дублирует проверку на бэкенде (`LocalUrlOrRoot`) — она и есть
 * настоящая защита от open redirect, здесь же просто не собираем заведомо негодную ссылку.
 */
function localPathOrRoot(returnUrl: string | null): string {
  if (returnUrl === null || !returnUrl.startsWith('/')) {
    return '/'
  }

  // `//host` и `/\host` браузер разбирает как переход на другой сайт.
  return returnUrl.startsWith('//') || returnUrl.startsWith('/\\') ? '/' : returnUrl
}

export function LoginPage() {
  const [searchParams] = useSearchParams()
  const { user, isLoading } = useSession()
  const returnUrl = localPathOrRoot(searchParams.get('returnUrl'))

  if (isLoading) {
    return <p className="p-8 text-gray-500">Загрузка…</p>
  }

  // Вошедшему показывать форму входа незачем.
  if (user) {
    return <Navigate to={returnUrl} replace />
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 p-8">
      <div className="flex flex-col items-center gap-2">
        <h1 className="text-3xl font-semibold">DocsFlow</h1>
        <p className="text-gray-500">Войдите, чтобы продолжить</p>
      </div>
      <LoginButton returnUrl={returnUrl} />
    </main>
  )
}
