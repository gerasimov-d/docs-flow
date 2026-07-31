import type {
  Answer,
  DialogHistoryEntry,
  DocumentLocator,
  MatchSource,
  SearchHit,
  SearchResult,
  SearchStrategy,
  TextRun,
} from '../model/dialog'

/*
 * Демонстрационный корпус фрагментов и маленький движок поиска поверх него.
 *
 * Движок нужен, чтобы экран поиска действительно отвечал на ввод, а не показывал один и тот
 * же заготовленный результат: без этого нельзя увидеть ни пустую выдачу, ни разницу между
 * «точно» и «по смыслу», ради которых экран и рисовался. Всё это удаляется вместе с файлом,
 * когда появится `POST /api/spaces/{spaceId}/search`.
 */

interface Fragment {
  id: string
  locator: DocumentLocator
  text: string
  /** Слова, по которым фрагмент находится «по смыслу», хотя в тексте их нет. */
  topics: string[]
}

const locator = (
  documentId: string,
  documentName: string,
  page: number | null,
  documentDate: string,
  contextName: string,
): DocumentLocator => ({ documentId, documentName, page, documentDate, contextName })

const corpus: Fragment[] = [
  {
    id: 'frag-discharge-2',
    locator: locator(
      'doc-discharge',
      'Выписка из истории болезни.pdf',
      2,
      '2026-02-04',
      'Медицина',
    ),
    text: 'Заключение: гонартроз правого коленного сустава I стадии. Рекомендовано ЛФК, контроль через 6 месяцев.',
    topics: ['колено', 'сустав', 'диагноз', 'здоровье'],
  },
  {
    id: 'frag-discharge-5',
    locator: locator(
      'doc-discharge',
      'Выписка из истории болезни.pdf',
      5,
      '2026-02-04',
      'Медицина',
    ),
    text: 'Динамика по гонартрозу без отрицательных изменений, объём движений сохранён.',
    topics: ['колено', 'сустав', 'здоровье'],
  },
  {
    id: 'frag-discharge-1',
    locator: locator(
      'doc-discharge',
      'Выписка из истории болезни.pdf',
      1,
      '2026-02-04',
      'Медицина',
    ),
    text: 'Страховая организация по полису ОМС: «СОГАЗ-Мед». Прикрепление к поликлинике № 14.',
    topics: ['страховка', 'медицина'],
  },
  {
    id: 'frag-labs-1',
    locator: locator('doc-labs', 'Результаты анализов 12.03.2026.pdf', 1, '2026-03-12', 'Медицина'),
    text: 'СОЭ — 14 мм/ч. С-реактивный белок — 2,1 мг/л (референс до 5,0).',
    topics: ['анализы', 'воспаление', 'здоровье'],
  },
  {
    id: 'frag-xray-1',
    locator: locator('doc-xray', 'Рентгенография колена.jpg', null, '2024-09-21', 'Медицина'),
    text: 'Умеренное сужение суставной щели медиального отдела. Начальные дегенеративные изменения, картина соответствует начальному гонартрозу.',
    topics: ['колено', 'сустав', 'снимок', 'здоровье'],
  },
  {
    id: 'frag-referral-1',
    locator: locator('doc-referral', 'Направление на ЛФК.pdf', 1, '2026-02-11', 'Медицина'),
    text: 'Диагноз при направлении: гонартроз I ст., курс 10 занятий.',
    topics: ['колено', 'лечение'],
  },
  {
    id: 'frag-oms-front',
    locator: locator('doc-oms', 'Полис ОМС.heic', null, '2025-11-18', 'Медицина'),
    text: 'Полис ОМС единого образца. Номер: 7712 3456 7890 1234. Выдан 18.11.2025.',
    topics: ['страховка', 'номер', 'медицина'],
  },
  {
    id: 'frag-oms-back',
    locator: locator('doc-oms', 'Полис ОМС.heic', null, '2025-11-18', 'Медицина'),
    text: 'Обратная сторона: полис ОМС действует на всей территории Российской Федерации.',
    topics: ['страховка', 'медицина'],
  },
  {
    id: 'frag-osago-1',
    locator: locator('doc-osago', 'Договор ОСАГО.pdf', 1, '2025-09-02', 'Авто'),
    text: 'Срок страхования: с 05.09.2025 по 04.09.2026. Транспортное средство: Skoda Octavia.',
    topics: ['страховка', 'машина', 'истекает', 'срок', 'осаго'],
  },
  {
    id: 'frag-osago-3',
    locator: locator('doc-osago', 'Договор ОСАГО.pdf', 3, '2025-09-02', 'Авто'),
    text: 'Договор прекращает действие по истечении периода, указанного в пункте 1.4, если не продлён.',
    topics: ['страховка', 'машина', 'истекает', 'срок', 'осаго'],
  },
  {
    id: 'frag-inspection-1',
    locator: locator('doc-inspection', 'Диагностическая карта.jpg', null, '2025-08-28', 'Авто'),
    text: 'Карта действительна до 28.08.2026, без неё оформление полиса невозможно.',
    topics: ['машина', 'страховка', 'срок'],
  },
  {
    id: 'frag-premium-1',
    locator: locator('doc-premium', 'Квитанция страховой премии.pdf', 1, '2025-09-05', 'Авто'),
    text: 'Оплата страховки по договору ХХХ 0345678901, период 12 месяцев.',
    topics: ['страховка', 'машина', 'оплата'],
  },
  {
    id: 'frag-lease-1',
    locator: locator('doc-lease', 'Договор аренды 2026.pdf', 1, '2026-01-10', 'Недвижимость'),
    text: 'Договор аренды квартиры сроком на 11 месяцев с 10.01.2026.',
    topics: ['квартира', 'аренда', 'жильё'],
  },
  {
    id: 'frag-ndfl-1',
    locator: locator('doc-ndfl', 'Справка 2-НДФЛ 2025.pdf', 1, '2025-02-15', 'Финансы'),
    text: 'Справка о доходах физического лица за 2025 год.',
    topics: ['доход', 'налог', 'деньги'],
  },
]

