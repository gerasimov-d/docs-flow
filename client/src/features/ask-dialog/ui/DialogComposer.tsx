import { useState } from 'react'
import type { ReactNode } from 'react'

import type { DialogMode, SearchStrategy } from '@/entities/dialog'
import { Icon, Segmented } from '@/shared/ui'

import { detectMode, detectStrategy } from '../model/detect'

interface DialogComposerProps {
  onSubmit: (query: string, mode: DialogMode, strategy: SearchStrategy) => void
  /**
   * Панель фильтров. Приходит извне, а не берётся отсюда: фильтры — соседняя фича, и слайсы
   * одного слоя друг друга не видят. Заодно видно, что фильтры общие с библиотекой.
   */
  filterBar?: ReactNode
  busy?: boolean
}

const modeOptions = [
  { value: 'search' as const, label: 'Поиск', icon: 'search' as const },
  { value: 'question' as const, label: 'Вопрос', icon: 'messages-square' as const },
]

const strategyOptions = [
  { value: 'hybrid' as const, label: 'Гибрид' },
  { value: 'exact' as const, label: 'Точно' },
  { value: 'semantic' as const, label: 'По смыслу' },
]

/**
 * Одно поле на оба сценария.
 *
 * Разделять поиск и вопрос по разным экранам значит заставлять выбирать до того, как человек
 * сформулировал мысль. Режим определяется по фразе, показывается переключателем и в любой
 * момент перебивается — тогда автоопределение больше не вмешивается.
 */
export function DialogComposer({ onSubmit, filterBar, busy = false }: DialogComposerProps) {
  const [query, setQuery] = useState('')
  const [modeOverride, setModeOverride] = useState<DialogMode | null>(null)
  const [strategyOverride, setStrategyOverride] = useState<SearchStrategy | null>(null)

  const mode = modeOverride ?? detectMode(query)
  const strategy = strategyOverride ?? detectStrategy(query)
  const quotesDetected = strategyOverride === null && detectStrategy(query) === 'exact'

  const submit = () => {
    if (query.trim() === '' || busy) {
      return
    }

    onSubmit(query.trim(), mode, strategy)
    setQuery('')
    setModeOverride(null)
    setStrategyOverride(null)
  }

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault()
        submit()
      }}
      className="border-line bg-surface shadow-pop flex flex-col gap-[11px] rounded-4xl border py-3 pr-3.5 pl-[18px]"
    >
      <div className="flex items-center gap-3">
        <input
          value={query}
          onChange={(event) => {
            setQuery(event.target.value)
          }}
          placeholder="Спросите о содержимом архива или найдите фрагмент…"
          aria-label="Вопрос или поисковый запрос"
          className="text-ink placeholder:text-ink-faint flex-1 bg-transparent text-[15px] focus-visible:outline-none"
        />
        <span className="text-ink-subtle text-[11px]">Enter — отправить</span>
        <button
          type="submit"
          disabled={busy || query.trim() === ''}
          aria-label={mode === 'question' ? 'Задать вопрос' : 'Найти'}
          className="bg-accent text-on-accent hover:bg-accent-strong flex size-[38px] cursor-pointer items-center justify-center rounded-full transition-colors disabled:cursor-not-allowed disabled:opacity-45"
        >
          <Icon name="send" className="text-[17px]" />
        </button>
      </div>

      <div className="bg-line-soft h-px" />

      <div className="flex flex-wrap items-center gap-[9px]">
        <Segmented
          label="Режим запроса"
          options={modeOptions}
          value={mode}
          onChange={setModeOverride}
        />

        {modeOverride === null && (
          <span className="text-ink-subtle text-[11px]">
            определено автоматически — можно перебить
          </span>
        )}

        {mode === 'search' && (
          <>
            <span className="bg-line mx-[3px] h-[18px] w-px" />
            <Segmented
              label="Способ поиска"
              tone="surface"
              options={strategyOptions}
              value={strategy}
              onChange={setStrategyOverride}
            />
            {quotesDetected && (
              <span className="text-ink-subtle text-[11px]">
                кавычки в запросе переключили режим на «Точно»
              </span>
            )}
          </>
        )}

        {filterBar !== undefined && (
          <>
            <span className="bg-line mx-[3px] h-[18px] w-px" />
            {filterBar}
          </>
        )}
      </div>
    </form>
  )
}
