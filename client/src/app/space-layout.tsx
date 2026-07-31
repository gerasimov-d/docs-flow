import type { ReactElement } from 'react'
import { Navigate, Outlet, useOutletContext, useParams } from 'react-router'

import { useCurrentSpace, useSpaces } from '@/entities/space'
import type { Space } from '@/entities/space'
import { ForbiddenPage } from '@/pages/forbidden'
import { routes } from '@/shared/config'

/**
 * Разрешает space из адреса и не пускает дальше, если доступа к нему нет.
 *
 * Чужой space и несуществующий здесь неотличимы: оба ведут на «страница недоступна». Это не
 * упрощение, а требование изоляции — иначе по разнице ответов можно перебирать чужие архивы.
 *
 * Настоящая проверка живёт в API: фильтр членства висит на группе `/api/spaces/{spaceId}`
 * и отвечает 404 независимо от того, что именно не так. Здесь — только чтобы пользователь
 * увидел объяснение вместо пустого экрана с ошибками запросов.
 */
export function SpaceLayout() {
  const { spaceId } = useParams()
  const { space, isLoading } = useCurrentSpace(spaceId)

  if (isLoading) {
    return (
      <main className="bg-page flex min-h-screen items-center justify-center">
        <p className="text-ink-muted text-[14px]">Загрузка…</p>
      </main>
    )
  }

  if (space === null) {
    return <ForbiddenPage />
  }

  return <Outlet context={space} />
}

/**
 * Мост между маршрутом и страницей: отдаёт разрешённый space как обычный проп.
 *
 * Страницы благодаря этому не знают про роутер и остаются функциями от данных — их можно
 * рендерить в тесте, не поднимая маршрутизацию ради одного значения.
 */
export function SpaceScreen({ children }: { children: (space: Space) => ReactElement }) {
  return children(useOutletContext<Space>())
}

/**
 * Корень приложения. Ведёт в инбокс первого доступного space — своего экрана у `/` нет,
 * потому что показывать без space нечего: любой список принадлежит какому-то архиву.
 */
export function SpaceHome() {
  const { spaces, isLoading } = useSpaces()

  if (isLoading) {
    return (
      <main className="bg-page flex min-h-screen items-center justify-center">
        <p className="text-ink-muted text-[14px]">Загрузка…</p>
      </main>
    )
  }

  const first = spaces.at(0)

  // Space заводится при первом входе на стороне API, поэтому пустой список означает сбой
  // провижининга, а не нового пользователя.
  return first === undefined ? <ForbiddenPage /> : <Navigate to={routes.inbox(first.id)} replace />
}
