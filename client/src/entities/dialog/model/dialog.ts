/**
 * Что пользователь просит одним и тем же полем. Режим определяется автоматически, но всегда
 * виден и всегда перебивается вручную — угадывание, которое нельзя поправить, хуже отсутствия
 * угадывания.
 */
export type DialogMode = 'search' | 'question'

/**
 * Как искать. Появилось вместе с полнотекстовым индексом рядом с векторным: чистый вектор
 * не умеет находить номер полиса как номер, а BM25 не понимает «когда истекает страховка».
 */
export type SearchStrategy = 'hybrid' | 'exact' | 'semantic'

/** Чем найден конкретный фрагмент. Показывается у каждого результата гибридной выдачи. */
export type MatchSource = 'exact' | 'semantic' | 'both'

/** Кусок текста выдачи: подсвечиваются только совпадения, а не весь фрагмент. */
export interface TextRun {
  text: string
  highlighted: boolean
}

/** Локатор первоисточника: без него фрагмент наружу не отдаётся. */
export interface DocumentLocator {
  documentId: string
  documentName: string
  /** У картинки страницы нет — это не пропуск данных, а свойство носителя. */
  page: number | null
  documentDate: string | null
  contextName: string | null
}

export interface SearchHit extends DocumentLocator {
  id: string
  runs: TextRun[]
  matchedBy: MatchSource
}

export interface SearchResult {
  hits: SearchHit[]
  documentCount: number
  strategy: SearchStrategy
  /** Сколько фрагментов отсечено минус-словами. В дизайне это отдельная подпись у счётчика. */
  excludedCount: number
  /** Разбор гибридной выдачи по индексам — правая колонка «Как собрана выдача». */
  breakdown: { exactCount: number; semanticCount: number; mergedCount: number } | null
}

/** Утверждение ответа вместе со ссылкой на фрагмент, из которого оно взято. */
export interface AnswerStatement {
  text: string
  citation: Citation
}

export interface Citation extends DocumentLocator {
  /** Номер сноски: `1`, `2`, `3`. Он же стоит в тексте ответа и на карточке источника. */
  index: number
  /** Дословная цитата. Пересказ здесь недопустим — на неё пользователь и опирается. */
  quote: string
}

export interface Answer {
  /** Пустой список утверждений — это «данных нет», и ответ так и звучит. */
  statements: AnswerStatement[]
  /** Текст, когда подходящих фрагментов не нашлось. Достраивать ответ в этом случае нельзя. */
  emptyReason: string | null
  /** Оговорка о полноте: ответ построен по найденным фрагментам, а не по всему содержимому. */
  disclaimer: string
}

/** Строка истории вопросов текущей сессии. Смена space обрывает диалог целиком. */
export interface DialogHistoryEntry {
  id: string
  question: string
  sourceCount: number
  askedAt: string
}

/** Отбор, которым сужается поиск. Повторяет фильтры библиотеки — так задумано в дизайне. */
export interface DialogQuery {
  contextId?: string | null
  from?: string
  to?: string
  recognizedOnly?: boolean
}
