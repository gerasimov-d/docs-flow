import { Link } from 'react-router'

import { ContextTag } from '@/entities/context'
import { DocumentStatusBadge, DocumentThumb } from '@/entities/document'
import type { DocumentSummary } from '@/entities/document'
import { routes } from '@/shared/config'
import { formatDate, pluralize } from '@/shared/lib'
import { Icon, IconButton } from '@/shared/ui'

/** Список или сетка. Выбор пользователя, а не свойство данных: список плотнее, сетка нагляднее. */
export type LibraryLayout = 'table' | 'grid'

interface LibraryViewProps {
  spaceId: string
  documents: DocumentSummary[]
  layout: LibraryLayout
}

export function LibraryView({ spaceId, documents, layout }: LibraryViewProps) {
  if (layout === 'grid') {
    return (
      <div className="grid min-h-0 flex-1 grid-cols-[repeat(auto-fill,minmax(220px,1fr))] content-start gap-4 overflow-y-auto">
        {documents.map((document) => (
          <Link
            key={document.id}
            to={routes.document(spaceId, document.id)}
            className="border-line bg-surface text-ink hover:border-line-dashed flex flex-col gap-[9px] rounded-2xl border p-3 no-underline transition-colors"
          >
            <span className="border-line bg-muted text-ink-faint flex h-[150px] items-center justify-center rounded-lg border">
              <Icon
                name={document.kind === 'pdf' ? 'file-text' : 'image'}
                className="text-[32px]"
              />
            </span>
            <span className="text-[13px] leading-[1.3] font-bold">{document.name}</span>
            <span className="text-ink-muted text-[11px]">{gridMeta(document)}</span>
            <span className="flex flex-wrap items-center gap-1.5">
              <DocumentStatusBadge status={document.status} />
              <ContextTag name={document.context?.name ?? null} />
            </span>
          </Link>
        ))}
      </div>
    )
  }

  return (
    <div className="border-line bg-surface flex min-h-0 flex-1 flex-col overflow-hidden rounded-2xl border">
      <div className="border-line bg-raised text-ink-muted flex shrink-0 items-center gap-4 border-b px-[18px] py-2.5 text-[11px] font-bold tracking-[0.08em] uppercase">
        <span className="w-10 shrink-0" />
        <span className="flex-1">Документ</span>
        <span className="w-[150px] shrink-0">Контекст</span>
        <span className="w-[130px] shrink-0">Дата документа</span>
        <span className="w-24 shrink-0">Тип</span>
        <span className="w-[120px] shrink-0">Статус</span>
        <span className="w-9 shrink-0" />
      </div>

      <ul className="min-h-0 flex-1 overflow-y-auto">
        {documents.map((document) => (
          <li
            key={document.id}
            className="border-line-soft flex items-center gap-4 border-b px-[18px] py-[11px] last:border-b-0"
          >
            <DocumentThumb
              kind={document.kind}
              failed={document.status === 'failed'}
              className="h-[50px]"
            />
            <Link
              to={routes.document(spaceId, document.id)}
              className="text-ink hover:text-accent-deep flex-1 truncate text-[14px] font-bold no-underline"
            >
              {document.name}
            </Link>
            <span className="w-[150px] shrink-0">
              <ContextTag name={document.context?.name ?? null} />
            </span>
            <span className="text-ink-muted w-[130px] shrink-0 text-[13px]">
              {document.documentDate === null ? 'не указана' : formatDate(document.documentDate)}
            </span>
            <span className="text-ink-muted w-24 shrink-0 text-[13px]">{typeLabel(document)}</span>
            <span className="w-[120px] shrink-0">
              <DocumentStatusBadge status={document.status} />
            </span>
            <span className="flex w-9 shrink-0 justify-end">
              <IconButton
                name="ellipsis"
                label={`Действия с «${document.name}»`}
                bordered={false}
                size="sm"
              />
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

function typeLabel(document: DocumentSummary): string {
  return document.pageCount === null
    ? document.formatLabel
    : `${document.formatLabel} · ${String(document.pageCount)} стр.`
}

function gridMeta(document: DocumentSummary): string {
  return [
    document.documentDate === null ? 'дата не указана' : formatDate(document.documentDate),
    document.formatLabel,
    document.pageCount === null
      ? null
      : pluralize(document.pageCount, 'страница', 'страницы', 'страниц'),
  ]
    .filter((part): part is string => part !== null)
    .join(' · ')
}
