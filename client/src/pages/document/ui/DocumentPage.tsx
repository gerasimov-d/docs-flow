import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useParams, useSearchParams } from 'react-router'

import { ContextTag } from '@/entities/context'
import {
  DocumentStatusBadge,
  documentKeys,
  reprocessDocument,
  useDocument,
} from '@/entities/document'
import type { Space } from '@/entities/space'
import { routes } from '@/shared/config'
import { formatDate, formatFileSize, formatUploadedAt, pluralize } from '@/shared/lib'
import { Button, Callout, EmptyState, Icon, IconButton } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'
import { DocumentReader } from '@/widgets/document-reader'

interface DocumentPageProps {
  space: Space
}

/**
 * Карточка документа.
 *
 * Открывается по цитате: номер страницы приходит в адресе, и подсвеченный фрагмент виден
 * сразу и в оригинале, и в тексте. Без этого ссылка из ответа приводила бы «примерно туда»,
 * а обещание «ответ со ссылкой на первоисточник» держалось бы на честном слове.
 */
export function DocumentPage({ space }: DocumentPageProps) {
  const { documentId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const queryClient = useQueryClient()
  const { document, isLoading, error } = useDocument(space.id, documentId)

  const reprocess = useMutation({
    mutationFn: () => reprocessDocument(space.id, documentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: documentKeys.detail(space.id, documentId) })
    },
  })

  if (isLoading) {
    return (
      <AppShell space={space}>
        <EmptyState
          className="flex-1"
          icon="file-text"
          title="Открываем документ"
          description="Секунду — забираем оригинал и распознанный текст."
        />
      </AppShell>
    )
  }

  if (document === null || error !== null) {
    return (
      <AppShell space={space}>
        <EmptyState
          className="flex-1"
          tone="neutral"
          icon="shield-alert"
          title="Страница недоступна"
          description="У вашей учётной записи нет доступа к запрошенному содержимому."
          actions={
            <Button variant="primary" onClick={() => window.history.back()}>
              Вернуться назад
            </Button>
          }
        />
      </AppShell>
    )
  }

  const page = Number(searchParams.get('page') ?? document.recognizedPage)
  const noText = document.status === 'ready' && document.recognizedParagraphs.length === 0

  return (
    <AppShell
      space={space}
      lead={
        <>
          <Icon name="chevron-right" className="text-ink-faint text-[15px]" />
          <Link
            to={routes.library(space.id)}
            className="text-ink-muted hover:text-accent-deep text-[13px] no-underline"
          >
            Библиотека
          </Link>
          <Icon name="chevron-right" className="text-ink-faint text-[15px]" />
          <span className="max-w-[320px] truncate text-[13px] font-bold">{document.name}</span>
        </>
      }
      actions={
        <>
          {/*
            TODO(бэкенд): `GET /api/spaces/{spaceId}/documents/{documentId}/original` —
            временная ссылка на файл в хранилище. Оригинал неизменен: скачивается ровно то,
            что загрузил пользователь.
          */}
          <Button icon="download" disabled>
            Скачать оригинал
          </Button>
          <IconButton name="ellipsis" label="Другие действия с документом" size="lg" />
        </>
      }
    >
      <div className="flex min-h-0 flex-1 flex-col gap-3.5 px-7 pt-5 pb-6">
        <div className="flex flex-wrap items-center gap-3.5">
          <h1 className="font-display text-[25px] leading-[1.12] font-extrabold tracking-[-0.015em]">
            {document.name}
          </h1>
          <IconButton name="pencil" label="Переименовать документ" bordered={false} size="sm" />
          <DocumentStatusBadge status={document.status} />
        </div>

        <div className="text-ink-muted flex flex-wrap items-center gap-4 text-[13px]">
          <span>{describe(document.formatLabel, document.sizeBytes, document.pageCount)}</span>
          <span className="bg-line h-3.5 w-px" />
          <span>Загружен {formatUploadedAt(document.uploadedAt)}</span>
          <span className="bg-line h-3.5 w-px" />
          <span className="flex items-center gap-[7px]">
            Контекст <ContextTag name={document.context?.name ?? null} />
          </span>
          <span className="bg-line h-3.5 w-px" />
          <span className="flex items-center gap-[7px]">
            Дата документа
            <span className="border-line bg-surface text-ink inline-flex items-center gap-1.5 rounded-full border px-[11px] py-[3px] text-[12px] font-semibold">
              {formatDate(document.documentDate)}
              <Icon name="calendar" className="text-ink-muted text-[13px]" />
            </span>
          </span>
        </div>

        {document.failure !== null && (
          <Callout
            tone="danger"
            action={
              <Button
                size="sm"
                variant="dangerOutline"
                icon="rotate-cw"
                disabled={reprocess.isPending}
                onClick={() => {
                  reprocess.mutate()
                }}
              >
                Повторить обработку
              </Button>
            }
          >
            {document.failure}
          </Callout>
        )}

        {document.recognitionWarning !== null && (
          <Callout
            tone="warning"
            action={
              <Button
                size="sm"
                variant="secondary"
                icon="rotate-cw"
                disabled={reprocess.isPending}
                onClick={() => {
                  reprocess.mutate()
                }}
              >
                Обработать заново
              </Button>
            }
          >
            {document.recognitionWarning}
          </Callout>
        )}

        {noText && (
          <Callout
            tone="warning"
            action={
              <Button
                size="sm"
                variant="primary"
                icon="upload"
                onClick={() => {
                  window.history.back()
                }}
              >
                Загрузить в лучшем качестве
              </Button>
            }
          >
            <strong className="block text-[14px] font-bold">
              Из файла удалось извлечь мало осмысленного текста
            </strong>
            Скорее всего, снимок размыт или сделан под углом. Документ сохранён и доступен — но в
            поиске он почти не участвует.
          </Callout>
        )}

        <DocumentReader document={document} page={Number.isNaN(page) ? 1 : page} />
      </div>
    </AppShell>
  )
}

function describe(format: string, sizeBytes: number, pageCount: number | null): string {
  return [
    format,
    formatFileSize(sizeBytes),
    pageCount === null ? null : pluralize(pageCount, 'страница', 'страницы', 'страниц'),
  ]
    .filter((part): part is string => part !== null)
    .join(' · ')
}
