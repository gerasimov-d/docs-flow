import { useQuery } from '@tanstack/react-query'

import { getContextUsage, getContexts } from '../api/context-api'

export const contextKeys = {
  all: ['contexts'] as const,
  list: (spaceId: string) => [...contextKeys.all, spaceId] as const,
  usage: (spaceId: string) => [...contextKeys.all, spaceId, 'usage'] as const,
}

/** Контексты текущего space. Пустой список — нормальное состояние, а не ошибка. */
export function useContexts(spaceId: string) {
  const query = useQuery({
    queryKey: contextKeys.list(spaceId),
    queryFn: () => getContexts(spaceId),
  })

  return { contexts: query.data ?? [], isLoading: query.isPending }
}

export function useContextUsage(spaceId: string) {
  const query = useQuery({
    queryKey: contextKeys.usage(spaceId),
    queryFn: () => getContextUsage(spaceId),
  })

  return query.data ?? {}
}
