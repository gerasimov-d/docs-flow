import { mockRequest } from '@/shared/api'

import type { DocumentDetail, DocumentQuery, DocumentSummary } from '../model/document'
import { documentDetailFixtures, documentFixtures } from './fixtures'

/*
 * Документов в API пока нет ни одного. Все функции ниже — заглушки поверх фикстур, каждая
 * помечена TODO с ожидаемым маршрутом.
 *
 * `spaceId` принимается всеми функциями и будет уходить в путь запроса — это не задел на
 * будущее, а изоляция арендатора: метода, читающего документы без указания space, в контракте
 * не должно появиться даже временно, иначе изоляция станет вопросом дисциплины вызывающего.
 */

/**
 * Инбокс: что загружено за последние дни и в каком оно состоянии.
 *
 * TODO(бэкенд): `GET /api/spaces/{spaceId}/documents?sort=uploadedAt`.
 */
export function getInbox(spaceId: string): Promise<DocumentSummary[]> {
  void spaceId

  return mockRequest(
    [...documentFixtures].sort((left, right) => right.uploadedAt.localeCompare(left.uploadedAt)),
  )
}

/**
 * Библиотека: весь архив с фильтрами и сортировкой по дате документа.
 *
 * TODO(бэкенд): `GET /api/spaces/{spaceId}/documents` с параметрами `contextId`, `from`, `to`,
 * `kind`, `status`. Фильтрация обязана выполняться в запросе к базе, а не на клиенте —
 * иначе «Найдено 42» перестанет совпадать с тем, что реально доступно в space.
 */
export function getLibrary(spaceId: string, query: DocumentQuery = {}): Promise<DocumentSummary[]> {
  void spaceId

  const matched = documentFixtures
    .filter((document) => matchesQuery(document, query))
    .sort(byDocumentDateDesc)

  return mockRequest(matched)
}

/**
 * Карточка документа.
 *
 * TODO(бэкенд): `GET /api/spaces/{spaceId}/documents/{documentId}` вместе с распознанным
 * текстом запрошенной страницы.
 */
export function getDocument(spaceId: string, documentId: string): Promise<DocumentDetail> {
  void spaceId

  const summary = documentFixtures.find((document) => document.id === documentId)

  if (summary === undefined) {
    return Promise.reject(new Error(`Документа ${documentId} нет в архиве`))
  }

  const extras = documentDetailFixtures[documentId] ?? {
    recognizedPage: 1,
    recognizedParagraphs: [],
    recognitionWarning: null,
  }

  return mockRequest<DocumentDetail>({ ...summary, ...extras })
}

/**
 * Повторная обработка после сбоя стадии.
 *
 * TODO(бэкенд): `POST /api/spaces/{spaceId}/documents/{documentId}/reprocess`. Оригинал при
 * этом не трогается — источник истины остаётся тем же файлом, что загрузил пользователь.
 */
export function reprocessDocument(spaceId: string, documentId: string): Promise<void> {
  void spaceId
  void documentId

  return mockRequest<void>(undefined)
}

/**
 * Состояние конвейера целиком, а не отдельного документа.
 *
 * TODO(бэкенд): отдавать вместе с инбоксом. Недоступность OCR или LLM — не ошибка документа:
 * файлы приняты и стоят в очереди, обработка возобновится сама, и сказать это нужно один раз
 * сверху, а не подписью под каждой строкой.
 */
export function getPipelineHealth(
  spaceId: string,
): Promise<{ delayed: boolean; reason: string | null }> {
  void spaceId

  return mockRequest({ delayed: false, reason: null })
}

/**
 * Сколько документов сейчас в обработке. Показывается в шапке и в переключателе space.
 *
 * TODO(бэкенд): отдавать счётчиком вместе со списком space, чтобы не тянуть весь архив
 * ради одного числа.
 */
export function getProcessingCount(spaceId: string): Promise<number> {
  void spaceId

  return mockRequest(
    documentFixtures.filter(
      (document) => document.status === 'processing' || document.status === 'accepted',
    ).length,
  )
}

function matchesQuery(document: DocumentSummary, query: DocumentQuery): boolean {
  if (query.contextId !== undefined && (document.context?.id ?? null) !== query.contextId) {
    return false
  }

  if (query.kind !== undefined && document.kind !== query.kind) {
    return false
  }

  if (query.status !== undefined && document.status !== query.status) {
    return false
  }

  // Документ без даты в период не попадает — и в дизайне об этом прямо сказано отдельной
  // строкой со ссылкой «Показать их», а не молчаливым исключением из выдачи.
  if (query.from !== undefined || query.to !== undefined) {
    if (document.documentDate === null) {
      return false
    }

    if (query.from !== undefined && document.documentDate < query.from) {
      return false
    }

    if (query.to !== undefined && document.documentDate > query.to) {
      return false
    }
  }

  return true
}

function byDocumentDateDesc(left: DocumentSummary, right: DocumentSummary): number {
  return (right.documentDate ?? '').localeCompare(left.documentDate ?? '')
}

/** Сколько документов отфильтровано по причине отсутствующей даты. */
export function countUndatedDocuments(): number {
  return documentFixtures.filter((document) => document.documentDate === null).length
}
