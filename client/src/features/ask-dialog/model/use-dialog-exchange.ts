import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'

import { askQuestion, runSearch } from '@/entities/dialog'
import type {
  Answer,
  DialogMode,
  DialogQuery,
  SearchResult,
  SearchStrategy,
} from '@/entities/dialog'

interface DialogExchange {
  question: string | null
  answer: Answer | null
  search: SearchResult | null
  pending: boolean
  submit: (query: string, mode: DialogMode, strategy: SearchStrategy) => void
  reset: () => void
}

/**
 * Один обмен репликами: вопрос ушёл — вернулся ответ или выдача.
 *
 * Оба режима держит одна мутация, потому что лента у них общая: показывать в ней можно
 * только последний результат, и два независимых состояния неминуемо разошлись бы —
 * ответ на прошлый вопрос остался бы висеть под новым поисковым запросом.
 *
 * `spaceId` — обязательный параметр, а не значение из контекста: запрос без указания space
 * не должен собираться даже случайно.
 */
export function useDialogExchange(spaceId: string, filters: DialogQuery): DialogExchange {
  const [question, setQuestion] = useState<string | null>(null)
  const [answer, setAnswer] = useState<Answer | null>(null)
  const [search, setSearch] = useState<SearchResult | null>(null)

  const exchange = useMutation<
    { answer: Answer | null; search: SearchResult | null },
    Error,
    { text: string; mode: DialogMode; strategy: SearchStrategy }
  >({
    mutationFn: async ({ text, mode, strategy }) =>
      mode === 'question'
        ? { answer: await askQuestion(spaceId, text, filters), search: null }
        : { answer: null, search: await runSearch(spaceId, text, strategy, filters) },
    onSuccess: (result) => {
      setAnswer(result.answer)
      setSearch(result.search)
    },
  })

  const reset = () => {
    setQuestion(null)
    setAnswer(null)
    setSearch(null)
    exchange.reset()
  }

  return {
    question,
    answer,
    search,
    pending: exchange.isPending,
    submit: (text, mode, strategy) => {
      setQuestion(text)
      setAnswer(null)
      setSearch(null)
      exchange.mutate({ text, mode, strategy })
    },
    reset,
  }
}
