import { useState } from 'react'
import { useNavigate } from 'react-router'

import { useInbox } from '@/entities/document'
import type { Space } from '@/entities/space'
import {
  DialogComposer,
  detectMode,
  detectStrategy,
  useDialogExchange,
} from '@/features/ask-dialog'
import { DocumentFilterBar, useDocumentFilters } from '@/features/filter-documents'
import { UploadDialog } from '@/features/upload-documents'
import { routes } from '@/shared/config'
import { Button, EmptyState } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'
import { DialogHistory, DialogThread, SearchBreakdown } from '@/widgets/dialog-thread'

interface DialogsPageProps {
  space: Space
}

/**
 * Диалоги: поиск и вопросы в одном поле и одной ленте.
 *
 * Запрос всегда уходит с идентификатором space из адреса — и поиск, и ответ ограничены
 * текущим архивом. Это не деталь реализации, а обещание, которое видно в шапке: «поиск и
 * ответы — только по документам этого space».
 */
export function DialogsPage({ space }: DialogsPageProps) {
  const [uploadOpen, setUploadOpen] = useState(false)
  const navigate = useNavigate()

  const { filters, query, activeCount, set, reset } = useDocumentFilters()
  const { documents } = useInbox(space.id)
  const {
    question,
    answer,
    search,
    pending,
    submit,
    reset: resetThread,
  } = useDialogExchange(space.id, { contextId: query.contextId, from: query.from, to: query.to })

  const hasReadyDocuments = documents.some((document) => document.status === 'ready')
  const processingCount = documents.filter((document) => document.status !== 'ready').length

  const emptyActions = (
    <div className="flex gap-2.5">
      <Button
        onClick={() => {
          void navigate(routes.inbox(space.id))
        }}
      >
        Смотреть инбокс
      </Button>
      <Button
        variant="primary"
        onClick={() => {
          setUploadOpen(true)
        }}
      >
        Загрузить документ
      </Button>
    </div>
  )

  return (
    <AppShell
      space={space}
      lead={
        <span className="text-ink-subtle text-[12px]">
          Поиск и ответы — только по документам этого space
        </span>
      }
      actions={
        <Button
          icon="upload"
          onClick={() => {
            setUploadOpen(true)
          }}
        >
          Загрузить
        </Button>
      }
    >
      <div className="flex min-h-0 flex-1">
        <DialogHistory
          spaceId={space.id}
          activeId={null}
          onSelect={(text) => {
            submit(text, detectMode(text), detectStrategy(text))
          }}
          onReset={resetThread}
        />

        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex min-h-0 flex-1 gap-5 overflow-hidden px-[26px] pt-6">
            {hasReadyDocuments ? (
              <DialogThread
                spaceId={space.id}
                spaceName={space.name}
                question={question}
                answer={answer}
                search={search}
                pending={pending}
                onCancel={resetThread}
                onResetFilters={reset}
                activeFilterCount={activeCount}
                emptyActions={emptyActions}
              />
            ) : (
              <EmptyState
                className="border-line bg-surface flex-1 rounded-2xl border"
                icon="scan-text"
                title="В этом space пока нет обработанных документов"
                description={
                  processingCount === 0
                    ? 'Загрузите документы — их содержимое начнёт находиться поиском и попадёт в ответы.'
                    : `${String(processingCount)} документа распознаются прямо сейчас. Как только они будут готовы, содержимое начнёт находиться поиском и попадёт в ответы.`
                }
                actions={emptyActions}
              />
            )}

            {search !== null && <SearchBreakdown result={search} />}
          </div>

          <div className="px-[26px] pt-3.5 pb-5">
            <DialogComposer
              onSubmit={submit}
              busy={pending}
              filterBar={
                <DocumentFilterBar
                  spaceId={space.id}
                  filters={filters}
                  onChange={set}
                  withStatus={false}
                />
              }
            />
          </div>
        </div>
      </div>

      <UploadDialog
        spaceId={space.id}
        spaceName={space.name}
        open={uploadOpen}
        onClose={() => {
          setUploadOpen(false)
        }}
      />
    </AppShell>
  )
}
