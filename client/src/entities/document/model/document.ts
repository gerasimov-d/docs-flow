/**
 * Состояние документа в конвейере. Четыре значения из спецификации, и все четыре видны
 * в интерфейсе: «принят» — это не «обрабатывается», а файл, до которого очередь не дошла.
 */
export type DocumentStatus = 'accepted' | 'processing' | 'ready' | 'failed'

/** Чем документ был исходно: сканом-картинкой или PDF. Определяет иконку и способ показа. */
export type DocumentKind = 'pdf' | 'image'

/** Снимок контекста внутри документа: у API это вложенный объект, а не отдельный запрос. */
export interface DocumentContextRef {
  id: string
  name: string
}

/**
 * Ход обработки. Названия стадий приходят с сервера списком, а не зашиты в клиент:
 * конвейер меняется (полнотекстовый индекс добавили седьмой стадией уже после первой
 * версии), и клиент, знающий их наперёд, врал бы при каждом таком изменении.
 */
export interface ProcessingState {
  stages: string[]
  /** Номер текущей стадии, считая с 1. У упавшего документа — та, на которой упало. */
  currentStage: number
  etaMinutes: number | null
  /** Строка под индикатором: «В очереди, 2-й из 3», «Приём и нормализация сохранены». */
  note: string | null
}

export interface DocumentSummary {
  id: string
  name: string
  kind: DocumentKind
  /** Как формат называется для человека: `PDF`, `JPEG`, `HEIC`. */
  formatLabel: string
  sizeBytes: number
  pageCount: number | null
  uploadedAt: string
  /** Дата самого документа, а не загрузки. Может отсутствовать — распознать удаётся не всегда. */
  documentDate: string | null
  context: DocumentContextRef | null
  status: DocumentStatus
  processing: ProcessingState | null
  /** Причина отказа человеческим языком: «Файл защищён паролем — распознать не удалось». */
  failure: string | null
}

/** Абзац распознанного текста. Подсвеченный — тот, по которому документ открыли из цитаты. */
export interface RecognizedParagraph {
  text: string
  highlighted: boolean
}

export interface DocumentDetail extends DocumentSummary {
  recognizedPage: number
  recognizedParagraphs: RecognizedParagraph[]
  /** Предупреждение о качестве распознавания — текст сохранён, но может содержать ошибки. */
  recognitionWarning: string | null
}

/** Отбор документов. Совпадает с фильтрами поиска — в дизайне это специально одно и то же. */
export interface DocumentQuery {
  /** `null` — «без контекста», `undefined` — контекст не фильтруется. */
  contextId?: string | null
  from?: string
  to?: string
  kind?: DocumentKind
  status?: DocumentStatus
}

interface StatusPresentation {
  label: string
  /** Классы плашки и точки. Статус различается и текстом, и цветом — не только цветом. */
  pill: string
  dot: string
}

export const documentStatusPresentation: Record<DocumentStatus, StatusPresentation> = {
  accepted: { label: 'Принят', pill: 'bg-muted text-ink-muted', dot: 'bg-ink-faint' },
  processing: {
    label: 'Распознаётся',
    pill: 'bg-accent-tint text-accent-deep',
    dot: 'bg-accent',
  },
  ready: { label: 'Готов', pill: 'bg-sage-tint text-sage-ink', dot: 'bg-sage-strong' },
  failed: { label: 'Ошибка', pill: 'bg-danger-tint text-danger-deep', dot: 'bg-danger' },
}
