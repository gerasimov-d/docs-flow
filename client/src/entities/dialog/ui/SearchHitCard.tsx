import { Icon } from '@/shared/ui'

import type { SearchHit } from '../model/dialog'
import { LocatorLine, MatchSourceTag, SourceLink } from './SourceRef'

interface SearchHitCardProps {
  spaceId: string
  hit: SearchHit
}

/**
 * Фрагмент в выдаче поиска.
 *
 * Подсвечивается совпадение, а не весь фрагмент: подсветка — это ответ на вопрос «почему
 * это найдено», и если светится всё, ответа нет.
 */
export function SearchHitCard({ spaceId, hit }: SearchHitCardProps) {
  return (
    <SourceLink
      spaceId={spaceId}
      locator={hit}
      className="border-line bg-surface text-ink hover:border-line-dashed flex gap-3.5 rounded-2xl border px-4 py-3.5 transition-colors"
    >
      <span className="border-line bg-muted text-ink-subtle flex h-[66px] w-[52px] shrink-0 items-center justify-center rounded-lg border">
        <Icon name={hit.page === null ? 'image' : 'file-text'} className="text-[18px]" />
      </span>

      <span className="flex min-w-0 flex-1 flex-col gap-1.5">
        <span className="flex flex-wrap items-center gap-2.5">
          <span className="text-[14px] font-bold">{hit.documentName}</span>
          {hit.page !== null && (
            <span className="bg-accent-tint text-accent-deep rounded-full px-[9px] py-0.5 text-[11px] font-bold">
              стр. {hit.page}
            </span>
          )}
          <MatchSourceTag matchedBy={hit.matchedBy} />
          <LocatorLine locator={{ ...hit, page: null }} />
        </span>

        <span className="text-ink-soft text-[13px] leading-[1.6]">
          …
          {hit.runs.map((run, index) =>
            run.highlighted ? (
              <mark
                key={index}
                className="text-accent-ink rounded bg-[color-mix(in_srgb,var(--color-accent)_32%,transparent)] px-[3px] py-px"
              >
                {run.text}
              </mark>
            ) : (
              <span key={index}>{run.text}</span>
            ),
          )}
          …
        </span>
      </span>

      <Icon name="chevron-right" className="text-ink-faint self-center text-[18px]" />
    </SourceLink>
  )
}
