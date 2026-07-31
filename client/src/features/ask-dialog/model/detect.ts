import type { DialogMode, SearchStrategy } from '@/entities/dialog'

const questionWords = [
  'что',
  'кто',
  'где',
  'когда',
  'какой',
  'какая',
  'какие',
  'сколько',
  'почему',
  'зачем',
  'как',
  'есть ли',
  'можно ли',
]

/**
 * Вопрос это или запрос на поиск.
 *
 * Определяется по форме фразы, а не по длине: «гонартроз» — поиск, «когда истекает
 * страховка» — вопрос. Результат всегда виден переключателем и всегда перебивается вручную:
 * угадывание, которое нельзя поправить, хуже отсутствия угадывания.
 */
export function detectMode(query: string): DialogMode {
  const text = query.trim().toLowerCase()

  if (text === '') {
    return 'search'
  }

  if (text.endsWith('?')) {
    return 'question'
  }

  return questionWords.some((word) => text.startsWith(word)) ? 'question' : 'search'
}

/**
 * Как искать. Кавычки и минус-слова — явная просьба о точном совпадении, и переключатель
 * это показывает: пользователь должен видеть, что его синтаксис услышан.
 */
export function detectStrategy(query: string): SearchStrategy {
  return /"[^"]+"|(^|\s)-\S/.test(query) ? 'exact' : 'hybrid'
}
