import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router'

import { documentKeys, reprocessDocument, useInbox, usePipelineHealth } from '@/entities/document'
import type { Space } from '@/entities/space'
import { UploadDialog } from '@/features/upload-documents'
import { ACCEPTED_FORMATS_HINT, routes } from '@/shared/config'
import { Button, Callout, EmptyState, Icon } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'
import { InboxList } from '@/widgets/inbox-list'

interface InboxPageProps {
  space: Space
}

/**
 * Инбокс — точка приземления после входа.
 *
 * Пустой space не показывает пустую таблицу: первый экран объясняет, что такое space и
 * контекст, двумя фразами вместо мастера настройки, и сразу даёт загрузить документ.
 */
export function InboxPage({ space }: InboxPageProps) {
  const [uploadOpen, setUploadOpen] = useState(false)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { documents, isLoading } = useInbox(space.id)
  const health = usePipelineHealth(space.id)

  const reprocess = useMutation({
    mutationFn: (documentId: string) => reprocessDocument(space.id, documentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: documentKeys.inbox(space.id) })
    },
  })

  const upload = (
    <UploadDialog
      spaceId={space.id}
      spaceName={space.name}
      open={uploadOpen}
      onClose={() => {
        setUploadOpen(false)
      }}
    />
  )

  if (!isLoading && documents.length === 0) {
    return (
      <AppShell space={space}>
        <div className="flex flex-1 items-center justify-center p-10">
          <div className="flex max-w-[660px] flex-col items-start gap-5">
            <span className="bg-accent-tint text-accent flex size-16 items-center justify-center rounded-full">
              <Icon name="upload" className="text-[30px]" />
            </span>
            <h1 className="font-display text-[42px] leading-[1.1] font-extrabold tracking-[-0.015em]">
              Загрузите первый документ
            </h1>
            <p className="text-ink-dim text-[16px] leading-[1.65] text-pretty">
              Space — это ваш архив и граница доступа: всё, что в него загружено, ищется и
              цитируется только внутри него. Контекст — необязательная тематическая метка вроде
              «медицина» или «авто», её можно проставить позже.
            </p>
            <div className="flex items-center gap-3">
              <Button
                variant="primary"
                size="lg"
                icon="upload"
                onClick={() => {
                  setUploadOpen(true)
                }}
              >
                Загрузить документы
              </Button>
              <span className="text-ink-muted text-[13px]">или перетащите файлы в это окно</span>
            </div>
            <p className="text-ink-subtle text-[12px]">{ACCEPTED_FORMATS_HINT}</p>
          </div>
        </div>
        {upload}
      </AppShell>
    )
  }

  return (
    <AppShell
      space={space}
      actions={
        <>
          <Button
            icon="search"
            onClick={() => {
              void navigate(routes.dialogs(space.id))
            }}
          >
            Найти
          </Button>
          <Button
            variant="primary"
            icon="upload"
            onClick={() => {
              setUploadOpen(true)
            }}
          >
            Загрузить
          </Button>
        </>
      }
    >
      <div className="flex min-h-0 flex-1 flex-col gap-[18px] px-7 py-[26px]">
        <div className="flex items-end gap-4">
          <div className="flex-1">
            <h1 className="font-display text-[28px] leading-[1.12] font-extrabold tracking-[-0.015em]">
              Инбокс
            </h1>
            <p className="text-ink-muted mt-1 text-[13px]">
              Что загружено за последние дни и в каком оно состоянии.
            </p>
          </div>
          <span className="text-ink-muted flex items-center gap-2 text-[13px]">
            <Icon name="clock" className="text-ink-subtle text-[16px]" />
            Дата загрузки
          </span>
        </div>

        {health.delayed && (
          <Callout tone="warning">
            <strong className="block text-[14px] font-bold">Обработка задерживается</strong>
            {health.reason ??
              'Сервис распознавания временно не отвечает. Документы приняты и стоят в очереди — они не потеряны, обработка возобновится автоматически.'}
          </Callout>
        )}

        {isLoading ? (
          <EmptyState
            className="border-line bg-surface flex-1 rounded-2xl border"
            icon="scan-text"
            title="Загружаем инбокс"
            description="Секунду — забираем список документов."
          />
        ) : (
          <InboxList
            spaceId={space.id}
            documents={documents}
            onReprocess={(documentId) => {
              reprocess.mutate(documentId)
            }}
          />
        )}
      </div>

      {upload}
    </AppShell>
  )
}
