import { Link } from 'react-router'

import { ContextTag } from '@/entities/context'
import { DocumentStatusBadge, DocumentThumb, ProcessingProgress } from '@/entities/document'
import type { DocumentSummary, ProgressVariant } from '@/entities/document'
import { routes } from '@/shared/config'
import { formatDate, formatFileSize, formatUploadedAt, pluralize } from '@/shared/lib'
import { Button, Icon, IconButton } from '@/shared/ui'

interface InboxListProps {
  spaceId: string
  documents: DocumentSummary[]
  progressVariant?: ProgressVariant
  onReprocess: (documentId: string) => void
}

/**
 * Инбокс: что загружено за последние дни и в каком оно состоянии.
 *
 * Единственный экран, отсортированный по дате загрузки, — здесь важен порядок появления,
 * а не дата самого документа.
 */
export function InboxList({
  spaceId,
  documents,
  progressVariant = 'stepper',
  onReprocess,
}: InboxListProps) {
  return (
    <ul className="border-line bg-surface flex min-h-0 flex-1 flex-col overflow-y-auto rounded-2xl border">
      {documents.map((document) => (
        <li
          key={document.id}
          className={`border-line-soft flex items-start gap-4 border-b px-[18px] py-3.5 last:border-b-0 ${
            document.status === 'failed' ? 'bg-danger-wash' : ''
          }`}
        >
          <DocumentThumb kind={document.kind} failed={document.status === 'failed'} />

          <div className="flex min-w-0 flex-1 flex-col gap-[3px]">
            <Link
              to={routes.document(spaceId, document.id)}
              className="text-ink hover:text-accent-deep truncate text-[15px] font-bold no-underline"
            >
              {document.name}
            </Link>
            <span className="text-ink-muted text-[12px]">{describe(document)}</span>

            {document.failure !== null && (
              <span className="text-danger-deep mt-0.5 flex items-center gap-2 text-[13px]">
                <Icon name="triangle-alert" className="text-[15px]" />
                {document.failure}
              </span>
            )}
          </div>

          <div className="w-[300px] shrink-0">
            {document.processing === null ? (
              <div className="flex items-center gap-2">
                <ContextTag name={document.context?.name ?? null} withIcon />
                <span className="text-ink-subtle text-[12px]">
                  {document.documentDate === null
                    ? '· дата не указана'
                    : `· дата документа ${formatDate(document.documentDate)}`}
                </span>
              </div>
            ) : (
              <>
                <ProcessingProgress
                  processing={document.processing}
                  failed={document.status === 'failed'}
                  variant={progressVariant}
                />
                {document.status === 'failed' && (
                  <Button
                    size="sm"
                    variant="dangerOutline"
                    icon="rotate-cw"
                    className="mt-[9px]"
                    onClick={() => {
                      onReprocess(document.id)
                    }}
                  >
                    Повторить обработку
                  </Button>
                )}
              </>
            )}
          </div>

          <div className="flex w-[132px] shrink-0 justify-end">
            <DocumentStatusBadge status={document.status} />
          </div>

          <div className="flex w-16 shrink-0 justify-end gap-1">
            <IconButton
              name="download"
              label={`Скачать оригинал «${document.name}»`}
              bordered={false}
              size="sm"
            />
            <IconButton
              name="ellipsis"
              label={`Действия с «${document.name}»`}
              bordered={false}
              size="sm"
            />
          </div>
        </li>
      ))}
    </ul>
  )
}

function describe(document: DocumentSummary): string {
  return [
    document.formatLabel,
    formatFileSize(document.sizeBytes),
    document.pageCount === null
      ? null
      : pluralize(document.pageCount, 'страница', 'страницы', 'страниц'),
    `загружен ${formatUploadedAt(document.uploadedAt)}`,
  ]
    .filter((part): part is string => part !== null)
    .join(' · ')
}
