import { useSession } from '@/entities/session'
import { LogoutButton } from '@/features/logout'

/** Стартовая страница. Продуктовое наполнение придёт отдельными фичами. */
export function HomePage() {
  const { user } = useSession()

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-8">
      <h1 className="text-3xl font-semibold">DocsFlow</h1>
      {/* Гард выше по дереву не пускает сюда без сессии, поэтому пользователь здесь всегда есть. */}
      <p className="text-gray-500">{user?.displayName ?? user?.email}</p>
      <LogoutButton />
    </main>
  )
}
