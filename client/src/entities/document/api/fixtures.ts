import type { DocumentDetail, DocumentSummary } from '../model/document'

/*
 * Демонстрационный архив: набор документов, на котором нарисован дизайн.
 *
 * Файл существует, пока у API нет документов, и удаляется целиком вместе с mock-функциями
 * в `document-api.ts`. Ничего, кроме этих двух файлов, при переходе на настоящие эндпоинты
 * править не нужно — компоненты уже работают с типами из `model/document.ts`.
 *
 * Одно отличие от макетов намеренное. В дизайне «Выписка из истории болезни.pdf» на экране
 * инбокса распознаётся, а на экране карточки уже готова — это разные макеты, и между собой
 * они не согласованы. В работающем приложении у документа одно состояние, поэтому выписка
 * здесь готова (её цитируют ответы и поиск), а стадию обработки показывает полис ОМС.
 * Все четыре статуса при этом остаются представленными.
 */

/** Конвейер обработки: названия стадий, как их показывает инбокс. */
const pipeline = ['Приём', 'Нормализация', 'Распознавание', 'Индексация', 'Полнотекстовый индекс']

const medicine = { id: 'ctx-medicine', name: 'Медицина' }
const auto = { id: 'ctx-auto', name: 'Авто' }
const finance = { id: 'ctx-finance', name: 'Финансы' }
const realty = { id: 'ctx-realty', name: 'Недвижимость' }
const children = { id: 'ctx-children', name: 'Дети' }

export const documentFixtures: DocumentSummary[] = [
  {
    id: 'doc-oms',
    name: 'Полис ОМС.heic',
    kind: 'image',
    formatLabel: 'HEIC',
    sizeBytes: 4_100_000,
    pageCount: null,
    uploadedAt: '2026-07-31T14:32:00+03:00',
    documentDate: '2025-11-18',
    context: medicine,
    status: 'processing',
    processing: { stages: pipeline, currentStage: 3, etaMinutes: 2, note: null },
    failure: null,
  },
  {
    id: 'doc-bank',
    name: 'Справка из банка.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 410_000,
    pageCount: 1,
    uploadedAt: '2026-07-31T14:32:00+03:00',
    documentDate: null,
    context: finance,
    status: 'accepted',
    processing: {
      stages: pipeline,
      currentStage: 0,
      etaMinutes: null,
      note: 'В очереди, 2-й из 3 · обработка ещё не начата',
    },
    failure: null,
  },
  {
    id: 'doc-lease',
    name: 'Договор аренды 2026.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 1_800_000,
    pageCount: 12,
    uploadedAt: '2026-07-30T19:04:00+03:00',
    documentDate: '2026-01-10',
    context: realty,
    status: 'failed',
    processing: {
      stages: pipeline,
      currentStage: 3,
      etaMinutes: null,
      note: 'Упала стадия «Распознавание». Приём и нормализация сохранены.',
    },
    failure: 'Файл защищён паролем — распознать не удалось. Снимите пароль и загрузите заново.',
  },
  {
    id: 'doc-discharge',
    name: 'Выписка из истории болезни.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 2_400_000,
    pageCount: 7,
    uploadedAt: '2026-07-30T14:32:00+03:00',
    documentDate: '2026-02-04',
    context: medicine,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-labs',
    name: 'Результаты анализов 12.03.2026.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 640_000,
    pageCount: 2,
    uploadedAt: '2026-07-30T11:20:00+03:00',
    documentDate: '2026-03-12',
    context: medicine,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-snils',
    name: 'СНИЛС.jpg',
    kind: 'image',
    formatLabel: 'JPEG',
    sizeBytes: 1_200_000,
    pageCount: null,
    uploadedAt: '2026-07-28T09:15:00+03:00',
    documentDate: null,
    context: null,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-referral',
    name: 'Направление на ЛФК.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 180_000,
    pageCount: 1,
    uploadedAt: '2026-02-12T10:00:00+03:00',
    documentDate: '2026-02-11',
    context: medicine,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-xray',
    name: 'Рентгенография колена.jpg',
    kind: 'image',
    formatLabel: 'JPEG',
    sizeBytes: 3_300_000,
    pageCount: null,
    uploadedAt: '2024-09-22T16:10:00+03:00',
    documentDate: '2024-09-21',
    context: medicine,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-osago',
    name: 'Договор ОСАГО.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 980_000,
    pageCount: 3,
    uploadedAt: '2025-09-05T10:00:00+03:00',
    documentDate: '2025-09-02',
    context: auto,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-inspection',
    name: 'Диагностическая карта.jpg',
    kind: 'image',
    formatLabel: 'JPEG',
    sizeBytes: 2_600_000,
    pageCount: null,
    uploadedAt: '2025-08-29T09:30:00+03:00',
    documentDate: '2025-08-28',
    context: auto,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-premium',
    name: 'Квитанция страховой премии.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 120_000,
    pageCount: 1,
    uploadedAt: '2025-09-05T10:05:00+03:00',
    documentDate: '2025-09-05',
    context: auto,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-ndfl',
    name: 'Справка 2-НДФЛ 2025.pdf',
    kind: 'pdf',
    formatLabel: 'PDF',
    sizeBytes: 320_000,
    pageCount: 1,
    uploadedAt: '2025-02-16T12:00:00+03:00',
    documentDate: '2025-02-15',
    context: finance,
    status: 'ready',
    processing: null,
    failure: null,
  },
  {
    id: 'doc-birth',
    name: 'Свидетельство о рождении.jpg',
    kind: 'image',
    formatLabel: 'JPEG',
    sizeBytes: 2_100_000,
    pageCount: null,
    uploadedAt: '2025-06-22T18:40:00+03:00',
    documentDate: '2025-06-21',
    context: children,
    status: 'ready',
    processing: null,
    failure: null,
  },
]

