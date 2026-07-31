import { apiFetch, mockRequest } from '@/shared/api'

/**
 * Контекст — тематическое направление внутри space: «авто», «медицина».
 * Плоский список без вложенности; у документа он один и он необязателен.
 */
export interface DocumentContext {
  id: string
  name: string
  createdAt: string
}

/** Сколько документов в контексте и когда был последний. Показывается на экране контекстов. */
export interface ContextUsage {
  documentCount: number
  lastDocumentDate: string | null
}

export function getContexts(spaceId: string): Promise<DocumentContext[]> {
  return apiFetch<DocumentContext[]>(`/spaces/${spaceId}/contexts`)
}

export function createContext(spaceId: string, name: string): Promise<DocumentContext> {
  return apiFetch<DocumentContext>(`/spaces/${spaceId}/contexts`, {
    method: 'POST',
    body: JSON.stringify({ name }),
  })
}

/**
 * Счётчики по контекстам, ключ — имя контекста.
 *
 * TODO(бэкенд): считать на сервере и отдавать прямо в `GET /api/spaces/{spaceId}/contexts` —
 * `documentCount` и `lastDocumentDate` рядом с именем. Пока документов в API нет, экран
 * контекстов показывает демонстрационные числа только для контекстов из фикстур, а для
 * настоящих, заведённых через API, — честный ноль.
 */
export function getContextUsage(spaceId: string): Promise<Record<string, ContextUsage>> {
  void spaceId

  return mockRequest<Record<string, ContextUsage>>({
    Медицина: { documentCount: 62, lastDocumentDate: '2026-03-12' },
    Финансы: { documentCount: 38, lastDocumentDate: '2026-02-15' },
    Авто: { documentCount: 21, lastDocumentDate: '2025-09-02' },
    Недвижимость: { documentCount: 17, lastDocumentDate: '2026-01-10' },
    Дети: { documentCount: 29, lastDocumentDate: '2025-06-21' },
    Образование: { documentCount: 14, lastDocumentDate: '2025-08-30' },
  })
}

/**
 * Удаление контекста.
 *
 * TODO(бэкенд): `DELETE /api/spaces/{spaceId}/contexts/{contextId}`. Документы при этом
 * остаются в space и становятся «без контекста» — контекст не папка хранения, и об этом
 * прямо сказано в подтверждении удаления.
 */
export function deleteContext(spaceId: string, contextId: string): Promise<void> {
  void spaceId
  void contextId

  return mockRequest<void>(undefined)
}
