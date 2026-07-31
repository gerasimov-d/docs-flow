import { mockRequest } from '@/shared/api'

import type {
  Answer,
  DialogHistoryEntry,
  DialogQuery,
  SearchResult,
  SearchStrategy,
} from '../model/dialog'
import { answerFixtures, emptyAnswer, historyFixtures, searchFixtures } from './fixtures'

/*
 * Ни поиска, ни ответов в API пока нет. Заглушки повторяют будущий контракт целиком, включая
 * обязательный `spaceId`: и BM25, и вектор фильтруются по текущему space внутри запроса,
 * и фрагмент из чужого space не может попасть в слияние.
 */

/**
 * Поиск по фрагментам.
 *
 * TODO(бэкенд): `POST /api/spaces/{spaceId}/search` с телом `{ query, strategy, filters }`.
 * Гибрид — это слияние RRF выдач OpenSearch и pgvector; клиент про слияние знать не должен,
 * ему нужен только признак `matchedBy` у каждого фрагмента.
 */
export function runSearch(
  spaceId: string,
  query: string,
  strategy: SearchStrategy,
  filters: DialogQuery = {},
): Promise<SearchResult> {
  void spaceId
  void filters

  return mockRequest(searchFixtures(query, strategy))
}

/**
 * Ответ на вопрос естественным языком.
 *
 * TODO(бэкенд): `POST /api/spaces/{spaceId}/answers`. Ответ без локатора документа и фрагмента
 * наружу не отдаётся — это проверяет `RagService`, а не текст промпта, поэтому и на клиенте
 * `AnswerStatement` не бывает без `citation`.
 */
export function askQuestion(
  spaceId: string,
  question: string,
  filters: DialogQuery = {},
): Promise<Answer> {
  void spaceId
  void filters

  return mockRequest(answerFixtures(question) ?? emptyAnswer(question), 900)
}

/**
 * История вопросов текущей сессии.
 *
 * TODO(бэкенд): `GET /api/spaces/{spaceId}/dialogs`. История живёт в рамках сессии, и смена
 * space обрывает диалог — общей истории на все space быть не должно.
 */
export function getDialogHistory(spaceId: string): Promise<DialogHistoryEntry[]> {
  void spaceId

  return mockRequest(historyFixtures)
}