type DocumentDetailExtras = Omit<DocumentDetail, keyof DocumentSummary>

/** Распознанный текст карточки. Подсвечен абзац, по цитате которого документ открывают. */
export const documentDetailFixtures: Record<string, DocumentDetailExtras> = {
  'doc-discharge': {
    recognizedPage: 2,
    recognizedParagraphs: [
      {
        text: 'Пациент: Герасимов Д. А., 1988 г. р. Дата приёма: 04.02.2026. Жалобы на боль в правом коленном суставе при нагрузке, сохраняющуюся около трёх месяцев.',
        highlighted: false,
      },
      {
        text: 'Заключение: гонартроз правого коленного сустава I стадии. Рекомендовано ЛФК, контроль через 6 месяцев.',
        highlighted: true,
      },
      {
        text: 'Назначено: хондропротекторы курсом 3 месяца, ограничение осевой нагрузки. Повторная рентгенография в динамике.',
        highlighted: false,
      },
      {
        text: 'Врач: Соколова М. И., травматолог-ортопед. Подпись, печать учреждения.',
        highlighted: false,
      },
    ],
    recognitionWarning:
      'Низкая уверенность распознавания на странице 5 — текст сохранён, но может содержать ошибки.',
  },
  'doc-labs': {
    recognizedPage: 1,
    recognizedParagraphs: [
      { text: 'Клинический анализ крови от 12.03.2026.', highlighted: false },
      {
        text: 'СОЭ — 14 мм/ч. С-реактивный белок — 2,1 мг/л (референс до 5,0).',
        highlighted: true,
      },
      { text: 'Гемоглобин — 148 г/л. Лейкоциты — 6,2 ×10⁹/л.', highlighted: false },
    ],
    recognitionWarning: null,
  },
  'doc-osago': {
    recognizedPage: 1,
    recognizedParagraphs: [
      {
        text: 'Срок страхования: с 05.09.2025 по 04.09.2026. Транспортное средство: Skoda Octavia.',
        highlighted: true,
      },
      { text: 'Страхователь: Герасимов Д. А. Договор ХХХ 0345678901.', highlighted: false },
    ],
    recognitionWarning: null,
  },
}
