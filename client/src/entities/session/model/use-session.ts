import { useQuery } from '@tanstack/react-query'

import { getSession } from '../api/get-session'

/** Ключ кеша сессии. Вынесен, чтобы её можно было сбросить после выхода или смены профиля. */
export const sessionQueryKey = ['session'] as const

/**
 * Текущая сессия. `user === null` означает «точно не вошёл», а не «ещё не знаем» —
 * для второго есть `isLoading`.
 */
export function useSession() {
  const query = useQuery({
    queryKey: sessionQueryKey,
    queryFn: getSession,
    staleTime: 5 * 60_000,
    // 401 уже разобран в getSession, поэтому повторять запрос не на что.
    retry: false,
  })

  return {
    user: query.data ?? null,
    isLoading: query.isPending,
    error: query.error,
  }
}
