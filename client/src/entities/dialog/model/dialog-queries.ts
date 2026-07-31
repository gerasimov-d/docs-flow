import { useQuery } from '@tanstack/react-query'

import { getDialogHistory } from '../api/dialog-api'

export const dialogKeys = {
  all: ['dialog'] as const,
  history: (spaceId: string) => [...dialogKeys.all, spaceId, 'history'] as const,
}

/** История вопросов текущей сессии. Живёт внутри space: переключение обрывает диалог. */
export function useDialogHistory(spaceId: string) {
  const query = useQuery({
    queryKey: dialogKeys.history(spaceId),
    queryFn: () => getDialogHistory(spaceId),
  })

  return query.data ?? []
}
