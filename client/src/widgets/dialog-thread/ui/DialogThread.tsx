import type { ReactNode } from 'react'

import { AnswerView, SearchHitCard } from '@/entities/dialog'
import type { Answer, CitationStyle, SearchResult } from '@/entities/dialog'
import { pluralize } from '@/shared/lib'
import { Button, Callout, EmptyState, Icon } from '@/shared/ui'

interface DialogThreadProps {
  spaceId: string
  spaceName: string
  question: string | null
  answer: Answer | null
  search: SearchResult | null
  pending: boolean
  citationStyle?: CitationStyle
  onCancel: () => void
  onResetFilters: () => void
  activeFilterCount: number
  /** Что предложить, когда ответа нет: инбокс и загрузка. Собирается страницей. */
  emptyActions?: ReactNode
}

/**
 * Лента диалога: вопрос, затем ответ или выдача поиска.
 *
 * Оба режима живут в одной ленте, потому что в дизайне это одно поле и одна история: человек
 * уточняет вопрос поиском и наоборот, и разрывать это на два экрана значит терять контекст.
 */
export function DialogThread({
  spaceId,
  spaceName,
  question,
  answer,
  search,
  pending,
  citationStyle = 'chips',
  onCancel,
  onResetFilters,
  activeFilterCount,
  emptyActions,
}: DialogThreadProps) {
  if (question === null) {
    return (
      <EmptyState
        className="flex-1"
        icon="messages-square"
        title={`Спросите о содержимом «${spaceName}»`}
        description="Поиск и ответы работают только по документам этого space. Каждое утверждение ответа приходит со ссылкой на документ и страницу."
      />
    )
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto">
      <div className="flex justify-end">
        <p className="border-accent-edge bg-accent-tint text-accent-ink max-w-[70%] rounded-4xl rounded-br-lg border px-5 py-3 text-[15px] font-semibold">
          {question}
        </p>
      </div>

      {pending && <PendingAnswer spaceName={spaceName} onCancel={onCancel} />}

      {!pending && answer !== null && (
        <AnswerView
          spaceId={spaceId}
          answer={answer}
          citationStyle={citationStyle}
          emptyActions={answer.emptyReason === null ? undefined : emptyActions}
        />
      )}

      {!pending && search !== null && (
        <>
          <div className="flex flex-wrap items-center gap-2.5">
            <span className="text-[13px] font-bold">
              {pluralize(search.hits.length, 'фрагмент', 'фрагмента', 'фрагментов')} в{' '}
              {pluralize(search.documentCount, 'документе', 'документах', 'документах')}
            </span>
            {search.excludedCount > 0 && (
              <span className="text-ink-subtle text-[12px]">
                исключено минус-словами: {search.excludedCount}
              </span>
            )}
            <span className="flex-1" />
            <span className="text-ink-muted text-[12px]">Сортировка: по релевантности</span>
          </div>

          {search.hits.length === 0 ? (
            <EmptyState
              tone="neutral"
              icon="search"
              className="border-line bg-surface rounded-2xl border"
              title="Ничего не нашлось"
              description={
                activeFilterCount === 0
                  ? 'Проверьте формулировку: кавычки ищут точную фразу, минус исключает слово.'
                  : `Активны фильтры (${String(activeFilterCount)}). Возможно, ответ есть, но отсечён ими.`
              }
              actions={
                activeFilterCount > 0 && <Button onClick={onResetFilters}>Снять все фильтры</Button>
              }
            />
          ) : (
            <div className="flex flex-col gap-2.5">
              {search.hits.map((hit) => (
                <SearchHitCard key={hit.id} spaceId={spaceId} hit={hit} />
              ))}
            </div>
          )}

          <Callout tone="quiet">
            Гибридный режим: слова запроса ищутся точно, смысл — векторно, результаты объединяются.
            Кавычки — точная фраза, минус — исключить слово.
          </Callout>
        </>
      )}
    </div>
  )
}

/** Ответ готовится. Запрос можно отменить — ждать молча дольше пары секунд не годится. */
function PendingAnswer({ spaceName, onCancel }: { spaceName: string; onCancel: () => void }) {
  return (
    <div className="flex gap-3">
      <span className="bg-sage text-on-sage flex size-8 shrink-0 items-center justify-center rounded-full">
        <Icon name="quote" className="text-[16px]" />
      </span>

      <div className="flex flex-1 flex-col gap-[11px]">
        <p className="text-accent-deep flex items-center gap-[9px] text-[13px] font-bold">
          <span className="animate-beat bg-accent size-2 rounded-full" />
          Ищу в «{spaceName}» и собираю ответ…
        </p>

        <div className="flex flex-col gap-[7px]" aria-hidden="true">
          <span className="bg-raised h-[11px] w-[92%] rounded-full" />
          <span className="bg-raised h-[11px] w-[78%] rounded-full" />
          <span className="bg-raised h-[11px] w-[46%] rounded-full" />
        </div>

        <Button size="sm" icon="x" className="self-start" onClick={onCancel}>
          Отменить запрос
        </Button>
      </div>
    </div>
  )
}