/** Грубая нормализация: без морфологии, по общему началу слова. Хватает для демонстрации. */
const stem = (word: string) => word.toLowerCase().replace('ё', 'е').slice(0, 5)

/**
 * Слова короче четырёх букв в поиске не участвуют.
 *
 * Без этого «за» из вопроса находится внутри «органи**за**ции», и выдача наполняется
 * документами, к вопросу не относящимися. Настоящий индекс решает это стоп-словами и
 * морфологией; здесь достаточно длины.
 */
const isSearchable = (word: string) => word.length >= 4

/** Совпадение только с начала слова: «полис» находит «полису», но не «монополист». */
function containsWord(haystack: string, word: string): boolean {
  return new RegExp(`(^|[^a-zа-яё])${escapeRegExp(stem(word))}`, 'i').test(haystack)
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

interface ParsedQuery {
  phrases: string[]
  terms: string[]
  excluded: string[]
}

function parse(query: string): ParsedQuery {
  const phrases = [...query.matchAll(/"([^"]+)"/g)].map((match) => match[1] ?? '')
  const rest = query.replace(/"[^"]*"/g, ' ')
  const words = rest.split(/\s+/).filter((word) => word.length > 0)

  return {
    phrases,
    terms: words.filter((word) => !word.startsWith('-') && isSearchable(word)),
    excluded: words
      .filter((word) => word.startsWith('-'))
      .map((word) => word.slice(1))
      .filter(isSearchable),
  }
}

/**
 * Разбивает текст на куски, помечая совпадения.
 *
 * Подсвечивается совпавшее, а не весь фрагмент: подсветка отвечает на вопрос «почему это
 * найдено», и если светится всё, ответа нет.
 */
function highlight(text: string, needles: string[]): TextRun[] {
  const found = needles
    .flatMap((needle) => {
      const isPhrase = needle.includes(' ')
      const pattern = isPhrase
        ? escapeRegExp(needle)
        : `(?<=^|[^a-zа-яё])${escapeRegExp(stem(needle))}`
      const match = new RegExp(pattern, 'i').exec(text)

      return match === null ? [] : [{ from: match.index, to: match.index + match[0].length }]
    })
    .sort((left, right) => left.from - right.from)

  const runs: TextRun[] = []
  let cursor = 0

  for (const { from, to } of found) {
    if (from < cursor) {
      continue
    }

    if (from > cursor) {
      runs.push({ text: text.slice(cursor, from), highlighted: false })
    }

    runs.push({ text: text.slice(from, to), highlighted: true })
    cursor = to
  }

  if (cursor < text.length) {
    runs.push({ text: text.slice(cursor), highlighted: false })
  }

  return runs
}

export function searchFixtures(query: string, strategy: SearchStrategy): SearchResult {
  const { phrases, terms, excluded } = parse(query)
  const needles = [...phrases, ...terms]
  const hits: SearchHit[] = []
  let excludedCount = 0
  let exactCount = 0
  let semanticCount = 0

  for (const fragment of corpus) {
    const haystack = fragment.text.toLowerCase()

    if (excluded.some((word) => containsWord(haystack, word))) {
      excludedCount += 1
      continue
    }

    const exact =
      phrases.some((phrase) => haystack.includes(phrase.toLowerCase())) ||
      terms.some((term) => containsWord(haystack, term))
    const semantic = terms.some((term) =>
      fragment.topics.some((topic) => stem(topic) === stem(term)),
    )

    if (exact) {
      exactCount += 1
    }

    if (semantic) {
      semanticCount += 1
    }

    const matchedBy = matchSource(exact, semantic)

    if (matchedBy === null || !allowed(matchedBy, strategy)) {
      continue
    }

    hits.push({
      ...fragment.locator,
      id: fragment.id,
      runs: highlight(fragment.text, needles),
      matchedBy,
    })
  }

  return {
    hits,
    documentCount: new Set(hits.map((hit) => hit.documentId)).size,
    strategy,
    excludedCount,
    breakdown:
      strategy === 'hybrid' ? { exactCount, semanticCount, mergedCount: hits.length } : null,
  }
}

function matchSource(exact: boolean, semantic: boolean): MatchSource | null {
  if (exact && semantic) {
    return 'both'
  }

  if (exact) {
    return 'exact'
  }

  return semantic ? 'semantic' : null
}

function allowed(matchedBy: MatchSource, strategy: SearchStrategy): boolean {
  if (strategy === 'hybrid') {
    return true
  }

  if (strategy === 'exact') {
    return matchedBy === 'exact' || matchedBy === 'both'
  }

  return matchedBy === 'semantic' || matchedBy === 'both'
}

const disclaimer =
  'Ответ построен по найденным фрагментам, а не по всему содержимому space. Система цитирует документы и не делает выводов о здоровье.'

/**
 * Ответ собирается из тех же фрагментов, что нашёл поиск: у каждого утверждения — своя
 * цитата и свой локатор. Утверждения без источника здесь просто неоткуда взять.
 */
export function answerFixtures(question: string): Answer | null {
  const found = searchFixtures(question, 'hybrid')
  const best = found.hits.slice(0, 3)

  if (best.length === 0) {
    return null
  }

  return {
    statements: best.map((hit, index) => ({
      text: hit.runs.map((run) => run.text).join(''),
      citation: {
        documentId: hit.documentId,
        documentName: hit.documentName,
        page: hit.page,
        documentDate: hit.documentDate,
        contextName: hit.contextName,
        index: index + 1,
        quote: hit.runs.map((run) => run.text).join(''),
      },
    })),
    emptyReason: null,
    disclaimer,
  }
}

/** Данных нет — ответ так и говорит и не достраивает недостающее. */
export function emptyAnswer(question: string): Answer {
  void question

  return {
    statements: [],
    emptyReason:
      'В этом space нет документа, который содержал бы ответ. Я не нашла подходящих фрагментов и не буду достраивать ответ.',
    disclaimer:
      'Ответ построен по найденным фрагментам. Документ мог быть загружен, но ещё не распознан.',
  }
}

export const historyFixtures: DialogHistoryEntry[] = [
  {
    id: 'ask-knee',
    question: 'Что было с коленом за последние два года?',
    sourceCount: 3,
    askedAt: '2026-07-31T14:41:00+03:00',
  },
  {
    id: 'ask-oms',
    question: 'Какой номер полиса ОМС?',
    sourceCount: 1,
    askedAt: '2026-07-31T14:28:00+03:00',
  },
  {
    id: 'ask-osago',
    question: 'Когда заканчивается ОСАГО?',
    sourceCount: 1,
    askedAt: '2026-07-30T18:02:00+03:00',
  },
  {
    id: 'ask-snils',
    question: 'Есть ли в архиве СНИЛС?',
    sourceCount: 0,
    askedAt: '2026-07-30T17:40:00+03:00',
  },
]
