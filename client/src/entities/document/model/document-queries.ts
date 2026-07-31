import { useQuery } from '@tanstack/react-query'

import {
  getDocument,
  getInbox,
  getLibrary,
  getPipelineHealth,
  getProcessingCount,
} from '../api/document-api'
import type { DocumentQuery } from './document'

export const documentKeys = {
  all: ['documents'] as const,
  inbox: (spaceId: string) => [...documentKeys.all, spaceId, 'inbox'] as const,
  library: (spaceId: string, query: DocumentQuery) =>
    [...documentKeys.all, spaceId, 'library', query] as const,
  detail: (spaceId: string, documentId: string) =>
    [...documentKeys.all, spaceId, 'detail', documentId] as const,
  processing: (spaceId: string) => [...documentKeys.all, spaceId, 'processing'] as const,
  health: (spaceId: string) => [...documentKeys.all, spaceId, 'health'] as const,
}

/** Задержка конвейера: провайдер распознавания не отвечает, документы стоят в очереди. */
export function usePipelineHealth(spaceId: string) {
  const query = useQuery({
    queryKey: documentKeys.health(spaceId),
    queryFn: () => getPipelineHealth(spaceId),
  })

  return query.data ?? { delayed: false, reason: null }
}

export function useInbox(spaceId: string) {
  const query = useQuery({
    queryKey: documentKeys.inbox(spaceId),
    queryFn: () => getInbox(spaceId),
  })

  return { documents: query.data ?? [], isLoading: query.isPending }
}

export function useLibrary(spaceId: string, filters: DocumentQuery) {
  const query = useQuery({
    queryKey: documentKeys.library(spaceId, filters),
    queryFn: () => getLibrary(spaceId, filters),
    // Смена фильтра не должна ронять таблицу в скелет: прошлая выдача остаётся на экране,
    // пока считается новая.
    placeholderData: (previous) => previous,
  })

  return { documents: query.data ?? [], isLoading: query.isPending }
}

export function useDocument(spaceId: string, documentId: string) {
  const query = useQuery({
    queryKey: documentKeys.detail(spaceId, documentId),
    queryFn: () => getDocument(spaceId, documentId),
  })

  return { document: query.data ?? null, isLoading: query.isPending, error: query.error }
}

/** Сколько документов сейчас в работе. Ноль означает «пульсирующую точку не показываем». */
export function useProcessingCount(spaceId: string) {
  const query = useQuery({
    queryKey: documentKeys.processing(spaceId),
    queryFn: () => getProcessingCount(spaceId),
  })

  return query.data ?? 0
}
