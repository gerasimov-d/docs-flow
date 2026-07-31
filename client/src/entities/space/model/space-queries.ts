import { useQuery } from '@tanstack/react-query'

import { getSpaceMembers, getSpaces } from '../api/space-api'
import type { Space } from '../api/space-api'

/** Ключи кеша space. Собраны в одном месте, чтобы мутации сбрасывали именно то, что меняют. */
export const spaceKeys = {
  all: ['spaces'] as const,
  list: () => [...spaceKeys.all, 'list'] as const,
  members: (spaceId: string) => [...spaceKeys.all, spaceId, 'members'] as const,
}

/** Все space вошедшего вместе с его ролью в каждом. Из этого списка живёт переключатель. */
export function useSpaces() {
  const query = useQuery({ queryKey: spaceKeys.list(), queryFn: getSpaces })

  return { spaces: query.data ?? [], isLoading: query.isPending, error: query.error }
}

/**
 * Текущий space — тот, что в маршруте.
 *
 * Ищется в списке доступных, а не запрашивается отдельно, и это не оптимизация: space,
 * которого нет в списке, для пользователя неотличим от несуществующего. Одна ветка
 * `space === null` закрывает и «нет доступа», и «удалён» — ровно как требует дизайн.
 */
export function useCurrentSpace(spaceId: string | undefined): {
  space: Space | null
  isLoading: boolean
} {
  const { spaces, isLoading } = useSpaces()

  return {
    space: spaces.find((candidate) => candidate.id === spaceId) ?? null,
    isLoading,
  }
}

/** Состав space. Видят только свои — фильтр членства стоит на группе маршрутов в API. */
export function useSpaceMembers(spaceId: string) {
  const query = useQuery({
    queryKey: spaceKeys.members(spaceId),
    queryFn: () => getSpaceMembers(spaceId),
  })

  return { members: query.data ?? [], isLoading: query.isPending }
}
