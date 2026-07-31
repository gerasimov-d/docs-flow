import type { ReactNode } from 'react'
import { Link } from 'react-router'

import { formatDate } from '@/shared/lib'
import { routes } from '@/shared/config'
import { Icon, Tag } from '@/shared/ui'

import type { DocumentLocator, MatchSource } from '../model/dialog'

/** Подпись под фрагментом: документ, страница, дата, контекст — всё, что нужно, чтобы проверить. */
export function LocatorLine({ locator }: { locator: DocumentLocator }) {
  const parts = [
    locator.page === null ? null : `стр. ${String(locator.page)}`,
    locator.documentDate === null ? null : formatDate(locator.documentDate),
    locator.contextName,
  ].filter((part): part is string => part !== null)

  return <span className="text-ink-muted text-[11px]">{parts.join(' · ')}</span>
}

/** Ссылка на первоисточник. Открывает карточку документа на той самой странице. */
export function SourceLink({
  spaceId,
  locator,
  className,
  children,
}: {
  spaceId: string
  locator: DocumentLocator
  className?: string
  children: ReactNode
}) {
  return (
    <Link
      to={`${routes.document(spaceId, locator.documentId)}${locator.page === null ? '' : `?page=${String(locator.page)}`}`}
      className={className}
    >
      {children}
    </Link>
  )
}

const matchLabels: Record<MatchSource, { label: string; icon: 'search' | 'database' | 'layers' }> =
  {
    exact: { label: 'точное совпадение', icon: 'search' },
    semantic: { label: 'по смыслу', icon: 'database' },
    both: { label: 'точно + по смыслу', icon: 'layers' },
  }

/**
 * Чем найден фрагмент. Показывается у каждого результата: в гибридной выдаче пользователь
 * иначе не поймёт, почему рядом оказались документ со словом из запроса и документ без него.
 */
export function MatchSourceTag({ matchedBy }: { matchedBy: MatchSource }) {
  const { label, icon } = matchLabels[matchedBy]

  if (matchedBy === 'both') {
    return (
      <span className="border-accent bg-surface text-accent-ink inline-flex items-center gap-1.5 rounded-full border px-[9px] py-0.5 text-[11px] font-bold">
        <Icon name={icon} className="text-accent-deep text-[11px]" />
        {label}
      </span>
    )
  }

  return (
    <Tag
      tone={matchedBy === 'exact' ? 'accent' : 'sage'}
      icon={icon}
      className="px-[9px] py-0.5 text-[11px] font-bold"
    >
      {label}
    </Tag>
  )
}
