import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'
import { Callout, Icon } from '@/shared/ui'

import type { Answer, Citation } from '../model/dialog'
import { LocatorLine, SourceLink } from './SourceRef'

/**
 * Как показывать ссылку на первоисточник. Три подачи из раздела «Варианты подачи»:
 *
 * - `chips` — номер прямо в тексте плюс карточки источников под ответом;
 * - `footnotes` — надстрочный номер и список сносок с дословными цитатами;
 * - `sidebar` — цитаты колонкой справа, напротив своих утверждений.
 */
export type CitationStyle = 'chips' | 'footnotes' | 'sidebar'

interface AnswerViewProps {
  spaceId: string
  answer: Answer
  citationStyle?: CitationStyle
  /** Что предложить, когда ответа нет: «Смотреть инбокс», «Загрузить документ». */
  emptyActions?: ReactNode
}

export function AnswerView({
  spaceId,
  answer,
  citationStyle = 'chips',
  emptyActions,
}: AnswerViewProps) {
  return (
    <div className="flex gap-3.5">
      <span className="bg-sage text-on-sage mt-0.5 flex size-[34px] shrink-0 items-center justify-center rounded-full">
        <Icon name="quote" className="text-[17px]" />
      </span>

      <div className="flex min-w-0 flex-1 flex-col gap-3.5">
        {answer.emptyReason === null ? (
          <Body spaceId={spaceId} answer={answer} citationStyle={citationStyle} />
        ) : (
          <p className="text-ink-soft text-[15px] leading-[1.65] text-pretty">
            {answer.emptyReason}
          </p>
        )}

        <Callout tone={answer.emptyReason === null ? 'warning' : 'quiet'}>
          {answer.disclaimer}
        </Callout>

        {emptyActions}
      </div>
    </div>
  )
}

function Body({
  spaceId,
  answer,
  citationStyle,
}: {
  spaceId: string
  answer: Answer
  citationStyle: CitationStyle
}) {
  const citations = answer.statements.map((statement) => statement.citation)

  if (citationStyle === 'sidebar') {
    return (
      <div className="flex min-w-0 gap-[18px]">
        <div className="flex min-w-0 flex-1 flex-col gap-3.5">
          {answer.statements.map((statement, index) => (
            <p
              key={statement.citation.index}
              className={cn(
                'text-ink-soft border-l-[3px] pl-[13px] text-[15px] leading-[1.7] text-pretty',
                index === 0 ? 'border-accent' : 'border-line-dashed',
              )}
            >
              {statement.text}
            </p>
          ))}
        </div>

        <div className="flex w-[300px] shrink-0 flex-col gap-2.5">
          {citations.map((citation, index) => (
            <SourceLink
              key={citation.index}
              spaceId={spaceId}
              locator={citation}
              className={cn(
                'bg-surface text-ink flex flex-col gap-1.5 rounded-2xl border px-[13px] py-[11px]',
                index === 0 ? 'border-accent shadow-card' : 'border-line',
              )}
            >
              <span className="text-ink-soft text-[12px] leading-[1.45]">«{citation.quote}»</span>
              <LocatorLine locator={citation} />
            </SourceLink>
          ))}
        </div>
      </div>
    )
  }

  if (citationStyle === 'footnotes') {
    return (
      <>
        <p className="text-ink-soft text-[15px] leading-[1.7] text-pretty">
          {answer.statements.map((statement) => (
            <span key={statement.citation.index}>
              {statement.text}
              <sup className="text-accent-deep text-[11px] font-extrabold">
                {statement.citation.index}
              </sup>{' '}
            </span>
          ))}
        </p>

        <div className="border-line flex flex-col gap-[9px] border-t pt-3.5">
          {citations.map((citation) => (
            <SourceLink
              key={citation.index}
              spaceId={spaceId}
              locator={citation}
              className="text-ink flex gap-[11px]"
            >
              <span className="text-accent-deep w-3.5 shrink-0 text-[12px] font-extrabold">
                {citation.index}
              </span>
              <span className="min-w-0 flex-1">
                <span className="border-accent text-ink-soft block border-l-[3px] pl-[11px] text-[13px] leading-[1.5]">
                  «{citation.quote}»
                </span>
                <span className="mt-1.5 block pl-3.5">
                  <span className="text-ink-muted text-[11px]">{citation.documentName} · </span>
                  <LocatorLine locator={citation} />
                </span>
              </span>
            </SourceLink>
          ))}
        </div>
      </>
    )
  }

  return (
    <>
      <p className="text-ink-soft text-[15px] leading-[1.7] text-pretty">
        {answer.statements.map((statement) => (
          <span key={statement.citation.index}>
            {statement.text}
            <CitationChip spaceId={spaceId} citation={statement.citation} />{' '}
          </span>
        ))}
      </p>

      <div className="flex flex-col gap-2">
        <div className="text-ink-subtle text-[11px] font-bold tracking-[0.08em] uppercase">
          Источники
        </div>
        <div className="flex flex-wrap gap-2.5">
          {citations.map((citation) => (
            <SourceLink
              key={citation.index}
              spaceId={spaceId}
              locator={citation}
              className="border-line bg-surface text-ink hover:border-line-dashed flex min-w-[220px] flex-1 flex-col gap-1.5 rounded-2xl border px-[13px] py-[11px] transition-colors"
            >
              <span className="flex items-center gap-[7px]">
                <span className="bg-accent text-on-accent flex size-[18px] items-center justify-center rounded-full text-[10px] font-extrabold">
                  {citation.index}
                </span>
                <span className="truncate text-[12px] font-bold">{citation.documentName}</span>
              </span>
              <LocatorLine locator={citation} />
            </SourceLink>
          ))}
        </div>
      </div>
    </>
  )
}

/** Номер источника прямо в предложении — кликабельный, ведёт на страницу первоисточника. */
function CitationChip({ spaceId, citation }: { spaceId: string; citation: Citation }) {
  return (
    <SourceLink
      spaceId={spaceId}
      locator={citation}
      className="bg-accent text-on-accent mx-[3px] inline-flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 align-[2px] text-[11px] font-extrabold no-underline"
    >
      {citation.index}
    </SourceLink>
  )
}
