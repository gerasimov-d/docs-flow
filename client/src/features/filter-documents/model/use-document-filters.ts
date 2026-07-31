import { useMemo, useState } from 'react'

import type { DocumentQuery } from '@/entities/document'

/**
 * Состояние фильтров. Пустая строка означает «не фильтруем» — это же значение выбрано
 * в списках по умолчанию, поэтому «сброшено» и «выбрано значение по умолчанию» не расходятся.
 */
export interface DocumentFilterState {
  contextId: string
  period: string
  kind: string
  status: string
}

export const emptyFilters: DocumentFilterState = {
  contextId: '',
  period: '',
  kind: '',
  status: '',
}

/**
 * Периоды заданы пресетами, а не произвольным диапазоном.
 *
 * В макете стоит конкретный интервал «01.01.2025 — 31.07.2026», но выбирать его нечем:
 * календарь-диапазон в дизайне не нарисован. Пресеты покрывают тот же сценарий и не требуют
 * додумывать интерфейс, которого нет.
 */
export const periodOptions = [
  { value: '', label: 'любой' },
  { value: '2026', label: '2026 год' },
  { value: '2025', label: '2025 год' },
  { value: 'last-year', label: 'последний год' },
]

export function useDocumentFilters(now = new Date()) {
  const [filters, setFilters] = useState<DocumentFilterState>(emptyFilters)

  const query = useMemo<DocumentQuery>(() => toQuery(filters, now), [filters, now])
  const activeCount = Object.values(filters).filter((value) => value !== '').length

  return {
    filters,
    query,
    activeCount,
    set: <TKey extends keyof DocumentFilterState>(key: TKey, value: string) => {
      setFilters((previous) => ({ ...previous, [key]: value }))
    },
    reset: () => {
      setFilters(emptyFilters)
    },
  }
}

function toQuery(filters: DocumentFilterState, now: Date): DocumentQuery {
  const query: DocumentQuery = {}

  if (filters.contextId !== '') {
    // `none` — это «без контекста», отдельное значение фильтра, а не его отсутствие.
    query.contextId = filters.contextId === 'none' ? null : filters.contextId
  }

  if (filters.kind === 'pdf' || filters.kind === 'image') {
    query.kind = filters.kind
  }

  if (
    filters.status === 'ready' ||
    filters.status === 'processing' ||
    filters.status === 'accepted' ||
    filters.status === 'failed'
  ) {
    query.status = filters.status
  }

  if (filters.period === 'last-year') {
    const from = new Date(now)

    from.setFullYear(from.getFullYear() - 1)
    query.from = from.toISOString().slice(0, 10)
    query.to = now.toISOString().slice(0, 10)
  } else if (filters.period !== '') {
    query.from = `${filters.period}-01-01`
    query.to = `${filters.period}-12-31`
  }

  return query
}
